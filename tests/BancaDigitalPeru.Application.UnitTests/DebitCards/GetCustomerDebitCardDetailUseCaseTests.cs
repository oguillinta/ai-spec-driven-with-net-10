using BancaDigitalPeru.Application.DebitCards.GetCustomerDebitCardDetail;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.DebitCards;

public class GetCustomerDebitCardDetailUseCaseTests
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

    private static DebitCard CrearTarjeta(CustomerId customerId, Account cuenta) =>
        DebitCard.Create(
            new DebitCardId(Guid.NewGuid()),
            customerId,
            cuenta,
            new CardNumber("4111111111114582"),
            new CardExpiration(11, 2027),
            CardStatus.Active);

    [Fact]
    public async Task ExecuteAsync_ConTarjetaPropia_DevuelveFoundConCuentaDelMismoCliente()
    {
        var cuenta = CrearCuenta(ClienteA);
        var tarjeta = CrearTarjeta(ClienteA, cuenta);
        var useCase = new GetCustomerDebitCardDetailUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([tarjeta]));

        var result = await useCase.ExecuteAsync(tarjeta.Id, CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(cuenta.Id.Value, result.Value!.AccountId);
    }

    [Fact]
    public async Task ExecuteAsync_ConTarjetaInexistente_DevuelveNotFound()
    {
        var useCase = new GetCustomerDebitCardDetailUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([]));

        var result = await useCase.ExecuteAsync(new DebitCardId(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ExecuteAsync_ConTarjetaDeOtroCliente_DevuelveNotFound()
    {
        var cuentaB = CrearCuenta(ClienteB);
        var tarjetaDeClienteB = CrearTarjeta(ClienteB, cuentaB);
        var useCase = new GetCustomerDebitCardDetailUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeDebitCardRepository([tarjetaDeClienteB]));

        var result = await useCase.ExecuteAsync(tarjetaDeClienteB.Id, CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ExecuteAsync_TarjetaInexistenteYTarjetaAjena_DevuelvenElMismoResultado()
    {
        // US5 / FR-022: "no existe" y "es de otro cliente" deben ser indistinguibles.
        var cuentaB = CrearCuenta(ClienteB);
        var tarjetaDeClienteB = CrearTarjeta(ClienteB, cuentaB);
        var repository = new FakeDebitCardRepository([tarjetaDeClienteB]);
        var useCase = new GetCustomerDebitCardDetailUseCase(new FakeCurrentCustomerProvider(ClienteA), repository);

        var resultadoInexistente = await useCase.ExecuteAsync(new DebitCardId(Guid.NewGuid()), CancellationToken.None);
        var resultadoAjena = await useCase.ExecuteAsync(tarjetaDeClienteB.Id, CancellationToken.None);

        Assert.Equal(resultadoInexistente.IsFound, resultadoAjena.IsFound);
        Assert.Equal(resultadoInexistente.Value, resultadoAjena.Value);
    }
}
