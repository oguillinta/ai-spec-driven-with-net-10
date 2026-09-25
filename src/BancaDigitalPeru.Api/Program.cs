using BancaDigitalPeru.Api;
using BancaDigitalPeru.Api.OpenApi;
using BancaDigitalPeru.Application;
using BancaDigitalPeru.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices();

// Key ring de Data Protection para firmar las referencias de vista previa de transferencias
// (research.md §4/§9 de 002): por defecto, en disco local; conocido no compatible con múltiples
// instancias sin almacén compartido (Technical Risks, plan.md).
builder.Services.AddDataProtection();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApiContracts();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Necesario para que WebApplicationFactory<Program> (IntegrationTests) pueda referenciar
// el entry point del ensamblado Api.
public partial class Program;
