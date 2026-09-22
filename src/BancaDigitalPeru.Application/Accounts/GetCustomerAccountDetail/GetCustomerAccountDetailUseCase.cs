using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Application.Common;
using BancaDigitalPeru.Domain.Accounts;

namespace BancaDigitalPeru.Application.Accounts.GetCustomerAccountDetail;

/// <summary>
/// Consulta el detalle de una cuenta propia (spec FR-003/FR-004). "No existe" y "es de otro
/// cliente" producen el mismo Result&lt;T&gt;.NotFound() (spec FR-022).
/// </summary>
public sealed class GetCustomerAccountDetailUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IAccountRepository _accountRepository;

    public GetCustomerAccountDetailUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IAccountRepository accountRepository)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _accountRepository = accountRepository;
    }

    public async Task<Result<AccountDetailDto>> ExecuteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);
        var account = await _accountRepository.GetByIdForCustomerAsync(customerId, accountId, cancellationToken);

        return account is null
            ? Result<AccountDetailDto>.NotFound()
            : Result<AccountDetailDto>.Found(AccountDetailDto.FromDomain(account));
    }
}
