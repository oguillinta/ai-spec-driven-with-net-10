using System.Net;
using System.Text.Json;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica que el contrato estático se sirve correctamente y que las respuestas reales de los 4
/// endpoints respetan las formas declaradas en contracts/openapi/banking-products-v1.yaml
/// (gate de la sección 6 del plan; plan.md "API First Strategy").
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class OpenApiConformanceTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public OpenApiConformanceTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _factory = new BankingApiFactory(_fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ContratoOpenApi_SeSirveEnLaRutaEsperada()
    {
        var response = await _client.GetAsync("/openapi/v1.yaml");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("openapi: 3.1.0", body, StringComparison.Ordinal);
        Assert.Contains("/accounts:", body, StringComparison.Ordinal);
        Assert.Contains("/debit-cards:", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/v1/accounts")]
    [InlineData("/api/v1/debit-cards")]
    public async Task ListEndpoints_DevuelvenUnArrayJsonConLosCamposDeclaradosEnElContrato(string path)
    {
        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.True(document.RootElement.GetArrayLength() > 0);

        var expectedFields = path.Contains("accounts", StringComparison.Ordinal)
            ? new[] { "accountId", "accountType", "maskedNumber", "balance", "status" }
            : new[] { "debitCardId", "maskedNumber", "last4Digits", "accountId", "status", "expiration" };

        foreach (var item in document.RootElement.EnumerateArray())
        {
            foreach (var field in expectedFields)
            {
                Assert.True(item.TryGetProperty(field, out _), $"Falta el campo '{field}' declarado en el contrato.");
            }
        }
    }

    [Fact]
    public async Task RespuestaDeError_UsaContentTypeProblemJson()
    {
        var response = await _client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
