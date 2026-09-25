using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Resultado observable de una transferencia completada (spec FR-018), reutilizado por la
/// confirmación exitosa (US1/US3) y por la consulta (US4). Los números de cuenta nunca aparecen
/// completos: solo su representación enmascarada, ya derivada de <c>AccountNumber.Masked</c>.
/// </summary>
public sealed record TransferResultDto(
    Guid TransferId,
    DateTimeOffset CompletedAtUtc,
    string SourceAccountMasked,
    string DestinationAccountMasked,
    decimal Amount,
    CurrencyCode Currency);
