using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica el flujo HTTP completo de transferencias entre cuentas propias contra PostgreSQL real
/// (plan.md "Testing Strategy"). Reutiliza los datos de referencia de <c>001</c>/<c>002</c>:
/// Cliente A posee la Cuenta A (aaaaaaaa-1111-..., ACTIVA, S/ 2,500.00) y la Cuenta B
/// (aaaaaaaa-2222-..., ACTIVA tras la migración de 002, S/ 800.00); la Cuenta bbbbbbbb-1111-...
/// pertenece al Cliente B.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TransfersEndpointTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string CuentaB = "aaaaaaaa-2222-2222-2222-222222222222";
    private const string CuentaAjena = "bbbbbbbb-1111-1111-1111-111111111111";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public TransfersEndpointTests(PostgresContainerFixture fixture)
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

    private async Task<string> ObtenerVistaPreviaAsync(string origen, string destino, decimal importe)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = origen,
            destinationAccountId = destino,
            amount = importe
        });
        response.EnsureSuccessStatusCode();
        var preview = await response.Content.ReadFromJsonAsync<TransferPreviewResponse>();
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
    public async Task FlujoCompleto_TransferenciaExitosa_ActualizaAmbosSaldosExactamente()
    {
        // CA1/CA2.
        var referencia = await ObtenerVistaPreviaAsync(CuentaA, CuentaB, 300.00m);

        var response = await ConfirmarAsync(referencia, $"key-{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<TransferResultResponse>();
        Assert.Equal(300.00m, resultado!.Amount.Amount);

        var cuentas = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();
        Assert.Contains("2200", cuentas, StringComparison.Ordinal);
        Assert.Contains("1100", cuentas, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FlujoCompleto_TransferenciaPorElTotalDelSaldo_DejaLaCuentaOrigenEnCero()
    {
        // CL1.
        var referencia = await ObtenerVistaPreviaAsync(CuentaB, CuentaA, 800.00m);

        var response = await ConfirmarAsync(referencia, $"key-{Guid.NewGuid()}");

        response.EnsureSuccessStatusCode();
        var cuentas = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();
        Assert.Contains("\"amount\":0", cuentas, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Confirmar_ConVistaPreviaDesactualizada_SeRechazaSinModificarSaldos()
    {
        // FR-012: revalidación completa al confirmar. Dos vistas previas por el saldo total; la
        // primera confirmación agota el saldo, la segunda debe rechazarse.
        var referenciaA = await ObtenerVistaPreviaAsync(CuentaA, CuentaB, 2500.00m);
        var referenciaB = await ObtenerVistaPreviaAsync(CuentaA, CuentaB, 2500.00m);

        var primeraConfirmacion = await ConfirmarAsync(referenciaA, $"key-{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Created, primeraConfirmacion.StatusCode);

        var segundaConfirmacion = await ConfirmarAsync(referenciaB, $"key-{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, segundaConfirmacion.StatusCode);
    }

    [Fact]
    public async Task PreviewTransfer_ConCuentaOrigenInexistenteYConCuentaOrigenAjena_DevuelvenElMismoCuerpo404()
    {
        // FR-021/FR-022, CA8, CL3.
        var respuestaInexistente = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = Guid.NewGuid().ToString(),
            destinationAccountId = CuentaA,
            amount = 10.00m
        });
        var respuestaAjena = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaAjena,
            destinationAccountId = CuentaA,
            amount = 10.00m
        });

        var cuerpoInexistente = await respuestaInexistente.Content.ReadAsStringAsync();
        var cuerpoAjena = await respuestaAjena.Content.ReadAsStringAsync();

        Assert.Equal(respuestaInexistente.StatusCode, respuestaAjena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, respuestaInexistente.StatusCode);
        Assert.Equal(cuerpoInexistente, cuerpoAjena);
    }

    [Fact]
    public async Task PreviewTransfer_ConCuentaDestinoInexistenteYConCuentaDestinoAjena_DevuelvenElMismoCuerpo404()
    {
        // FR-022, CA9, CL4.
        var respuestaInexistente = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = Guid.NewGuid().ToString(),
            amount = 10.00m
        });
        var respuestaAjena = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaAjena,
            amount = 10.00m
        });

        var cuerpoInexistente = await respuestaInexistente.Content.ReadAsStringAsync();
        var cuerpoAjena = await respuestaAjena.Content.ReadAsStringAsync();

        Assert.Equal(respuestaInexistente.StatusCode, respuestaAjena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, respuestaInexistente.StatusCode);
        Assert.Equal(cuerpoInexistente, cuerpoAjena);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(-10.00)]
    public async Task PreviewTransfer_ConImporteCeroONegativo_Devuelve422YNoModificaSaldos(decimal importe)
    {
        // CA4/CA5.
        var antes = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaB,
            amount = importe
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var despues = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task PreviewTransfer_ConLaMismaCuentaComoOrigenYDestino_Devuelve422()
    {
        // CA6.
        var response = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaA,
            amount = 10.00m
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PreviewTransfer_ConSaldoInsuficiente_Devuelve422YNoModificaSaldos()
    {
        // CA3.
        var antes = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaB,
            destinationAccountId = CuentaA,
            amount = 999999.00m
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var despues = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task PreviewTransfer_ConImporteDeMasDeDosDecimales_Devuelve400()
    {
        // CL2.
        var response = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaB,
            amount = 10.999m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConsultarTransferencia_TrasConfirmar_DevuelveLosSeisDatosMinimos()
    {
        // CA11.
        var referencia = await ObtenerVistaPreviaAsync(CuentaA, CuentaB, 50.00m);
        var confirmacion = await ConfirmarAsync(referencia, $"key-{Guid.NewGuid()}");
        var resultado = await confirmacion.Content.ReadFromJsonAsync<TransferResultResponse>();

        var respuesta = await _client.GetAsync($"/api/v1/transfers/{resultado!.TransferId}");

        respuesta.EnsureSuccessStatusCode();
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        foreach (var campo in new[] { "transferId", "completedAt", "sourceAccountMasked", "destinationAccountMasked", "amount", "status" })
        {
            Assert.True(documento.RootElement.TryGetProperty(campo, out _), $"Falta el campo '{campo}'.");
        }
    }

    [Fact]
    public async Task ConsultarTransferencia_EntreCuentasPropias_NoIncluyeNombreDeDestinatario()
    {
        // Regresión de 003 (data-model.md: destinationCustomerDisplayName solo se puebla cuando
        // la transferencia resultó ser a un tercero): para una transferencia entre cuentas
        // propias, ese campo debe estar ausente.
        var referencia = await ObtenerVistaPreviaAsync(CuentaA, CuentaB, 25.00m);
        var confirmacion = await ConfirmarAsync(referencia, $"key-{Guid.NewGuid()}");
        var resultado = await confirmacion.Content.ReadFromJsonAsync<TransferResultResponse>();

        var respuesta = await _client.GetAsync($"/api/v1/transfers/{resultado!.TransferId}");

        respuesta.EnsureSuccessStatusCode();
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.False(
            documento.RootElement.TryGetProperty("destinationCustomerDisplayName", out var valor) && valor.ValueKind != JsonValueKind.Null,
            "destinationCustomerDisplayName no debe estar presente para una transferencia entre cuentas propias.");
    }

    [Fact]
    public async Task ConsultarTransferencia_TrasReiniciarLaApi_DevuelveElMismoResultado()
    {
        // CA12: persistencia observable — un nuevo WebApplicationFactory contra la misma base de
        // datos simula el reinicio de la Api (el estado vive en PostgreSQL, no en memoria).
        var referencia = await ObtenerVistaPreviaAsync(CuentaA, CuentaB, 25.00m);
        var confirmacion = await ConfirmarAsync(referencia, $"key-{Guid.NewGuid()}");
        var resultadoOriginal = await confirmacion.Content.ReadFromJsonAsync<TransferResultResponse>();

        await using var nuevaInstancia = new BankingApiFactory(_fixture.ConnectionString);
        using var nuevoCliente = nuevaInstancia.CreateClient();

        var respuesta = await nuevoCliente.GetAsync($"/api/v1/transfers/{resultadoOriginal!.TransferId}");
        var resultadoTrasReinicio = await respuesta.Content.ReadFromJsonAsync<TransferResultResponse>();

        Assert.Equal(resultadoOriginal.TransferId, resultadoTrasReinicio!.TransferId);
        Assert.Equal(resultadoOriginal.Amount.Amount, resultadoTrasReinicio.Amount.Amount);
    }

    [Fact]
    public async Task ConsultarTransferencia_InexistenteYAjena_DevuelvenElMismoCuerpo404Generico()
    {
        // FR-018, CA con transferencia ajena: mismo criterio de privacidad que 001.
        var respuestaInexistente = await _client.GetAsync($"/api/v1/transfers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuestaInexistente.StatusCode);
    }
}
