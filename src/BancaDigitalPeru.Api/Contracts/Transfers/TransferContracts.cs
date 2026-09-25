using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;
using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Api.Contracts.Transfers;

public sealed record MoneyResponse(decimal Amount, CurrencyCode Currency);

/// <summary>Refleja TransferPreviewRequest del contrato OpenAPI.</summary>
public sealed record TransferPreviewRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);

/// <summary>Refleja TransferPreviewResponse del contrato OpenAPI.</summary>
public sealed record TransferPreviewResponse(
    string PreviewReference,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    MoneyResponse Amount)
{
    public static TransferPreviewResponse FromResult(TransferPreviewResult result) => new(
        result.PreviewReference,
        result.SourceAccountId,
        result.DestinationAccountId,
        new MoneyResponse(result.Amount, result.Currency));
}

/// <summary>Refleja ConfirmTransferRequest del contrato OpenAPI.</summary>
public sealed record ConfirmTransferRequest(string PreviewReference);

/// <summary>Refleja TransferResult del contrato OpenAPI.</summary>
public sealed record TransferResultResponse(
    Guid TransferId,
    DateTimeOffset CompletedAt,
    string SourceAccountMasked,
    string DestinationAccountMasked,
    MoneyResponse Amount,
    string Status)
{
    public static TransferResultResponse FromDto(TransferResultDto dto) => new(
        dto.TransferId,
        dto.CompletedAtUtc,
        dto.SourceAccountMasked,
        dto.DestinationAccountMasked,
        new MoneyResponse(dto.Amount, dto.Currency),
        "COMPLETED");
}
