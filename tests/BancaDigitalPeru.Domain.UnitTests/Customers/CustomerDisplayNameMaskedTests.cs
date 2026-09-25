using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.Customers;

public class CustomerDisplayNameMaskedTests
{
    [Fact]
    public void DisplayNameMasked_ConNombreYApellido_DevuelvePrimerNombreMasInicialDelApellido()
    {
        var customer = new Customer(new CustomerId(Guid.NewGuid()), "Juan Pérez García");

        Assert.Equal("Juan P***", customer.DisplayNameMasked);
    }

    [Fact]
    public void DisplayNameMasked_ConUnSoloToken_DevuelveEseTokenSeguidoDeAsteriscos()
    {
        var customer = new Customer(new CustomerId(Guid.NewGuid()), "Cliente");

        Assert.Equal("Cliente***", customer.DisplayNameMasked);
    }
}
