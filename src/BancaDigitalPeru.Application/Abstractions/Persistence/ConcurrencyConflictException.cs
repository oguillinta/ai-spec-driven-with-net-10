namespace BancaDigitalPeru.Application.Abstractions.Persistence;

/// <summary>
/// Señala que <see cref="IUnitOfWork.SaveChangesAsync"/> detectó un conflicto de concurrencia
/// optimista (research.md §6). Traducida por Infrastructure a partir de la excepción propia de EF
/// Core, para que Application nunca dependa de tipos de EF Core (Dependency Rule).
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
