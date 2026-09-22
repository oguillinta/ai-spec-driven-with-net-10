using FluentValidation;

namespace BancaDigitalPeru.Api.Validation;

/// <summary>Valida únicamente el formato del identificador de tarjeta (debe ser un GUID).</summary>
public sealed class DebitCardIdRouteValidator : AbstractValidator<DebitCardIdRouteParameter>
{
    public DebitCardIdRouteValidator()
    {
        RuleFor(x => x.Value)
            .Must(value => Guid.TryParse(value, out _))
            .WithMessage("El identificador de tarjeta debe ser un GUID válido.");
    }
}
