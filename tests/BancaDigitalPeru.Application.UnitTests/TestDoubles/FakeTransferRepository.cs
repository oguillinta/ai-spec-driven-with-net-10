using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;

namespace BancaDigitalPeru.Application.UnitTests.TestDoubles;

/// <summary>Doble de prueba en memoria; <see cref="AddedTransfers"/> permite inspeccionar lo añadido sin persistir.</summary>
public sealed class FakeTransferRepository : ITransferRepository
{
    private readonly List<Transfer> _transfers;

    public FakeTransferRepository(IEnumerable<Transfer>? transfers = null)
    {
        _transfers = transfers?.ToList() ?? [];
    }

    public List<Transfer> AddedTransfers { get; } = [];

    public void Add(Transfer transfer)
    {
        _transfers.Add(transfer);
        AddedTransfers.Add(transfer);
    }

    /// <summary>
    /// Solo para pruebas: simula que un <see cref="Add"/> previo nunca llegó a persistirse (p. ej.
    /// porque el SaveChangesAsync que lo incluía falló como parte del mismo batch). EF Core real no
    /// necesita este método: un SaveChangesAsync fallido nunca confirma ningún cambio rastreado.
    /// </summary>
    public void DiscardAsIfNeverSaved(Transfer transfer)
    {
        _transfers.Remove(transfer);
        AddedTransfers.Remove(transfer);
    }

    public Task<Transfer?> GetByIdForCustomerAsync(CustomerId customerId, TransferId transferId, CancellationToken cancellationToken)
    {
        var transfer = _transfers.FirstOrDefault(t => t.CustomerId == customerId && t.Id == transferId);
        return Task.FromResult(transfer);
    }

    public Task<Transfer?> GetByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken)
    {
        var transfer = _transfers.FirstOrDefault(t => t.IdempotencyKey.Equals(idempotencyKey));
        return Task.FromResult(transfer);
    }
}
