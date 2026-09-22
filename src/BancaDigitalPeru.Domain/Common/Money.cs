namespace BancaDigitalPeru.Domain.Common;

/// <summary>
/// Importe monetario con exactamente 2 decimales (spec RB3). Value object inmutable.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }

    private Money(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, CurrencyCode currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "El saldo disponible no puede ser negativo.");
        }

        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded != amount)
        {
            throw new ArgumentException("El importe debe tener como máximo 2 posiciones decimales.", nameof(amount));
        }

        return new Money(rounded, currency);
    }

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
