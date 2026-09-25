using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;

namespace BancaDigitalPeru.Application.Transfers.ConfirmOwnAccountTransfer;

/// <summary>
/// Ejecuta la transferencia a partir de una referencia de vista previa válida (spec FR-011/FR-012).
/// Débito, crédito y registro de <see cref="Transfer"/> viajan en una única
/// <see cref="IUnitOfWork.SaveChangesAsync"/> (plan.md "Application Use Case", 13 pasos). No usa
/// <c>DbContext</c>/<c>DbSet</c> ni contiene lógica HTTP.
/// </summary>
public sealed class ConfirmOwnAccountTransferUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IPreviewTokenSigner _previewTokenSigner;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOwnAccountTransferUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IAccountRepository accountRepository,
        ITransferRepository transferRepository,
        IPreviewTokenSigner previewTokenSigner,
        IUnitOfWork unitOfWork)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _accountRepository = accountRepository;
        _transferRepository = transferRepository;
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

        // Paso 9 (research.md §5): la idempotencia se comprueba ANTES de cualquier validación de
        // negocio y ANTES de tocar cuentas. Un replay de una transferencia ya completada debe
        // devolver siempre su resultado original, incluso si el estado actual de las cuentas (p.
        // ej. el saldo origen, ya reducido por la primera confirmación) ya no pasaría la
        // revalidación de saldo suficiente — revalidar aquí rompería RF-024/RF-025 para
        // transferencias que agotan el saldo disponible.
        var existingTransfer = await _transferRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingTransfer is not null)
        {
            return IsSameLogicalOperation(existingTransfer, sourceAccountId, destinationAccountId, payload.Amount)
                ? TransferOutcome<TransferResultDto>.Succeeded(await BuildResultDtoAsync(customerId, existingTransfer, cancellationToken), isReplay: true)
                : TransferOutcome<TransferResultDto>.Rejected(TransferRejectionReason.IdempotencyConflict);
        }

        var sourceAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, sourceAccountId, cancellationToken);
        var destinationAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, destinationAccountId, cancellationToken);

        var rejection = OwnAccountTransferValidation.Validate(sourceAccount, destinationAccount, sourceAccountId, destinationAccountId, payload.Amount);
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
            // Otra solicitud con la misma Idempotency-Key ganó la carrera (research.md §5, punto
            // 2): el índice único de PostgreSQL es el respaldo ante la condición de carrera que
            // la comprobación previa no puede ver. Se recarga y se devuelve el resultado ganador.
            var winningTransfer = await _transferRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("Se esperaba encontrar la transferencia ganadora tras un conflicto de unicidad de Idempotency-Key.");

            return TransferOutcome<TransferResultDto>.Succeeded(
                await BuildResultDtoAsync(customerId, winningTransfer, cancellationToken), isReplay: true);
        }

        var result = new TransferResultDto(
            transfer.Id.Value,
            transfer.CompletedAtUtc,
            sourceAccount.Number.Masked,
            destinationAccount.Number.Masked,
            amount.Amount,
            amount.Currency);

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
        var destinationAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, transfer.DestinationAccountId, cancellationToken)
            ?? throw new InvalidOperationException("La cuenta destino de una transferencia ya completada debería seguir existiendo.");

        return new TransferResultDto(
            transfer.Id.Value,
            transfer.CompletedAtUtc,
            sourceAccount.Number.Masked,
            destinationAccount.Number.Masked,
            transfer.Amount.Amount,
            transfer.Amount.Currency);
    }
}
