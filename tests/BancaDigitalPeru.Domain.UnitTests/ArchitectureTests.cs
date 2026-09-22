using System.Reflection;
using BancaDigitalPeru.Domain.Common;
using Xunit;

namespace BancaDigitalPeru.Domain.UnitTests;

/// <summary>
/// Verifica la Dependency Rule (Principio IV de la constitución): Domain debe permanecer C#
/// puro, sin depender de ASP.NET Core, EF Core, FluentValidation, OpenAPI ni Scalar.
/// </summary>
public class ArchitectureTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "FluentValidation",
        "Microsoft.OpenApi",
        "Scalar"
    ];

    [Fact]
    public void Domain_NoReferenciaAssembliesDeInfraestructuraOFrameworksDeEntrega()
    {
        var domainAssembly = typeof(CurrencyCode).Assembly;

        var referenced = domainAssembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

        var violations = referenced
            .Where(name => ForbiddenAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Domain_NoReferenciaProyectosDeInfrastructureNiApi()
    {
        var domainAssembly = typeof(CurrencyCode).Assembly;

        var referenced = domainAssembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referenced, name =>
            name.Contains("Infrastructure", StringComparison.Ordinal) || name.Contains(".Api", StringComparison.Ordinal));
    }
}
