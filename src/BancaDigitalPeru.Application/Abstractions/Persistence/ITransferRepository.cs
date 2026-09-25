using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;

namespace BancaDigitalPeru.Application.Abstractions.Persistence;

/// <summary>
/// Abstracción de persistencia para <see cref="Transfer"/>, un Aggregate Root independiente de
/// <see cref="BancaDigitalPeru.Domain.Accounts.Account"/> (plan.md "Repository Strategy").
/// </summary>
public interface ITransferRepository
{
    void Add(Transfer transfer);

    Task<Transfer?> GetByIdForCustomerAsync(CustomerId customerId, TransferId transferId, CancellationToken cancellationToken);

    Task<Transfer?> GetByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken);
}
