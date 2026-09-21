using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);
        builder.HasAlternateKey(customer => new { customer.StoreId, customer.Id });
        builder.Property(customer => customer.Name).HasMaxLength(Customer.MaxNameLength).IsRequired();
        builder.Property(customer => customer.Phone).HasMaxLength(Customer.MaxPhoneLength);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(customer => customer.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(customer => new { customer.StoreId, customer.Name });
        builder.HasIndex(customer => new { customer.StoreId, customer.Phone });
    }
}
