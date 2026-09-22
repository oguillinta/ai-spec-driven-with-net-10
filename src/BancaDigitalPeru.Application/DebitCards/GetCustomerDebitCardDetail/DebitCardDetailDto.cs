using BancaDigitalPeru.Domain.DebitCards;

namespace BancaDigitalPeru.Application.DebitCards.GetCustomerDebitCardDetail;

/// <summary>Mismos campos que DebitCardSummaryDto (data-model.md): el detalle no añade atributos.</summary>
public sealed record DebitCardDetailDto(
    Guid DebitCardId,
    string MaskedNumber,
    string Last4Digits,
    Guid AccountId,
    CardStatus Status,
    int ExpirationMonth,
    int ExpirationYear)
{
    public static DebitCardDetailDto FromDomain(DebitCard card) => new(
        card.Id.Value,
        card.Number.Masked,
        card.Number.Last4Digits,
        card.AccountId.Value,
        card.Status,
        card.Expiration.Month,
        card.Expiration.Year);
}
