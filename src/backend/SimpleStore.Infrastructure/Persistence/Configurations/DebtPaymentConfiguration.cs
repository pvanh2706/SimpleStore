using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Debts;
using SimpleStore.Domain.Stores;
using SimpleStore.Domain.Suppliers;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class DebtPaymentConfiguration : IEntityTypeConfiguration<DebtPayment>
{
    public void Configure(EntityTypeBuilder<DebtPayment> builder)
    {
        builder.ToTable("DebtPayments", table =>
        {
            table.HasCheckConstraint("CK_DebtPayments_Amount", "[Amount] > 0");
            table.HasCheckConstraint(
                "CK_DebtPayments_PartyPurposeDirection",
                "([Purpose] = 'CustomerDebtCollection' AND [Direction] = 'MoneyIn' AND [CustomerId] IS NOT NULL AND [SupplierId] IS NULL) OR "
                + "([Purpose] = 'SupplierDebtSettlement' AND [Direction] = 'MoneyOut' AND [SupplierId] IS NOT NULL AND [CustomerId] IS NULL)");
        });
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Direction).HasConversion<string>().HasMaxLength(32);
        builder.Property(payment => payment.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(32);
        builder.Property(payment => payment.Note).HasMaxLength(DebtPayment.MaxNoteLength);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(payment => payment.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(payment => new { payment.StoreId, payment.CustomerId })
            .HasPrincipalKey(customer => new { customer.StoreId, customer.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(payment => new { payment.StoreId, payment.SupplierId })
            .HasPrincipalKey(supplier => new { supplier.StoreId, supplier.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(payment => payment.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => payment.OperationId).IsUnique();
        builder.HasIndex(payment => new { payment.StoreId, payment.CustomerId, payment.OccurredAt })
            .HasFilter("[CustomerId] IS NOT NULL");
        builder.HasIndex(payment => new { payment.StoreId, payment.SupplierId, payment.OccurredAt })
            .HasFilter("[SupplierId] IS NOT NULL");
        builder.HasIndex(payment => new { payment.StoreId, payment.Purpose, payment.OccurredAt });
        builder.HasIndex(payment => payment.PerformedByUserId);
    }
}
