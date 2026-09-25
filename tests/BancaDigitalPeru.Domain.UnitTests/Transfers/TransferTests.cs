using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests.Transfers;

public class TransferTests
{
    private static readonly CustomerId ClienteA = new(Guid.NewGuid());
    private static readonly AccountId CuentaOrigen = new(Guid.NewGuid());
    private static readonly AccountId CuentaDestino = new(Guid.NewGuid());

    [Fact]
    public void Create_ConDatosValidos_CreaLaTransferenciaCompletada()
    {
        var transferencia = Transfer.Create(
            new TransferId(Guid.NewGuid()),
            ClienteA,
            CuentaOrigen,
            CuentaDestino,
            Money.Create(300.00m, CurrencyCode.PEN),
            new IdempotencyKey("clave-001"),
            DateTimeOffset.UtcNow);

        Assert.Equal(TransferStatus.Completed, transferencia.Status);
        Assert.Equal(300.00m, transferencia.Amount.Amount);
    }

    [Fact]
    public void Create_ConCuentaOrigenIgualADestino_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() => Transfer.Create(
            new TransferId(Guid.NewGuid()),
            ClienteA,
            CuentaOrigen,
            CuentaOrigen,
            Money.Create(300.00m, CurrencyCode.PEN),
            new IdempotencyKey("clave-001"),
            DateTimeOffset.UtcNow));
    }
}
