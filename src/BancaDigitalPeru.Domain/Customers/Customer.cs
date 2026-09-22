namespace BancaDigitalPeru.Domain.Customers;

/// <summary>
/// Propietario ficticio de productos bancarios. Su gestión (alta, autenticación) está fuera del
/// alcance de la feature 001; existe para dar soporte referencial a Account/DebitCard.
/// </summary>
public sealed class Customer
{
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
}
