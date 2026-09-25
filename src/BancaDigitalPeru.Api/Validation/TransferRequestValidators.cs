using BancaDigitalPeru.Api.Contracts.Transfers;
using FluentValidation;

namespace BancaDigitalPeru.Api.Validation;

/// <summary>Envuelve el header Idempotency-Key crudo para poder validarlo con FluentValidation.</summary>
public sealed record IdempotencyKeyHeader(string? Value);

/// <summary>
/// Valida únicamente forma/sintaxis (plan.md "Validation Strategy"): el importe debe tener como
/// máximo 2 decimales. No valida que el importe sea > 0 ni ninguna regla de propiedad/elegibilidad
/// de las cuentas: esas son reglas de negocio de Application/Domain.
/// </summary>
public sealed class TransferPreviewRequestValidator : AbstractValidator<TransferPreviewRequest>
{
    public TransferPreviewRequestValidator()
    {
        RuleFor(x => x.Amount)
            .Must(TieneComoMaximoDosDecimales)
            .WithMessage("El importe debe tener como máximo dos decimales.");
    }

    private static bool TieneComoMaximoDosDecimales(decimal amount) => decimal.Round(amount, 2) == amount;
}

/// <summary>Valida únicamente que la referencia de vista previa esté presente (formato de solicitud).</summary>
public sealed class ConfirmTransferRequestValidator : AbstractValidator<ConfirmTransferRequest>
{
    public ConfirmTransferRequestValidator()
    {
        RuleFor(x => x.PreviewReference)
            .NotEmpty()
            .WithMessage("La referencia de vista previa es obligatoria.");
    }
}

/// <summary>Valida el formato del header Idempotency-Key (1-255 caracteres, obligatorio en la confirmación).</summary>
public sealed class IdempotencyKeyHeaderValidator : AbstractValidator<IdempotencyKeyHeader>
{
    public IdempotencyKeyHeaderValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("El header Idempotency-Key es obligatorio.")
            .MaximumLength(255)
            .WithMessage("El header Idempotency-Key debe tener como máximo 255 caracteres.");
    }
}
