using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Application.Abstractions.Persistence;

/// <summary>
/// Abstracción de persistencia para <see cref="Customer"/>, un Aggregate Root independiente
/// (data-model.md de 003). Único método necesario: resolver el nombre del destinatario de una
/// transferencia a terceros para su enmascaramiento (spec 003 FR-011).
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId customerId, CancellationToken cancellationToken);
}
