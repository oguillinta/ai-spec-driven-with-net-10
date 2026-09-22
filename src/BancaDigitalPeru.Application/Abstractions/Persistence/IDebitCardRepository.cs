using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;

namespace BancaDigitalPeru.Application.Abstractions.Persistence;

/// <summary>Mismo patrón que IAccountRepository (plan.md "Repository Strategy").</summary>
public interface IDebitCardRepository
{
    Task<IReadOnlyList<DebitCard>> GetByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken);

    Task<DebitCard?> GetByIdForCustomerAsync(CustomerId customerId, DebitCardId debitCardId, CancellationToken cancellationToken);
}
