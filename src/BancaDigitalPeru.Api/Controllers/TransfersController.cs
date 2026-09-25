using BancaDigitalPeru.Api.Contracts.Transfers;
using BancaDigitalPeru.Api.ErrorHandling;
using BancaDigitalPeru.Api.Validation;
using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Application.Transfers.ConfirmOwnAccountTransfer;
using BancaDigitalPeru.Application.Transfers.GetOwnAccountTransfer;
using BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Transfers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BancaDigitalPeru.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class TransfersController : ControllerBase
{
    private readonly PreviewOwnAccountTransferUseCase _previewOwnAccountTransfer;
    private readonly ConfirmOwnAccountTransferUseCase _confirmOwnAccountTransfer;
    private readonly GetOwnAccountTransferUseCase _getOwnAccountTransfer;
    private readonly IValidator<TransferPreviewRequest> _previewRequestValidator;
    private readonly IValidator<ConfirmTransferRequest> _confirmRequestValidator;
    private readonly IValidator<IdempotencyKeyHeader> _idempotencyKeyHeaderValidator;
    private readonly IValidator<TransferIdRouteParameter> _transferIdValidator;

    public TransfersController(
        PreviewOwnAccountTransferUseCase previewOwnAccountTransfer,
        ConfirmOwnAccountTransferUseCase confirmOwnAccountTransfer,
        GetOwnAccountTransferUseCase getOwnAccountTransfer,
        IValidator<TransferPreviewRequest> previewRequestValidator,
        IValidator<ConfirmTransferRequest> confirmRequestValidator,
        IValidator<IdempotencyKeyHeader> idempotencyKeyHeaderValidator,
        IValidator<TransferIdRouteParameter> transferIdValidator)
    {
        _previewOwnAccountTransfer = previewOwnAccountTransfer;
        _confirmOwnAccountTransfer = confirmOwnAccountTransfer;
        _getOwnAccountTransfer = getOwnAccountTransfer;
        _previewRequestValidator = previewRequestValidator;
        _confirmRequestValidator = confirmRequestValidator;
        _idempotencyKeyHeaderValidator = idempotencyKeyHeaderValidator;
        _transferIdValidator = transferIdValidator;
    }

    /// <summary>POST /api/v1/transfer-previews — spec FR-010. Sin efecto financiero.</summary>
    [HttpPost("transfer-previews")]
    [ProducesResponseType<TransferPreviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PreviewTransfer([FromBody] TransferPreviewRequest request, CancellationToken cancellationToken)
    {
        var validation = await _previewRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.BadRequest(HttpContext, validation.Errors[0].ErrorMessage);
        }

        if (request.SourceAccountId == Guid.Empty || request.DestinationAccountId == Guid.Empty)
        {
            // Guid.Empty es sintácticamente válido pero AccountId lo rechaza como identidad real;
            // debe tratarse como cualquier otra cuenta inexistente, nunca como error del servidor
            // (mismo criterio que la regresión de 001, ahora aplicado también en 002).
            return TransferRejectionReason.AccountNotEligible.ToProblemDetails(HttpContext);
        }

        var outcome = await _previewOwnAccountTransfer.ExecuteAsync(
            new AccountId(request.SourceAccountId),
            new AccountId(request.DestinationAccountId),
            request.Amount,
            cancellationToken);

        return outcome.IsSuccess
            ? Ok(TransferPreviewResponse.FromResult(outcome.Value!))
            : outcome.RejectionReason!.Value.ToProblemDetails(HttpContext);
    }

    /// <summary>POST /api/v1/transfers — spec FR-011/FR-012. Requiere Idempotency-Key.</summary>
    [HttpPost("transfers")]
    [ProducesResponseType<TransferResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<TransferResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmTransfer(
        [FromBody] ConfirmTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var bodyValidation = await _confirmRequestValidator.ValidateAsync(request, cancellationToken);
        if (!bodyValidation.IsValid)
        {
            return ApiProblemDetails.BadRequest(HttpContext, bodyValidation.Errors[0].ErrorMessage);
        }

        var headerValidation = await _idempotencyKeyHeaderValidator.ValidateAsync(new IdempotencyKeyHeader(idempotencyKey), cancellationToken);
        if (!headerValidation.IsValid)
        {
            return ApiProblemDetails.BadRequest(HttpContext, headerValidation.Errors[0].ErrorMessage);
        }

        var outcome = await _confirmOwnAccountTransfer.ExecuteAsync(
            request.PreviewReference,
            new IdempotencyKey(idempotencyKey!),
            cancellationToken);

        if (!outcome.IsSuccess)
        {
            return outcome.RejectionReason!.Value.ToProblemDetails(HttpContext);
        }

        var response = TransferResultResponse.FromDto(outcome.Value!);

        return outcome.IsReplay
            ? Ok(response)
            : CreatedAtAction(nameof(GetTransferById), new { transferId = response.TransferId }, response);
    }

    /// <summary>GET /api/v1/transfers/{transferId} — spec FR-018, User Story 4. 404 genérico ante "no existe" o "es de otro cliente".</summary>
    [HttpGet("transfers/{transferId}")]
    [ProducesResponseType<TransferResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransferById(string transferId, CancellationToken cancellationToken)
    {
        var validation = await _transferIdValidator.ValidateAsync(new TransferIdRouteParameter(transferId), cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.BadRequest(HttpContext, "El identificador de transferencia provisto no tiene un formato válido.");
        }

        var parsedId = Guid.Parse(transferId);
        if (parsedId == Guid.Empty)
        {
            return ApiProblemDetails.NotFound(HttpContext);
        }

        var result = await _getOwnAccountTransfer.ExecuteAsync(new TransferId(parsedId), cancellationToken);

        return result.IsFound
            ? Ok(TransferResultResponse.FromDto(result.Value!))
            : ApiProblemDetails.NotFound(HttpContext);
    }
}
