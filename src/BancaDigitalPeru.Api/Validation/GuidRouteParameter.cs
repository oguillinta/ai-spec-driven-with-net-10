namespace BancaDigitalPeru.Api.Validation;

/// <summary>
/// Envuelve un parámetro de ruta crudo para poder validarlo con FluentValidation. Un tipo por
/// parámetro (no uno compartido) para que cada AbstractValidator se resuelva sin ambigüedad en DI.
/// </summary>
public sealed record AccountIdRouteParameter(string Value);

public sealed record DebitCardIdRouteParameter(string Value);
