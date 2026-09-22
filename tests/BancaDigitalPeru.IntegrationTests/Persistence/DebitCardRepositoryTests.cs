using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;
using BancaDigitalPeru.Infrastructure.Persistence;
using BancaDigitalPeru.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class DebitCardRepositoryTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));

    private readonly PostgresContainerFixture _fixture;

    public DebitCardRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private BancaDigitalPeruDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BancaDigitalPeruDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new BancaDigitalPeruDbContext(options);
    }

    [Fact]
    public async Task GetByCustomerAsync_DevuelveSoloLasTarjetasDelClienteA_IncluidaLaBloqueada()
    {
        await using var dbContext = CreateDbContext();
        var repository = new DebitCardRepository(dbContext);

        var cards = await repository.GetByCustomerAsync(ClienteA, CancellationToken.None);

        Assert.Equal(2, cards.Count);
        Assert.Contains(cards, c => c.Status == CardStatus.Active);
        Assert.Contains(cards, c => c.Status == CardStatus.Blocked);
        Assert.All(cards, c => Assert.Equal(ClienteA, c.CustomerId));
    }

    [Fact]
    public async Task GetByIdForCustomerAsync_ConTarjetaDeOtroCliente_DevuelveNull()
    {
        await using var dbContext = CreateDbContext();
        var repository = new DebitCardRepository(dbContext);
        var tarjetaDelClienteB = new DebitCardId(Guid.Parse("dddddddd-1111-1111-1111-111111111111"));

        var card = await repository.GetByIdForCustomerAsync(ClienteA, tarjetaDelClienteB, CancellationToken.None);

        Assert.Null(card);
    }
}
