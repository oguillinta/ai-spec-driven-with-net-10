using BancaDigitalPeru.Domain.Accounts;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Reglas de validación comunes a cualquier transferencia (entre cuentas propias o a terceros),
/// que dependen únicamente de la cuenta origen y el importe — nunca de la cuenta destino
/// (research.md de 003 §6). El importe se recibe como <c>decimal</c> crudo, no como
/// <see cref="BancaDigitalPeru.Domain.Common.Money"/>, para poder rechazar un importe ≤ 0 como
/// regla de negocio (InvalidAmount) en vez de dejar que <c>Money.Create</c> lance una excepción
/// de programación.
/// </summary>
internal static class CommonTransferValidation
{
    public static TransferRejectionReason? ValidateSource(Account? sourceAccount, decimal amount)
    {
        if (sourceAccount is null)
        {
            return TransferRejectionReason.AccountNotEligible;
        }

        if (sourceAccount.Status != AccountStatus.Active)
        {
            return TransferRejectionReason.AccountBlocked;
        }

        if (amount <= 0)
        {
            return TransferRejectionReason.InvalidAmount;
        }

        if (amount > sourceAccount.Balance.Amount)
        {
            return TransferRejectionReason.InsufficientFunds;
        }

        return null;
    }
}
