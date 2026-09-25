using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Application.Transfers.PreviewThirdPartyTransfer;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Transfers;

public class PreviewThirdPartyTransferUseCaseTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));
    private static readonly CustomerId ClienteB = new(Guid.Parse("b2222222-2222-2222-2222-222222222222"));

    private static Account CrearCuenta(CustomerId customerId, decimal saldo, string numero = "00123456780001", AccountStatus status = AccountStatus.Active) => new(
        new AccountId(Guid.NewGuid()),
        customerId,
        AccountType.Savings,
        new AccountNumber(numero),
        Money.Create(saldo, CurrencyCode.PEN),
        status);

    private static PreviewThirdPartyTransferUseCase CrearUseCase(IEnumerable<Account> cuentas, IEnumerable<Customer> clientes) => new(
        new FakeCurrentCustomerProvider(ClienteA),
        new FakeAccountRepository(cuentas),
        new FakeCustomerRepository(clientes),
        new FakePreviewTokenSigner());

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_DevuelveReferenciaConNombreEnmascaradoYSinModificarSaldos()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, "00123456780001");
        var destino = CrearCuenta(ClienteB, 700.00m, "00123456780003");
        var clienteDestino = new Customer(ClienteB, "Juan Pérez García");
        var useCase = CrearUseCase([origen, destino], [clienteDestino]);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Number, 300.00m, CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Value!.PreviewReference);
        Assert.Equal(300.00m, outcome.Value.Amount);
        Assert.Equal("Juan P***", outcome.Value.DestinationCustomerDisplayNameMasked);
        Assert.Equal("****0003", outcome.Value.DestinationAccountMasked);
        Assert.Equal(2500.00m, origen.Balance.Amount);
        Assert.Equal(700.00m, destino.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenInexistente_RechazaComoAccountNotEligible()
    {
        var destino = CrearCuenta(ClienteB, 700.00m, "00123456780003");
        var useCase = CrearUseCase([destino], [new Customer(ClienteB, "Juan Pérez García")]);

        var outcome = await useCase.ExecuteAsync(new AccountId(Guid.NewGuid()), destino.Number, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountNotEligible, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenAjena_RechazaComoAccountNotEligible()
    {
        var origenAjena = CrearCuenta(ClienteB, 2500.00m, "00123456780001");
        var destino = CrearCuenta(ClienteB, 700.00m, "00123456780003");
        var useCase = CrearUseCase([origenAjena, destino], [new Customer(ClienteB, "Juan Pérez García")]);

        var outcome = await useCase.ExecuteAsync(origenAjena.Id, destino.Number, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountNotEligible, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenBloqueada_RechazaComoAccountBlockedYNoModificaSaldos()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, "00123456780001", AccountStatus.Blocked);
        var destino = CrearCuenta(ClienteB, 700.00m, "00123456780003");
        var useCase = CrearUseCase([origen, destino], [new Customer(ClienteB, "Juan Pérez García")]);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Number, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountBlocked, outcome.RejectionReason);
        Assert.Equal(2500.00m, origen.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDestinoInexistente_RechazaComoDestinationAccountNotFound()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, "00123456780001");
        var useCase = CrearUseCase([origen], []);

        var outcome = await useCase.ExecuteAsync(origen.Id, new AccountNumber("00123456780099"), 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.DestinationAccountNotFound, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDestinoQueResultaSerPropia_RechazaComoDestinationIsOwnAccount()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, "00123456780001");
        var otraCuentaPropia = CrearCuenta(ClienteA, 800.00m, "00123456780002");
        var useCase = CrearUseCase([origen, otraCuentaPropia], []);

        var outcome = await useCase.ExecuteAsync(origen.Id, otraCuentaPropia.Number, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.DestinationIsOwnAccount, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDestinoBloqueada_RechazaComoAccountBlocked()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, "00123456780001");
        var destino = CrearCuenta(ClienteB, 700.00m, "00123456780003", AccountStatus.Blocked);
        var useCase = CrearUseCase([origen, destino], [new Customer(ClienteB, "Juan Pérez García")]);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Number, 100.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountBlocked, outcome.RejectionReason);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(-10.00)]
    public async Task ExecuteAsync_ConImporteCeroONegativo_RechazaComoInvalidAmount(decimal importe)
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, "00123456780001");
        var destino = CrearCuenta(ClienteB, 700.00m, "00123456780003");
        var useCase = CrearUseCase([origen, destino], [new Customer(ClienteB, "Juan Pérez García")]);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Number, importe, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.InvalidAmount, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConSaldoInsuficiente_RechazaComoInsufficientFunds()
    {
        var origen = CrearCuenta(ClienteA, 100.00m, "00123456780001");
        var destino = CrearCuenta(ClienteB, 700.00m, "00123456780003");
        var useCase = CrearUseCase([origen, destino], [new Customer(ClienteB, "Juan Pérez García")]);

        var outcome = await useCase.ExecuteAsync(origen.Id, destino.Number, 150.00m, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.InsufficientFunds, outcome.RejectionReason);
    }
}
