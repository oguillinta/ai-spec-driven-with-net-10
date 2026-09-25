using System.Net;
using System.Net.Http.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica que una misma solicitud lógica de confirmación hacia un tercero nunca aplica el
/// movimiento financiero más de una vez, incluso ante reenvíos concurrentes (RB11 de 003, mismo
/// mecanismo que `TransferIdempotencyTests` de 002 — research.md de 003 §1).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ThirdPartyTransferIdempotencyTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string NumeroCuentaB1DeTercero = "00123456780003";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ThirdPartyTransferIdempotencyTests(PostgresContainerFixture fixture)
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

    private async Task<string> ObtenerVistaPreviaAsync(decimal importe)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = importe
        });
        response.EnsureSuccessStatusCode();
        var preview = await response.Content.ReadFromJsonAsync<ThirdPartyTransferPreviewResponse>();
        return preview!.PreviewReference;
    }

    private Task<HttpResponseMessage> ConfirmarAsync(string previewReference, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers")
        {
            Content = JsonContent.Create(new { previewReference })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task SolicitudDuplicadaSecuencial_AplicaElMovimientoUnaSolaVez()
    {
        // CA11/CL9.
        var idempotencyKey = $"key-{Guid.NewGuid()}";
        var referencia = await ObtenerVistaPreviaAsync(300.00m);

        var primera = await ConfirmarAsync(referencia, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        var resultadoPrimera = await primera.Content.ReadFromJsonAsync<TransferResultResponse>();

        var saldosTrasPrimera = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();

        var segunda = await ConfirmarAsync(referencia, idempotencyKey);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        var resultadoSegunda = await segunda.Content.ReadFromJsonAsync<TransferResultResponse>();

        Assert.Equal(resultadoPrimera!.TransferId, resultadoSegunda!.TransferId);

        var saldosTrasSegunda = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();
        Assert.Equal(saldosTrasPrimera, saldosTrasSegunda);
    }

    [Fact]
    public async Task DosConfirmacionesSimultaneasHaciaUnTerceroConLaMismaIdempotencyKey_SoloUnaAplicaElMovimiento()
    {
        var idempotencyKey = $"key-{Guid.NewGuid()}";
        var referenciaA = await ObtenerVistaPreviaAsync(300.00m);
        var referenciaB = await ObtenerVistaPreviaAsync(300.00m);

        var tareaA = ConfirmarAsync(referenciaA, idempotencyKey);
        var tareaB = ConfirmarAsync(referenciaB, idempotencyKey);
        var respuestas = await Task.WhenAll(tareaA, tareaB);

        var codigosExitosos = respuestas.Count(r => r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK);
        Assert.Equal(2, codigosExitosos);

        var resultados = await Task.WhenAll(respuestas.Select(r => r.Content.ReadFromJsonAsync<TransferResultResponse>()));
        Assert.Equal(resultados[0]!.TransferId, resultados[1]!.TransferId);
    }
}
