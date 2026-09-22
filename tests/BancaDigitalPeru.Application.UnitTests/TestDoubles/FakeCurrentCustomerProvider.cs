using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Application.UnitTests.TestDoubles;

public sealed class FakeCurrentCustomerProvider : ICurrentCustomerProvider
{
    private readonly CustomerId _customerId;

    public FakeCurrentCustomerProvider(CustomerId customerId)
    {
        _customerId = customerId;
    }

    public Task<CustomerId> GetCurrentCustomerIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_customerId);
}
