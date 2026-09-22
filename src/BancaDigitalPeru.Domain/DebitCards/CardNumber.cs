using System.Text.RegularExpressions;

namespace BancaDigitalPeru.Domain.DebitCards;

/// <summary>
/// Número de tarjeta de débito (16 dígitos). Almacena el valor completo pero solo expone
/// <see cref="Masked"/> y <see cref="Last4Digits"/> (spec RB7/FR-019, criterio CA9).
/// </summary>
public sealed partial class CardNumber : IEquatable<CardNumber>
{
    private const int Length = 16;

    private readonly string _fullNumber;

    public CardNumber(string fullNumber)
    {
        if (string.IsNullOrWhiteSpace(fullNumber) || !DigitsOnlyRegex().IsMatch(fullNumber) || fullNumber.Length != Length)
        {
            throw new ArgumentException(
                $"El número de tarjeta debe contener exactamente {Length} dígitos.",
                nameof(fullNumber));
        }

        _fullNumber = fullNumber;
    }

    public string Last4Digits => _fullNumber[^4..];

    public string Masked => $"**** **** **** {Last4Digits}";

    /// <summary>Solo visible para Infrastructure (mapeo EF Core), vía InternalsVisibleTo.</summary>
    internal string FullNumber => _fullNumber;

    public bool Equals(CardNumber? other) => other is not null && _fullNumber == other._fullNumber;

    public override bool Equals(object? obj) => Equals(obj as CardNumber);

    public override int GetHashCode() => _fullNumber.GetHashCode();

    public override string ToString() => Masked;

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
