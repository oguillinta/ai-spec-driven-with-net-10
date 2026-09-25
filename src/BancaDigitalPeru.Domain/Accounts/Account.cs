using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Domain.Accounts;

/// <summary>
/// Cuenta de ahorro perteneciente a un cliente (spec RF-001…RF-007). Desde la feature 002, esta
/// entidad expone comportamiento explícito (<see cref="Debit"/>/<see cref="Credit"/>) para mutar
/// el saldo; nada fuera de <see cref="Account"/> puede escribir <see cref="Balance"/> directamente
/// (plan.md "Domain Design", evita el modelo anémico).
/// </summary>
public sealed class Account
{
    public AccountId Id { get; }
    public CustomerId CustomerId { get; }
    public AccountType Type { get; }
    public AccountNumber Number { get; }
    public Money Balance { get; private set; }
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

    /// <summary>
    /// Debita el importe de la cuenta (spec RF-010/RF-016). Invariantes — segunda línea de defensa
    /// tras la validación de Application (plan.md "Domain Design"): misma moneda, importe > 0,
    /// cuenta ACTIVA (RF-005/FR-023) y saldo suficiente (RF-009/FR-019/RB7).
    /// </summary>
    public void Debit(Money amount)
    {
        if (amount.Currency != Balance.Currency)
        {
            throw new InvalidOperationException("El importe a debitar debe estar en la misma moneda que el saldo de la cuenta.");
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount.Amount, "El importe a debitar debe ser mayor que cero.");
        }

        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Solo una cuenta ACTIVA puede debitarse.");
        }

        if (amount.Amount > Balance.Amount)
        {
            throw new InvalidOperationException("El saldo disponible es insuficiente para este débito.");
        }

        Balance = Money.Create(Balance.Amount - amount.Amount, Balance.Currency);
    }

    /// <summary>
    /// Acredita el importe a la cuenta (spec RF-011). Invariantes: misma moneda, importe > 0, y
    /// cuenta ACTIVA (FR-006, clarificación 2026-09-24: una cuenta BLOQUEADA no puede ser destino).
    /// </summary>
    public void Credit(Money amount)
    {
        if (amount.Currency != Balance.Currency)
        {
            throw new InvalidOperationException("El importe a acreditar debe estar en la misma moneda que el saldo de la cuenta.");
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount.Amount, "El importe a acreditar debe ser mayor que cero.");
        }

        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Solo una cuenta ACTIVA puede recibir un crédito.");
        }

        Balance = Money.Create(Balance.Amount + amount.Amount, Balance.Currency);
    }
}
