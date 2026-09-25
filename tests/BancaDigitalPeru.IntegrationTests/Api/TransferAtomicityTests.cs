using System.Net;
using System.Net.Http.Json;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;
using BancaDigitalPeru.Infrastructure.Persistence;
using BancaDigitalPeru.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BancaDigitalPeru.IntegrationTests.Api;

/// <summary>
/// Escenario de fallo deliberado (sección 11 del input de planificación, RB6): si la transferencia
/// no puede completarse íntegramente, ningún efecto financiero queda observable. Se fuerza el
/// fallo en <see cref="ITransferRepository.Add"/>, antes de que <c>SaveChangesAsync</c> pueda
/// confirmar ningún cambio — el débito y el crédito ya aplicados a las entidades rastreadas nunca
/// llegan a persistirse porque la excepción interrumpe el caso de uso antes de esa única llamada.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TransferAtomicityTests : IAsyncLifetime
{
    private const string CuentaA = "aaaaaaaa-1111-1111-1111-111111111111";
    private const string CuentaB = "aaaaaaaa-2222-2222-2222-222222222222";

    private readonly PostgresContainerFixture _fixture;
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    public TransferAtomicityTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _factory = new BankingApiFactory(_fixture.ConnectionString, services =>
        {
            services.RemoveAll<ITransferRepository>();
            services.AddScoped<ITransferRepository>(sp =>
                new PoisonedTransferRepository(new TransferRepository(sp.GetRequiredService<BancaDigitalPeruDbContext>())));
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
    public async Task Confirmar_AnteFalloTecnicoAntesDeGuardar_NoDejaNingunEfectoParcial()
    {
        var preview = await _client.PostAsJsonAsync("/api/v1/transfer-previews", new
        {
            sourceAccountId = CuentaA,
            destinationAccountId = CuentaB,
            amount = 300.00m
        });
        preview.EnsureSuccessStatusCode();
        var previewBody = await preview.Content.ReadFromJsonAsync<JsonPreviewReference>();

        var saldosAntes = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transfers")
        {
            Content = JsonContent.Create(new { previewReference = previewBody!.PreviewReference })
        };
        request.Headers.Add("Idempotency-Key", $"key-{Guid.NewGuid()}");
        var confirmacion = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, confirmacion.StatusCode);

        var saldosDespues = await (await _client.GetAsync("/api/v1/accounts")).Content.ReadAsStringAsync();
        Assert.Equal(saldosAntes, saldosDespues);
    }

    private sealed record JsonPreviewReference(string PreviewReference);

    private sealed class PoisonedTransferRepository : ITransferRepository
    {
        private readonly ITransferRepository _inner;

        public PoisonedTransferRepository(ITransferRepository inner)
        {
            _inner = inner;
        }

        public void Add(Transfer transfer) =>
            throw new InvalidOperationException("Fallo técnico simulado antes de SaveChangesAsync (TransferAtomicityTests).");

        public Task<Transfer?> GetByIdForCustomerAsync(CustomerId customerId, TransferId transferId, CancellationToken cancellationToken) =>
            _inner.GetByIdForCustomerAsync(customerId, transferId, cancellationToken);

        public Task<Transfer?> GetByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken) =>
            _inner.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
    }
}
