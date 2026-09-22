using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Domain.ProductImports;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Sales;
using SimpleStore.Domain.Returns;
using SimpleStore.Domain.Corrections;
using SimpleStore.Domain.Debts;
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

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleLine> SaleLines => Set<SaleLine>();

    public DbSet<SalePayment> SalePayments => Set<SalePayment>();

    public DbSet<NegativeStockSettingAudit> NegativeStockSettingAudits =>
        Set<NegativeStockSettingAudit>();

    public DbSet<CustomerReturn> Returns => Set<CustomerReturn>();
    public DbSet<ReturnLine> ReturnLines => Set<ReturnLine>();
    public DbSet<ReturnRefundPayment> ReturnRefundPayments => Set<ReturnRefundPayment>();
    public DbSet<SaleVoid> SaleVoids => Set<SaleVoid>();
    public DbSet<PurchaseVoid> PurchaseVoids => Set<PurchaseVoid>();
    public DbSet<PurchaseLineReversalBasis> PurchaseLineReversalBases => Set<PurchaseLineReversalBasis>();
    public DbSet<DebtPayment> DebtPayments => Set<DebtPayment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
