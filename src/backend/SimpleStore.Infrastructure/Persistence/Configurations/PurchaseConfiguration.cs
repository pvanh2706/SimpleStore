using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Stores;
using SimpleStore.Domain.Suppliers;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases", table =>
            table.HasCheckConstraint("CK_Purchases_TotalAmount", "[TotalAmount] >= 0"));
        builder.HasKey(purchase => purchase.Id);
        builder.HasAlternateKey(purchase => new { purchase.StoreId, purchase.Id });
        builder.Property(purchase => purchase.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(purchase => purchase.TotalAmount).HasPrecision(18, 2);
        builder.Property(purchase => purchase.RowVersion).IsRowVersion();
        builder.Ignore(purchase => purchase.PaidAmount);
        builder.Ignore(purchase => purchase.OutstandingAmount);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(purchase => purchase.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(purchase => new { purchase.StoreId, purchase.SupplierId })
            .HasPrincipalKey(supplier => new { supplier.StoreId, supplier.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(purchase => purchase.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(purchase => purchase.Lines)
            .WithOne()
            .HasForeignKey(line => new { line.StoreId, line.PurchaseId })
            .HasPrincipalKey(purchase => new { purchase.StoreId, purchase.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(purchase => purchase.Payments)
            .WithOne()
            .HasForeignKey(payment => new { payment.StoreId, payment.PurchaseId })
            .HasPrincipalKey(purchase => new { purchase.StoreId, purchase.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(purchase => purchase.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(purchase => purchase.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(purchase => new { purchase.StoreId, purchase.Status, purchase.CreatedAt });
        builder.HasIndex(purchase => new { purchase.StoreId, purchase.SupplierId, purchase.Status });
    }
}

public sealed class PurchaseLineConfiguration : IEntityTypeConfiguration<PurchaseLine>
{
    public void Configure(EntityTypeBuilder<PurchaseLine> builder)
    {
        builder.ToTable("PurchaseLines", table =>
        {
            table.HasCheckConstraint("CK_PurchaseLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_PurchaseLines_UnitPrice", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_PurchaseLines_LineAmount", "[LineAmount] >= 0");
        });
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Quantity).HasPrecision(18, 3);
        builder.Property(line => line.UnitPrice).HasPrecision(18, 2);
        builder.Property(line => line.LineAmount).HasPrecision(18, 2);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(line => new { line.StoreId, line.ProductId })
            .HasPrincipalKey(product => new { product.StoreId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(line => new { line.PurchaseId, line.ProductId }).IsUnique();
        builder.HasIndex(line => new { line.StoreId, line.ProductId });
    }
}

public sealed class PurchasePaymentConfiguration : IEntityTypeConfiguration<PurchasePayment>
{
    public void Configure(EntityTypeBuilder<PurchasePayment> builder)
    {
        builder.ToTable("PurchasePayments", table =>
            table.HasCheckConstraint("CK_PurchasePayments_Amount", "[Amount] > 0"));
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(payment => payment.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => new { payment.StoreId, payment.PurchaseId });
        builder.HasIndex(payment => payment.PerformedByUserId);
    }
}
