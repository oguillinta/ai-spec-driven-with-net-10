using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;

namespace BancaDigitalPeru.Application.Transfers;

/// <summary>
/// Validación de negocio específica de transferencias a terceros (spec 003). Delega en
/// <see cref="CommonTransferValidation"/> las reglas que dependen únicamente de la cuenta origen
/// (research.md de 003 §6); mantiene aquí solo sus reglas propias: la cuenta destino debe existir
/// (rechazo revelador, a diferencia del origen — research.md §7), pertenecer a un cliente
/// **distinto** del ordenante, y estar ACTIVA.
/// </summary>
internal static class ThirdPartyTransferValidation
{
    public static TransferRejectionReason? Validate(
        Account? sourceAccount,
        Account? destinationAccount,
        CustomerId currentCustomerId,
        decimal amount)
    {
        var commonRejection = CommonTransferValidation.ValidateSource(sourceAccount, amount);
        if (commonRejection is not null)
        {
            return commonRejection;
        }

        if (destinationAccount is null)
        {
            return TransferRejectionReason.DestinationAccountNotFound;
        }

        if (destinationAccount.CustomerId == currentCustomerId)
        {
            return TransferRejectionReason.DestinationIsOwnAccount;
        }

        if (destinationAccount.Status != AccountStatus.Active)
        {
            return TransferRejectionReason.AccountBlocked;
        }

        return null;
    }
}
