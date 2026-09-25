using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;

/// <summary>Vista previa válida: sin efecto financiero, solo los datos identificados (spec FR-010).</summary>
public sealed record TransferPreviewResult(
    string PreviewReference,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    CurrencyCode Currency);
