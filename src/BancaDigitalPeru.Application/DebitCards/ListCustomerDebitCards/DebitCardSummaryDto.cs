using BancaDigitalPeru.Domain.DebitCards;

namespace BancaDigitalPeru.Application.DebitCards.ListCustomerDebitCards;

public sealed record DebitCardSummaryDto(
    Guid DebitCardId,
    string MaskedNumber,
    string Last4Digits,
    Guid AccountId,
    CardStatus Status,
    int ExpirationMonth,
    int ExpirationYear)
{
    public static DebitCardSummaryDto FromDomain(DebitCard card) => new(
        card.Id.Value,
        card.Number.Masked,
        card.Number.Last4Digits,
        card.AccountId.Value,
        card.Status,
        card.Expiration.Month,
        card.Expiration.Year);
}
