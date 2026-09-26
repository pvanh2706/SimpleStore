using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.StoreId, item.Id });
        builder.Property(item => item.QuantityDelta).HasPrecision(18, 3);
        builder.Property(item => item.AdjustmentUnitCost).HasPrecision(18, 4);
        builder.Property(item => item.EffectiveUnitCost).HasPrecision(18, 4);
        builder.Property(item => item.InventoryValueDelta).HasPrecision(18, 2);
        builder.Property(item => item.QuantityBefore).HasPrecision(18, 3);
        builder.Property(item => item.QuantityAfter).HasPrecision(18, 3);
        builder.Property(item => item.InventoryValueBefore).HasPrecision(18, 2);
        builder.Property(item => item.InventoryValueAfter).HasPrecision(18, 2);
        builder.Property(item => item.AverageCostAfter).HasPrecision(18, 4);
        builder.Property(item => item.CostReliability).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.Reason).HasMaxLength(StockAdjustment.MaxReasonLength).IsRequired();
        ConfigureScope(builder);
        builder.HasIndex(item => new { item.StoreId, item.OperationId }).IsUnique();
        builder.HasIndex(item => new { item.StoreId, item.ProductId, item.OccurredAt });
        builder.HasIndex(item => item.InventoryMovementId).IsUnique();
    }

    private static void ConfigureScope(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.HasOne<Store>().WithMany().HasForeignKey(item => item.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.WarehouseId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.ProductId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(item => item.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryMovement>().WithMany()
            .HasForeignKey(item => item.InventoryMovementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StocktakeResultConfiguration : IEntityTypeConfiguration<StocktakeResult>
{
    public void Configure(EntityTypeBuilder<StocktakeResult> builder)
    {
        builder.ToTable("StocktakeResults");
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.StoreId, item.Id });
        builder.Property(item => item.ExpectedQuantity).HasPrecision(18, 3);
        builder.Property(item => item.ExpectedBalanceRowVersion).HasMaxLength(8).IsRequired();
        builder.Property(item => item.CountedQuantity).HasPrecision(18, 3);
        builder.Property(item => item.Difference).HasPrecision(18, 3);
        builder.Property(item => item.AdjustmentUnitCost).HasPrecision(18, 4);
        builder.Property(item => item.EffectiveUnitCost).HasPrecision(18, 4);
        builder.Property(item => item.InventoryValueDelta).HasPrecision(18, 2);
        builder.Property(item => item.InventoryValueBefore).HasPrecision(18, 2);
        builder.Property(item => item.InventoryValueAfter).HasPrecision(18, 2);
        builder.Property(item => item.AverageCostAfter).HasPrecision(18, 4);
        builder.Property(item => item.CostReliability).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.Note).HasMaxLength(StocktakeResult.MaxNoteLength);
        builder.HasOne<Store>().WithMany().HasForeignKey(item => item.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.WarehouseId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany()
            .HasForeignKey(item => new { item.StoreId, item.ProductId })
            .HasPrincipalKey(item => new { item.StoreId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(item => item.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryMovement>().WithMany()
            .HasForeignKey(item => item.InventoryMovementId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.StoreId, item.OperationId }).IsUnique();
        builder.HasIndex(item => new { item.StoreId, item.ProductId, item.OccurredAt });
        builder.HasIndex(item => item.InventoryMovementId).IsUnique().HasFilter("[InventoryMovementId] IS NOT NULL");
    }
}
