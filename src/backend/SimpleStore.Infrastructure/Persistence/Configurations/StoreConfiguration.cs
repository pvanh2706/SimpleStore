using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");
        builder.HasKey(store => store.Id);
        builder.Property(store => store.Name).HasMaxLength(120).IsRequired();
        builder.Property(store => store.AllowNegativeStock).HasDefaultValue(false);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(store => store.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(store => store.OwnerUserId).IsUnique();
    }
}

public sealed class NegativeStockSettingAuditConfiguration
    : IEntityTypeConfiguration<NegativeStockSettingAudit>
{
    public void Configure(EntityTypeBuilder<NegativeStockSettingAudit> builder)
    {
        builder.ToTable("NegativeStockSettingAudits");
        builder.HasKey(audit => audit.Id);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(audit => audit.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(audit => audit.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(audit => new { audit.StoreId, audit.ChangedAt });
    }
}

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(warehouse => warehouse.Id);
        builder.HasAlternateKey(warehouse => new { warehouse.StoreId, warehouse.Id });
        builder.Property(warehouse => warehouse.Name).HasMaxLength(120).IsRequired();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(warehouse => warehouse.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(warehouse => warehouse.StoreId);
        builder.HasIndex(warehouse => warehouse.StoreId)
            .IsUnique()
            .HasFilter("[IsMain] = 1");
    }
}
