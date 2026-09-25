using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Common;
using BancaDigitalPeru.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BancaDigitalPeru.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new AccountId(value));

        builder.Property(a => a.CustomerId)
            .HasColumnName("customer_id")
            .HasConversion(id => id.Value, value => new CustomerId(value));

        builder.Property(a => a.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Number)
            .HasColumnName("number")
            .HasConversion(
                number => number.FullNumber,
                value => new AccountNumber(value))
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(a => a.Balance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("balance_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("balance_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(a => a.Balance).IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(a => a.CustomerId).HasDatabaseName("ix_accounts_customer_id");

        // Resolución de la cuenta destino por número (spec 003 FR-009): primera consulta real por
        // este campo; la unicidad del número de cuenta es una invariante de negocio real que la
        // base de datos debe reforzar como segunda línea de defensa (research.md de 003 §3/§9).
        builder.HasIndex(a => a.Number).IsUnique().HasDatabaseName("ux_accounts_number");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .HasConstraintName("fk_accounts_customers")
            .OnDelete(DeleteBehavior.Restrict);

        // Concurrency token optimista nativo de PostgreSQL (research.md §6 de 002): la columna de
        // sistema `xmin`, mapeada como shadow property (no se añade ningún miembro a Account,
        // preservando Domain puro). Patrón verificado contra Npgsql.EntityFrameworkCore.PostgreSQL
        // 10.0.3: no existe un método de extensión "UseXminAsConcurrencyToken" en esta versión del
        // proveedor (Technical Risk anticipado en plan.md); la forma soportada es declarar la
        // columna de sistema explícitamente con IsRowVersion()/IsConcurrencyToken().
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
