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

    /// <summary>
    /// Resuelve una cuenta por su número, sin restricción de propietario (spec 003 FR-009): el
    /// cliente ordenante no puede conocer de antemano el <see cref="CustomerId"/> del
    /// destinatario. Usado únicamente por la vista previa de transferencias a terceros
    /// (research.md de 003 §3).
    /// </summary>
    Task<Account?> GetByNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Resuelve una cuenta por su identificador interno, sin restricción de propietario. Debe
    /// invocarse únicamente con un <see cref="AccountId"/> ya resuelto por el propio backend (una
    /// referencia de vista previa firmada, o el <c>DestinationAccountId</c> de una transferencia
    /// ya persistida) — nunca con un identificador provisto directamente por el cliente HTTP
    /// (research.md de 003 §3, nota de seguridad).
    /// </summary>
    Task<Account?> GetByIdAsync(AccountId accountId, CancellationToken cancellationToken);
}
