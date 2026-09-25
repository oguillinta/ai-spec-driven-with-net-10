using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;

namespace BancaDigitalPeru.Application.Transfers.ConfirmTransfer;

/// <summary>
/// Ejecuta una transferencia a partir de una referencia de vista previa válida — compartido por
/// transferencias entre cuentas propias (spec 002 FR-011/FR-012) y a terceros (spec 003), sin un
/// flag de tipo: la naturaleza de la operación se determina comparando el propietario real de la
/// cuenta destino contra el cliente actual (research.md de 003 §5), una sola vez, justo después de
/// cargar ambas cuentas. Débito, crédito y registro de <see cref="Transfer"/> viajan en una única
/// <see cref="IUnitOfWork.SaveChangesAsync"/>. No usa <c>DbContext</c>/<c>DbSet</c> ni contiene
/// lógica HTTP.
/// </summary>
public sealed class ConfirmTransferUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPreviewTokenSigner _previewTokenSigner;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmTransferUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IAccountRepository accountRepository,
        ITransferRepository transferRepository,
        ICustomerRepository customerRepository,
        IPreviewTokenSigner previewTokenSigner,
        IUnitOfWork unitOfWork)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _accountRepository = accountRepository;
        _transferRepository = transferRepository;
        _customerRepository = customerRepository;
        _previewTokenSigner = previewTokenSigner;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransferOutcome<TransferResultDto>> ExecuteAsync(
        string previewReference,
        IdempotencyKey idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payload = _previewTokenSigner.Unprotect(previewReference);
        if (payload is null)
        {
            return TransferOutcome<TransferResultDto>.Rejected(TransferRejectionReason.InvalidPreviewReference);
        }

        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);
        var sourceAccountId = new AccountId(payload.SourceAccountId);
        var destinationAccountId = new AccountId(payload.DestinationAccountId);

        // Paso 9 (research.md de 002 §5): la idempotencia se comprueba ANTES de cualquier
        // validación de negocio y ANTES de tocar cuentas. Un replay de una transferencia ya
        // completada debe devolver siempre su resultado original, incluso si el estado actual de
        // las cuentas (p. ej. el saldo origen, ya reducido por la primera confirmación) ya no
        // pasaría la revalidación de saldo suficiente — revalidar aquí rompería RF-024/RF-025
        // para transferencias que agotan el saldo disponible.
        var existingTransfer = await _transferRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingTransfer is not null)
        {
            return IsSameLogicalOperation(existingTransfer, sourceAccountId, destinationAccountId, payload.Amount)
                ? TransferOutcome<TransferResultDto>.Succeeded(await BuildResultDtoAsync(customerId, existingTransfer, cancellationToken), isReplay: true)
                : TransferOutcome<TransferResultDto>.Rejected(TransferRejectionReason.IdempotencyConflict);
        }

        var sourceAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, sourceAccountId, cancellationToken);
        // Sin restricción de propietario (a diferencia de 002): el destino puede pertenecer a
        // otro cliente. La propiedad real de la cuenta destino ya fue resuelta y validada por la
        // vista previa que emitió este payload; aquí se revalida contra el estado vigente
        // (research.md de 003 §3/§5).
        var destinationAccount = await _accountRepository.GetByIdAsync(destinationAccountId, cancellationToken);

        // Si destinationAccount es null, isThirdParty es false y la validación recae en la rama
        // de cuentas propias (AccountNotEligible), no en ThirdPartyTransferValidation
        // (DestinationAccountNotFound): sin una cuenta destino no hay propietario que comparar,
        // por lo que la clasificación es indeterminable y el sistema recae en el resultado más
        // conservador en privacidad. En operación normal esta rama nunca debería alcanzarse
        // (ambos flujos de vista previa ya validaron la existencia del destino antes de firmar, y
        // ninguna cuenta se elimina en este sistema).
        var isThirdParty = destinationAccount is not null && destinationAccount.CustomerId != customerId;

        var rejection = isThirdParty
            ? ThirdPartyTransferValidation.Validate(sourceAccount, destinationAccount, customerId, payload.Amount)
            : OwnAccountTransferValidation.Validate(sourceAccount, destinationAccount, sourceAccountId, destinationAccountId, payload.Amount);
        if (rejection is not null)
        {
            return TransferOutcome<TransferResultDto>.Rejected(rejection.Value);
        }

        var amount = Money.Create(payload.Amount, payload.Currency);

        sourceAccount!.Debit(amount);
        destinationAccount!.Credit(amount);

        var transfer = Transfer.Create(
            new TransferId(Guid.NewGuid()),
            customerId,
            sourceAccountId,
            destinationAccountId,
            amount,
            idempotencyKey,
            DateTimeOffset.UtcNow);

        _transferRepository.Add(transfer);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return TransferOutcome<TransferResultDto>.Rejected(TransferRejectionReason.ConcurrencyConflict);
        }
        catch (UniqueConstraintViolationException)
        {
            // Otra solicitud con la misma Idempotency-Key ganó la carrera (research.md de 002 §5,
            // punto 2): el índice único de PostgreSQL es el respaldo ante la condición de carrera
            // que la comprobación previa no puede ver. Se recarga y se devuelve el resultado
            // ganador.
            var winningTransfer = await _transferRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("Se esperaba encontrar la transferencia ganadora tras un conflicto de unicidad de Idempotency-Key.");

            return TransferOutcome<TransferResultDto>.Succeeded(
                await BuildResultDtoAsync(customerId, winningTransfer, cancellationToken), isReplay: true);
        }

        var destinationCustomerDisplayNameMasked = await ResolveDestinationCustomerDisplayNameMaskedAsync(
            destinationAccount, customerId, cancellationToken);

        var result = new TransferResultDto(
            transfer.Id.Value,
            transfer.CompletedAtUtc,
            sourceAccount.Number.Masked,
            destinationAccount.Number.Masked,
            amount.Amount,
            amount.Currency,
            destinationCustomerDisplayNameMasked);

        return TransferOutcome<TransferResultDto>.Succeeded(result);
    }

    private static bool IsSameLogicalOperation(Transfer existingTransfer, AccountId sourceAccountId, AccountId destinationAccountId, decimal amount) =>
        existingTransfer.SourceAccountId == sourceAccountId
        && existingTransfer.DestinationAccountId == destinationAccountId
        && existingTransfer.Amount.Amount == amount;

    private async Task<TransferResultDto> BuildResultDtoAsync(CustomerId customerId, Transfer transfer, CancellationToken cancellationToken)
    {
        var sourceAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, transfer.SourceAccountId, cancellationToken)
            ?? throw new InvalidOperationException("La cuenta origen de una transferencia ya completada debería seguir existiendo.");
        var destinationAccount = await _accountRepository.GetByIdAsync(transfer.DestinationAccountId, cancellationToken)
            ?? throw new InvalidOperationException("La cuenta destino de una transferencia ya completada debería seguir existiendo.");

        var destinationCustomerDisplayNameMasked = await ResolveDestinationCustomerDisplayNameMaskedAsync(
            destinationAccount, customerId, cancellationToken);

        return new TransferResultDto(
            transfer.Id.Value,
            transfer.CompletedAtUtc,
            sourceAccount.Number.Masked,
            destinationAccount.Number.Masked,
            transfer.Amount.Amount,
            transfer.Amount.Currency,
            destinationCustomerDisplayNameMasked);
    }

    private async Task<string?> ResolveDestinationCustomerDisplayNameMaskedAsync(
        Account destinationAccount, CustomerId customerId, CancellationToken cancellationToken)
    {
        if (destinationAccount.CustomerId == customerId)
        {
            return null;
        }

        var destinationCustomer = await _customerRepository.GetByIdAsync(destinationAccount.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("El cliente propietario de la cuenta destino debería seguir existiendo.");

        return destinationCustomer.DisplayNameMasked;
    }
}
