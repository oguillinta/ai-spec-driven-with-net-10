using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.Accounts;

public class AccountDebitCreditTests
{
    private static Account CrearCuenta(decimal saldo, AccountStatus status = AccountStatus.Active) => new(
        new AccountId(Guid.NewGuid()),
        new CustomerId(Guid.NewGuid()),
        AccountType.Savings,
        new AccountNumber("00123456780001"),
        Money.Create(saldo, CurrencyCode.PEN),
        status);

    [Fact]
    public void Debit_ConImporteValido_DisminuyeElSaldoExactamente()
    {
        var cuenta = CrearCuenta(2500.00m);

        cuenta.Debit(Money.Create(300.00m, CurrencyCode.PEN));

        Assert.Equal(2200.00m, cuenta.Balance.Amount);
    }

    [Fact]
    public void Credit_ConImporteValido_AumentaElSaldoExactamente()
    {
        var cuenta = CrearCuenta(800.00m);

        cuenta.Credit(Money.Create(300.00m, CurrencyCode.PEN));

        Assert.Equal(1100.00m, cuenta.Balance.Amount);
    }

    [Fact]
    public void Debit_ConSaldoInsuficiente_LanzaExcepcionYNoModificaElSaldo()
    {
        var cuenta = CrearCuenta(100.00m);

        Assert.Throws<InvalidOperationException>(() => cuenta.Debit(Money.Create(150.00m, CurrencyCode.PEN)));
        Assert.Equal(100.00m, cuenta.Balance.Amount);
    }

    [Fact]
    public void Debit_ConImporteCero_LanzaExcepcion()
    {
        var cuenta = CrearCuenta(500.00m);

        Assert.Throws<ArgumentOutOfRangeException>(() => cuenta.Debit(Money.Create(0.00m, CurrencyCode.PEN)));
    }

    [Fact]
    public void Credit_ConImporteCero_LanzaExcepcion()
    {
        var cuenta = CrearCuenta(500.00m);

        Assert.Throws<ArgumentOutOfRangeException>(() => cuenta.Credit(Money.Create(0.00m, CurrencyCode.PEN)));
    }


    [Fact]
    public void Debit_ConCuentaBloqueada_LanzaExcepcionYNoModificaElSaldo()
    {
        var cuenta = CrearCuenta(500.00m, AccountStatus.Blocked);

        Assert.Throws<InvalidOperationException>(() => cuenta.Debit(Money.Create(100.00m, CurrencyCode.PEN)));
        Assert.Equal(500.00m, cuenta.Balance.Amount);
    }

    [Fact]
    public void Credit_ConCuentaBloqueada_LanzaExcepcionYNoModificaElSaldo()
    {
        var cuenta = CrearCuenta(500.00m, AccountStatus.Blocked);

        Assert.Throws<InvalidOperationException>(() => cuenta.Credit(Money.Create(100.00m, CurrencyCode.PEN)));
        Assert.Equal(500.00m, cuenta.Balance.Amount);
    }

    [Fact]
    public void Debit_TransferenciaPorElTotalDelSaldo_DejaElSaldoEnCero()
    {
        var cuenta = CrearCuenta(500.00m);

        cuenta.Debit(Money.Create(500.00m, CurrencyCode.PEN));

        Assert.Equal(0.00m, cuenta.Balance.Amount);
    }

    [Fact]
    public void Balance_SoloCambiaATravesDeDebitOCredit()
    {
        // Regresión (Technical Risks, plan.md): Balance no debe exponer un setter público ni
        // ninguna otra vía de mutación fuera de Debit/Credit.
        var balanceProperty = typeof(Account).GetProperty(nameof(Account.Balance))!;

        Assert.False(balanceProperty.SetMethod!.IsPublic);
    }
}
