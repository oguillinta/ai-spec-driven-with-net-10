using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Vista previa válida: sin efecto financiero, solo los datos identificados (spec 002 FR-010).
/// <see cref="DestinationAccountMasked"/> y <see cref="DestinationCustomerDisplayNameMasked"/> los
/// puebla únicamente la vista previa a terceros (spec 003 FR-010/FR-011): el ordenante ya conoce
/// el identificador interno de sus propias cuentas, pero el número de cuenta y el nombre de un
/// tercero deben enmascararse (research.md de 003 §6/§7). La vista previa entre cuentas propias
/// deja ambos en <c>null</c> (data-model.md de 003).
/// </summary>
public sealed record TransferPreviewResult(
    string PreviewReference,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    CurrencyCode Currency,
    string? DestinationAccountMasked = null,
    string? DestinationCustomerDisplayNameMasked = null);
