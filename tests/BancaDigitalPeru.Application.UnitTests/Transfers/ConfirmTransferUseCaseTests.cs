using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Application.Transfers.ConfirmTransfer;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Transfers;

public class ConfirmTransferUseCaseTests
{
    private static readonly CustomerId ClienteA = new(Guid.Parse("a1111111-1111-1111-1111-111111111111"));
    private static readonly CustomerId ClienteB = new(Guid.Parse("b2222222-2222-2222-2222-222222222222"));
    private static readonly FakePreviewTokenSigner Signer = new();

    private static Account CrearCuenta(CustomerId customerId, decimal saldo, AccountStatus status = AccountStatus.Active) => new(
        new AccountId(Guid.NewGuid()),
        customerId,
        AccountType.Savings,
        new AccountNumber("00123456780001"),
        Money.Create(saldo, CurrencyCode.PEN),
        status);

    private static string CrearReferencia(AccountId origen, AccountId destino, decimal importe) =>
        Signer.Protect(new TransferPreviewPayload(origen.Value, destino.Value, importe, CurrencyCode.PEN, DateTimeOffset.UtcNow));

    private static ConfirmTransferUseCase CrearUseCase(
        FakeAccountRepository accountRepository,
        FakeTransferRepository? transferRepository = null,
        FakeUnitOfWork? unitOfWork = null,
        FakeCustomerRepository? customerRepository = null) => new(
        new FakeCurrentCustomerProvider(ClienteA),
        accountRepository,
        transferRepository ?? new FakeTransferRepository(),
        customerRepository ?? new FakeCustomerRepository([]),
        Signer,
        unitOfWork ?? new FakeUnitOfWork());

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_DebitaOrigenYAcreditaDestinoExactamente()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var accountRepository = new FakeAccountRepository([origen, destino]);
        var transferRepository = new FakeTransferRepository();
        var useCase = CrearUseCase(accountRepository, transferRepository);

        var referencia = CrearReferencia(origen.Id, destino.Id, 300.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.False(outcome.IsReplay);
        Assert.Equal(2200.00m, origen.Balance.Amount);
        Assert.Equal(1100.00m, destino.Balance.Amount);
        Assert.Single(transferRepository.AddedTransfers);
    }

