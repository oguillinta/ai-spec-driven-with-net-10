using BancaDigitalPeru.Application.Accounts.GetCustomerAccountDetail;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Accounts;

public class GetCustomerAccountDetailUseCaseTests
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
    public async Task ExecuteAsync_ConCuentaPropia_DevuelveFound()
    {
        var cuenta = CrearCuenta(ClienteA);
        var useCase = new GetCustomerAccountDetailUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([cuenta]));

        var result = await useCase.ExecuteAsync(cuenta.Id, CancellationToken.None);

        Assert.True(result.IsFound);
        Assert.Equal(cuenta.Id.Value, result.Value!.AccountId);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaInexistente_DevuelveNotFound()
    {
        var useCase = new GetCustomerAccountDetailUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([]));

        var result = await useCase.ExecuteAsync(new AccountId(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDeOtroCliente_DevuelveNotFound()
    {
        var cuentaDeClienteB = CrearCuenta(ClienteB);
        var useCase = new GetCustomerAccountDetailUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([cuentaDeClienteB]));

        var result = await useCase.ExecuteAsync(cuentaDeClienteB.Id, CancellationToken.None);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ExecuteAsync_CuentaInexistenteYCuentaAjena_DevuelvenElMismoResultado()
    {
        // US5 / FR-022: "no existe" y "es de otro cliente" deben ser indistinguibles.
        var cuentaDeClienteB = CrearCuenta(ClienteB);
        var repository = new FakeAccountRepository([cuentaDeClienteB]);
        var useCase = new GetCustomerAccountDetailUseCase(new FakeCurrentCustomerProvider(ClienteA), repository);

        var resultadoInexistente = await useCase.ExecuteAsync(new AccountId(Guid.NewGuid()), CancellationToken.None);
        var resultadoAjena = await useCase.ExecuteAsync(cuentaDeClienteB.Id, CancellationToken.None);

        Assert.Equal(resultadoInexistente.IsFound, resultadoAjena.IsFound);
        Assert.Equal(resultadoInexistente.Value, resultadoAjena.Value);
    }
}
