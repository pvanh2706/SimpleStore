using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_SalePrice", "[SalePrice] >= 0");
            table.HasCheckConstraint(
                "CK_Products_ReferencePurchaseCost",
                "[ReferencePurchaseCost] IS NULL OR [ReferencePurchaseCost] >= 0");
        });
        builder.HasKey(product => product.Id);
        builder.HasAlternateKey(product => new { product.StoreId, product.Id });
        builder.Property(product => product.Sku).HasMaxLength(Product.MaxSkuLength).IsRequired();
        builder.Property(product => product.NormalizedSku).HasMaxLength(Product.MaxSkuLength).IsRequired();
        builder.Property(product => product.Barcode).HasMaxLength(Product.MaxBarcodeLength);
        builder.Property(product => product.NormalizedBarcode).HasMaxLength(Product.MaxBarcodeLength);
        builder.Property(product => product.Name).HasMaxLength(Product.MaxNameLength).IsRequired();
        builder.Property(product => product.Unit).HasMaxLength(Product.MaxUnitLength).IsRequired();
        builder.Property(product => product.SalePrice).HasPrecision(18, 2);
        builder.Property(product => product.ReferencePurchaseCost).HasPrecision(18, 2);
        builder.Property(product => product.ReferencePurchaseCostRevision).HasDefaultValue(0L);
        builder.Property(product => product.RowVersion).IsRowVersion();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(product => product.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(product => new { product.StoreId, product.NormalizedSku }).IsUnique();
        builder.HasIndex(product => new { product.StoreId, product.NormalizedBarcode })
            .IsUnique()
            .HasFilter("[NormalizedBarcode] IS NOT NULL");
        builder.HasIndex(product => new { product.StoreId, product.Name });
    }
}
