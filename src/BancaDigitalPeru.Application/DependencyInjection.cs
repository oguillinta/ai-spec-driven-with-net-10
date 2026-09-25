using BancaDigitalPeru.Application.Accounts.GetCustomerAccountDetail;
using BancaDigitalPeru.Application.Accounts.ListCustomerAccounts;
using BancaDigitalPeru.Application.DebitCards.GetCustomerDebitCardDetail;
using BancaDigitalPeru.Application.DebitCards.ListCustomerDebitCards;
using BancaDigitalPeru.Application.Transfers.ConfirmTransfer;
using BancaDigitalPeru.Application.Transfers.GetTransfer;
using BancaDigitalPeru.Application.Transfers.PreviewOwnAccountTransfer;
using BancaDigitalPeru.Application.Transfers.PreviewThirdPartyTransfer;
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
        services.AddScoped<PreviewOwnAccountTransferUseCase>();
        services.AddScoped<PreviewThirdPartyTransferUseCase>();
        services.AddScoped<ConfirmTransferUseCase>();
        services.AddScoped<GetTransferUseCase>();

        return services;
    }
}
