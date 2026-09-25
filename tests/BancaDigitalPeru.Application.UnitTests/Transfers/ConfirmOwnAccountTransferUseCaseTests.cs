using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Application.Transfers.ConfirmOwnAccountTransfer;
using BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;
using BancaDigitalPeru.Application.UnitTests.TestDoubles;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;
using Xunit;

namespace BancaDigitalPeru.Application.UnitTests.Transfers;

public class ConfirmOwnAccountTransferUseCaseTests
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

    private static ConfirmOwnAccountTransferUseCase CrearUseCase(
        FakeAccountRepository accountRepository,
        FakeTransferRepository? transferRepository = null,
        FakeUnitOfWork? unitOfWork = null) => new(
        new FakeCurrentCustomerProvider(ClienteA),
        accountRepository,
        transferRepository ?? new FakeTransferRepository(),
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
}
