using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Domain.DebitCards;

/// <summary>
/// Tarjeta de débito asociada a exactamente una cuenta de ahorro del mismo cliente (spec
/// RF-012/RB6). Feature 001 es de solo lectura: no expone operaciones de bloqueo/activación.
/// </summary>
public sealed class DebitCard
{
    public DebitCardId Id { get; }
    public AccountId AccountId { get; }
    public CustomerId CustomerId { get; }
    public CardNumber Number { get; }
    public CardExpiration Expiration { get; }
    public CardStatus Status { get; }

    private DebitCard(
        DebitCardId id,
        AccountId accountId,
        CustomerId customerId,
        CardNumber number,
        CardExpiration expiration,
        CardStatus status)
    {
        Id = id;
        AccountId = accountId;
        CustomerId = customerId;
        Number = number;
        Expiration = expiration;
        Status = status;
    }

    /// <summary>
    /// Crea una tarjeta validando la invariante RB6: la cuenta asociada debe pertenecer al mismo
    /// cliente propietario de la tarjeta.
    /// </summary>
    public static DebitCard Create(
        DebitCardId id,
        CustomerId customerId,
        Account associatedAccount,
        CardNumber number,
        CardExpiration expiration,
        CardStatus status)
    {
        if (associatedAccount.CustomerId != customerId)
        {
            throw new ArgumentException(
                "La cuenta asociada a la tarjeta debe pertenecer al mismo cliente propietario de la tarjeta (spec RB6).",
                nameof(associatedAccount));
        }

        return new DebitCard(id, associatedAccount.Id, customerId, number, expiration, status);
    }

    /// <summary>Constructor de materialización usado exclusivamente por EF Core (Infrastructure).</summary>
    private DebitCard()
    {
        Number = null!;
        Expiration = null!;
    }
}
