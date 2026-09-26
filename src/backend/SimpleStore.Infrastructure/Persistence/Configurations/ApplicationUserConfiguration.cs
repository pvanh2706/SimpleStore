using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.IsEnabled).HasDefaultValue(true);
        builder.Property(user => user.MustChangePassword).HasDefaultValue(false);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(user => user.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(user => user.StoreId);
    }
}
