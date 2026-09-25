using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Transfers;

public class PreviewOwnAccountTransferUseCaseTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));
    private static readonly CustomerId ClienteB = new(Guid.Parse("b2222222-2222-2222-2222-222222222222"));

    private static Account CrearCuenta(CustomerId customerId, decimal saldo, AccountStatus status = AccountStatus.Active) => new(
        new AccountId(Guid.NewGuid()),
        customerId,
        AccountType.Savings,
        new AccountNumber("00123456780001"),
        Money.Create(saldo, CurrencyCode.PEN),
        status);

    private static PreviewOwnAccountTransferUseCase CrearUseCase(params Account[] cuentas) => new(
        new FakeCurrentCustomerProvider(ClienteA),
        new FakeAccountRepository(cuentas),
        new FakePreviewTokenSigner());

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_DevuelveReferenciaSinModificarSaldos()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(origen, destino);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Id, 300.00m, CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Value!.PreviewReference);
        Assert.Equal(300.00m, outcome.Value.Amount);
        Assert.Equal(2500.00m, origen.Balance.Amount);
        Assert.Equal(800.00m, destino.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenInexistente_RechazaComoAccountNotEligible()
    {
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(destino);

        var outcome = await useCase.ExecuteAsync(new AccountId(Guid.NewGuid()), destino.Id, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountNotEligible, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDestinoInexistente_RechazaComoAccountNotEligible()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var useCase = CrearUseCase(origen);

        var outcome = await useCase.ExecuteAsync(origen.Id, new AccountId(Guid.NewGuid()), 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountNotEligible, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenAjena_RechazaComoAccountNotEligible()
    {
        var origenAjena = CrearCuenta(ClienteB, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(origenAjena, destino);

        var outcome = await useCase.ExecuteAsync(origenAjena.Id, destino.Id, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountNotEligible, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDestinoAjena_DevuelveElMismoResultadoQueInexistente()
    {
        // FR-022: "no existe" y "es de otro cliente" deben ser indistinguibles.
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destinoAjena = CrearCuenta(ClienteB, 800.00m);
        var useCase = CrearUseCase(origen, destinoAjena);

        var resultadoAjena = await useCase.ExecuteAsync(origen.Id, destinoAjena.Id, 100.00m, CancellationToken.None);
        var resultadoInexistente = await useCase.ExecuteAsync(origen.Id, new AccountId(Guid.NewGuid()), 100.00m, CancellationToken.None);

        Assert.Equal(resultadoInexistente.RejectionReason, resultadoAjena.RejectionReason);
        Assert.False(resultadoAjena.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_ConLaMismaCuentaComoOrigenYDestino_RechazaComoSameAccount()
    {
        var cuenta = CrearCuenta(ClienteA, 2500.00m);
        var useCase = CrearUseCase(cuenta);

        var outcome = await useCase.ExecuteAsync(cuenta.Id, cuenta.Id, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.SameAccount, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenBloqueada_RechazaComoAccountBlocked()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, AccountStatus.Blocked);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(origen, destino);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Id, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountBlocked, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDestinoBloqueada_RechazaComoAccountBlocked()
    {
        // FR-006, clarificación 2026-09-24: destino BLOQUEADA se rechaza igual que origen.
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m, AccountStatus.Blocked);
        var useCase = CrearUseCase(origen, destino);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Id, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountBlocked, outcome.RejectionReason);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(-10.00)]
    public async Task ExecuteAsync_ConImporteCeroONegativo_RechazaComoInvalidAmount(decimal importe)
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(origen, destino);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Id, importe, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.InvalidAmount, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConSaldoInsuficiente_RechazaComoInsufficientFunds()
    {
        var origen = CrearCuenta(ClienteA, 100.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(origen, destino);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Id, 150.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.InsufficientFunds, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_TransferenciaPorElTotalDelSaldo_EsPermitida()
    {
        var origen = CrearCuenta(ClienteA, 500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(origen, destino);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Id, 500.00m, CancellationToken.None);

        Assert.True(outcome.IsSuccess);
    }
}
