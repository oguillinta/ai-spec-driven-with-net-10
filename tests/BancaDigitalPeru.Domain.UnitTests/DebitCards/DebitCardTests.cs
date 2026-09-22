using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.DebitCards;

public class DebitCardTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));
    private static readonly CustomerId ClienteB = new(Guid.Parse("b2222222-2222-2222-2222-222222222222"));

    private static Account CrearCuenta(CustomerId customerId) => new(
        new AccountId(Guid.NewGuid()),
        customerId,
        AccountType.Savings,
        new AccountNumber("00123456781234"),
        Money.Create(2500.00m, CurrencyCode.PEN),
        AccountStatus.Active);

    [Fact]
    public void Create_ConCuentaDelMismoCliente_CreaLaTarjeta()
    {
        var cuenta = CrearCuenta(ClienteA);

        var tarjeta = DebitCard.Create(
            new DebitCardId(Guid.NewGuid()),
            ClienteA,
            cuenta,
            new CardNumber("4111111111114582"),
            new CardExpiration(11, 2027),
            CardStatus.Active);

        Assert.Equal(ClienteA, tarjeta.CustomerId);
        Assert.Equal(cuenta.Id, tarjeta.AccountId);
    }

    [Fact]
    public void Create_ConCuentaDeOtroCliente_LanzaExcepcion()
    {
        // RB6: la cuenta asociada debe pertenecer al mismo cliente propietario de la tarjeta.
        var cuentaDeClienteB = CrearCuenta(ClienteB);

        Assert.Throws<ArgumentException>(() => DebitCard.Create(
            new DebitCardId(Guid.NewGuid()),
            ClienteA,
            cuentaDeClienteB,
            new CardNumber("4111111111114582"),
            new CardExpiration(11, 2027),
            CardStatus.Active));
    }
}
