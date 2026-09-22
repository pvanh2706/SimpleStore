using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Sales;
using SimpleStore.Domain.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales", table =>
            table.HasCheckConstraint("CK_Sales_TotalAmount", "[TotalAmount] >= 0"));
        builder.HasKey(sale => sale.Id);
        builder.HasAlternateKey(sale => new { sale.StoreId, sale.Id });
        builder.Property(sale => sale.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(sale => sale.TotalAmount).HasPrecision(18, 2);
        builder.Ignore(sale => sale.PaidAmount);
        builder.Ignore(sale => sale.OutstandingAmount);
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(sale => sale.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(sale => new { sale.StoreId, sale.WarehouseId })
            .HasPrincipalKey(warehouse => new { warehouse.StoreId, warehouse.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(sale => new { sale.StoreId, sale.CustomerId })
            .HasPrincipalKey(customer => new { customer.StoreId, customer.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(sale => sale.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(sale => sale.Lines)
            .WithOne()
            .HasForeignKey(line => new { line.StoreId, line.SaleId })
            .HasPrincipalKey(sale => new { sale.StoreId, sale.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(sale => sale.Payments)
            .WithOne()
            .HasForeignKey(payment => new { payment.StoreId, payment.SaleId })
            .HasPrincipalKey(sale => new { sale.StoreId, sale.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(sale => sale.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(sale => sale.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(sale => new { sale.StoreId, sale.CompletedAt });
        builder.HasIndex(sale => new { sale.StoreId, sale.CustomerId, sale.CompletedAt });
    }
}

public sealed class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines", table =>
        {
            table.HasCheckConstraint("CK_SaleLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_SaleLines_UnitSalePrice", "[UnitSalePrice] >= 0");
            table.HasCheckConstraint("CK_SaleLines_LineAmount", "[LineAmount] >= 0");
            table.HasCheckConstraint("CK_SaleLines_UnitCostAtSale", "[UnitCostAtSale] >= 0");
        });
        builder.HasKey(line => line.Id);
        builder.HasAlternateKey(line => new { line.StoreId, line.Id });
        builder.Property(line => line.ProductName).HasMaxLength(160).IsRequired();
        builder.Property(line => line.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(line => line.ProductUnit).HasMaxLength(32).IsRequired();
        builder.Property(line => line.Quantity).HasPrecision(18, 3);
        builder.Property(line => line.UnitSalePrice).HasPrecision(18, 2);
        builder.Property(line => line.LineAmount).HasPrecision(18, 2);
        builder.Property(line => line.UnitCostAtSale).HasPrecision(18, 4);
        builder.Property(line => line.CostReliability).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(line => new { line.StoreId, line.ProductId })
            .HasPrincipalKey(product => new { product.StoreId, product.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(line => new { line.SaleId, line.ProductId }).IsUnique();
        builder.HasIndex(line => new { line.StoreId, line.ProductId });
    }
}

public sealed class SalePaymentConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> builder)
    {
        builder.ToTable("SalePayments", table =>
            table.HasCheckConstraint("CK_SalePayments_Amount", "[Amount] > 0"));
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(payment => payment.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => new { payment.StoreId, payment.SaleId });
        builder.HasIndex(payment => payment.PerformedByUserId);
    }
}
