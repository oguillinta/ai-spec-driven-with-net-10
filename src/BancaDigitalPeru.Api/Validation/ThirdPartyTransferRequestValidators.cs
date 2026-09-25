using System.Text.RegularExpressions;
using BancaDigitalPeru.Api.Contracts.Transfers;
using FluentValidation;

namespace BancaDigitalPeru.Api.Validation;

/// <summary>
/// Valida únicamente forma/sintaxis (plan.md "Validation Strategy"): el número de cuenta destino
/// debe contener solo dígitos con al menos 4 caracteres — mismo invariante que el constructor de
/// <c>AccountNumber</c> (research.md de 003 §3), sin duplicar ni divergir de él — y el importe debe
/// tener como máximo 2 decimales. No valida propiedad, existencia ni elegibilidad de las cuentas:
/// esas son reglas de negocio de Application/Domain.
/// </summary>
public sealed partial class ThirdPartyTransferPreviewRequestValidator : AbstractValidator<ThirdPartyTransferPreviewRequest>
{
    public ThirdPartyTransferPreviewRequestValidator()
    {
        RuleFor(x => x.DestinationAccountNumber)
            .Must(EsUnNumeroDeCuentaConFormatoValido)
            .WithMessage("El número de cuenta destino debe contener solo dígitos y al menos 4 caracteres.");

        RuleFor(x => x.Amount)
            .Must(TieneComoMaximoDosDecimales)
            .WithMessage("El importe debe tener como máximo dos decimales.");
    }

    private static bool EsUnNumeroDeCuentaConFormatoValido(string destinationAccountNumber) =>
        !string.IsNullOrWhiteSpace(destinationAccountNumber)
        && destinationAccountNumber.Length >= 4
        && DigitsOnlyRegex().IsMatch(destinationAccountNumber);

    private static bool TieneComoMaximoDosDecimales(decimal amount) => decimal.Round(amount, 2) == amount;

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();
}
