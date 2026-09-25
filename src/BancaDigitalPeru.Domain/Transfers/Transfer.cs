using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Domain.Transfers;

/// <summary>
/// Transferencia entre dos cuentas propias ya completada (spec RB10, FR-016…FR-018). Aggregate
/// Root independiente de <see cref="Account"/> (research.md §1): solo se crea y persiste cuando la
/// operación tuvo éxito, nunca para representar un intento rechazado.
/// </summary>
public sealed class Transfer
{
    public TransferId Id { get; }
    public CustomerId CustomerId { get; }
    public AccountId SourceAccountId { get; }
    public AccountId DestinationAccountId { get; }
    public Money Amount { get; }
    public TransferStatus Status { get; }
    public IdempotencyKey IdempotencyKey { get; }
    public DateTimeOffset CompletedAtUtc { get; }

    private Transfer(
        TransferId id,
        CustomerId customerId,
        AccountId sourceAccountId,
        AccountId destinationAccountId,
        Money amount,
        TransferStatus status,
        IdempotencyKey idempotencyKey,
        DateTimeOffset completedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        Status = status;
        IdempotencyKey = idempotencyKey;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>
    /// Crea una transferencia completada. Invariantes de construcción (data-model.md) — segunda
    /// línea de defensa: Application ya valida moneda y cuentas distintas antes de llegar aquí.
    /// </summary>
    public static Transfer Create(
        TransferId id,
        CustomerId customerId,
        AccountId sourceAccountId,
        AccountId destinationAccountId,
        Money amount,
        IdempotencyKey idempotencyKey,
        DateTimeOffset completedAtUtc)
    {
        if (amount.Currency != CurrencyCode.PEN)
        {
            throw new ArgumentException("Toda transferencia de esta versión debe estar denominada en PEN (spec RB3).", nameof(amount));
        }

        if (sourceAccountId == destinationAccountId)
        {
            throw new ArgumentException("La cuenta origen y la cuenta destino de una transferencia deben ser diferentes (spec RB2).", nameof(destinationAccountId));
        }

        return new Transfer(id, customerId, sourceAccountId, destinationAccountId, amount, TransferStatus.Completed, idempotencyKey, completedAtUtc);
    }

    /// <summary>Constructor de materialización usado exclusivamente por EF Core (Infrastructure).</summary>
    private Transfer()
    {
        Amount = null!;
        IdempotencyKey = null!;
    }
}
