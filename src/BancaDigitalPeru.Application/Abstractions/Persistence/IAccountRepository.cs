using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Application.Abstractions.Persistence;

/// <summary>
/// Abstracción de persistencia expresada en términos de capacidad de negocio, no de acceso
/// genérico a datos (plan.md "Repository Strategy"). <see cref="GetByIdForCustomerAsync"/> filtra
/// por propietario e identificador en la misma consulta: "no existe" y "es de otro cliente" ya
/// llegan indistinguibles como null (research.md §6, spec FR-022).
/// </summary>
public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> GetByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken);

    Task<Account?> GetByIdForCustomerAsync(CustomerId customerId, AccountId accountId, CancellationToken cancellationToken);
}
