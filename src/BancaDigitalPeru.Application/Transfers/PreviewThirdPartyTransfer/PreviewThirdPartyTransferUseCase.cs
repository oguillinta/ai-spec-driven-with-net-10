using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Transfers.PreviewThirdPartyTransfer;

/// <summary>
/// Valida una transferencia hacia la cuenta de un tercero y devuelve una referencia de vista
/// previa sin ningún efecto financiero (spec 003 FR-010). A diferencia de la vista previa entre
/// cuentas propias, resuelve el destino por número de cuenta, sin restricción de propietario
/// (research.md de 003 §3), y expone el nombre parcialmente oculto del destinatario (FR-011). No
/// usa <c>DbContext</c>/<c>DbSet</c> ni contiene lógica HTTP.
/// </summary>
public sealed class PreviewThirdPartyTransferUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPreviewTokenSigner _previewTokenSigner;

    public PreviewThirdPartyTransferUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IPreviewTokenSigner previewTokenSigner)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _previewTokenSigner = previewTokenSigner;
    }

    public async Task<TransferOutcome<TransferPreviewResult>> ExecuteAsync(
        AccountId sourceAccountId,
        AccountNumber destinationAccountNumber,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);

        var sourceAccount = await _accountRepository.GetByIdForCustomerAsync(customerId, sourceAccountId, cancellationToken);
        var destinationAccount = await _accountRepository.GetByNumberAsync(destinationAccountNumber, cancellationToken);

        var rejection = ThirdPartyTransferValidation.Validate(sourceAccount, destinationAccount, customerId, amount);
        if (rejection is not null)
        {
            return TransferOutcome<TransferPreviewResult>.Rejected(rejection.Value);
        }

        var destinationCustomer = await _customerRepository.GetByIdAsync(destinationAccount!.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("El cliente propietario de la cuenta destino debería existir.");

        var money = Money.Create(amount, CurrencyCode.PEN);
        var payload = new TransferPreviewPayload(sourceAccountId.Value, destinationAccount.Id.Value, money.Amount, money.Currency, DateTimeOffset.UtcNow);
        var reference = _previewTokenSigner.Protect(payload);

        return TransferOutcome<TransferPreviewResult>.Succeeded(
            new TransferPreviewResult(
                reference,
                sourceAccountId.Value,
                destinationAccount.Id.Value,
                money.Amount,
                money.Currency,
                destinationAccount.Number.Masked,
                destinationCustomer.DisplayNameMasked));
    }
}
