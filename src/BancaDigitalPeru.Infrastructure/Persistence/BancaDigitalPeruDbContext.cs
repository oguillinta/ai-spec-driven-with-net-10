using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;
using Microsoft.EntityFrameworkCore;

namespace BancaDigitalPeru.Infrastructure.Persistence;

public sealed class BancaDigitalPeruDbContext : DbContext
{
    public BancaDigitalPeruDbContext(DbContextOptions<BancaDigitalPeruDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<DebitCard> DebitCards => Set<DebitCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BancaDigitalPeruDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
