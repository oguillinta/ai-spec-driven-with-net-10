namespace BancaDigitalPeru.Domain.DebitCards;

/// <summary>Identificador interno de una tarjeta. No es el número real de tarjeta (research.md §1).</summary>
public readonly struct DebitCardId : IEquatable<DebitCardId>
{
    public Guid Value { get; }

    public DebitCardId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("El identificador de tarjeta no puede ser vacío.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(DebitCardId other) => Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is DebitCardId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(DebitCardId left, DebitCardId right) => left.Equals(right);

    public static bool operator !=(DebitCardId left, DebitCardId right) => !left.Equals(right);
}
