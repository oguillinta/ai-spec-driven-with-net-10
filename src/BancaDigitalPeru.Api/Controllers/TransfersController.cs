using BancaDigitalPeru.Api.Contracts.Transfers;
using BancaDigitalPeru.Api.ErrorHandling;
using BancaDigitalPeru.Api.Validation;
using BancaDigitalPeru.Application.Transfers;
using BancaDigitalPeru.Application.Transfers.ConfirmTransfer;
using BancaDigitalPeru.Application.Transfers.GetTransfer;
using BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;
using BancaDigitalPeru.Application.Transfers.PreviewThirdPartyTransfer;
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
    private readonly PreviewThirdPartyTransferUseCase _previewThirdPartyTransfer;
    private readonly ConfirmTransferUseCase _confirmTransfer;
    private readonly GetTransferUseCase _getTransfer;
    private readonly IValidator<TransferPreviewRequest> _previewRequestValidator;
    private readonly IValidator<ThirdPartyTransferPreviewRequest> _thirdPartyPreviewRequestValidator;
    private readonly IValidator<ConfirmTransferRequest> _confirmRequestValidator;
    private readonly IValidator<IdempotencyKeyHeader> _idempotencyKeyHeaderValidator;
    private readonly IValidator<TransferIdRouteParameter> _transferIdValidator;

    public TransfersController(
        PreviewOwnAccountTransferUseCase previewOwnAccountTransfer,
        PreviewThirdPartyTransferUseCase previewThirdPartyTransfer,
        ConfirmTransferUseCase confirmTransfer,
        GetTransferUseCase getTransfer,
        IValidator<TransferPreviewRequest> previewRequestValidator,
        IValidator<ThirdPartyTransferPreviewRequest> thirdPartyPreviewRequestValidator,
        IValidator<ConfirmTransferRequest> confirmRequestValidator,
        IValidator<IdempotencyKeyHeader> idempotencyKeyHeaderValidator,
        IValidator<TransferIdRouteParameter> transferIdValidator)
    {
        _previewOwnAccountTransfer = previewOwnAccountTransfer;
        _previewThirdPartyTransfer = previewThirdPartyTransfer;
        _confirmTransfer = confirmTransfer;
        _getTransfer = getTransfer;
        _previewRequestValidator = previewRequestValidator;
        _thirdPartyPreviewRequestValidator = thirdPartyPreviewRequestValidator;
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

    /// <summary>POST /api/v1/third-party-transfer-previews — spec 003 FR-010. Sin efecto financiero.</summary>
    [HttpPost("third-party-transfer-previews")]
    [ProducesResponseType<ThirdPartyTransferPreviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PreviewThirdPartyTransfer([FromBody] ThirdPartyTransferPreviewRequest request, CancellationToken cancellationToken)
    {
        var validation = await _thirdPartyPreviewRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.BadRequest(HttpContext, validation.Errors[0].ErrorMessage);
        }

        if (request.SourceAccountId == Guid.Empty)
        {
            // Guid.Empty es sintácticamente válido pero AccountId lo rechaza como identidad real;
            // debe tratarse como cualquier otra cuenta origen inexistente (mismo criterio que 002).
            return TransferRejectionReason.AccountNotEligible.ToProblemDetails(HttpContext);
        }

        var outcome = await _previewThirdPartyTransfer.ExecuteAsync(
            new AccountId(request.SourceAccountId),
            new AccountNumber(request.DestinationAccountNumber),
            request.Amount,
            cancellationToken);

        return outcome.IsSuccess
            ? Ok(ThirdPartyTransferPreviewResponse.FromResult(outcome.Value!))
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

        var outcome = await _confirmTransfer.ExecuteAsync(
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

        var result = await _getTransfer.ExecuteAsync(new TransferId(parsedId), cancellationToken);

        return result.IsFound
            ? Ok(TransferResultResponse.FromDto(result.Value!))
            : ApiProblemDetails.NotFound(HttpContext);
    }
}
