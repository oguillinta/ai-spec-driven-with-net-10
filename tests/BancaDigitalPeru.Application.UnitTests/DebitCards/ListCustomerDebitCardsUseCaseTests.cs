using BancaDigitalPeru.Application.DebitCards.ListCustomerDebitCards;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.DebitCards;

public class ListCustomerDebitCardsUseCaseTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));
    private static readonly CustomerId ClienteB = new(Guid.Parse("b2222222-2222-2222-2222-222222222222"));

    private static Account CrearCuenta(CustomerId customerId, AccountStatus status = AccountStatus.Active) => new(
        new AccountId(Guid.NewGuid()),
        customerId,
        AccountType.Savings,
        new AccountNumber("00123456781234"),
        Money.Create(2500.00m, CurrencyCode.PEN),
        status);

    private static DebitCard CrearTarjeta(CustomerId customerId, Account cuenta, CardStatus status) =>
        DebitCard.Create(
            new DebitCardId(Guid.NewGuid()),
            customerId,
            cuenta,
            new CardNumber("4111111111114582"),
            new CardExpiration(11, 2027),
            status);

    [Fact]
    public async Task ExecuteAsync_ConTarjetasPropias_DevuelveExactamenteLasDelClienteActual()
    {
        var cuentaA = CrearCuenta(ClienteA);
        var cuentaB = CrearCuenta(ClienteB);
        var tarjetaActivaA = CrearTarjeta(ClienteA, cuentaA, CardStatus.Active);
        var tarjetaBloqueadaA = CrearTarjeta(ClienteA, cuentaA, CardStatus.Blocked);
        var tarjetaDeOtroCliente = CrearTarjeta(ClienteB, cuentaB, CardStatus.Active);

        var useCase = new ListCustomerDebitCardsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([tarjetaActivaA, tarjetaBloqueadaA, tarjetaDeOtroCliente]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, c => c.DebitCardId == tarjetaDeOtroCliente.Id.Value);
    }

    [Fact]
    public async Task ExecuteAsync_ConClienteSinTarjetas_DevuelveColeccionVacia()
    {
        var useCase = new ListCustomerDebitCardsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_ConTarjetaBloqueada_LaIncluyeConEstadoBloqueada()
    {
        var cuentaA = CrearCuenta(ClienteA);
        var tarjetaBloqueada = CrearTarjeta(ClienteA, cuentaA, CardStatus.Blocked);

        var useCase = new ListCustomerDebitCardsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([tarjetaBloqueada]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(CardStatus.Blocked, result[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_ConTodasLasTarjetasBloqueadas_TodasSiguenApareciendo()
    {
        // CL4: si todas las tarjetas están bloqueadas, continúan apareciendo con su estado.
        var cuentaA = CrearCuenta(ClienteA);
        var tarjeta1 = CrearTarjeta(ClienteA, cuentaA, CardStatus.Blocked);
        var tarjeta2 = CrearTarjeta(ClienteA, cuentaA, CardStatus.Blocked);

        var useCase = new ListCustomerDebitCardsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([tarjeta1, tarjeta2]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal(CardStatus.Blocked, c.Status));
    }

    [Fact]
    public async Task ExecuteAsync_ConTarjetaCuyaCuentaEstaBloqueada_LaTarjetaSigueSiendoVisible()
    {
        // CL7: una tarjeta asociada a una cuenta bloqueada continúa siendo visible; esta spec no
        // define ningún comportamiento operativo adicional derivado de esa condición.
        var cuentaBloqueada = CrearCuenta(ClienteA, AccountStatus.Blocked);
        var tarjetaActiva = CrearTarjeta(ClienteA, cuentaBloqueada, CardStatus.Active);

        var useCase = new ListCustomerDebitCardsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([tarjetaActiva]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(CardStatus.Active, result[0].Status);
        Assert.Equal(cuentaBloqueada.Id.Value, result[0].AccountId);
    }
}
