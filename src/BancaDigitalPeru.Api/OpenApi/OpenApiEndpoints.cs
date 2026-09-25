using Scalar.AspNetCore;

namespace BancaDigitalPeru.Api.OpenApi;

/// <summary>
/// Sirve los contratos OpenAPI estáticos versionados (uno por capacidad de negocio, plan.md "API
/// First Strategy" de 002) y configura Scalar con ambas fuentes (research.md §7 de 001): Scalar
/// nunca genera ningún contrato, solo los muestra.
/// </summary>
public static class OpenApiEndpoints
{
    private const string BankingProductsRoutePattern = "/openapi/v1.yaml";
    private const string OwnAccountTransfersRoutePattern = "/openapi/transfers-v1.yaml";
    private const string ThirdPartyTransfersRoutePattern = "/openapi/third-party-transfers-v1.yaml";

    public static IEndpointRouteBuilder MapOpenApiContracts(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(BankingProductsRoutePattern, async context =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "OpenApi", "banking-products-v1.yaml");
            context.Response.ContentType = "application/yaml";
            await context.Response.SendFileAsync(filePath);
        });

        endpoints.MapGet(OwnAccountTransfersRoutePattern, async context =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "OpenApi", "own-account-transfers-v1.yaml");
            context.Response.ContentType = "application/yaml";
            await context.Response.SendFileAsync(filePath);
        });

        endpoints.MapGet(ThirdPartyTransfersRoutePattern, async context =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "OpenApi", "third-party-transfers-v1.yaml");
            context.Response.ContentType = "application/yaml";
            await context.Response.SendFileAsync(filePath);
        });

        endpoints.MapScalarApiReference(options =>
        {
            options.WithTitle("Banca Digital Perú");
            options.AddDocument("banking-products", "Consulta de Productos Bancarios", BankingProductsRoutePattern);
            options.AddDocument("own-account-transfers", "Transferencias entre cuentas propias", OwnAccountTransfersRoutePattern);
            options.AddDocument("third-party-transfers", "Transferencias a cuentas de terceros", ThirdPartyTransfersRoutePattern);
        });

        return endpoints;
    }
}
