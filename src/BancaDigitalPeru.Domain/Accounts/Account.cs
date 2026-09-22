using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Domain.Accounts;

/// <summary>
/// Cuenta de ahorro perteneciente a un cliente (spec RF-001…RF-007). Feature 001 es de solo
/// lectura: esta entidad no expone operaciones que muten saldo o estado.
/// </summary>
public sealed class Account
{
    public AccountId Id { get; }
    public CustomerId CustomerId { get; }
    public AccountType Type { get; }
    public AccountNumber Number { get; }
    public Money Balance { get; }
    public AccountStatus Status { get; }

    public Account(
        AccountId id,
        CustomerId customerId,
        AccountType type,
        AccountNumber number,
        Money balance,
        AccountStatus status)
    {
        if (balance.Currency != CurrencyCode.PEN)
        {
            throw new ArgumentException("Toda cuenta de esta versión debe estar denominada en PEN (spec RB2).", nameof(balance));
        }

        Id = id;
        CustomerId = customerId;
        Type = type;
        Number = number;
        Balance = balance;
        Status = status;
    }

    /// <summary>
    /// Constructor de materialización usado exclusivamente por EF Core (Infrastructure): las
    /// navegaciones a owned types (Balance) no pueden vincularse mediante constructor binding
    /// del propietario, por lo que EF completa esta instancia vía backing fields
    /// (research.md §4, plan.md "Technical Risks").
    /// </summary>
    private Account()
    {
        Number = null!;
        Balance = null!;
    }
}
