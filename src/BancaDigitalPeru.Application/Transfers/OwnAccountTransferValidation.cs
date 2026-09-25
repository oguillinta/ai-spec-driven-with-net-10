using BancaDigitalPeru.Domain.Accounts;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Validación de negocio compartida entre vista previa y confirmación (FR-012 exige revalidar
/// exactamente las mismas reglas en ambos pasos, plan.md "Application Use Case" pasos 4-8).
/// Delega en <see cref="CommonTransferValidation"/> las reglas que dependen únicamente de la
/// cuenta origen (research.md de 003 §6); mantiene aquí solo sus reglas propias (cuentas
/// distintas, destino del mismo cliente y ACTIVA).
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
        // Nota: para un input inválido en más de una dimensión a la vez (p. ej. misma cuenta como
        // origen/destino Y un importe inválido simultáneamente), esta implementación puede
        // devolver un motivo distinto al que devolvería una comprobación estrictamente secuencial
        // que evaluara "cuentas distintas" antes que las reglas de origen. Ningún escenario de la
        // spec combina más de una violación a la vez, por lo que esta diferencia de prioridad
        // nunca es observable en la práctica (research.md de 003 §6).
        var commonRejection = CommonTransferValidation.ValidateSource(sourceAccount, amount);
        if (commonRejection is not null)
        {
            return commonRejection;
        }

        if (destinationAccount is null)
        {
            return TransferRejectionReason.AccountNotEligible;
        }

        if (sourceAccountId == destinationAccountId)
        {
            return TransferRejectionReason.SameAccount;
        }

        if (destinationAccount.Status != AccountStatus.Active)
        {
            return TransferRejectionReason.AccountBlocked;
        }

        return null;
    }
}
