using System.Net;
using System.Net.Http.Json;
using BancaDigitalPeru.Api.Contracts.DebitCards;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class DebitCardsEndpointTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public DebitCardsEndpointTests(PostgresContainerFixture fixture)
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
    public async Task GetDebitCards_DevuelveLasDosTarjetasDelClienteA()
    {
        var response = await _client.GetAsync("/api/v1/debit-cards");

        response.EnsureSuccessStatusCode();
        var cards = await response.Content.ReadFromJsonAsync<List<DebitCardSummaryResponse>>();

        Assert.NotNull(cards);
        Assert.Equal(2, cards!.Count);
        Assert.All(cards, c => Assert.StartsWith("**** **** **** ", c.MaskedNumber, StringComparison.Ordinal));
        Assert.All(cards, c => Assert.Equal(4, c.Last4Digits.Length));
    }

    [Fact]
    public async Task GetDebitCards_NuncaExponeElNumeroCompletoDeTarjeta()
    {
        var response = await _client.GetAsync("/api/v1/debit-cards");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("4111111111114582", body, StringComparison.Ordinal);
        Assert.DoesNotContain("4111111111119911", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDebitCards_DevuelveStatus200()
    {
        var response = await _client.GetAsync("/api/v1/debit-cards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDebitCardById_ConTarjetaPropia_DevuelveSuDetalle()
    {
        var response = await _client.GetAsync("/api/v1/debit-cards/cccccccc-1111-1111-1111-111111111111");

        response.EnsureSuccessStatusCode();
        var card = await response.Content.ReadFromJsonAsync<DebitCardSummaryResponse>();

        Assert.NotNull(card);
        Assert.Equal("4582", card!.Last4Digits);
        Assert.Equal(new Guid("aaaaaaaa-1111-1111-1111-111111111111"), card.AccountId);
    }

    [Fact]
    public async Task GetDebitCardById_ConTarjetaDeOtroCliente_Devuelve404Generico()
    {
        var response = await _client.GetAsync("/api/v1/debit-cards/dddddddd-1111-1111-1111-111111111111");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDebitCardById_TarjetaInexistenteYTarjetaAjena_DevuelvenElMismoCuerpoProblemDetails()
    {
        // US5 / FR-022 / SC-002: la respuesta debe ser indistinguible entre ambos casos.
        var respuestaInexistente = await _client.GetAsync($"/api/v1/debit-cards/{Guid.NewGuid()}");
        var respuestaAjena = await _client.GetAsync("/api/v1/debit-cards/dddddddd-1111-1111-1111-111111111111");

        var cuerpoInexistente = await respuestaInexistente.Content.ReadAsStringAsync();
        var cuerpoAjena = await respuestaAjena.Content.ReadAsStringAsync();

        Assert.Equal(respuestaInexistente.StatusCode, respuestaAjena.StatusCode);
        Assert.Equal(cuerpoInexistente, cuerpoAjena);
    }

    [Fact]
    public async Task GetDebitCardById_ConFormatoInvalido_Devuelve400()
    {
        var response = await _client.GetAsync("/api/v1/debit-cards/no-es-un-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDebitCardById_ConGuidVacio_Devuelve404YNo500()
    {
        // Regresión: mismo caso que AccountsController (Guid.Empty no debe causar 500).
        var response = await _client.GetAsync("/api/v1/debit-cards/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
