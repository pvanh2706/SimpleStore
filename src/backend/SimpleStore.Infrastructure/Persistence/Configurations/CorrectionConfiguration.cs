using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Corrections;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Returns;
using SimpleStore.Domain.Sales;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class ReturnConfiguration : IEntityTypeConfiguration<CustomerReturn>
{
    public void Configure(EntityTypeBuilder<CustomerReturn> builder)
    {
        builder.ToTable("Returns", table =>
        {
            table.HasCheckConstraint("CK_Returns_TotalReturnAmount", "[TotalReturnAmount] >= 0");
            table.HasCheckConstraint("CK_Returns_RefundAmount", "[RefundAmount] >= 0 AND [RefundAmount] <= [TotalReturnAmount]");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.StoreId, item.Id });
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.TotalReturnAmount).HasPrecision(18, 2);
        builder.Property(item => item.RefundAmount).HasPrecision(18, 2);
        builder.HasOne<Store>().WithMany().HasForeignKey(item => item.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.OriginalSaleId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Lines).WithOne()
            .HasForeignKey(item => new { item.StoreId, item.ReturnId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.RefundPayments).WithOne()
            .HasForeignKey(item => new { item.StoreId, item.ReturnId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.RefundPayments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(item => new { item.StoreId, item.OriginalSaleId, item.CompletedAt });
    }
}

public sealed class ReturnLineConfiguration : IEntityTypeConfiguration<ReturnLine>
{
    public void Configure(EntityTypeBuilder<ReturnLine> builder)
    {
        builder.ToTable("ReturnLines", table =>
        {
            table.HasCheckConstraint("CK_ReturnLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_ReturnLines_Values", "[UnitSalePriceBasis] >= 0 AND [ReturnLineAmount] >= 0 AND [UnitCostBasis] >= 0 AND [RestockedInventoryValue] >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.StoreId, item.Id });
        builder.Property(item => item.Quantity).HasPrecision(18, 3);
        builder.Property(item => item.UnitSalePriceBasis).HasPrecision(18, 2);
        builder.Property(item => item.ReturnLineAmount).HasPrecision(18, 2);
        builder.Property(item => item.UnitCostBasis).HasPrecision(18, 4);
        builder.Property(item => item.RestockedInventoryValue).HasPrecision(18, 2);
        builder.HasOne<SaleLine>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.OriginalSaleLineId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.ProductId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.ReturnId, item.OriginalSaleLineId }).IsUnique();
        builder.HasIndex(item => new { item.StoreId, item.OriginalSaleLineId });
    }
}

public sealed class ReturnRefundPaymentConfiguration : IEntityTypeConfiguration<ReturnRefundPayment>
{
    public void Configure(EntityTypeBuilder<ReturnRefundPayment> builder)
    {
        builder.ToTable("ReturnRefundPayments", table =>
            table.HasCheckConstraint("CK_ReturnRefundPayments_Amount", "[Amount] > 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.Method).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.StoreId, item.ReturnId }).IsUnique();
    }
}

public sealed class SaleVoidConfiguration : IEntityTypeConfiguration<SaleVoid>
{
    public void Configure(EntityTypeBuilder<SaleVoid> builder)
    {
        builder.ToTable("SaleVoids");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.StoreId, item.Id });
        builder.Property(item => item.Reason).HasMaxLength(SaleVoid.MaxReasonLength).IsRequired();
        builder.HasOne<Store>().WithMany().HasForeignKey(item => item.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.OriginalSaleId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.VoidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.OriginalSaleId).IsUnique();
        builder.HasIndex(item => new { item.StoreId, item.OriginalSaleId });
    }
}

public sealed class PurchaseVoidConfiguration : IEntityTypeConfiguration<PurchaseVoid>
{
    public void Configure(EntityTypeBuilder<PurchaseVoid> builder)
    {
        builder.ToTable("PurchaseVoids");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.StoreId, item.Id });
        builder.Property(item => item.Reason).HasMaxLength(SaleVoid.MaxReasonLength).IsRequired();
        builder.HasOne<Store>().WithMany().HasForeignKey(item => item.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Purchase>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.OriginalPurchaseId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.VoidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.OriginalPurchaseId).IsUnique();
        builder.HasIndex(item => new { item.StoreId, item.OriginalPurchaseId });
    }
}

public sealed class PurchaseLineReversalBasisConfiguration : IEntityTypeConfiguration<PurchaseLineReversalBasis>
{
    public void Configure(EntityTypeBuilder<PurchaseLineReversalBasis> builder)
    {
        builder.ToTable("PurchaseLineReversalBases");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.QuantityBefore).HasPrecision(18, 3);
        builder.Property(item => item.InventoryValueBefore).HasPrecision(18, 2);
        builder.Property(item => item.AverageCostBefore).HasPrecision(18, 4);
        builder.Property(item => item.ReferencePurchaseCostBefore).HasPrecision(18, 2);
        builder.Property(item => item.ReferencePurchaseCostApplied).HasPrecision(18, 2);
        builder.HasOne<Purchase>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.PurchaseId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseLine>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.PurchaseLineId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.ProductId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.WarehouseId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryMovement>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.PurchaseMovementId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.PurchaseLineId).IsUnique();
        builder.HasIndex(item => new { item.StoreId, item.PurchaseId });
        builder.HasIndex(item => new { item.StoreId, item.PurchaseMovementId }).IsUnique();
    }
}
