using System.Net;
using System.Net.Http.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Escenarios de concurrencia de la sección 16 del input de planificación de 003. El primero
/// (overspending del ordenante) reutiliza exactamente el mecanismo ya probado en
/// `TransferConcurrencyTests` de 002. El segundo (créditos concurrentes al mismo destino) es
/// nuevo: nunca fue ejercitado en 002, aunque el mecanismo `xmin` ya lo protege por construcción
/// (research.md de 003 §2) — esta prueba lo verifica explícitamente por primera vez.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ThirdPartyTransferConcurrencyTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string CuentaB2DelMismoCliente = "aaaaaaaa-2222-2222-2222-222222222222";
    private const string NumeroCuentaB1DeTercero = "00123456780003";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ThirdPartyTransferConcurrencyTests(PostgresContainerFixture fixture)
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

    private async Task<string> ObtenerVistaPreviaAsync(string origen, decimal importe)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = origen,
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

    private async Task<decimal> ObtenerSaldoAsync(string accountId)
    {
        var cuentas = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadFromJsonAsync<List<AccountBalanceView>>();
        return cuentas!.Single(a => a.AccountId == Guid.Parse(accountId)).Balance.Amount;
    }

    private sealed record AccountBalanceView(Guid AccountId, MoneyView Balance);

    private sealed record MoneyView(decimal Amount);

    [Fact]
    public async Task DosTransferenciasConcurrentesATercerosQueSuperanElSaldoDelOrdenante_NuncaProducenOverspending()
    {
        // Cuenta A: S/ 2,500.00. Dos transferencias de S/ 1,600.00 cada una hacia el mismo tercero
        // (individualmente válidas, conjuntamente superan el saldo) — mismo patrón que
        // TransferConcurrencyTests de 002, sección 16 del input.
        var referenciaA = await ObtenerVistaPreviaAsync(CuentaA, 1600.00m);
        var referenciaB = await ObtenerVistaPreviaAsync(CuentaA, 1600.00m);

        var tareaA = ConfirmarAsync(referenciaA, $"key-a-{Guid.NewGuid()}");
        var tareaB = ConfirmarAsync(referenciaB, $"key-b-{Guid.NewGuid()}");
        var respuestas = await Task.WhenAll(tareaA, tareaB);

        var exitosas = respuestas.Count(r => r.StatusCode == HttpStatusCode.Created);
        var rechazadas = respuestas.Count(r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity);

        Assert.Equal(1, exitosas);
        Assert.Equal(1, rechazadas);

        var saldoOrigen = await ObtenerSaldoAsync(CuentaA);
        Assert.True(saldoOrigen >= 0, "El saldo de la cuenta origen nunca debe quedar negativo.");
    }

    [Fact]
    public async Task DosCreditosConcurrentesALaMismaCuentaDestinoDeUnTercero_NingunoSePierdeTrasReintentar()
    {
        // Nuevo respecto de 002 (research.md de 003 §2): dos confirmaciones simultáneas, cada una
        // desde una cuenta origen distinta del Cliente A, acreditando la MISMA cuenta destino de
        // un tercero (Cuenta B1 del Cliente B). El mecanismo xmin protege la fila `accounts` de la
        // cuenta destino exactamente igual que protege la fila de una cuenta origen: el
        // concurrency check de EF Core opera por fila actualizada, sin distinguir el rol que esa
        // cuenta cumple en la operación. Por lo tanto — igual que en el escenario de overspending
        // — como MÁXIMO una de las dos gana la carrera al confirmar; la otra recibe 409 en vez de
        // aplicar su crédito silenciosamente encima de una lectura obsoleta (eso sería, según
        // research.md §2, la pérdida de actualización que xmin existe para impedir). La garantía
        // de "ningún crédito se pierde" se demuestra reintentando la perdedora con una vista
        // previa fresca: tras el reintento, el saldo final refleja ambos importes.
        var destinoId = "bbbbbbbb-1111-1111-1111-111111111111";
        var saldoDestinoAntes = await ObtenerSaldoAsync(destinoId);

        var referenciaA = await ObtenerVistaPreviaAsync(CuentaA, 20.00m);
        var referenciaB = await ObtenerVistaPreviaAsync(CuentaB2DelMismoCliente, 30.00m);

        var tareaA = ConfirmarAsync(referenciaA, $"key-credit-a-{Guid.NewGuid()}");
        var tareaB = ConfirmarAsync(referenciaB, $"key-credit-b-{Guid.NewGuid()}");
        var respuestas = await Task.WhenAll(tareaA, tareaB);

        var exitosas = respuestas.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictos = respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.True(exitosas is 1 or 2, "Al menos una confirmación debe tener éxito.");
        Assert.Equal(2, exitosas + conflictos);

        if (conflictos > 0)
        {
            // La perdedora reintenta con una vista previa fresca (el saldo destino ya cambió, y
            // una vista previa vieja no se reutiliza — mismo criterio de FR-012 para el origen).
            var origenPerdedor = respuestas[0].StatusCode == HttpStatusCode.Conflict ? CuentaA : CuentaB2DelMismoCliente;
            var importePerdedor = origenPerdedor == CuentaA ? 20.00m : 30.00m;
            var referenciaReintento = await ObtenerVistaPreviaAsync(origenPerdedor, importePerdedor);
            var reintento = await ConfirmarAsync(referenciaReintento, $"key-retry-{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.Created, reintento.StatusCode);
        }

        var saldoDestinoDespues = await ObtenerSaldoAsync(destinoId);
        Assert.Equal(saldoDestinoAntes + 20.00m + 30.00m, saldoDestinoDespues);
    }
}
