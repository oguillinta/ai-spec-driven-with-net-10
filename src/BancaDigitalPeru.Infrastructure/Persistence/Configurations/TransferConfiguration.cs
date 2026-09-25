using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BancaDigitalPeru.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new TransferId(value));

        builder.Property(t => t.CustomerId)
            .HasColumnName("customer_id")
            .HasConversion(id => id.Value, value => new CustomerId(value));

        builder.Property(t => t.SourceAccountId)
            .HasColumnName("source_account_id")
            .HasConversion(id => id.Value, value => new AccountId(value));

        builder.Property(t => t.DestinationAccountId)
            .HasColumnName("destination_account_id")
            .HasConversion(id => id.Value, value => new AccountId(value));

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("amount_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Navigation(t => t.Amount).IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasConversion(key => key.Value, value => new IdempotencyKey(value))
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .IsRequired();

        builder.HasIndex(t => t.CustomerId).HasDatabaseName("ix_transfers_customer_id");

        // Respaldo persistente ante condiciones de carrera entre solicitudes concurrentes con la
        // misma Idempotency-Key (research.md §5): la verificación previa en Application no puede
        // ver una fila que otra transacción aún no confirmó.
        builder.HasIndex(t => t.IdempotencyKey).IsUnique().HasDatabaseName("ux_transfers_idempotency_key");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .HasConstraintName("fk_transfers_customers")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.SourceAccountId)
            .HasConstraintName("fk_transfers_source_account")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.DestinationAccountId)
            .HasConstraintName("fk_transfers_destination_account")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
