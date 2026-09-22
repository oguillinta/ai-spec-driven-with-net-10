using System.Text.RegularExpressions;

namespace BancaDigitalPeru.Domain.Accounts;

/// <summary>
/// Número de cuenta. Almacena el valor completo pero solo expone <see cref="Masked"/>: el número
/// completo nunca debe salir de Domain/Infrastructure (spec FR-018, clarificación 2026-09-20:
/// mismo formato de enmascaramiento que las tarjetas, solo últimos 4 dígitos visibles).
/// </summary>
public sealed partial class AccountNumber : IEquatable<AccountNumber>
{
    private readonly string _fullNumber;

    public AccountNumber(string fullNumber)
    {
        if (string.IsNullOrWhiteSpace(fullNumber) || !DigitsOnlyRegex().IsMatch(fullNumber) || fullNumber.Length < 4)
        {
            throw new ArgumentException(
                "El número de cuenta debe contener solo dígitos y al menos 4 caracteres.",
                nameof(fullNumber));
        }

        _fullNumber = fullNumber;
    }

    public string Masked => "****" + _fullNumber[^4..];

    /// <summary>Solo visible para Infrastructure (mapeo EF Core), vía InternalsVisibleTo.</summary>
    internal string FullNumber => _fullNumber;

    public bool Equals(AccountNumber? other) => other is not null && _fullNumber == other._fullNumber;

    public override bool Equals(object? obj) => Equals(obj as AccountNumber);

    public override int GetHashCode() => _fullNumber.GetHashCode();

    public override string ToString() => Masked;

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
