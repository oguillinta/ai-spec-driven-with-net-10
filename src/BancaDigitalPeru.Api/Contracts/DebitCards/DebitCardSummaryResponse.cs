using BancaDigitalPeru.Application.DebitCards.GetCustomerDebitCardDetail;
using BancaDigitalPeru.Application.DebitCards.ListCustomerDebitCards;
using BancaDigitalPeru.Domain.DebitCards;

namespace BancaDigitalPeru.Api.Contracts.DebitCards;

/// <summary>Refleja el schema DebitCardSummary/DebitCardDetail del contrato OpenAPI (mismos campos).</summary>
public sealed record DebitCardSummaryResponse(
    Guid DebitCardId,
    string MaskedNumber,
    string Last4Digits,
    Guid AccountId,
    CardStatus Status,
    CardExpirationResponse Expiration)
{
    public static DebitCardSummaryResponse FromDto(DebitCardSummaryDto dto) => new(
        dto.DebitCardId,
        dto.MaskedNumber,
        dto.Last4Digits,
        dto.AccountId,
        dto.Status,
        new CardExpirationResponse(dto.ExpirationMonth, dto.ExpirationYear));

    public static DebitCardSummaryResponse FromDto(DebitCardDetailDto dto) => new(
        dto.DebitCardId,
        dto.MaskedNumber,
        dto.Last4Digits,
        dto.AccountId,
        dto.Status,
        new CardExpirationResponse(dto.ExpirationMonth, dto.ExpirationYear));
}

public sealed record CardExpirationResponse(int Month, int Year);
