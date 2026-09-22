using BancaDigitalPeru.Domain.DebitCards;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.DebitCards;

public class CardNumberTests
{
    [Fact]
    public void Masked_MuestraSoloLosUltimosCuatroDigitos()
    {
        var cardNumber = new CardNumber("4111111111114582");

        Assert.Equal("**** **** **** 4582", cardNumber.Masked);
        Assert.Equal("4582", cardNumber.Last4Digits);
    }

    [Fact]
    public void Masked_NuncaContieneElNumeroCompleto()
    {
        var cardNumber = new CardNumber("4111111111114582");

        Assert.DoesNotContain("411111111111", cardNumber.Masked, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("41111111111145820")]
    [InlineData("411111111111458a")]
    public void Constructor_ConNumeroInvalido_LanzaExcepcion(string invalidNumber)
    {
        Assert.Throws<ArgumentException>(() => new CardNumber(invalidNumber));
    }
}
