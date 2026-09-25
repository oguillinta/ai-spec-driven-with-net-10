using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Application.UnitTests.TestDoubles;

public sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers;

    public FakeCustomerRepository(IEnumerable<Customer> customers)
    {
        _customers = customers.ToList();
    }

    public Task<Customer?> GetByIdAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        var customer = _customers.FirstOrDefault(c => c.Id == customerId);
        return Task.FromResult(customer);
    }
}
