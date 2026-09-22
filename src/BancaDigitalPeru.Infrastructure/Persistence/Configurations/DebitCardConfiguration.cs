using BancaDigitalPeru.Domain.Accounts;
using BancaDigitalPeru.Domain.Customers;
using BancaDigitalPeru.Domain.DebitCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BancaDigitalPeru.Infrastructure.Persistence.Configurations;

public sealed class DebitCardConfiguration : IEntityTypeConfiguration<DebitCard>
{
    public void Configure(EntityTypeBuilder<DebitCard> builder)
    {
        builder.ToTable("debit_cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new DebitCardId(value));

        builder.Property(c => c.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => new AccountId(value));

        builder.Property(c => c.CustomerId)
            .HasColumnName("customer_id")
            .HasConversion(id => id.Value, value => new CustomerId(value));

        builder.Property(c => c.Number)
            .HasColumnName("number")
            .HasConversion(number => number.FullNumber, value => new CardNumber(value))
            .HasMaxLength(16)
            .IsRequired();

        builder.OwnsOne(c => c.Expiration, expiration =>
        {
            expiration.Property(e => e.Month).HasColumnName("expiration_month").IsRequired();
            expiration.Property(e => e.Year).HasColumnName("expiration_year").IsRequired();
        });

        builder.Navigation(c => c.Expiration).IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.CustomerId).HasDatabaseName("ix_debit_cards_customer_id");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(c => c.AccountId)
            .HasConstraintName("fk_debit_cards_accounts")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .HasConstraintName("fk_debit_cards_customers")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
