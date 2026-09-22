using Scalar.AspNetCore;

namespace BancaDigitalPeru.Api.OpenApi;

/// <summary>
/// Sirve el contrato OpenAPI estático versionado (contracts/openapi/banking-products-v1.yaml) y
/// configura Scalar como su único consumidor visual (research.md §7): Scalar nunca genera el
/// contrato, solo lo muestra.
/// </summary>
public static class OpenApiEndpoints
{
    private const string ContractRoutePattern = "/openapi/v1.yaml";

    public static IEndpointRouteBuilder MapBankingProductsOpenApiContract(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ContractRoutePattern, async context =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "OpenApi", "banking-products-v1.yaml");
            context.Response.ContentType = "application/yaml";
            await context.Response.SendFileAsync(filePath);
        });

        endpoints.MapScalarApiReference(options =>
        {
            options.WithTitle("Banca Digital Perú - Consulta de Productos Bancarios");
            options.WithOpenApiRoutePattern(ContractRoutePattern);
        });

        return endpoints;
    }
}
