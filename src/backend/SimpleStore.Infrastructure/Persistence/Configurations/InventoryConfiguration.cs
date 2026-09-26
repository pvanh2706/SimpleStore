using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances");
        builder.HasKey(balance => balance.Id);
        builder.Property(balance => balance.QuantityOnHand).HasPrecision(18, 3);
        builder.Property(balance => balance.InventoryValue).HasPrecision(18, 2);
        builder.Property(balance => balance.AverageCost).HasPrecision(18, 4);
        builder.Property(balance => balance.HasAverageCost).HasDefaultValue(false);
        builder.Property(balance => balance.RowVersion).IsRowVersion();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(balance => balance.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(balance => new { balance.StoreId, balance.WarehouseId })
            .HasPrincipalKey(warehouse => new { warehouse.StoreId, warehouse.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(balance => new { balance.StoreId, balance.ProductId })
            .HasPrincipalKey(product => new { product.StoreId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(balance => new { balance.StoreId, balance.WarehouseId, balance.ProductId })
            .IsUnique();
    }
}

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(movement => movement.Id);
        builder.HasAlternateKey(movement => new { movement.StoreId, movement.Id });
        builder.Property(movement => movement.LedgerSequence).UseIdentityColumn();
        builder.Property(movement => movement.QuantityDelta).HasPrecision(18, 3);
        builder.Property(movement => movement.InventoryValueDelta).HasPrecision(18, 2);
        builder.Property(movement => movement.UnitCost).HasPrecision(18, 4);
        builder.Property(movement => movement.CostReliability).HasConversion<string>().HasMaxLength(32);
        builder.Property(movement => movement.Reason).HasMaxLength(StockAdjustment.MaxReasonLength);
        builder.Property(movement => movement.StocktakeExpectedQuantity).HasPrecision(18, 3);
        builder.Property(movement => movement.StocktakeCountedQuantity).HasPrecision(18, 3);
        builder.Property(movement => movement.MovementType).HasConversion<string>().HasMaxLength(32);
        builder.Property(movement => movement.SourceType).HasMaxLength(64).IsRequired();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(movement => movement.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(movement => new { movement.StoreId, movement.WarehouseId })
            .HasPrincipalKey(warehouse => new { warehouse.StoreId, warehouse.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(movement => new { movement.StoreId, movement.ProductId })
            .HasPrincipalKey(product => new { product.StoreId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(movement => movement.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(movement => new { movement.StoreId, movement.ProductId, movement.OccurredAt });
        builder.HasIndex(movement => new
        {
            movement.StoreId,
            movement.WarehouseId,
            movement.ProductId,
            movement.LedgerSequence
        });
        builder.HasIndex(movement => new { movement.SourceType, movement.SourceId });
    }
}
