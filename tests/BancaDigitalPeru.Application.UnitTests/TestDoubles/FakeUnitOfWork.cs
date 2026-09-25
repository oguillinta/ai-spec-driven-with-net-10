using BancaDigitalPeru.Application.Abstractions.Persistence;

namespace BancaDigitalPeru.Application.UnitTests.TestDoubles;

/// <summary>
/// Doble de prueba configurable: por defecto simula un SaveChangesAsync exitoso; un
/// <paramref name="behavior"/> personalizado permite simular fallos (concurrencia, unicidad, error
/// genérico del repositorio) sin depender de EF Core/PostgreSQL reales.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Func<CancellationToken, Task<int>>? _behavior;

    public FakeUnitOfWork(Func<CancellationToken, Task<int>>? behavior = null)
    {
        _behavior = behavior;
    }

    public bool WasCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        WasCalled = true;
        return _behavior?.Invoke(cancellationToken) ?? Task.FromResult(1);
    }
}
