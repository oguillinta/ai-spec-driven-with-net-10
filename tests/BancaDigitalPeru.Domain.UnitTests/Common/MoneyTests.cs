using BancaDigitalPeru.Domain.Common;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.Common;

public class MoneyTests
{
    [Theory]
    [InlineData(2500.00)]
    [InlineData(0.00)]
    [InlineData(250.50)]
    public void Create_ConAImporteConDosDecimales_CreaElValueObject(decimal amount)
    {
        var money = Money.Create(amount, CurrencyCode.PEN);

        Assert.Equal(amount, money.Amount);
        Assert.Equal(CurrencyCode.PEN, money.Currency);
    }

    [Fact]
    public void Create_ConImporteNegativo_LanzaExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(-1m, CurrencyCode.PEN));
    }

    [Fact]
    public void Create_ConMasDeDosDecimales_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(10.999m, CurrencyCode.PEN));
    }

    [Fact]
    public void Equals_ConMismoImporteYMoneda_SonIguales()
    {
        var a = Money.Create(100.00m, CurrencyCode.PEN);
        var b = Money.Create(100.00m, CurrencyCode.PEN);

        Assert.Equal(a, b);
    }
}
