using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Resultado observable de una transferencia completada (spec 002 FR-018), reutilizado por la
/// confirmación exitosa y por la consulta de ambas features de transferencia (002 y 003). Los
/// números de cuenta nunca aparecen completos: solo su representación enmascarada, ya derivada de
/// <c>AccountNumber.Masked</c>. <see cref="DestinationCustomerDisplayNameMasked"/> lo puebla
/// únicamente una transferencia a terceros (spec 003 FR-011/FR-021); para una transferencia entre
/// cuentas propias queda en <c>null</c> (data-model.md de 003).
/// </summary>
public sealed record TransferResultDto(
    Guid TransferId,
    DateTimeOffset CompletedAtUtc,
    string SourceAccountMasked,
    string DestinationAccountMasked,
    decimal Amount,
    CurrencyCode Currency,
    string? DestinationCustomerDisplayNameMasked = null);
