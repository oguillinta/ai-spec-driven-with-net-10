using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BancaDigitalPeru.IntegrationTests;

/// <summary>
/// WebApplicationFactory que apunta la Api al contenedor PostgreSQL real de
/// <see cref="PostgresContainerFixture"/>, en lugar de la base de datos de desarrollo local.
/// </summary>
public sealed class BankingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly Action<IServiceCollection>? _configureTestServices;

    public BankingApiFactory(string connectionString, Action<IServiceCollection>? configureTestServices = null)
    {
        _connectionString = connectionString;
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString
            });
        });

        if (_configureTestServices is not null)
        {
            builder.ConfigureTestServices(_configureTestServices);
        }
    }
}
