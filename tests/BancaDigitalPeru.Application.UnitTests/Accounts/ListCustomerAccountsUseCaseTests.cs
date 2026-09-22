using BancaDigitalPeru.Application.Accounts.ListCustomerAccounts;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Accounts;

public class ListCustomerAccountsUseCaseTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));
    private static readonly CustomerId ClienteB = new(Guid.Parse("b2222222-2222-2222-2222-222222222222"));

    private static Account CrearCuenta(CustomerId customerId, AccountStatus status, decimal saldo) => new(
        new AccountId(Guid.NewGuid()),
        customerId,
        AccountType.Savings,
        new AccountNumber("00123456781234"),
        Money.Create(saldo, CurrencyCode.PEN),
        status);

    [Fact]
    public async Task ExecuteAsync_ConCuentasPropias_DevuelveExactamenteLasDelClienteActual()
    {
        var cuentaActivaA = CrearCuenta(ClienteA, AccountStatus.Active, 2500.00m);
        var cuentaBloqueadaA = CrearCuenta(ClienteA, AccountStatus.Blocked, 800.00m);
        var cuentaDeOtroCliente = CrearCuenta(ClienteB, AccountStatus.Active, 1000.00m);

        var useCase = new ListCustomerAccountsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([cuentaActivaA, cuentaBloqueadaA, cuentaDeOtroCliente]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.DoesNotContain(a.AccountId, new[] { cuentaDeOtroCliente.Id.Value }));
    }

    [Fact]
    public async Task ExecuteAsync_ConClienteSinCuentas_DevuelveColeccionVacia()
    {
        var useCase = new ListCustomerAccountsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaBloqueada_LaIncluyeConEstadoBloqueada()
    {
        var cuentaBloqueada = CrearCuenta(ClienteA, AccountStatus.Blocked, 800.00m);

        var useCase = new ListCustomerAccountsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([cuentaBloqueada]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(AccountStatus.Blocked, result[0].Status);
    }

    [Fact]
    public async Task ExecuteAsync_ConSaldoCero_LaCuentaSigueSiendoVisible()
    {
        // CL3: una cuenta con saldo disponible S/ 0.00 continúa siendo visible normalmente.
        var cuentaConSaldoCero = CrearCuenta(ClienteA, AccountStatus.Active, 0.00m);

        var useCase = new ListCustomerAccountsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([cuentaConSaldoCero]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(0.00m, result[0].BalanceAmount);
    }

    [Fact]
    public async Task ExecuteAsync_ConTodasLasCuentasBloqueadas_TodasSiguenApareciendo()
    {
        // CL4: si todas las cuentas están bloqueadas, continúan apareciendo con su estado.
        var cuenta1 = CrearCuenta(ClienteA, AccountStatus.Blocked, 800.00m);
        var cuenta2 = CrearCuenta(ClienteA, AccountStatus.Blocked, 250.50m);

        var useCase = new ListCustomerAccountsUseCase(
            new FakeCurrentCustomerProvider(ClienteA),
            new FakeAccountRepository([cuenta1, cuenta2]));

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal(AccountStatus.Blocked, a.Status));
    }
}
