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

    // Sin AsNoTracking (a diferencia de GetByCustomerAsync): desde 002, este método también carga
    // la cuenta origen/destino de una confirmación de transferencia, que Application muta vía
    // Account.Debit/Credit y que debe quedar rastreada para que SaveChangesAsync la persista y
    // aplique el concurrency token `xmin` (plan.md "Repository Strategy": la interfaz no cambia de
    // forma, solo esta implementación). El uso de solo lectura de 001 no se ve afectado: el
    // DbContext es de duración por solicitud y se descarta sin cambios pendientes.
    public Task<Account?> GetByIdForCustomerAsync(CustomerId customerId, AccountId accountId, CancellationToken cancellationToken) =>
        _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.Id == accountId, cancellationToken);
}
