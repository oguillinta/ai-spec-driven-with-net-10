using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Application.Common;
using BancaDigitalPeru.Domain.DebitCards;

namespace BancaDigitalPeru.Application.DebitCards.GetCustomerDebitCardDetail;

/// <summary>
/// Consulta el detalle de una tarjeta propia (spec FR-010/FR-011). "No existe" y "es de otro
/// cliente" producen el mismo Result&lt;T&gt;.NotFound() (spec FR-022).
/// </summary>
public sealed class GetCustomerDebitCardDetailUseCase
{
    private readonly ICurrentCustomerProvider _currentCustomerProvider;
    private readonly IDebitCardRepository _debitCardRepository;

    public GetCustomerDebitCardDetailUseCase(
        ICurrentCustomerProvider currentCustomerProvider,
        IDebitCardRepository debitCardRepository)
    {
        _currentCustomerProvider = currentCustomerProvider;
        _debitCardRepository = debitCardRepository;
    }

    public async Task<Result<DebitCardDetailDto>> ExecuteAsync(DebitCardId debitCardId, CancellationToken cancellationToken)
    {
        var customerId = await _currentCustomerProvider.GetCurrentCustomerIdAsync(cancellationToken);
        var card = await _debitCardRepository.GetByIdForCustomerAsync(customerId, debitCardId, cancellationToken);

        return card is null
            ? Result<DebitCardDetailDto>.NotFound()
            : Result<DebitCardDetailDto>.Found(DebitCardDetailDto.FromDomain(card));
    }
}
