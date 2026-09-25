using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Application.Common;
using BancaDigitalPeru.Domain.Transfers;

namespace BancaDigitalPeru.Application.Transfers.GetOwnAccountTransfer;

/// <summary>
/// Consulta el resultado de una transferencia propia ya completada (spec FR-018, User Story 4).
/// "No existe" y "es de otro cliente" producen el mismo <see cref="Result{T}.NotFound"/>, mismo
/// patrón que <c>001</c> (spec FR-021/FR-022).
/// </summary>
public sealed class GetOwnAccountTransferUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly ITransferRepository _transferRepository;
    private readonly IAccountRepository _accountRepository;

    public GetOwnAccountTransferUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        ITransferRepository transferRepository,
        IAccountRepository accountRepository)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _transferRepository = transferRepository;
        _accountRepository = accountRepository;
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
        var destinationAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, transfer.DestinationAccountId, cancellationToken)
            ?? throw new InvalidOperationException("La cuenta destino de una transferencia ya completada debería seguir existiendo.");

        var dto = new TransferResultDto(
            transfer.Id.Value,
            transfer.CompletedAtUtc,
            sourceAccount.Number.Masked,
            destinationAccount.Number.Masked,
            transfer.Amount.Amount,
            transfer.Amount.Currency);

        return Result<TransferResultDto>.Found(dto);
    }
}
