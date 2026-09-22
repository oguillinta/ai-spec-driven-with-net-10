using FluentValidation;

namespace BancaDigitalPeru.Api.Validation;

/// <summary>
/// Valida únicamente el formato del identificador de cuenta (debe ser un GUID). La propiedad del
/// producto y su existencia son reglas de negocio de Application, no de este validator
/// (plan.md "Validation Strategy").
/// </summary>
public sealed class AccountIdRouteValidator : AbstractValidator<AccountIdRouteParameter>
{
    public AccountIdRouteValidator()
    {
        RuleFor(x => x.Value)
            .Must(value => Guid.TryParse(value, out _))
            .WithMessage("El identificador de cuenta debe ser un GUID válido.");
    }
}
