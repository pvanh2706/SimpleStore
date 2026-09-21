using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class BusinessOperationConfiguration : IEntityTypeConfiguration<BusinessOperation>
{
    public void Configure(EntityTypeBuilder<BusinessOperation> builder)
    {
        builder.ToTable("BusinessOperations");
        builder.HasKey(operation => operation.OperationId);
        builder.Property(operation => operation.OperationType)
            .HasMaxLength(BusinessOperation.MaxOperationTypeLength)
            .IsRequired();
        builder.Property(operation => operation.RequestFingerprint)
            .HasMaxLength(BusinessOperation.FingerprintLength)
            .IsFixedLength()
            .IsRequired();
        builder.Property(operation => operation.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(operation => operation.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(operation => new { operation.StoreId, operation.OperationId }).IsUnique();
        builder.HasIndex(operation => new { operation.StoreId, operation.OperationType, operation.CreatedAt });
    }
}
