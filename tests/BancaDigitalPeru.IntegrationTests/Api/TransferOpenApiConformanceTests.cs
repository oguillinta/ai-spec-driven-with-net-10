using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica que el contrato <c>own-account-transfers-v1.yaml</c> se sirve correctamente y que las
/// respuestas reales de los 3 endpoints respetan las formas declaradas (plan.md "API First
/// Strategy" de 002; mismo gate que <c>OpenApiConformanceTests</c> de 001).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TransferOpenApiConformanceTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string CuentaB = "aaaaaaaa-2222-2222-2222-222222222222";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public TransferOpenApiConformanceTests(PostgresContainerFixture fixture)
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
        var response = await _client.GetAsync("/openapi/transfers-v1.yaml");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("openapi: 3.1.0", body, StringComparison.Ordinal);
        Assert.Contains("/transfer-previews:", body, StringComparison.Ordinal);
        Assert.Contains("/transfers:", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewResponse_CumpleElSchemaDeclarado()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaB,
            amount = 10.00m
        });
        response.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var campo in new[] { "previewReference", "sourceAccountId", "destinationAccountId", "amount" })
        {
            Assert.True(documento.RootElement.TryGetProperty(campo, out _), $"Falta el campo '{campo}' en TransferPreviewResponse.");
        }
    }

    [Fact]
    public async Task TransferResult_CumpleElSchemaDeclarado()
    {
        var preview = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaB,
            amount = 10.00m
        });
        var previewBody = await preview.Content.ReadFromJsonAsync<TransferPreviewResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers")
        {
            Content = JsonContent.Create(new { previewReference = previewBody!.PreviewReference })
        };
        request.Headers.Add("Idempotency-Key", $"key-{Guid.NewGuid()}");
        var confirmacion = await _client.SendAsync(request);
        confirmacion.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await confirmacion.Content.ReadAsStringAsync());
        foreach (var campo in new[] { "transferId", "completedAt", "sourceAccountMasked", "destinationAccountMasked", "amount", "status" })
        {
            Assert.True(documento.RootElement.TryGetProperty(campo, out _), $"Falta el campo '{campo}' en TransferResult.");
        }
    }

    [Fact]
    public async Task RespuestaDeError_UsaContentTypeProblemJson()
    {
        var response = await _client.GetAsync($"/api/v1/transfers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
