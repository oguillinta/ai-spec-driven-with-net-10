using FluentValidation;

namespace BancaDigitalPeru.Api.Validation;

/// <summary>
/// Valida únicamente el formato del identificador de transferencia (debe ser un GUID). La
/// propiedad y existencia son reglas de negocio de Application (plan.md "Validation Strategy").
/// </summary>
public sealed class TransferIdRouteValidator : AbstractValidator<TransferIdRouteParameter>
{
    public TransferIdRouteValidator()
    {
        RuleFor(x => x.Value)
            .Must(value => Guid.TryParse(value, out _))
            .WithMessage("El identificador de transferencia debe ser un GUID válido.");
    }
}
