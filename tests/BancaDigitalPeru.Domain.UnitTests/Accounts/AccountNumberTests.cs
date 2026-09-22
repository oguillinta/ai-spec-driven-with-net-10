using BancaDigitalPeru.Domain.Accounts;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.Accounts;

public class AccountNumberTests
{
    [Fact]
    public void Masked_MuestraSoloLosUltimosCuatroDigitos()
    {
        var accountNumber = new AccountNumber("00123456781234");

        Assert.Equal("****1234", accountNumber.Masked);
    }

    [Fact]
    public void Masked_NuncaContieneElNumeroCompleto()
    {
        var accountNumber = new AccountNumber("00123456781234");

        Assert.DoesNotContain("0012345678", accountNumber.Masked, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12a4")]
    [InlineData("12")]
    public void Constructor_ConNumeroInvalido_LanzaExcepcion(string invalidNumber)
    {
        Assert.Throws<ArgumentException>(() => new AccountNumber(invalidNumber));
    }
}
