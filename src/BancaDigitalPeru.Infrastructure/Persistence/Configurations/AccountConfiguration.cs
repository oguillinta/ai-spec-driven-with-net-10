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

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .HasConstraintName("fk_accounts_customers")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
