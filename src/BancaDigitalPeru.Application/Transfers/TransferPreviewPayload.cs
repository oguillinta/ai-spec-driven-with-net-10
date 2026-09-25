using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Contenido codificado dentro de la referencia de vista previa (research.md de 002 §4),
/// compartido por la vista previa entre cuentas propias y la vista previa a terceros (research.md
/// de 003 §4). No es una entidad de Domain: es un contrato de datos interno entre los dos pasos
/// del flujo, serializado y protegido criptográficamente por <see cref="Abstractions.IPreviewTokenSigner"/>.
/// </summary>
public sealed record TransferPreviewPayload(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    CurrencyCode Currency,
    DateTimeOffset IssuedAtUtc);
