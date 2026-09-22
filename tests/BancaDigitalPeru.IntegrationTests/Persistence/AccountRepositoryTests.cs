using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Infrastructure.Persistence;
using BancaDigitalPeru.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class AccountRepositoryTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));

    private readonly PostgresContainerFixture _fixture;

    public AccountRepositoryTests(PostgresContainerFixture fixture)
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
    public async Task GetByCustomerAsync_DevuelveSoloLasCuentasDelClienteA_IncluidaLaBloqueada()
    {
        await using var dbContext = CreateDbContext();
        var repository = new AccountRepository(dbContext);

        var accounts = await repository.GetByCustomerAsync(ClienteA, CancellationToken.None);

        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, a => a.Status == AccountStatus.Active);
        Assert.Contains(accounts, a => a.Status == AccountStatus.Blocked);
        Assert.All(accounts, a => Assert.Equal(ClienteA, a.CustomerId));
    }

    [Fact]
    public async Task GetByIdForCustomerAsync_ConCuentaDeOtroCliente_DevuelveNull()
    {
        await using var dbContext = CreateDbContext();
        var repository = new AccountRepository(dbContext);
        var cuentaDelClienteB = new AccountId(Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"));

        var account = await repository.GetByIdForCustomerAsync(ClienteA, cuentaDelClienteB, CancellationToken.None);

        Assert.Null(account);
    }
}
