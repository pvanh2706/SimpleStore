using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class PilotReadinessMigrationUpgradeTests
{
    private const string ApprovedBaselineMigration = "20260924010903_ImplementSlice6Stage6BC14";

    [Fact]
    public async Task ApprovedBaselineDataUpgradesWithoutChangingIdentityOrInventoryState()
    {
        var connectionString = CustomWebApplicationFactory.CreateIsolatedConnectionString(
            "SimpleStorePrAMigrationTests");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(ApprovedBaselineMigration);
            var userId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var balanceId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 9, 26, 2, 0, 0, TimeSpan.Zero);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [AspNetUsers]
                    ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
                     [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [StoreId])
                VALUES
                    ({userId}, {"existing-owner@example.test"}, {"EXISTING-OWNER@EXAMPLE.TEST"},
                     {"existing-owner@example.test"}, {"EXISTING-OWNER@EXAMPLE.TEST"}, {true},
                     {false}, {false}, {true}, {0}, NULL);

                INSERT INTO [Stores]
                    ([Id], [OwnerUserId], [Name], [CreatedAt], [AllowNegativeStock], [TimeZoneId])
                VALUES
                    ({storeId}, {userId}, {"Existing Store"}, {now}, {false}, {"Asia/Ho_Chi_Minh"});

                UPDATE [AspNetUsers] SET [StoreId] = {storeId} WHERE [Id] = {userId};

                INSERT INTO [Warehouses] ([Id], [StoreId], [Name], [IsMain], [CreatedAt])
                VALUES ({warehouseId}, {storeId}, {"Main Warehouse"}, {true}, {now});

                INSERT INTO [Products]
                    ([Id], [StoreId], [Sku], [NormalizedSku], [Name], [Unit], [SalePrice],
                     [ReferencePurchaseCost], [ReferencePurchaseCostRevision], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                    ({productId}, {storeId}, {"PRA-EXISTING"}, {"PRA-EXISTING"}, {"Existing product"},
                     {"item"}, {100m}, {25m}, {1L}, {true}, {now}, {now});

                INSERT INTO [InventoryBalances]
                    ([Id], [StoreId], [WarehouseId], [ProductId], [QuantityOnHand], [InventoryValue],
                     [AverageCost], [UpdatedAt], [HasAverageCost])
                VALUES
                    ({balanceId}, {storeId}, {warehouseId}, {productId}, {8m}, {200m}, {25m}, {now}, {true});
                """);

            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();

            var user = await db.Users.SingleAsync(item => item.Id == userId);
            Assert.True(user.IsEnabled);
            Assert.False(user.MustChangePassword);
            Assert.Null(user.DisabledAt);
            var balance = await db.InventoryBalances.SingleAsync(item => item.Id == balanceId);
            Assert.Equal(8, balance.QuantityOnHand);
            Assert.Equal(200, balance.InventoryValue);
            Assert.Equal(25, balance.AverageCost);
            Assert.True(balance.HasAverageCost);
            Assert.Empty(await db.AccountLifecycleAudits.ToArrayAsync());
            Assert.Empty(await db.StockAdjustments.ToArrayAsync());
            Assert.Empty(await db.StocktakeResults.ToArrayAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
