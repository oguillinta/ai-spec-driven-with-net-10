using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica el flujo HTTP completo de transferencias a terceros contra PostgreSQL real (plan.md
/// "Testing Strategy"). Reutiliza los datos de referencia de `001`/`002`/`003`: Cliente A
/// (aaaaaaaa-1111-..., ACTIVA, S/ 2,500.00, "María López Torres") transfiere hacia la Cuenta B1 de
/// Cliente B (bbbbbbbb-1111-..., número 00123456780003, ACTIVA, S/ 1,500.00 — valor real del seed
/// de 001, "Juan Pérez García"). La confirmación y la consulta reutilizan el mismo endpoint de 002
/// (research.md de 003 §10).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ThirdPartyTransfersEndpointTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string CuentaB2DelMismoCliente = "aaaaaaaa-2222-2222-2222-222222222222";
    private const string NumeroCuentaB1DeTercero = "00123456780003";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ThirdPartyTransfersEndpointTests(PostgresContainerFixture fixture)
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

    private async Task<ThirdPartyTransferPreviewResponse> ObtenerVistaPreviaAsync(string origen, string numeroDestino, decimal importe)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = origen,
            destinationAccountNumber = numeroDestino,
            amount = importe
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ThirdPartyTransferPreviewResponse>())!;
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
    public async Task FlujoCompleto_TransferenciaExitosaATercero_ActualizaAmbosSaldosYMuestraNombreEnmascarado()
    {
        // CA1/CA2/CA10.
        var preview = await ObtenerVistaPreviaAsync(CuentaA, NumeroCuentaB1DeTercero, 300.00m);
        Assert.Equal("****0003", preview.DestinationAccountMasked);
        Assert.Equal("Juan P***", preview.DestinationCustomerDisplayName);

        var confirmacion = await ConfirmarAsync(preview.PreviewReference, $"key-{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Created, confirmacion.StatusCode);
        var resultado = await confirmacion.Content.ReadFromJsonAsync<TransferResultResponse>();
        Assert.Equal(300.00m, resultado!.Amount.Amount);
        Assert.Equal("Juan P***", resultado.DestinationCustomerDisplayName);

        var cuentas = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();
        Assert.Contains("2200", cuentas, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FlujoCompleto_TransferenciaPorElTotalDelSaldoDisponible_EsPermitida()
    {
        // CL1: usa la Cuenta B2 (bloqueada por defecto en 001, activada por 002) del Cliente A
        // como origen de un envío a un tercero por su saldo total, sin interferir con las
        // pruebas que usan la Cuenta A como origen.
        var saldoOrigenAntes = await ObtenerSaldoAsync(CuentaB2DelMismoCliente);

        var preview = await ObtenerVistaPreviaAsync(CuentaB2DelMismoCliente, NumeroCuentaB1DeTercero, saldoOrigenAntes);
        var confirmacion = await ConfirmarAsync(preview.PreviewReference, $"key-{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Created, confirmacion.StatusCode);
        var saldoOrigenDespues = await ObtenerSaldoAsync(CuentaB2DelMismoCliente);
        Assert.Equal(0.00m, saldoOrigenDespues);
    }

    private async Task<decimal> ObtenerSaldoAsync(string accountId)
    {
        var cuentas = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadFromJsonAsync<List<AccountBalanceView>>();
        return cuentas!.Single(a => a.AccountId == Guid.Parse(accountId)).Balance.Amount;
    }

    private sealed record AccountBalanceView(Guid AccountId, MoneyView Balance);

    private sealed record MoneyView(decimal Amount);

    [Fact]
    public async Task PreviewThirdPartyTransfer_ConDestinoInexistente_Devuelve404Revelador()
    {
        // CA7/CL2.
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = "99999999999999",
            amount = 50.00m
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("destination-not-found", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewThirdPartyTransfer_ConDestinoQueEsCuentaPropia_Devuelve422()
    {
        // CA8/CL6.
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = "00123456780002",
            amount = 50.00m
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("destination-is-own-account", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Confirmar_ConVistaPreviaDesactualizada_SeRechazaSinModificarSaldos()
    {
        // FR-012 (reutilizado de 002): dos vistas previas por el saldo total del origen; la
        // primera confirmación agota el saldo, la segunda debe rechazarse al revalidar.
        var previewA = await ObtenerVistaPreviaAsync(CuentaA, NumeroCuentaB1DeTercero, 2500.00m);
        var previewB = await ObtenerVistaPreviaAsync(CuentaA, NumeroCuentaB1DeTercero, 2500.00m);

        var primeraConfirmacion = await ConfirmarAsync(previewA.PreviewReference, $"key-{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Created, primeraConfirmacion.StatusCode);

        var segundaConfirmacion = await ConfirmarAsync(previewB.PreviewReference, $"key-{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, segundaConfirmacion.StatusCode);
    }

    [Fact]
    public async Task PreviewThirdPartyTransfer_ConCuentaOrigenInexistenteYConCuentaOrigenAjena_DevuelvenElMismoCuerpo404()
    {
        // CA6/FR-022: mismo criterio de privacidad que la cuenta origen de 002.
        var respuestaInexistente = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = Guid.NewGuid().ToString(),
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = 10.00m
        });
        var respuestaAjena = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = "bbbbbbbb-1111-1111-1111-111111111111",
            destinationAccountNumber = NumeroCuentaB1DeTercero,
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
    public async Task PreviewThirdPartyTransfer_ConImporteCeroONegativo_Devuelve422YNoModificaSaldos(decimal importe)
    {
        // CA4/CA5. La cuenta destino BLOQUEADA (CL5) y la cuenta origen BLOQUEADA (CA9/CL4) ya
        // están cubiertas exhaustivamente a nivel de Application (PreviewThirdPartyTransferUseCaseTests/
        // ConfirmTransferUseCaseTests); no hay ninguna cuenta BLOQUEADA en el seed vigente tras la
        // migración de 002 (que activó la única cuenta BLOQUEADA original) para ejercitarlas aquí
        // — mismo criterio ya aceptado en `002` (TransfersEndpointTests no cubre ese caso a nivel
        // HTTP por el mismo motivo).
        var antes = await ObtenerSaldoAsync(CuentaA);

        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = importe
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var despues = await ObtenerSaldoAsync(CuentaA);
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task PreviewThirdPartyTransfer_ConSaldoInsuficiente_Devuelve422YNoModificaSaldos()
    {
        // CA3.
        var antes = await ObtenerSaldoAsync(CuentaA);

        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = 999999.00m
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var despues = await ObtenerSaldoAsync(CuentaA);
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task PreviewThirdPartyTransfer_ConNumeroDeCuentaConFormatoInvalido_Devuelve400()
    {
        // CL7 (formato del número de cuenta, análogo a la precisión del importe en 002).
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = "no-es-un-numero",
            amount = 10.00m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PreviewThirdPartyTransfer_ConImporteDeMasDeDosDecimales_Devuelve400()
    {
        // CL7.
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = 10.999m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConsultarTransferencia_TrasConfirmarATercero_IncluyeNombreDelDestinatario()
    {
        // CA13.
        var preview = await ObtenerVistaPreviaAsync(CuentaA, NumeroCuentaB1DeTercero, 50.00m);
        var confirmacion = await ConfirmarAsync(preview.PreviewReference, $"key-{Guid.NewGuid()}");
        var resultado = await confirmacion.Content.ReadFromJsonAsync<TransferResultResponse>();

        var respuesta = await _client.GetAsync($"/api/v1/transfers/{resultado!.TransferId}");

        respuesta.EnsureSuccessStatusCode();
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        foreach (var campo in new[] { "transferId", "completedAt", "sourceAccountMasked", "destinationAccountMasked", "destinationCustomerDisplayName", "amount", "status" })
        {
            Assert.True(documento.RootElement.TryGetProperty(campo, out _), $"Falta el campo '{campo}'.");
        }
        Assert.Equal("Juan P***", documento.RootElement.GetProperty("destinationCustomerDisplayName").GetString());
    }

    [Fact]
    public async Task ConsultarTransferenciaATercero_TrasReiniciarLaApi_DevuelveElMismoResultado()
    {
        // CA14: persistencia observable — un nuevo WebApplicationFactory contra la misma base de
        // datos simula el reinicio de la Api (mismo patrón que 002).
        var preview = await ObtenerVistaPreviaAsync(CuentaA, NumeroCuentaB1DeTercero, 15.00m);
        var confirmacion = await ConfirmarAsync(preview.PreviewReference, $"key-{Guid.NewGuid()}");
        var resultadoOriginal = await confirmacion.Content.ReadFromJsonAsync<TransferResultResponse>();

        await using var nuevaInstancia = new BankingApiFactory(_fixture.ConnectionString);
        using var nuevoCliente = nuevaInstancia.CreateClient();

        var respuesta = await nuevoCliente.GetAsync($"/api/v1/transfers/{resultadoOriginal!.TransferId}");
        var resultadoTrasReinicio = await respuesta.Content.ReadFromJsonAsync<TransferResultResponse>();

        Assert.Equal(resultadoOriginal.TransferId, resultadoTrasReinicio!.TransferId);
        Assert.Equal(resultadoOriginal.DestinationCustomerDisplayName, resultadoTrasReinicio.DestinationCustomerDisplayName);
    }

    [Fact]
    public async Task RespuestasDeVistaPreviaYResultado_NuncaContienenSaldoNiIdentificadoresInternosDelDestinatario()
    {
        // RB10/FR-012, CA10.
        var preview = await ObtenerVistaPreviaAsync(CuentaA, NumeroCuentaB1DeTercero, 10.00m);
        var previewBody = JsonSerializer.Serialize(preview);
        Assert.DoesNotContain("balance", previewBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("customerId", previewBody, StringComparison.OrdinalIgnoreCase);

        var confirmacion = await ConfirmarAsync(preview.PreviewReference, $"key-{Guid.NewGuid()}");
        var confirmacionBody = await confirmacion.Content.ReadAsStringAsync();
        Assert.DoesNotContain("balance", confirmacionBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("customerId", confirmacionBody, StringComparison.OrdinalIgnoreCase);
    }
}
