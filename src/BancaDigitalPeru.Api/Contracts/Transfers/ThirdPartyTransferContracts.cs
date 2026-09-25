using BancaDigitalPeru.Application.Transfers;

namespace BancaDigitalPeru.Api.Contracts.Transfers;

/// <summary>Refleja ThirdPartyTransferPreviewRequest del contrato third-party-transfers-v1.yaml.</summary>
public sealed record ThirdPartyTransferPreviewRequest(Guid SourceAccountId, string DestinationAccountNumber, decimal Amount);

/// <summary>Refleja ThirdPartyTransferPreviewResponse del contrato third-party-transfers-v1.yaml.</summary>
public sealed record ThirdPartyTransferPreviewResponse(
    string PreviewReference,
    Guid SourceAccountId,
    string DestinationAccountMasked,
    string DestinationCustomerDisplayName,
    MoneyResponse Amount)
{
    public static ThirdPartyTransferPreviewResponse FromResult(TransferPreviewResult result) => new(
        result.PreviewReference,
        result.SourceAccountId,
        result.DestinationAccountMasked!,
        result.DestinationCustomerDisplayNameMasked!,
        new MoneyResponse(result.Amount, result.Currency));
}
