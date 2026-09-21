using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Products;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Sales;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class DatabaseIntegrityTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task InventoryBalanceRejectsCrossStoreProductAndWarehouse()
    {
        var seed = await SeedTwoStoresAsync();
        var openingInventory = OpeningInventory.Create(0, null);

        await AssertForeignKeyRejectedAsync(InventoryBalance.Create(
            seed.StoreA.Id,
            seed.StoreA.MainWarehouseId,
            seed.ProductB.Id,
            openingInventory,
            DateTimeOffset.UtcNow));
        await AssertForeignKeyRejectedAsync(InventoryBalance.Create(
            seed.StoreA.Id,
            seed.StoreB.MainWarehouseId,
            seed.ProductA.Id,
            openingInventory,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task InventoryMovementRejectsCrossStoreProductAndWarehouse()
    {
        var seed = await SeedTwoStoresAsync();
        var openingInventory = OpeningInventory.Create(1, 1);

        await AssertForeignKeyRejectedAsync(InventoryMovement.CreateOpeningBalance(
            seed.StoreA.Id,
            seed.StoreA.MainWarehouseId,
            seed.ProductB.Id,
            openingInventory,
            "IntegrityTest",
            Guid.NewGuid(),
            seed.OwnerAId,
            DateTimeOffset.UtcNow));
        await AssertForeignKeyRejectedAsync(InventoryMovement.CreateOpeningBalance(
            seed.StoreA.Id,
            seed.StoreB.MainWarehouseId,
            seed.ProductA.Id,
            openingInventory,
            "IntegrityTest",
            Guid.NewGuid(),
            seed.OwnerAId,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task PurchaseRejectsCrossStoreSupplierAndProduct()
    {
        var seed = await SeedTwoStoresAsync();
        var credentialsA = await factory.CreateOwnerAsync();
        using var clientA = factory.CreateHttpsClient();
        await clientA.LoginAsync(credentialsA.Email, credentialsA.Password);
        var storeA = await clientA.InitializeStoreAsync($"Purchase integrity A {Guid.NewGuid():N}");
        var supplierA = await clientA.CreateSupplierAsync("Supplier A");
        var productA = await clientA.CreateProductAsync(name: "Purchase product A");
        var ownerAId = await factory.WithDbContextAsync(dbContext => dbContext.Stores
            .Where(store => store.Id == storeA.Id)
            .Select(store => store.OwnerUserId)
            .SingleAsync());

        var credentialsB = await factory.CreateOwnerAsync();
        using var clientB = factory.CreateHttpsClient();
        await clientB.LoginAsync(credentialsB.Email, credentialsB.Password);
        _ = await clientB.InitializeStoreAsync($"Purchase integrity B {Guid.NewGuid():N}");
        var supplierB = await clientB.CreateSupplierAsync("Supplier B");
        var productB = seed.ProductB;

        await AssertForeignKeyRejectedAsync(Purchase.CreateDraft(
            storeA.Id,
            supplierB.Id,
            ownerAId,
            [new PurchaseLineInput(productA.Id, 1, 10)],
            DateTimeOffset.UtcNow));
        await AssertForeignKeyRejectedAsync(Purchase.CreateDraft(
            storeA.Id,
            supplierA.Id,
            ownerAId,
            [new PurchaseLineInput(productB.Id, 1, 10)],
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task SaleRejectsCrossStoreProductCustomerAndWarehouse()
    {
        var seed = await SeedTwoStoresAsync();
        var customerB = Customer.Create(
            seed.StoreB.Id,
            "Store B customer",
            null,
            DateTimeOffset.UtcNow);
        await factory.WithDbContextAsync(async dbContext =>
        {
            dbContext.Customers.Add(customerB);
            await dbContext.SaveChangesAsync();
            return true;
        });

        await AssertForeignKeyRejectedAsync(CreateSale(
            seed.StoreA.Id,
            seed.StoreA.MainWarehouseId,
            null,
            seed.OwnerAId,
            seed.ProductB.Id));
        await AssertForeignKeyRejectedAsync(CreateSale(
            seed.StoreA.Id,
            seed.StoreA.MainWarehouseId,
            customerB.Id,
            seed.OwnerAId,
            seed.ProductA.Id,
            paid: false));
        await AssertForeignKeyRejectedAsync(CreateSale(
            seed.StoreA.Id,
            seed.StoreB.MainWarehouseId,
            null,
            seed.OwnerAId,
            seed.ProductA.Id));
    }

    private async Task AssertForeignKeyRejectedAsync(object entity)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            factory.WithDbContextAsync(async dbContext =>
            {
                dbContext.Add(entity);
                await dbContext.SaveChangesAsync();
                return true;
            }));

        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Equal(547, sqlException.Number);
    }

    private async Task<IntegritySeed> SeedTwoStoresAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var credentialsA = await factory.CreateOwnerAsync();
        var credentialsB = await factory.CreateOwnerAsync();
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();

        await clientA.LoginAsync(credentialsA.Email, credentialsA.Password);
        await clientB.LoginAsync(credentialsB.Email, credentialsB.Password);
        var storeA = await clientA.InitializeStoreAsync($"Integrity A {suffix}");
        var storeB = await clientB.InitializeStoreAsync($"Integrity B {suffix}");
        var productA = await clientA.CreateProductAsync($"INT-A-{suffix}");
        var productB = await clientB.CreateProductAsync($"INT-B-{suffix}");
        var ownerAId = await factory.WithDbContextAsync(dbContext =>
            dbContext.Stores
                .Where(store => store.Id == storeA.Id)
                .Select(store => store.OwnerUserId)
                .SingleAsync());

        return new IntegritySeed(storeA, storeB, productA, productB, ownerAId);
    }

    private static Sale CreateSale(
        Guid storeId,
        Guid warehouseId,
        Guid? customerId,
        Guid userId,
        Guid productId,
        bool paid = true) =>
        Sale.Complete(
            storeId,
            warehouseId,
            customerId,
            userId,
            [new SaleLineInput(
                productId,
                "Product",
                "SKU",
                "unit",
                1,
                10,
                5,
                CostReliability.Reliable)],
            paid ? [new SalePaymentInput(10, PaymentMethod.Cash)] : [],
            DateTimeOffset.UtcNow);

    private sealed record IntegritySeed(
        StoreResult StoreA,
        StoreResult StoreB,
        ProductResult ProductA,
        ProductResult ProductB,
        Guid OwnerAId);
}
