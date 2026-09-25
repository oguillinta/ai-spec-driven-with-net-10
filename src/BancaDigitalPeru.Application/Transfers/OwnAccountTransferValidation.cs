using BancaDigitalPeru.Domain.Accounts;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Validación de negocio compartida entre vista previa y confirmación (FR-012 exige revalidar
/// exactamente las mismas reglas en ambos pasos, plan.md "Application Use Case" pasos 4-8). El
/// importe se recibe como <c>decimal</c> crudo, no como <see cref="BancaDigitalPeru.Domain.Common.Money"/>,
/// para poder rechazar un importe ≤ 0 como regla de negocio (InvalidAmount) en vez de dejar que
/// <c>Money.Create</c> lance una excepción de programación.
/// </summary>
internal static class OwnAccountTransferValidation
{
    public static TransferRejectionReason? Validate(
        Account? sourceAccount,
        Account? destinationAccount,
        AccountId sourceAccountId,
        AccountId destinationAccountId,
        decimal amount)
    {
        if (sourceAccount is null || destinationAccount is null)
        {
            return TransferRejectionReason.AccountNotEligible;
        }

        if (sourceAccountId == destinationAccountId)
        {
            return TransferRejectionReason.SameAccount;
        }

        if (sourceAccount.Status != AccountStatus.Active || destinationAccount.Status != AccountStatus.Active)
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
