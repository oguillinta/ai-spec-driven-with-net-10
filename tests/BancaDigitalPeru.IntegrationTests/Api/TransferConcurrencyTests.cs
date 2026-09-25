using System.Net;
using System.Net.Http.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Escenario de la sección 12 del input de planificación: dos confirmaciones concurrentes sobre la
/// misma cuenta origen, con importes individualmente válidos pero conjuntamente superiores al
/// saldo disponible, nunca deben permitir overspending (research.md §6, `xmin`).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TransferConcurrencyTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string CuentaB = "aaaaaaaa-2222-2222-2222-222222222222";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public TransferConcurrencyTests(PostgresContainerFixture fixture)
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
        var response = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaB,
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
    public async Task DosTransferenciasConcurrentesQueSuperanElSaldoDisponible_NuncaProducenOverspending()
    {
        // Cuenta A: S/ 2,500.00. Dos transferencias de S/ 1,600.00 cada una (individualmente
        // válidas, conjuntamente superan el saldo).
        var referenciaA = await ObtenerVistaPreviaAsync(1600.00m);
        var referenciaB = await ObtenerVistaPreviaAsync(1600.00m);

        var tareaA = ConfirmarAsync(referenciaA, $"key-a-{Guid.NewGuid()}");
        var tareaB = ConfirmarAsync(referenciaB, $"key-b-{Guid.NewGuid()}");
        var respuestas = await Task.WhenAll(tareaA, tareaB);

        var exitosas = respuestas.Count(r => r.StatusCode == HttpStatusCode.Created);
        var rechazadas = respuestas.Count(r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity);

        Assert.Equal(1, exitosas);
        Assert.Equal(1, rechazadas);

        var cuentas = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadFromJsonAsync<List<AccountBalanceView>>();
        var cuentaOrigen = cuentas!.Single(a => a.AccountId == Guid.Parse(CuentaA));
        Assert.True(cuentaOrigen.Balance.Amount >= 0, "El saldo de la cuenta origen nunca debe quedar negativo.");
        Assert.Equal(900.00m, cuentaOrigen.Balance.Amount);
    }

    private sealed record AccountBalanceView(Guid AccountId, MoneyView Balance);

    private sealed record MoneyView(decimal Amount);
}
