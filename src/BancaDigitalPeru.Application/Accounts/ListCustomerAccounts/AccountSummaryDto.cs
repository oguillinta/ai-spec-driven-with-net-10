using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Application.Accounts.ListCustomerAccounts;

public sealed record AccountSummaryDto(
    Guid AccountId,
    AccountType AccountType,
    string MaskedNumber,
    decimal BalanceAmount,
    CurrencyCode BalanceCurrency,
    AccountStatus Status)
{
    public static AccountSummaryDto FromDomain(Account account) => new(
        account.Id.Value,
        account.Type,
        account.Number.Masked,
        account.Balance.Amount,
        account.Balance.Currency,
        account.Status);
}
