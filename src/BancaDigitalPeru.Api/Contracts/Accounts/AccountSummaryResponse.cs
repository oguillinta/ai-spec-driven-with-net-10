using BancaDigitalPeru.Application.Accounts.GetCustomerAccountDetail;
using BancaDigitalPeru.Application.Accounts.ListCustomerAccounts;
using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;

namespace BancaDigitalPeru.Api.Contracts.Accounts;

/// <summary>Refleja el schema AccountSummary/AccountDetail del contrato OpenAPI (mismos campos).</summary>
public sealed record AccountSummaryResponse(
    Guid AccountId,
    AccountType AccountType,
    string MaskedNumber,
    MoneyResponse Balance,
    AccountStatus Status)
{
    public static AccountSummaryResponse FromDto(AccountSummaryDto dto) => new(
        dto.AccountId,
        dto.AccountType,
        dto.MaskedNumber,
        new MoneyResponse(dto.BalanceAmount, dto.BalanceCurrency),
        dto.Status);

    public static AccountSummaryResponse FromDto(AccountDetailDto dto) => new(
        dto.AccountId,
        dto.AccountType,
        dto.MaskedNumber,
        new MoneyResponse(dto.BalanceAmount, dto.BalanceCurrency),
        dto.Status);
}

public sealed record MoneyResponse(decimal Amount, CurrencyCode Currency);
