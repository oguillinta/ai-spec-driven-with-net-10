using System.Net;
using System.Net.Http.Json;
using BancaDigitalPeru.Api.Contracts.Transfers;
using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Domain.Customers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Verifica que un error inesperado (500) durante una vista previa o confirmación de
/// transferencia a terceros nunca expone stack traces, excepciones de EF Core/Npgsql ni SQL
/// (Principio VI; mismo criterio que `TransferErrorHandlingTests` de 002).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ThirdPartyTransferErrorHandlingTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string NumeroCuentaB1DeTercero = "00123456780003";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factoryNormal = null!;
    private HttpClient _clienteNormal = null!;
    private BankingApiFactory _factoryConFallo = null!;
    private HttpClient _clienteConFallo = null!;

    public ThirdPartyTransferErrorHandlingTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _factoryNormal = new BankingApiFactory(_fixture.ConnectionString);
        _clienteNormal = _factoryNormal.CreateClient();

        _factoryConFallo = new BankingApiFactory(_fixture.ConnectionString, services =>
        {
            services.RemoveAll<ICurrentCustomerProvider>();
            services.AddScoped<ICurrentCustomerProvider, ThrowingCurrentCustomerProvider>();
        });
        _clienteConFallo = _factoryConFallo.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _clienteNormal.Dispose();
        await _factoryNormal.DisposeAsync();
        _clienteConFallo.Dispose();
        await _factoryConFallo.DisposeAsync();
    }

    [Fact]
    public async Task ErrorInesperadoDuranteVistaPreviaATerceros_Devuelve500SinDetallesInternos()
    {
        var response = await _clienteConFallo.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = 10.00m
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Npgsql", body, StringComparison.Ordinal);
        Assert.DoesNotContain("at BancaDigitalPeru", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Error inesperado", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorInesperadoDuranteConfirmacionATerceros_Devuelve500SinDetallesInternos()
    {
        var preview = await _clienteNormal.PostAsJsonAsync("/api/v1/third-party-transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountNumber = NumeroCuentaB1DeTercero,
            amount = 10.00m
        });
        preview.EnsureSuccessStatusCode();
        var previewBody = await preview.Content.ReadFromJsonAsync<ThirdPartyTransferPreviewResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers")
        {
            Content = JsonContent.Create(new { previewReference = previewBody!.PreviewReference })
        };
        request.Headers.Add("Idempotency-Key", $"key-{Guid.NewGuid()}");

        var response = await _clienteConFallo.SendAsync(request);
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
            throw new InvalidOperationException("Fallo simulado en Npgsql.Connection para la prueba de manejo de errores de transferencias a terceros.");
    }
}
