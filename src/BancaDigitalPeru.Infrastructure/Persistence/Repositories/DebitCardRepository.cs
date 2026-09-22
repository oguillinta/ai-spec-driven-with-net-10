using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;
using Microsoft.EntityFrameworkCore;

namespace BancaDigitalPeru.Infrastructure.Persistence.Repositories;

public sealed class DebitCardRepository : IDebitCardRepository
{
    private readonly BancaDigitalPeruDbContext _dbContext;

    public DebitCardRepository(BancaDigitalPeruDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DebitCard>> GetByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await _dbContext.DebitCards
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    public Task<DebitCard?> GetByIdForCustomerAsync(CustomerId customerId, DebitCardId debitCardId, CancellationToken cancellationToken) =>
        _dbContext.DebitCards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Id == debitCardId, cancellationToken);
}
