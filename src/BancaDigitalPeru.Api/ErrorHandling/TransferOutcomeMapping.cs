using BancaDigitalPeru.Application.Transfers;
using Microsoft.AspNetCore.Mvc;

namespace BancaDigitalPeru.Api.ErrorHandling;

/// <summary>
/// Mapea cada <see cref="TransferRejectionReason"/> a su <c>ProblemDetails</c> exacto según el
/// contrato <c>own-account-transfers-v1.yaml</c> (research.md §8 de 002). "No existe" y "es de otro
/// cliente" para la cuenta origen o destino producen el mismo cuerpo 404 genérico (FR-021/FR-022).
/// </summary>
public static class TransferOutcomeMapping
{
    public static ObjectResult ToProblemDetails(this TransferRejectionReason reason, HttpContext httpContext) => reason switch
    {
        TransferRejectionReason.InvalidPreviewReference => BadRequest(httpContext, "La referencia de vista previa no es válida."),
        TransferRejectionReason.AccountNotEligible => AccountNotEligible(httpContext),
        TransferRejectionReason.SameAccount => TransferRejected(httpContext, "La cuenta origen y la cuenta destino deben ser diferentes."),
        TransferRejectionReason.AccountBlocked => TransferRejected(httpContext, "La cuenta origen o destino no está disponible para esta operación."),
        TransferRejectionReason.InvalidAmount => TransferRejected(httpContext, "El importe debe ser mayor que cero."),
        TransferRejectionReason.InsufficientFunds => TransferRejected(httpContext, "El saldo disponible de la cuenta origen es insuficiente."),
        TransferRejectionReason.IdempotencyConflict => Conflict(
            httpContext, "idempotency-conflict", "Conflicto de idempotencia", "Esta Idempotency-Key ya fue utilizada con datos diferentes."),
        TransferRejectionReason.ConcurrencyConflict => Conflict(
            httpContext, "concurrency-conflict", "Conflicto de concurrencia", "La cuenta origen fue modificada por otra operación. Intente nuevamente."),
        TransferRejectionReason.DestinationAccountNotFound => DestinationAccountNotFound(httpContext),
        TransferRejectionReason.DestinationIsOwnAccount => DestinationIsOwnAccount(httpContext),
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Motivo de rechazo de transferencia no mapeado.")
    };

    private static ObjectResult BadRequest(HttpContext httpContext, string detail) => new(new ProblemDetails
    {
        Type = "https://banca-digital-pe.dev/errors/bad-request",
        Title = "Solicitud inválida",
        Status = StatusCodes.Status400BadRequest,
        Detail = detail,
        Instance = httpContext.Request.Path
    })
    {
        StatusCode = StatusCodes.Status400BadRequest,
        ContentTypes = { "application/problem+json" }
    };

    private static ObjectResult AccountNotEligible(HttpContext httpContext) => new(new ProblemDetails
    {
        Type = "https://banca-digital-pe.dev/errors/not-found",
        Title = "Recurso no encontrado",
        Status = StatusCodes.Status404NotFound,
        Detail = "La cuenta indicada no existe o no está disponible para esta operación.",
        Instance = httpContext.Request.Path
    })
    {
        StatusCode = StatusCodes.Status404NotFound,
        ContentTypes = { "application/problem+json" }
    };

    private static ObjectResult DestinationAccountNotFound(HttpContext httpContext) => new(new ProblemDetails
    {
        Type = "https://banca-digital-pe.dev/errors/destination-not-found",
        Title = "Cuenta destino no encontrada",
        Status = StatusCodes.Status404NotFound,
        Detail = "No existe ninguna cuenta con el número de cuenta indicado.",
        Instance = httpContext.Request.Path
    })
    {
        StatusCode = StatusCodes.Status404NotFound,
        ContentTypes = { "application/problem+json" }
    };

    private static ObjectResult DestinationIsOwnAccount(HttpContext httpContext) => new(new ProblemDetails
    {
        Type = "https://banca-digital-pe.dev/errors/destination-is-own-account",
        Title = "Transferencia rechazada",
        Status = StatusCodes.Status422UnprocessableEntity,
        Detail = "La cuenta destino indicada le pertenece a usted; use transferencias entre cuentas propias.",
        Instance = httpContext.Request.Path
    })
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity,
        ContentTypes = { "application/problem+json" }
    };

    private static ObjectResult TransferRejected(HttpContext httpContext, string detail) => new(new ProblemDetails
    {
        Type = "https://banca-digital-pe.dev/errors/transfer-rejected",
        Title = "Transferencia rechazada",
        Status = StatusCodes.Status422UnprocessableEntity,
        Detail = detail,
        Instance = httpContext.Request.Path
    })
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity,
        ContentTypes = { "application/problem+json" }
    };

    private static ObjectResult Conflict(HttpContext httpContext, string typeSuffix, string title, string detail) => new(new ProblemDetails
    {
        Type = $"https://banca-digital-pe.dev/errors/{typeSuffix}",
        Title = title,
        Status = StatusCodes.Status409Conflict,
        Detail = detail,
        Instance = httpContext.Request.Path
    })
    {
        StatusCode = StatusCodes.Status409Conflict,
        ContentTypes = { "application/problem+json" }
    };
}