    [Fact]
    public async Task ExecuteAsync_ConReferenciaNoDecodificable_RechazaComoInvalidPreviewReference()
    {
        var useCase = CrearUseCase(new FakeAccountRepository([]));

        var outcome = await useCase.ExecuteAsync("token-no-valido", new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.InvalidPreviewReference, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenAjena_RechazaComoAccountNotEligibleYNoModificaSaldos()
    {
        var origenAjena = CrearCuenta(ClienteB, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(new FakeAccountRepository([origenAjena, destino]));

        var referencia = CrearReferencia(origenAjena.Id, destino.Id, 100.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountNotEligible, outcome.RejectionReason);
        Assert.Equal(2500.00m, origenAjena.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_ConMismaCuentaComoOrigenYDestino_RechazaComoSameAccount()
    {
        var cuenta = CrearCuenta(ClienteA, 2500.00m);
        var useCase = CrearUseCase(new FakeAccountRepository([cuenta]));

        var referencia = CrearReferencia(cuenta.Id, cuenta.Id, 100.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.SameAccount, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaOrigenBloqueada_RechazaYNoModificaSaldos()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m, AccountStatus.Blocked);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(new FakeAccountRepository([origen, destino]));

        var referencia = CrearReferencia(origen.Id, destino.Id, 100.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountBlocked, outcome.RejectionReason);
        Assert.Equal(2500.00m, origen.Balance.Amount);
        Assert.Equal(800.00m, destino.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_ConCuentaDestinoBloqueada_RechazaYNoModificaSaldos()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m, AccountStatus.Blocked);
        var useCase = CrearUseCase(new FakeAccountRepository([origen, destino]));

        var referencia = CrearReferencia(origen.Id, destino.Id, 100.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountBlocked, outcome.RejectionReason);
        Assert.Equal(2500.00m, origen.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_ConSaldoInsuficiente_RechazaYNoModificaSaldos()
    {
        var origen = CrearCuenta(ClienteA, 100.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(new FakeAccountRepository([origen, destino]));

        var referencia = CrearReferencia(origen.Id, destino.Id, 150.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.InsufficientFunds, outcome.RejectionReason);
        Assert.Equal(100.00m, origen.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_ConLaMismaClaveYLosMismosDatos_DevuelveElResultadoOriginalSinRedebitar()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var accountRepository = new FakeAccountRepository([origen, destino]);
        var transferRepository = new FakeTransferRepository();
        var useCase = CrearUseCase(accountRepository, transferRepository);
        var idempotencyKey = new IdempotencyKey("key-001");

        var referencia = CrearReferencia(origen.Id, destino.Id, 300.00m);
        var primeraConfirmacion = await useCase.ExecuteAsync(referencia, idempotencyKey, CancellationToken.None);
        var segundaConfirmacion = await useCase.ExecuteAsync(referencia, idempotencyKey, CancellationToken.None);

        Assert.True(primeraConfirmacion.IsSuccess);
        Assert.False(primeraConfirmacion.IsReplay);
        Assert.True(segundaConfirmacion.IsSuccess);
        Assert.True(segundaConfirmacion.IsReplay);
        Assert.Equal(primeraConfirmacion.Value!.TransferId, segundaConfirmacion.Value!.TransferId);
        Assert.Equal(2200.00m, origen.Balance.Amount);
        Assert.Equal(1100.00m, destino.Balance.Amount);
        Assert.Single(transferRepository.AddedTransfers);
    }

    [Fact]
    public async Task ExecuteAsync_ConLaMismaClaveYDatosDistintos_RechazaComoIdempotencyConflict()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var accountRepository = new FakeAccountRepository([origen, destino]);
        var useCase = CrearUseCase(accountRepository);
        var idempotencyKey = new IdempotencyKey("key-001");

        var primeraReferencia = CrearReferencia(origen.Id, destino.Id, 300.00m);
        await useCase.ExecuteAsync(primeraReferencia, idempotencyKey, CancellationToken.None);

        var segundaReferencia = CrearReferencia(origen.Id, destino.Id, 50.00m);
        var outcome = await useCase.ExecuteAsync(segundaReferencia, idempotencyKey, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.IdempotencyConflict, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_AnteConflictoDeConcurrencia_RechazaComoConcurrencyConflict()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var accountRepository = new FakeAccountRepository([origen, destino]);
        var unitOfWork = new FakeUnitOfWork(_ => throw new ConcurrencyConflictException("conflicto simulado", new InvalidOperationException()));
        var useCase = CrearUseCase(accountRepository, unitOfWork: unitOfWork);

        var referencia = CrearReferencia(origen.Id, destino.Id, 300.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.ConcurrencyConflict, outcome.RejectionReason);
        Assert.True(unitOfWork.WasCalled);
    }

    [Fact]
    public async Task ExecuteAsync_AnteViolacionDeUnicidadDeIdempotencyKey_DevuelveElResultadoGanador()
    {
        // research.md §5, punto 2: otra solicitud con la misma Idempotency-Key ganó la carrera.
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var accountRepository = new FakeAccountRepository([origen, destino]);
        var idempotencyKey = new IdempotencyKey("key-001");

        var ganador = Transfer.Create(
            new TransferId(Guid.NewGuid()), ClienteA, origen.Id, destino.Id,
            Money.Create(300.00m, CurrencyCode.PEN), idempotencyKey, DateTimeOffset.UtcNow);
        var transferRepository = new FakeTransferRepository();
        var unitOfWork = new FakeUnitOfWork(_ =>
        {
            // El caso de uso ya llamó a Add() con su propio intento antes de SaveChangesAsync; en
            // una base de datos real ese INSERT nunca se habría confirmado (todo el batch falla
            // junto ante la violación de unicidad). Se descarta y se simula que solo el registro
            // ganador de la solicitud concurrente quedó persistido.
            var intentoPropio = transferRepository.AddedTransfers.Single();
            transferRepository.DiscardAsIfNeverSaved(intentoPropio);
            transferRepository.Add(ganador);
            throw new UniqueConstraintViolationException("ux_transfers_idempotency_key", "conflicto simulado", new InvalidOperationException());
        });
        var useCase = CrearUseCase(accountRepository, transferRepository, unitOfWork);

        var referencia = CrearReferencia(origen.Id, destino.Id, 300.00m);
        var outcome = await useCase.ExecuteAsync(referencia, idempotencyKey, CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.True(outcome.IsReplay);
        Assert.Equal(ganador.Id.Value, outcome.Value!.TransferId);
    }

    // --- Rama third-party (spec 003): la clasificación own-vs-third-party se determina
    // dinámicamente comparando el propietario de la cuenta destino, no por la forma en que se
    // construyó la referencia (research.md de 003 §5). ---

    [Fact]
    public async Task ExecuteAsync_HaciaUnTercero_DebitaOrigenAcreditaDestinoYPueblaNombreDelDestinatario()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteB, 700.00m);
        var accountRepository = new FakeAccountRepository([origen, destino]);
        var customerRepository = new FakeCustomerRepository([new Customer(ClienteB, "Juan Pérez García")]);
        var useCase = CrearUseCase(accountRepository, customerRepository: customerRepository);

        var referencia = CrearReferencia(origen.Id, destino.Id, 300.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(2200.00m, origen.Balance.Amount);
        Assert.Equal(1000.00m, destino.Balance.Amount);
        Assert.Equal("Juan P***", outcome.Value!.DestinationCustomerDisplayNameMasked);
    }

    [Fact]
    public async Task ExecuteAsync_EntreCuentasPropias_NoPueblaNombreDelDestinatario()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteA, 800.00m);
        var useCase = CrearUseCase(new FakeAccountRepository([origen, destino]));

        var referencia = CrearReferencia(origen.Id, destino.Id, 300.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.Null(outcome.Value!.DestinationCustomerDisplayNameMasked);
    }

    [Fact]
    public async Task ExecuteAsync_ConReferenciaAUnaCuentaDestinoQueYaNoExiste_RechazaComoAccountNotEligible()
    {
        // Estado inalcanzable en operación normal: ambos flujos de vista previa (propia y a
        // terceros) ya validaron que la cuenta destino existe antes de firmar la referencia, y
        // ninguna cuenta se elimina jamás en este sistema — por lo que una referencia válidamente
        // firmada siempre debería apuntar a una cuenta destino que sigue existiendo al confirmar.
        // Si igualmente ocurriera (p. ej. un error de programación en otra parte), no hay forma de
        // determinar si la operación pretendía ser entre cuentas propias o a un tercero (esa
        // clasificación depende de conocer al propietario de una cuenta que no existe), por lo que
        // ConfirmTransferUseCase recae de forma segura en la rama de cuentas propias — el
        // resultado más conservador en privacidad (research.md de 003 §5), no
        // DestinationAccountNotFound.
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var useCase = CrearUseCase(new FakeAccountRepository([origen]));

        var referencia = CrearReferencia(origen.Id, new AccountId(Guid.NewGuid()), 100.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountNotEligible, outcome.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_HaciaCuentaDestinoBloqueadaDeUnTercero_RechazaComoAccountBlockedYNoModificaSaldos()
    {
        var origen = CrearCuenta(ClienteA, 2500.00m);
        var destino = CrearCuenta(ClienteB, 700.00m, AccountStatus.Blocked);
        var useCase = CrearUseCase(new FakeAccountRepository([origen, destino]));

        var referencia = CrearReferencia(origen.Id, destino.Id, 100.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.AccountBlocked, outcome.RejectionReason);
        Assert.Equal(2500.00m, origen.Balance.Amount);
        Assert.Equal(700.00m, destino.Balance.Amount);
    }

    [Fact]
    public async Task ExecuteAsync_HaciaUnTercero_ConSaldoInsuficiente_RechazaComoInsufficientFundsYNoModificaSaldos()
    {
        var origen = CrearCuenta(ClienteA, 100.00m);
        var destino = CrearCuenta(ClienteB, 700.00m);
        var useCase = CrearUseCase(new FakeAccountRepository([origen, destino]));

        var referencia = CrearReferencia(origen.Id, destino.Id, 150.00m);
        var outcome = await useCase.ExecuteAsync(referencia, new IdempotencyKey("key-001"), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(TransferRejectionReason.InsufficientFunds, outcome.RejectionReason);
        Assert.Equal(100.00m, origen.Balance.Amount);
    }
}
