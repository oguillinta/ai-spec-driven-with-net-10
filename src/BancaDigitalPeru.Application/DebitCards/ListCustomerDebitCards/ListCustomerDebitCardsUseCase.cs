using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;

namespace BancaDigitalPeru.Application.DebitCards.ListCustomerDebitCards;

/// <summary>Lista las tarjetas de débito del cliente actual, incluidas las BLOQUEADAS (spec FR-008, RB5).</summary>
public sealed class ListCustomerDebitCardsUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IDebitCardRepository _debitCardRepository;

    public ListCustomerDebitCardsUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IDebitCardRepository debitCardRepository)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _debitCardRepository = debitCardRepository;
    }

    public async Task<IReadOnlyList<DebitCardSummaryDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);
        var cards = await _debitCardRepository.GetByCustomerAsync(customerId, cancellationToken);

        return cards.Select(DebitCardSummaryDto.FromDomain).ToList();
    }
}
