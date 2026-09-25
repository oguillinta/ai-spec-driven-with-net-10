using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Application.Common;
using BancaDigitalPeru.Domain.Transfers;

namespace BancaDigitalPeru.Application.Transfers.GetTransfer;

/// <summary>
/// Consulta el resultado de una transferencia ya completada — compartido por transferencias entre
/// cuentas propias (spec 002 FR-018) y a terceros (spec 003 FR-021), sin un flag de tipo:
/// <c>Transfer.CustomerId</c> ya identifica al cliente ordenante en ambos casos (data-model.md de
/// 003). "No existe" y "es de otro cliente [ordenante]" producen el mismo
/// <see cref="Result{T}.NotFound"/>, mismo patrón que <c>001</c>/<c>002</c> (spec FR-021/FR-022).
/// El nombre enmascarado del destinatario se agrega a la respuesta únicamente cuando el
/// propietario actual de la cuenta destino es distinto del cliente ordenante (research.md de 003
/// §5).
/// </summary>
public sealed class GetTransferUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly ITransferRepository _transferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetTransferUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        ITransferRepository transferRepository,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _transferRepository = transferRepository;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
    }

    public async Task<Result<TransferResultDto>> ExecuteAsync(TransferId transferId, CancellationToken cancellationToken)
    {
        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);
        var transfer = await _transferRepository.GetByIdForCustomerAsync(customerId, transferId, cancellationToken);

        if (transfer is null)
        {
            return Result<TransferResultDto>.NotFound();
        }

        var sourceAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, transfer.SourceAccountId, cancellationToken)
            ?? throw new InvalidOperationException("La cuenta origen de una transferencia ya completada debería seguir existiendo.");
        // Sin restricción de propietario (a diferencia de 002): la cuenta destino puede pertenecer
        // a otro cliente si esta fue una transferencia a terceros (research.md de 003 §5).
        var destinationAccount = await _accountRepository.GetByIdAsync(transfer.DestinationAccountId, cancellationToken)
            ?? throw new InvalidOperationException("La cuenta destino de una transferencia ya completada debería seguir existiendo.");

        string? destinationCustomerDisplayNameMasked = null;
        if (destinationAccount.CustomerId != customerId)
        {
            var destinationCustomer = await _customerRepository.GetByIdAsync(destinationAccount.CustomerId, cancellationToken)
                ?? throw new InvalidOperationException("El cliente propietario de la cuenta destino debería seguir existiendo.");
            destinationCustomerDisplayNameMasked = destinationCustomer.DisplayNameMasked;
        }

        var dto = new TransferResultDto(
            transfer.Id.Value,
            transfer.CompletedAtUtc,
            sourceAccount.Number.Masked,
            destinationAccount.Number.Masked,
            transfer.Amount.Amount,
            transfer.Amount.Currency,
            destinationCustomerDisplayNameMasked);

        return Result<TransferResultDto>.Found(dto);
    }
}
