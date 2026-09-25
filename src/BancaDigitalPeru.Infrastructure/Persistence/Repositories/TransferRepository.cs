using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;
using Microsoft.EntityFrameworkCore;

namespace BancaDigitalPeru.Infrastructure.Persistence.Repositories;

public sealed class TransferRepository : ITransferRepository
{
    private readonly BancaDigitalPeruDbContext _dbContext;

    public TransferRepository(BancaDigitalPeruDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Transfer transfer) => _dbContext.Transfers.Add(transfer);

    public Task<Transfer?> GetByIdForCustomerAsync(CustomerId customerId, TransferId transferId, CancellationToken cancellationToken) =>
        _dbContext.Transfers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CustomerId == customerId && t.Id == transferId, cancellationToken);

    public Task<Transfer?> GetByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken) =>
        _dbContext.Transfers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
}
