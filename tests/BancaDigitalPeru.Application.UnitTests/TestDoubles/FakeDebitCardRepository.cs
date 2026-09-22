using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;

namespace BancaDigitalPeru.Application.UnitTests.TestDoubles;

public sealed class FakeDebitCardRepository : IDebitCardRepository
{
    private readonly List<DebitCard> _cards;

    public FakeDebitCardRepository(IEnumerable<DebitCard> cards)
    {
        _cards = cards.ToList();
    }

    public Task<IReadOnlyList<DebitCard>> GetByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<DebitCard> result = _cards.Where(c => c.CustomerId == customerId).ToList();
        return Task.FromResult(result);
    }

    public Task<DebitCard?> GetByIdForCustomerAsync(CustomerId customerId, DebitCardId debitCardId, CancellationToken cancellationToken)
    {
        var card = _cards.FirstOrDefault(c => c.CustomerId == customerId && c.Id == debitCardId);
        return Task.FromResult(card);
    }
}
