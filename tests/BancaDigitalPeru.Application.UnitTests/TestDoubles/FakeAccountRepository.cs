using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Application.UnitTests.TestDoubles;

/// <summary>
/// Doble de prueba en memoria que replica el comportamiento real del repositorio: filtra por
/// propietario e identificador en la misma operación (research.md §6).
/// </summary>
public sealed class FakeAccountRepository : IAccountRepository
{
    private readonly List<Account> _accounts;

    public FakeAccountRepository(IEnumerable<Account> accounts)
    {
        _accounts = accounts.ToList();
    }

    public Task<IReadOnlyList<Account>> GetByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Account> result = _accounts.Where(a => a.CustomerId == customerId).ToList();
        return Task.FromResult(result);
    }

    public Task<Account?> GetByIdForCustomerAsync(CustomerId customerId, AccountId accountId, CancellationToken cancellationToken)
    {
        var account = _accounts.FirstOrDefault(a => a.CustomerId == customerId && a.Id == accountId);
        return Task.FromResult(account);
    }
}
