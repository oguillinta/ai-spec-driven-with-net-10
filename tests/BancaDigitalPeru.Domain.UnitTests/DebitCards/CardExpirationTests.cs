using BancaDigitalPeru.Domain.DebitCards;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.DebitCards;

public class CardExpirationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void Constructor_ConMesValido_CreaElValueObject(int month)
    {
        var expiration = new CardExpiration(month, 2027);

        Assert.Equal(month, expiration.Month);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Constructor_ConMesFueraDeRango_LanzaExcepcion(int invalidMonth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CardExpiration(invalidMonth, 2027));
    }
}
