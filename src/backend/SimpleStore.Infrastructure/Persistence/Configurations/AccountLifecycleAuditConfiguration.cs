using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class AccountLifecycleAuditConfiguration : IEntityTypeConfiguration<AccountLifecycleAudit>
{
    public void Configure(EntityTypeBuilder<AccountLifecycleAudit> builder)
    {
        builder.ToTable("AccountLifecycleAudits");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Action).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(item => item.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.StoreId, item.TargetUserId, item.OccurredAt });
    }
}
