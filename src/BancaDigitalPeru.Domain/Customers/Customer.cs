namespace BancaDigitalPeru.Domain.Customers;

/// <summary>
/// Propietario ficticio de productos bancarios. Su gestión (alta, autenticación) está fuera del
/// alcance de la feature 001; existe para dar soporte referencial a Account/DebitCard.
/// </summary>
public sealed class Customer
{
    private static readonly char[] NameSeparators = [' '];

    public CustomerId Id { get; }
    public string DisplayName { get; }

    public Customer(CustomerId id, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("El nombre del cliente no puede estar vacío.", nameof(displayName));
        }

        Id = id;
        DisplayName = displayName;
    }

    /// <summary>
    /// Nombre parcialmente oculto del destinatario (spec 003, FR-011, clarificación 2026-09-25):
    /// primer nombre completo + inicial del primer apellido + asteriscos (p. ej.
    /// "Juan Pérez García" → "Juan P***"). Si <see cref="DisplayName"/> tiene un solo token,
    /// devuelve ese token seguido de asteriscos sin inicial adicional.
    /// </summary>
    public string DisplayNameMasked
    {
        get
        {
            var tokens = DisplayName.Split(NameSeparators, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length == 1
                ? $"{tokens[0]}***"
                : $"{tokens[0]} {tokens[1][0]}***";
        }
    }
}
