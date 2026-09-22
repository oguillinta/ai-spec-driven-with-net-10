namespace BancaDigitalPeru.Application.Abstractions.Persistence;

/// <summary>
/// Frontera mínima para confirmar cambios de una operación. Deliberadamente delgada: reconoce
/// que el DbContext de Infrastructure ya es conceptualmente una Unit of Work (plan.md "Unit of
/// Work Strategy"). Ningún caso de uso de la feature 001 (solo lectura) la invoca; queda
/// preparada para las futuras features de transferencias (002/003).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
