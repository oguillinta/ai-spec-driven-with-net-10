namespace BancaDigitalPeru.Domain.Customers;

/// <summary>
/// Identificador interno del cliente. Envoltorio de <see cref="Guid"/> con invariante no-vacío.
/// </summary>
public readonly struct CustomerId : IEquatable<CustomerId>
{
    public Guid Value { get; }

    public CustomerId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("El identificador de cliente no puede ser vacío.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(CustomerId other) => Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is CustomerId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(CustomerId left, CustomerId right) => left.Equals(right);

    public static bool operator !=(CustomerId left, CustomerId right) => !left.Equals(right);
}
