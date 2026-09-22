using BancaDigitalPeru.Application.Abstractions.Persistence;

namespace BancaDigitalPeru.Infrastructure.Persistence;

/// <summary>
/// Delega directamente en BancaDigitalPeruDbContext.SaveChangesAsync, sin una segunda capa
/// transaccional paralela (plan.md "Unit of Work Strategy").
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BancaDigitalPeruDbContext _dbContext;

    public UnitOfWork(BancaDigitalPeruDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
