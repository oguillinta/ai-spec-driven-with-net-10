namespace BancaDigitalPeru.Application.Common;

/// <summary>
/// Resultado mínimo de un caso de uso de consulta por identificador: encontrado o no encontrado.
/// "No encontrado" cubre tanto "no existe" como "pertenece a otro cliente" con el mismo valor,
/// de modo que ambos casos sean indistinguibles para quien los consuma (spec FR-022).
/// </summary>
public sealed class Result<T>
{
    public bool IsFound { get; }
    public T? Value { get; }

    private Result(bool isFound, T? value)
    {
        IsFound = isFound;
        Value = value;
    }

    public static Result<T> Found(T value) => new(true, value);

    public static Result<T> NotFound() => new(false, default);
}
