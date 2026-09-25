using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica que `third-party-transfers-v1.yaml` se sirve correctamente y que las respuestas reales
/// cumplen las formas declaradas — tanto el endpoint nuevo (vista previa) como el campo opcional
/// nuevo de `TransferResult` en el `own-account-transfers-v1.yaml` ya actualizado (research.md de
/// 003 §10; mismo gate que `TransferOpenApiConformanceTests` de 002).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ThirdPartyTransferOpenApiConformanceTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string NumeroCuentaB1DeTercero = "00123456780003";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ThirdPartyTransferOpenApiConformanceTests(PostgresContainerFixture fixture)
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
        var response = await _client.GetAsync("/openapi/third-party-transfers-v1.yaml");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("openapi: 3.1.0", body, StringComparison.Ordinal);
        Assert.Contains("/third-party-transfer-previews:", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewResponse_CumpleElSchemaDeclarado()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = 10.00m
        });
        response.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var campo in new[] { "previewReference", "sourceAccountId", "destinationAccountMasked", "destinationCustomerDisplayName", "amount" })
        {
            Assert.True(documento.RootElement.TryGetProperty(campo, out _), $"Falta el campo '{campo}' en ThirdPartyTransferPreviewResponse.");
        }
    }

    [Fact]
    public async Task TransferResult_DeUnaConfirmacionATerceros_IncluyeElCampoOpcionalNuevo()
    {
        var preview = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = 10.00m
        });
        var previewBody = await preview.Content.ReadFromJsonAsync<ThirdPartyTransferPreviewResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers")
        {
            Content = JsonContent.Create(new { previewReference = previewBody!.PreviewReference })
        };
        request.Headers.Add("Idempotency-Key", $"key-{Guid.NewGuid()}");
        var confirmacion = await _client.SendAsync(request);
        confirmacion.EnsureSuccessStatusCode();

        using var documento = JsonDocument.Parse(await confirmacion.Content.ReadAsStringAsync());
        Assert.True(documento.RootElement.TryGetProperty("destinationCustomerDisplayName", out _));
    }

    [Fact]
    public async Task RespuestaDeError_UsaContentTypeProblemJson()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = "99999999999999",
            amount = 10.00m
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
