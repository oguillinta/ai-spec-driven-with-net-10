namespace BancaDigitalPeru.Application.Abstractions.Persistence;

/// <summary>
/// Señala que <see cref="IUnitOfWork.SaveChangesAsync"/> violó una restricción única persistente
/// (research.md §5, respaldo ante condiciones de carrera de <c>Idempotency-Key</c>). Traducida por
/// Infrastructure a partir de la excepción propia de Npgsql, para que Application nunca dependa de
/// tipos de EF Core/Npgsql (Dependency Rule).
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public string ConstraintName { get; }

    public UniqueConstraintViolationException(string constraintName, string message, Exception innerException)
        : base(message, innerException)
    {
        ConstraintName = constraintName;
    }
}
