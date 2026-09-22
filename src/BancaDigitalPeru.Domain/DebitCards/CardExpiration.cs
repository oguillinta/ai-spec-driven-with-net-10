namespace BancaDigitalPeru.Domain.DebitCards;

/// <summary>Mes/año de vencimiento de una tarjeta de débito. Invariante: mes entre 1 y 12.</summary>
public sealed class CardExpiration : IEquatable<CardExpiration>
{
    public int Month { get; }
    public int Year { get; }

    public CardExpiration(int month, int year)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "El mes de vencimiento debe estar entre 1 y 12.");
        }

        Month = month;
        Year = year;
    }

    public bool Equals(CardExpiration? other) => other is not null && Month == other.Month && Year == other.Year;

    public override bool Equals(object? obj) => Equals(obj as CardExpiration);

    public override int GetHashCode() => HashCode.Combine(Month, Year);

    public override string ToString() => $"{Month:00}/{Year % 100:00}";
}
