using BancaDigitalPeru.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BancaDigitalPeru.Infrastructure.Persistence;

/// <summary>
/// Delega directamente en BancaDigitalPeruDbContext.SaveChangesAsync, sin una segunda capa
/// transaccional paralela (plan.md "Unit of Work Strategy"). Traduce las excepciones propias de EF
/// Core/Npgsql a excepciones de Application (research.md §5/§6 de 002), para que Application nunca
/// dependa de tipos de EF Core/Npgsql (Dependency Rule, Principio IV).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BancaDigitalPeruDbContext _dbContext;

    public UnitOfWork(BancaDigitalPeruDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "La operación no pudo completarse porque los datos fueron modificados por otra operación.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException)
        {
            throw new UniqueConstraintViolationException(
                postgresException.ConstraintName ?? string.Empty,
                "Ya existe un registro con esa clave única.",
                ex);
        }
    }
}
