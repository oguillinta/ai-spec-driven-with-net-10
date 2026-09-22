using BancaDigitalPeru.Api.ErrorHandling;
using FluentValidation;

namespace BancaDigitalPeru.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
