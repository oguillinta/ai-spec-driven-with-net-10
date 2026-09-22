using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Application.Abstractions;

/// <summary>
/// Resuelve el cliente actualmente activo sin depender de autenticación (spec §9). Los casos de
/// uso nunca reciben un CustomerId proporcionado libremente por el cliente HTTP.
/// </summary>
public interface ICurrentCustomerProvider
{
    Task<CustomerId> GetCurrentCustomerIdAsync(CancellationToken cancellationToken);
}
