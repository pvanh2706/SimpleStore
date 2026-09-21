using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.ProductImports;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class ProductImportConfiguration : IEntityTypeConfiguration<ProductImport>
{
    public void Configure(EntityTypeBuilder<ProductImport> builder)
    {
        builder.ToTable("ProductImports");
        builder.HasKey(productImport => productImport.Id);
        builder.Property(productImport => productImport.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(productImport => productImport.RowVersion).IsRowVersion();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(productImport => productImport.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(productImport => new { productImport.StoreId, productImport.CreatedAt });
        builder.HasMany(productImport => productImport.Rows)
            .WithOne()
            .HasForeignKey(row => row.ProductImportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(productImport => productImport.Rows)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductImportRowConfiguration : IEntityTypeConfiguration<ProductImportRow>
{
    public void Configure(EntityTypeBuilder<ProductImportRow> builder)
    {
        builder.ToTable("ProductImportRows");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Sku).HasMaxLength(64).IsRequired();
        builder.Property(row => row.Barcode).HasMaxLength(64);
        builder.Property(row => row.Name).HasMaxLength(160).IsRequired();
        builder.Property(row => row.Unit).HasMaxLength(32).IsRequired();
        builder.Property(row => row.SalePrice).HasPrecision(18, 2);
        builder.Property(row => row.OpeningCost).HasPrecision(18, 2);
        builder.Property(row => row.OpeningQuantity).HasPrecision(18, 3);
        builder.HasIndex(row => new { row.ProductImportId, row.RowNumber }).IsUnique();
    }
}
