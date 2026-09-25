using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;

/// <summary>
/// Valida una transferencia entre cuentas propias y devuelve una referencia de vista previa sin
/// ningún efecto financiero (spec FR-010, research.md §4). No usa <c>DbContext</c>/<c>DbSet</c> ni
/// contiene lógica HTTP (plan.md "Application Use Case").
/// </summary>
public sealed class PreviewOwnAccountTransferUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IAccountRepository _accountRepository;
    private readonly IPreviewTokenSigner _previewTokenSigner;

    public PreviewOwnAccountTransferUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IAccountRepository accountRepository,
        IPreviewTokenSigner previewTokenSigner)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _accountRepository = accountRepository;
        _previewTokenSigner = previewTokenSigner;
    }

    public async Task<TransferOutcome<TransferPreviewResult>> ExecuteAsync(
        AccountId sourceAccountId,
        AccountId destinationAccountId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);

        var sourceAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, sourceAccountId, cancellationToken);
        var destinationAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, destinationAccountId, cancellationToken);

        var rejection = OwnAccountTransferValidation.Validate(sourceAccount, destinationAccount, sourceAccountId, destinationAccountId, amount);
        if (rejection is not null)
        {
            return TransferOutcome<TransferPreviewResult>.Rejected(rejection.Value);
        }

        var money = Money.Create(amount, CurrencyCode.PEN);
        var payload = new TransferPreviewPayload(sourceAccountId.Value, destinationAccountId.Value, money.Amount, money.Currency, DateTimeOffset.UtcNow);
        var reference = _previewTokenSigner.Protect(payload);

        return TransferOutcome<TransferPreviewResult>.Succeeded(
            new TransferPreviewResult(reference, sourceAccountId.Value, destinationAccountId.Value, money.Amount, money.Currency));
    }
}
