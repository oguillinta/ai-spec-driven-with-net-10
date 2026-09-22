using BancaDigitalPeru.Application.Accounts.GetCustomerAccountDetail;
using BancaDigitalPeru.Application.Accounts.ListCustomerAccounts;
using BancaDigitalPeru.Application.DebitCards.GetCustomerDebitCardDetail;
using BancaDigitalPeru.Application.DebitCards.ListCustomerDebitCards;
using Microsoft.Extensions.DependencyInjection;

namespace BancaDigitalPeru.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los casos de uso de Application. Se completa incrementalmente a medida que cada
    /// historia de usuario añade su propio caso de uso.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListCustomerAccountsUseCase>();
        services.AddScoped<GetCustomerAccountDetailUseCase>();
        services.AddScoped<ListCustomerDebitCardsUseCase>();
        services.AddScoped<GetCustomerDebitCardDetailUseCase>();

        return services;
    }
}
