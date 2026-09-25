using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Transfers;

public class CommonTransferValidationTests
{
    private static Account CrearCuenta(decimal saldo, AccountStatus status = AccountStatus.Active) => new(
        new AccountId(Guid.NewGuid()),
        new CustomerId(Guid.NewGuid()),
        AccountType.Savings,
        new AccountNumber("00123456780001"),
        Money.Create(saldo, CurrencyCode.PEN),
        status);

    [Fact]
    public void ValidateSource_ConCuentaNula_DevuelveAccountNotEligible()
    {
        var resultado = CommonTransferValidation.ValidateSource(null, 100.00m);

        Assert.Equal(TransferRejectionReason.AccountNotEligible, resultado);
    }

    [Fact]
    public void ValidateSource_ConCuentaBloqueada_DevuelveAccountBlocked()
    {
        var cuenta = CrearCuenta(500.00m, AccountStatus.Blocked);

        var resultado = CommonTransferValidation.ValidateSource(cuenta, 100.00m);

        Assert.Equal(TransferRejectionReason.AccountBlocked, resultado);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(-10.00)]
    public void ValidateSource_ConImporteCeroONegativo_DevuelveInvalidAmount(decimal importe)
    {
        var cuenta = CrearCuenta(500.00m);

        var resultado = CommonTransferValidation.ValidateSource(cuenta, importe);

        Assert.Equal(TransferRejectionReason.InvalidAmount, resultado);
    }

    [Fact]
    public void ValidateSource_ConSaldoInsuficiente_DevuelveInsufficientFunds()
    {
        var cuenta = CrearCuenta(100.00m);

        var resultado = CommonTransferValidation.ValidateSource(cuenta, 150.00m);

        Assert.Equal(TransferRejectionReason.InsufficientFunds, resultado);
    }

    [Fact]
    public void ValidateSource_ConDatosValidos_DevuelveNull()
    {
        var cuenta = CrearCuenta(500.00m);

        var resultado = CommonTransferValidation.ValidateSource(cuenta, 100.00m);

        Assert.Null(resultado);
    }
}
