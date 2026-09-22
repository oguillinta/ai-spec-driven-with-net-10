using BancaDigitalPeru.Api.Contracts.DebitCards;
using BancaDigitalPeru.Api.ErrorHandling;
using BancaDigitalPeru.Api.Validation;
using BancaDigitalPeru.Application.DebitCards.GetCustomerDebitCardDetail;
using BancaDigitalPeru.Application.DebitCards.ListCustomerDebitCards;
using BancaDigitalPeru.Domain.DebitCards;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BancaDigitalPeru.Api.Controllers;

[ApiController]
[Route("api/v1/debit-cards")]
public sealed class DebitCardsController : ControllerBase
{
    private readonly ListCustomerDebitCardsUseCase _listCustomerDebitCards;
    private readonly GetCustomerDebitCardDetailUseCase _getCustomerDebitCardDetail;
    private readonly IValidator<DebitCardIdRouteParameter> _debitCardIdValidator;

    public DebitCardsController(
        ListCustomerDebitCardsUseCase listCustomerDebitCards,
        GetCustomerDebitCardDetailUseCase getCustomerDebitCardDetail,
        IValidator<DebitCardIdRouteParameter> debitCardIdValidator)
    {
        _listCustomerDebitCards = listCustomerDebitCards;
        _getCustomerDebitCardDetail = getCustomerDebitCardDetail;
        _debitCardIdValidator = debitCardIdValidator;
    }

    /// <summary>GET /api/v1/debit-cards — spec FR-008.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DebitCardSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyDebitCards(CancellationToken cancellationToken)
    {
        var cards = await _listCustomerDebitCards.ExecuteAsync(cancellationToken);
        return Ok(cards.Select(DebitCardSummaryResponse.FromDto));
    }

    /// <summary>GET /api/v1/debit-cards/{debitCardId} — spec FR-010. 404 genérico ante "no existe" o "es de otro cliente" (FR-022).</summary>
    [HttpGet("{debitCardId}")]
    [ProducesResponseType<DebitCardSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyDebitCardById(string debitCardId, CancellationToken cancellationToken)
    {
        var validation = await _debitCardIdValidator.ValidateAsync(new DebitCardIdRouteParameter(debitCardId), cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.BadRequest(HttpContext, "El identificador de tarjeta provisto no tiene un formato válido.");
        }

        var parsedId = Guid.Parse(debitCardId);
        if (parsedId == Guid.Empty)
        {
            // Ver AccountsController.GetMyAccountById: mismo tratamiento para GUID bien formado
            // pero que nunca corresponde a un producto real (spec FR-022, SC-002).
            return ApiProblemDetails.NotFound(HttpContext);
        }

        var result = await _getCustomerDebitCardDetail.ExecuteAsync(new DebitCardId(parsedId), cancellationToken);

        return result.IsFound
            ? Ok(DebitCardSummaryResponse.FromDto(result.Value!))
            : ApiProblemDetails.NotFound(HttpContext);
    }
}
