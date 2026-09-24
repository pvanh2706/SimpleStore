using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SimpleStore.Domain.Experiments;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice6MigrationUpgradeTests
{
    private const string Slice6ABaselineMigration =
        "20260922151651_ImplementSlice5Stage5ADebtBackend";

    [Fact]
    public async Task ExistingSlice6ADataUpgradesWithOnlyC14ExperimentEventAddition()
    {
        var connectionString = CustomWebApplicationFactory.CreateIsolatedConnectionString(
            "SimpleStoreSlice6MigrationTests");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(Slice6ABaselineMigration);
            var userId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 9, 23, 3, 0, 0, TimeSpan.Zero);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [AspNetUsers]
                    ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
                     [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [StoreId])
                VALUES
                    ({userId}, {"slice6-owner@example.test"}, {"SLICE6-OWNER@EXAMPLE.TEST"},
                     {"slice6-owner@example.test"}, {"SLICE6-OWNER@EXAMPLE.TEST"}, {true},
                     {false}, {false}, {true}, {0}, NULL);

                INSERT INTO [Stores]
                    ([Id], [OwnerUserId], [Name], [CreatedAt], [AllowNegativeStock], [TimeZoneId])
                VALUES
                    ({storeId}, {userId}, {"Existing Slice 6A Store"}, {now}, {false}, {"Asia/Ho_Chi_Minh"});

                UPDATE [AspNetUsers] SET [StoreId] = {storeId} WHERE [Id] = {userId};

                INSERT INTO [Products]
                    ([Id], [StoreId], [Sku], [NormalizedSku], [Name], [Unit], [SalePrice],
                     [ReferencePurchaseCost], [ReferencePurchaseCostRevision], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                    ({productId}, {storeId}, {"S6A-EXISTING"}, {"S6A-EXISTING"}, {"Existing product"},
                     {"item"}, {100m}, {60m}, {1L}, {true}, {now}, {now});
                """);

            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();

            Assert.Equal("Existing Slice 6A Store",
                await db.Stores.Where(item => item.Id == storeId).Select(item => item.Name).SingleAsync());
            Assert.Equal("Existing product",
                await db.Products.Where(item => item.Id == productId).Select(item => item.Name).SingleAsync());
            Assert.Empty(await db.C14ExperimentEvents.ToArrayAsync());

            db.C14ExperimentEvents.Add(C14ExperimentEvent.Create(
                Guid.NewGuid(), storeId, userId,
                C14ExperimentEventTypes.SignalShown,
                productId, C14AttentionKinds.LowStockRisk,
                new DateOnly(2026, 9, 23), now));
            await db.SaveChangesAsync();
            Assert.Equal(1, await db.C14ExperimentEvents.CountAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
