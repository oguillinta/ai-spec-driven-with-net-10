using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BancaDigitalPeru.Api.ErrorHandling;

/// <summary>
/// Captura cualquier excepción no controlada y la traduce a un ProblemDetails 500 genérico, sin
/// exponer stack traces, excepciones de EF Core/Npgsql ni nombres de tabla (Principio VI).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error inesperado procesando {Path}", httpContext.Request.Path);

        var problemDetails = ApiProblemDetails.InternalServerError(httpContext);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
