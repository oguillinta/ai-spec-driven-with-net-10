using Microsoft.AspNetCore.Mvc;

namespace BancaDigitalPeru.Api.ErrorHandling;

/// <summary>
/// Cuerpos de error ProblemDetails genéricos, en español de Perú (Principio III de la
/// constitución), sin detalles internos de persistencia (Principio VI). "No existe" y "pertenece
/// a otro cliente" usan exactamente el mismo cuerpo (spec FR-022, SC-002).
/// </summary>
public static class ApiProblemDetails
{
    public static ObjectResult NotFound(HttpContext httpContext) => new(new ProblemDetails
    {
        Type = "https://banca-digital-pe.dev/errors/not-found",
        Title = "Recurso no encontrado",
        Status = StatusCodes.Status404NotFound,
        Detail = "El producto solicitado no existe o no está disponible para este cliente.",
        Instance = httpContext.Request.Path
    })
    {
        StatusCode = StatusCodes.Status404NotFound,
        ContentTypes = { "application/problem+json" }
    };

    public static ObjectResult BadRequest(HttpContext httpContext, string detail) => new(new ProblemDetails
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

    public static ProblemDetails InternalServerError(HttpContext httpContext) => new()
    {
        Type = "https://banca-digital-pe.dev/errors/internal",
        Title = "Error inesperado",
        Status = StatusCodes.Status500InternalServerError,
        Detail = "Ocurrió un error inesperado. Intente nuevamente más tarde.",
        Instance = httpContext.Request.Path
    };
}
