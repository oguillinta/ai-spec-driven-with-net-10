using System.Net;
using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Domain.Customers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica que un error inesperado (500) nunca expone stack traces, excepciones de EF Core/
/// Npgsql ni nombres de tabla (Principio VI de la constitución; plan.md "Error Handling").
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ErrorHandlingTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public ErrorHandlingTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Reemplaza ICurrentCustomerProvider por uno que lanza, forzando el camino de error 500.
        _factory = new BankingApiFactory(_fixture.ConnectionString, services =>
        {
            services.RemoveAll<ICurrentCustomerProvider>();
            services.AddScoped<ICurrentCustomerProvider, ThrowingCurrentCustomerProvider>();
        });
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ErrorInesperado_Devuelve500SinDetallesInternos()
    {
        var response = await _client.GetAsync("/api/v1/accounts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Npgsql", body, StringComparison.Ordinal);
        Assert.DoesNotContain("at BancaDigitalPeru", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Error inesperado", body, StringComparison.Ordinal);
    }

    private sealed class ThrowingCurrentCustomerProvider : ICurrentCustomerProvider
    {
        public Task<CustomerId> GetCurrentCustomerIdAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Fallo simulado en Npgsql.Connection para la prueba de manejo de errores.");
    }
}
