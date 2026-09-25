using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Transfers;

public class ThirdPartyTransferValidationTests
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

    [Fact]
    public void Validate_ConCuentaDestinoNula_DevuelveDestinationAccountNotFound()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);

        var resultado = ThirdPartyTransferValidation.Validate(origen, null, ClienteA, 100.00m);

        Assert.Equal(TransferRejectionReason.DestinationAccountNotFound, resultado);
    }

    [Fact]
    public void Validate_ConCuentaDestinoDelMismoCliente_DevuelveDestinationIsOwnAccount()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destinoPropia = CrearCuenta(ClienteA, 800.00m);

        var resultado = ThirdPartyTransferValidation.Validate(origen, destinoPropia, ClienteA, 100.00m);

        Assert.Equal(TransferRejectionReason.DestinationIsOwnAccount, resultado);
    }

    [Fact]
    public void Validate_ConCuentaDestinoBloqueada_DevuelveAccountBlocked()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteB, 700.00m, AccountStatus.Blocked);

        var resultado = ThirdPartyTransferValidation.Validate(origen, destino, ClienteA, 100.00m);

        Assert.Equal(TransferRejectionReason.AccountBlocked, resultado);
    }

    [Fact]
    public void Validate_ConDatosValidos_DevuelveNull()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteB, 700.00m);

        var resultado = ThirdPartyTransferValidation.Validate(origen, destino, ClienteA, 100.00m);

        Assert.Null(resultado);
    }

    [Fact]
    public void Validate_ConCuentaOrigenBloqueada_DelegaEnCommonTransferValidation()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, AccountStatus.Blocked);
        var destino = CrearCuenta(ClienteB, 700.00m);

        var resultado = ThirdPartyTransferValidation.Validate(origen, destino, ClienteA, 100.00m);

        Assert.Equal(TransferRejectionReason.AccountBlocked, resultado);
    }
}
