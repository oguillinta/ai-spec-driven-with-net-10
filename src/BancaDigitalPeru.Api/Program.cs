using BancaDigitalPeru.Api;
using BancaDigitalPeru.Api.OpenApi;
using BancaDigitalPeru.Application;
using BancaDigitalPeru.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapBankingProductsOpenApiContract();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Necesario para que WebApplicationFactory<Program> (IntegrationTests) pueda referenciar
// el entry point del ensamblado Api.
public partial class Program;
