using System.Text.Json.Serialization;
using BancaDigitalPeru.Api.ErrorHandling;
using BancaDigitalPeru.Api.Serialization;
using FluentValidation;

namespace BancaDigitalPeru.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
                // Los contratos OpenAPI declaran AccountType/AccountStatus/CardStatus/CurrencyCode
                // como enums de string ("SAVINGS", "ACTIVE"/"BLOCKED", "PEN"). Sin este converter,
                // System.Text.Json serializa cualquier enum por su valor numérico subyacente (0, 1,
                // ...), incumpliendo el contrato.
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(new UpperInvariantJsonNamingPolicy())));
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
