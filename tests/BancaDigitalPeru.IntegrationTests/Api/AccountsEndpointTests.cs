using System.Net;
using System.Net.Http.Json;
using BancaDigitalPeru.Api.Contracts.Accounts;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class AccountsEndpointTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public AccountsEndpointTests(PostgresContainerFixture fixture)
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
    public async Task GetAccounts_DevuelveLasDosCuentasDelClienteA()
    {
        var response = await _client.GetAsync("/api/v1/accounts");

        response.EnsureSuccessStatusCode();
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountSummaryResponse>>();

        Assert.NotNull(accounts);
        Assert.Equal(2, accounts!.Count);
        Assert.All(accounts, a => Assert.StartsWith("****", a.MaskedNumber, StringComparison.Ordinal));
        Assert.All(accounts, a => Assert.Equal(4, a.MaskedNumber.Length - 4));
    }

    [Fact]
    public async Task GetAccounts_NuncaExponeElNumeroCompletoDeCuenta()
    {
        var response = await _client.GetAsync("/api/v1/accounts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("00123456780001", body, StringComparison.Ordinal);
        Assert.DoesNotContain("00123456780002", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAccounts_DevuelveStatus200()
    {
        var response = await _client.GetAsync("/api/v1/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_ConCuentaPropia_DevuelveSuDetalle()
    {
        var response = await _client.GetAsync("/api/v1/accounts/aaaaaaaa-1111-1111-1111-111111111111");

        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<AccountSummaryResponse>();

        Assert.NotNull(account);
        Assert.Equal(2500.00m, account!.Balance.Amount);
    }

    [Fact]
    public async Task GetAccountById_ConCuentaDeOtroCliente_Devuelve404Generico()
    {
        var response = await _client.GetAsync("/api/v1/accounts/bbbbbbbb-1111-1111-1111-111111111111");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_CuentaInexistenteYCuentaAjena_DevuelvenElMismoCuerpoProblemDetails()
    {
        // US5 / FR-022 / SC-002: la respuesta debe ser indistinguible entre ambos casos.
        var respuestaInexistente = await _client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}");
        var respuestaAjena = await _client.GetAsync("/api/v1/accounts/bbbbbbbb-1111-1111-1111-111111111111");

        var cuerpoInexistente = await respuestaInexistente.Content.ReadAsStringAsync();
        var cuerpoAjena = await respuestaAjena.Content.ReadAsStringAsync();

        Assert.Equal(respuestaInexistente.StatusCode, respuestaAjena.StatusCode);
        Assert.Equal(cuerpoInexistente, cuerpoAjena);
    }

    [Fact]
    public async Task GetAccountById_ConFormatoInvalido_Devuelve400()
    {
        var response = await _client.GetAsync("/api/v1/accounts/no-es-un-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_ConGuidVacio_Devuelve404YNo500()
    {
        // Regresión: Guid.Empty es sintácticamente válido pero AccountId lo rechaza como
        // identidad real; debe tratarse como "no encontrado", nunca como error del servidor.
        var response = await _client.GetAsync("/api/v1/accounts/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
