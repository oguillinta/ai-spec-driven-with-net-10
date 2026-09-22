using System.Net.Http.Json;
using BancaDigitalPeru.Api.Contracts.Accounts;
using BancaDigitalPeru.Api.Contracts.DebitCards;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// US5 - Proteger mis productos: verifica de extremo a extremo, sobre ambos listados a la vez,
/// que el Cliente A (cliente actual configurado) nunca recibe ningún identificador sembrado del
/// Cliente B (spec FR-015, HU5).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CrossCustomerIsolationTests : IAsyncLifetime
{
    private static readonly Guid[] AccountIdsDelClienteB = [new("bbbbbbbb-1111-1111-1111-111111111111")];
    private static readonly Guid[] DebitCardIdsDelClienteB = [new("dddddddd-1111-1111-1111-111111111111")];

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public CrossCustomerIsolationTests(PostgresContainerFixture fixture)
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
    public async Task GetAccounts_NuncaIncluyeCuentasDelClienteB()
    {
        var accounts = await _client.GetFromJsonAsync<List<AccountSummaryResponse>>("/api/v1/accounts");

        Assert.NotNull(accounts);
        Assert.DoesNotContain(accounts!, a => AccountIdsDelClienteB.Contains(a.AccountId));
    }

    [Fact]
    public async Task GetDebitCards_NuncaIncluyeTarjetasDelClienteB()
    {
        var cards = await _client.GetFromJsonAsync<List<DebitCardSummaryResponse>>("/api/v1/debit-cards");

        Assert.NotNull(cards);
        Assert.DoesNotContain(cards!, c => DebitCardIdsDelClienteB.Contains(c.DebitCardId));
    }
}
