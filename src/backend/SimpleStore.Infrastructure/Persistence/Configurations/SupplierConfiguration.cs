using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Stores;
using SimpleStore.Domain.Suppliers;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(supplier => supplier.Id);
        builder.HasAlternateKey(supplier => new { supplier.StoreId, supplier.Id });
        builder.Property(supplier => supplier.Name).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        builder.Property(supplier => supplier.Phone).HasMaxLength(Supplier.MaxPhoneLength);
        builder.Property(supplier => supplier.Note).HasMaxLength(Supplier.MaxNoteLength);
        builder.Property(supplier => supplier.RowVersion).IsRowVersion();
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(supplier => supplier.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(supplier => new { supplier.StoreId, supplier.Name });
        builder.HasIndex(supplier => new { supplier.StoreId, supplier.Phone });
    }
}
