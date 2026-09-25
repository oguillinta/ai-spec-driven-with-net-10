namespace BancaDigitalPeru.Domain.Transfers;

/// <summary>
/// Identificador opaco de solicitud lógica de confirmación (spec RB9). Invariante: 1-255
/// caracteres (data-model.md); no se exige ningún formato específico, se persiste tal cual para
/// la comparación exacta de duplicados (research.md §5).
/// </summary>
public sealed class IdempotencyKey : IEquatable<IdempotencyKey>
{
    private const int MaxLength = 255;

    public string Value { get; }

    public IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            throw new ArgumentException(
                $"La Idempotency-Key debe tener entre 1 y {MaxLength} caracteres.",
                nameof(value));
        }

        Value = value;
    }

    public bool Equals(IdempotencyKey? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as IdempotencyKey);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
