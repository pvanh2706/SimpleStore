using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Products;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Inventory;
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

    private sealed record IntegritySeed(
        StoreResult StoreA,
        StoreResult StoreB,
        ProductResult ProductA,
        ProductResult ProductB,
        Guid OwnerAId);
}
