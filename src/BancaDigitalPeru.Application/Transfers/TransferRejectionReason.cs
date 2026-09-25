namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Motivos de rechazo de una vista previa o confirmación de transferencia (plan.md "Application
/// Use Case"). Las razones se consumen progresivamente: US1 introduce las de validación básica,
/// US3 añade las de idempotencia/concurrencia.
/// </summary>
public enum TransferRejectionReason
{
    /// <summary>
    /// La referencia de vista previa no es decodificable (manipulada, corrupta, o firmada con una
    /// clave distinta) — mapeada a 400, mismo cuerpo que un error de formato (research.md §8,
    /// contrato OpenAPI "BadRequest").
    /// </summary>
    InvalidPreviewReference,

    /// <summary>Cuenta origen o destino inexistente o de otro cliente (404 genérico, FR-021/FR-022).</summary>
    AccountNotEligible,

    /// <summary>La cuenta origen y la cuenta destino son la misma (RB2).</summary>
    SameAccount,

    /// <summary>Cuenta origen o destino BLOQUEADA (RB8/FR-006).</summary>
    AccountBlocked,

    /// <summary>Importe cero, negativo o con más de 2 decimales (RB4).</summary>
    InvalidAmount,

    /// <summary>El importe supera el saldo disponible de la cuenta origen (RB7).</summary>
    InsufficientFunds,

    /// <summary>La misma Idempotency-Key ya fue usada con datos distintos (research.md §5).</summary>
    IdempotencyConflict,

    /// <summary>Conflicto de concurrencia optimista sobre la cuenta origen (research.md §6).</summary>
    ConcurrencyConflict
}
