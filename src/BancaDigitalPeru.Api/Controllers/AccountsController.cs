using BancaDigitalPeru.Api.Contracts.Accounts;
using BancaDigitalPeru.Api.ErrorHandling;
using BancaDigitalPeru.Api.Validation;
using BancaDigitalPeru.Application.Accounts.GetCustomerAccountDetail;
using BancaDigitalPeru.Application.Accounts.ListCustomerAccounts;
using BancaDigitalPeru.Domain.Accounts;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BancaDigitalPeru.Api.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly ListCustomerAccountsUseCase _listCustomerAccounts;
    private readonly GetCustomerAccountDetailUseCase _getCustomerAccountDetail;
    private readonly IValidator<AccountIdRouteParameter> _accountIdValidator;

    public AccountsController(
        ListCustomerAccountsUseCase listCustomerAccounts,
        GetCustomerAccountDetailUseCase getCustomerAccountDetail,
        IValidator<AccountIdRouteParameter> accountIdValidator)
    {
        _listCustomerAccounts = listCustomerAccounts;
        _getCustomerAccountDetail = getCustomerAccountDetail;
        _accountIdValidator = accountIdValidator;
    }

    /// <summary>GET /api/v1/accounts — spec FR-001.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _listCustomerAccounts.ExecuteAsync(cancellationToken);
        return Ok(accounts.Select(AccountSummaryResponse.FromDto));
    }

    /// <summary>GET /api/v1/accounts/{accountId} — spec FR-003. 404 genérico ante "no existe" o "es de otro cliente" (FR-022).</summary>
    [HttpGet("{accountId}")]
    [ProducesResponseType<AccountSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyAccountById(string accountId, CancellationToken cancellationToken)
    {
        var validation = await _accountIdValidator.ValidateAsync(new AccountIdRouteParameter(accountId), cancellationToken);
        if (!validation.IsValid)
        {
            return ApiProblemDetails.BadRequest(HttpContext, "El identificador de cuenta provisto no tiene un formato válido.");
        }

        var parsedId = Guid.Parse(accountId);
        if (parsedId == Guid.Empty)
        {
            // Un GUID bien formado que jamás corresponde a un producto real (AccountId no admite
            // Guid.Empty) debe responder igual que cualquier otro identificador que no exista,
            // nunca con un error del servidor (spec FR-022, SC-002).
            return ApiProblemDetails.NotFound(HttpContext);
        }

        var result = await _getCustomerAccountDetail.ExecuteAsync(new AccountId(parsedId), cancellationToken);

        return result.IsFound
            ? Ok(AccountSummaryResponse.FromDto(result.Value!))
            : ApiProblemDetails.NotFound(HttpContext);
    }
}
