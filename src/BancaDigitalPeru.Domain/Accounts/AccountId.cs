namespace BancaDigitalPeru.Domain.Accounts;

/// <summary>
/// Identificador interno de una cuenta. No es el número real de cuenta (research.md §1).
/// </summary>
public readonly struct AccountId : IEquatable<AccountId>
{
    public Guid Value { get; }

    public AccountId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("El identificador de cuenta no puede ser vacío.", nameof(value));
        }

        Value = value;
    }

    public bool Equals(AccountId other) => Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is AccountId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(AccountId left, AccountId right) => left.Equals(right);

    public static bool operator !=(AccountId left, AccountId right) => !left.Equals(right);
}
