using BancaDigitalPeru.Application.Abstractions;
using BancaDigitalPeru.Application.Abstractions.Persistence;
using BancaDigitalPeru.Infrastructure.CurrentCustomer;
using BancaDigitalPeru.Infrastructure.Persistence;
using BancaDigitalPeru.Infrastructure.Persistence.Repositories;
using BancaDigitalPeru.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BancaDigitalPeru.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BancaDigitalPeruDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.Configure<DemoCustomerOptions>(configuration.GetSection(DemoCustomerOptions.SectionName));
        services.AddScoped<ICurrentCustomerProvider, ConfiguredCurrentCustomerProvider>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IDebitCardRepository, DebitCardRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPreviewTokenSigner, DataProtectionPreviewTokenSigner>();

        return services;
    }
}
