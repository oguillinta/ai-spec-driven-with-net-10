using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;

namespace BancaDigitalPeru.Application.Accounts.ListCustomerAccounts;

/// <summary>
/// Lista las cuentas de ahorro del cliente actual, incluidas las BLOQUEADAS (spec FR-001, RB4).
/// </summary>
public sealed class ListCustomerAccountsUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IAccountRepository _accountRepository;

    public ListCustomerAccountsUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IAccountRepository accountRepository)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _accountRepository = accountRepository;
    }

    public async Task<IReadOnlyList<AccountSummaryDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);
        var accounts = await _accountRepository.GetByCustomerAsync(customerId, cancellationToken);

        return accounts.Select(AccountSummaryDto.FromDomain).ToList();
    }
}
