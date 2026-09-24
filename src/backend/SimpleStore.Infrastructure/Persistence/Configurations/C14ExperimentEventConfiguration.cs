using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Experiments;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class C14ExperimentEventConfiguration
    : IEntityTypeConfiguration<C14ExperimentEvent>
{
    public void Configure(EntityTypeBuilder<C14ExperimentEvent> builder)
    {
        builder.ToTable("C14ExperimentEvents", table =>
        {
            table.HasCheckConstraint(
                "CK_C14ExperimentEvents_Identity",
                "([EventType] = 'TodayOpened' AND [ProductId] IS NULL AND [AttentionKind] IS NULL) OR "
                + "([EventType] IN ('SignalShown', 'WhyOpened', 'PurchaseDraftStarted') "
                + "AND [ProductId] IS NOT NULL "
                + "AND [AttentionKind] IN ('NegativeStock', 'OutOfStock', 'LowStockRisk'))");
        });
        builder.HasKey(item => item.EventId);
        builder.Property(item => item.EventType)
            .HasMaxLength(C14ExperimentEvent.MaxEventTypeLength)
            .IsRequired();
        builder.Property(item => item.AttentionKind)
            .HasMaxLength(C14ExperimentEvent.MaxAttentionKindLength);
        builder.Property(item => item.BusinessDate).HasColumnType("date");
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(item => item.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => new { item.StoreId, item.ProductId })
            .HasPrincipalKey(product => new { product.StoreId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.StoreId, item.OccurredAt });
        builder.HasIndex(item => new { item.StoreId, item.EventType, item.OccurredAt });
    }
}
