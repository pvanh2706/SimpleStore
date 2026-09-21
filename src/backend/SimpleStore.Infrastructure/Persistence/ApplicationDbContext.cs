using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Domain.ProductImports;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Stores;
using SimpleStore.Domain.Suppliers;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();

    public DbSet<ProductImport> ProductImports => Set<ProductImport>();

    public DbSet<ProductImportRow> ProductImportRows => Set<ProductImportRow>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();

    public DbSet<PurchasePayment> PurchasePayments => Set<PurchasePayment>();

    public DbSet<BusinessOperation> BusinessOperations => Set<BusinessOperation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
