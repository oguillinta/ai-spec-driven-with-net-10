namespace BancaDigitalPeru.Domain.Transfers;

/// <summary>
/// Identificador de operación de una transferencia, generado por el backend al confirmar
/// (spec FR-016). Mismo patrón que <see cref="Accounts.AccountId"/>.
/// </summary>
public readonly struct TransferId : IEquatable<TransferId>
{
    public Guid Value { get; }

    public TransferId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("El identificador de transferencia no puede ser vacío.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(TransferId other) => Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is TransferId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(TransferId left, TransferId right) => left.Equals(right);

    public static bool operator !=(TransferId left, TransferId right) => !left.Equals(right);
}
