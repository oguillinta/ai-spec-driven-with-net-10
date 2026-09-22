using BancaDigitalPeru.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests;

/// <summary>
/// Levanta un contenedor PostgreSQL real por sesión de pruebas (research.md §8) y aplica las
/// migraciones (con su seed de datos ficticios) una sola vez.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("banca_digital_pe_test")
            .WithUsername("postgres")
            .WithPassword("devpassword")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<BancaDigitalPeruDbContext>()
            .UseNpgsql(ConnectionString);

        await using var dbContext = new BancaDigitalPeruDbContext(optionsBuilder.Options);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres collection";
}
