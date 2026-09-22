using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Accounts.GetCustomerAccountDetail;

/// <summary>Mismos campos que AccountSummaryDto (data-model.md): el detalle no añade atributos.</summary>
public sealed record AccountDetailDto(
    Guid AccountId,
    AccountType AccountType,
    string MaskedNumber,
    decimal BalanceAmount,
    CurrencyCode BalanceCurrency,
    AccountStatus Status)
{
    public static AccountDetailDto FromDomain(Account account) => new(
        account.Id.Value,
        account.Type,
        account.Number.Masked,
        account.Balance.Amount,
        account.Balance.Currency,
        account.Status);
}
