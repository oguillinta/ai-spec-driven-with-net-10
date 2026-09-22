using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace BancaDigitalPeru.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly BancaDigitalPeruDbContext _dbContext;

    public AccountRepository(BancaDigitalPeruDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Account>> GetByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    public Task<Account?> GetByIdForCustomerAsync(CustomerId customerId, AccountId accountId, CancellationToken cancellationToken) =>
        _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.Id == accountId, cancellationToken);
}
