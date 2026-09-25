using BancaDigitalPeru.Application.Transfers.GetTransfer;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Transfers;

public class GetTransferUseCaseTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));
    private static readonly CustomerId ClienteB = new(Guid.Parse("b2222222-2222-2222-2222-222222222222"));

    private static Account CrearCuenta(CustomerId customerId) => new(
        new AccountId(Guid.NewGuid()),
        customerId,
        AccountType.Savings,
        new AccountNumber("00123456780001"),
        Money.Create(2500.00m, CurrencyCode.PEN),
        AccountStatus.Active);

    [Fact]
    public async Task ExecuteAsync_ConTransferenciaPropia_DevuelveFoundConLosSeisDatosMinimos()
    {
        var origen = CrearCuenta(ClienteA);
        var destino = CrearCuenta(ClienteA);
        var transferencia = Transfer.Create(
            new TransferId(Guid.NewGuid()), ClienteA, origen.Id, destino.Id,
            Money.Create(300.00m, CurrencyCode.PEN), new IdempotencyKey("key-001"), DateTimeOffset.UtcNow);

        var useCase = new GetTransferUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeTransferRepository([transferencia]),
            new FakeAccountRepository([origen, destino]),
            new FakeCustomerRepository([]));

        var result = await useCase.ExecuteAsync(transferencia.Id, CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(transferencia.Id.Value, result.Value!.TransferId);
        Assert.Equal(transferencia.CompletedAtUtc, result.Value.CompletedAtUtc);
        Assert.Equal(origen.Number.Masked, result.Value.SourceAccountMasked);
        Assert.Equal(destino.Number.Masked, result.Value.DestinationAccountMasked);
        Assert.Equal(300.00m, result.Value.Amount);
        Assert.Equal(CurrencyCode.PEN, result.Value.Currency);
    }

    [Fact]
    public async Task ExecuteAsync_ConTransferenciaInexistente_DevuelveNotFound()
    {
        var useCase = new GetTransferUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeTransferRepository(),
            new FakeAccountRepository([]),
            new FakeCustomerRepository([]));

        var result = await useCase.ExecuteAsync(new TransferId(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ExecuteAsync_TransferenciaInexistenteYTransferenciaAjena_DevuelvenElMismoResultado()
    {
        var origen = CrearCuenta(ClienteB);
        var destino = CrearCuenta(ClienteB);
        var transferenciaAjena = Transfer.Create(
            new TransferId(Guid.NewGuid()), ClienteB, origen.Id, destino.Id,
            Money.Create(300.00m, CurrencyCode.PEN), new IdempotencyKey("key-001"), DateTimeOffset.UtcNow);

        var useCase = new GetTransferUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeTransferRepository([transferenciaAjena]),
            new FakeAccountRepository([origen, destino]),
            new FakeCustomerRepository([]));

        var resultadoInexistente = await useCase.ExecuteAsync(new TransferId(Guid.NewGuid()), CancellationToken.None);
        var resultadoAjena = await useCase.ExecuteAsync(transferenciaAjena.Id, CancellationToken.None);

        Assert.Equal(resultadoInexistente.IsFound, resultadoAjena.IsFound);
    }
}
