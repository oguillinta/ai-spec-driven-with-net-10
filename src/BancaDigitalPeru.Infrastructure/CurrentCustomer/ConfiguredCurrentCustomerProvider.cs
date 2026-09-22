using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Domain.Customers;
using Microsoft.Extensions.Options;

namespace BancaDigitalPeru.Infrastructure.CurrentCustomer;

/// <summary>
/// Implementación de demostración: resuelve el cliente actual desde configuración
/// (DemoCustomer:CustomerId). Sustituible en una feature futura por una implementación que lea
/// la identidad desde autenticación real, sin tocar Application ni Domain (research.md §2).
/// </summary>
public sealed class ConfiguredCurrentCustomerProvider : ICurrentCustomerProvider
{
    private readonly CustomerId _customerId;

    public ConfiguredCurrentCustomerProvider(IOptions<DemoCustomerOptions> options)
    {
        _customerId = new CustomerId(options.Value.CustomerId);
    }

    public Task<CustomerId> GetCurrentCustomerIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_customerId);
}
