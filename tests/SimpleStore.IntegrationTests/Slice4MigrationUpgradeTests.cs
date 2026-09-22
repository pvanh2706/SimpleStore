using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Corrections;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Inventory;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice4MigrationUpgradeTests
{
    private const string Slice3TerminalMigration = "20260921163650_CompleteSaleTimestamps";

    [Fact]
    public async Task ExistingSlice3DatabaseUpgradesWithoutInventingPurchaseReversalEvidence()
    {
        var connectionString = CustomWebApplicationFactory.CreateIsolatedConnectionString(
            "SimpleStoreMigrationTests");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        await using var db = new ApplicationDbContext(options);
        try
        {
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(Slice3TerminalMigration);

            var ids = await SeedSlice3DataAsync(db);
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();

            var product = await db.Products.SingleAsync(item => item.Id == ids.ProductId);
            Assert.Equal(20m, product.ReferencePurchaseCost);
            Assert.Equal(0, product.ReferencePurchaseCostRevision);
            Assert.Equal(1, await db.Purchases.CountAsync(item => item.Id == ids.PurchaseId));
            Assert.Equal(1, await db.PurchaseLines.CountAsync(item => item.Id == ids.PurchaseLineId));
            Assert.Equal(1, await db.PurchasePayments.CountAsync(item => item.PurchaseId == ids.PurchaseId));
            Assert.Equal(1, await db.Sales.CountAsync(item => item.Id == ids.SaleId));
            Assert.Equal(1, await db.SaleLines.CountAsync(item => item.Id == ids.SaleLineId));
            Assert.Equal(1, await db.SalePayments.CountAsync(item => item.SaleId == ids.SaleId));
            Assert.Equal(0, await db.PurchaseLineReversalBases.CountAsync());

            var legacySequences = await db.InventoryMovements
                .Where(item => ids.MovementIds.Contains(item.Id))
                .Select(item => item.LedgerSequence)
                .ToArrayAsync();
            Assert.Equal(ids.MovementIds.Length, legacySequences.Length);
            Assert.All(legacySequences, sequence => Assert.True(sequence > 0));
            Assert.Equal(legacySequences.Length, legacySequences.Distinct().Count());

            var laterMovement = InventoryMovement.CreateOpeningBalance(
                ids.StoreId,
                ids.WarehouseId,
                ids.ProductId,
                OpeningInventory.Create(1, 1),
                "MigrationUpgradeTest",
                Guid.NewGuid(),
                ids.UserId,
                DateTimeOffset.UtcNow);
            db.InventoryMovements.Add(laterMovement);
            await db.SaveChangesAsync();
            Assert.True(laterMovement.LedgerSequence > legacySequences.Max());

            var useCase = new VoidPurchaseUseCase(
                new MigrationTestCurrentUser(ids.UserId),
                new Slice1Repository(db),
                new Slice4Repository(db),
                new Slice5Repository(db),
                TimeProvider.System);
            var exception = await Assert.ThrowsAsync<ApplicationConflictException>(() =>
                useCase.ExecuteAsync(
                    ids.PurchaseId,
                    new VoidTransactionCommand(Guid.NewGuid(), "Legacy purchase correction"),
                    CancellationToken.None));
            Assert.Equal("purchase-void-reversal-basis-unavailable", exception.Code);
            Assert.Equal(0, await db.PurchaseVoids.CountAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<Slice3SeedIds> SeedSlice3DataAsync(ApplicationDbContext db)
    {
        var now = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var ids = new Slice3SeedIds(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(),
            [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [AspNetUsers]
                ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
                 [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [StoreId])
            VALUES
                ({ids.UserId}, {"migration-owner@example.test"}, {"MIGRATION-OWNER@EXAMPLE.TEST"},
                 {"migration-owner@example.test"}, {"MIGRATION-OWNER@EXAMPLE.TEST"}, {true},
                 {false}, {false}, {true}, {0}, NULL);

            INSERT INTO [Stores] ([Id], [OwnerUserId], [Name], [CreatedAt], [AllowNegativeStock])
            VALUES ({ids.StoreId}, {ids.UserId}, {"Migration Store"}, {now}, {false});

            UPDATE [AspNetUsers] SET [StoreId] = {ids.StoreId} WHERE [Id] = {ids.UserId};

            INSERT INTO [Warehouses] ([Id], [StoreId], [Name], [IsMain], [CreatedAt])
            VALUES ({ids.WarehouseId}, {ids.StoreId}, {"Main Warehouse"}, {true}, {now});

            INSERT INTO [Products]
                ([Id], [StoreId], [Sku], [NormalizedSku], [Name], [Unit], [SalePrice],
                 [ReferencePurchaseCost], [IsActive], [CreatedAt], [UpdatedAt])
            VALUES
                ({ids.ProductId}, {ids.StoreId}, {"LEGACY-001"}, {"LEGACY-001"}, {"Legacy Product"},
                 {"item"}, {12m}, {20m}, {true}, {now}, {now});

            INSERT INTO [InventoryBalances]
                ([Id], [StoreId], [WarehouseId], [ProductId], [QuantityOnHand], [InventoryValue],
                 [AverageCost], [UpdatedAt], [HasAverageCost])
            VALUES
                ({ids.BalanceId}, {ids.StoreId}, {ids.WarehouseId}, {ids.ProductId},
                 {10m}, {110m}, {11m}, {now}, {true});

            INSERT INTO [Suppliers]
                ([Id], [StoreId], [Name], [IsActive], [CreatedAt], [UpdatedAt])
            VALUES
                ({ids.SupplierId}, {ids.StoreId}, {"Legacy Supplier"}, {true}, {now}, {now});

            INSERT INTO [Purchases]
                ([Id], [StoreId], [SupplierId], [Status], [CreatedByUserId], [CreatedAt], [UpdatedAt],
                 [CompletedAt], [TotalAmount])
            VALUES
                ({ids.PurchaseId}, {ids.StoreId}, {ids.SupplierId}, {"Completed"}, {ids.UserId}, {now},
                 {now}, {now}, {20m});

            INSERT INTO [PurchaseLines]
                ([Id], [StoreId], [PurchaseId], [ProductId], [Quantity], [UnitPrice], [LineAmount])
            VALUES
                ({ids.PurchaseLineId}, {ids.StoreId}, {ids.PurchaseId}, {ids.ProductId}, {1m}, {20m}, {20m});

            INSERT INTO [PurchasePayments]
                ([Id], [StoreId], [PurchaseId], [Amount], [Method], [PaidAt], [PerformedByUserId])
            VALUES
                ({ids.PurchasePaymentId}, {ids.StoreId}, {ids.PurchaseId}, {10m}, {"Cash"}, {now}, {ids.UserId});

            INSERT INTO [Sales]
                ([Id], [StoreId], [WarehouseId], [CustomerId], [CompletedByUserId], [Status], [CompletedAt],
                 [TotalAmount], [CreatedAt])
            VALUES
                ({ids.SaleId}, {ids.StoreId}, {ids.WarehouseId}, NULL, {ids.UserId}, {"Completed"}, {now},
                 {12m}, {now});

            INSERT INTO [SaleLines]
                ([Id], [StoreId], [SaleId], [ProductId], [ProductName], [ProductSku], [ProductUnit],
                 [Quantity], [UnitSalePrice], [LineAmount], [UnitCostAtSale], [CostReliability])
            VALUES
                ({ids.SaleLineId}, {ids.StoreId}, {ids.SaleId}, {ids.ProductId}, {"Legacy Product"},
                 {"LEGACY-001"}, {"item"}, {1m}, {12m}, {12m}, {10m}, {"Reliable"});

            INSERT INTO [SalePayments]
                ([Id], [StoreId], [SaleId], [Amount], [Method], [OccurredAt], [PerformedByUserId])
            VALUES
                ({ids.SalePaymentId}, {ids.StoreId}, {ids.SaleId}, {12m}, {"Cash"}, {now}, {ids.UserId});

            INSERT INTO [InventoryMovements]
                ([Id], [StoreId], [WarehouseId], [ProductId], [QuantityDelta], [InventoryValueDelta],
                 [UnitCost], [MovementType], [SourceType], [SourceId], [PerformedByUserId], [OccurredAt])
            VALUES
                ({ids.MovementIds[0]}, {ids.StoreId}, {ids.WarehouseId}, {ids.ProductId}, {10m}, {100m},
                 {10m}, {"OpeningBalance"}, {"Product"}, {ids.ProductId}, {ids.UserId}, {now}),
                ({ids.MovementIds[1]}, {ids.StoreId}, {ids.WarehouseId}, {ids.ProductId}, {1m}, {20m},
                 {20m}, {"Purchase"}, {"PurchaseLine"}, {ids.PurchaseLineId}, {ids.UserId}, {now.AddMinutes(1)}),
                ({ids.MovementIds[2]}, {ids.StoreId}, {ids.WarehouseId}, {ids.ProductId}, {-1m}, {-10m},
                 {10m}, {"Sale"}, {"SaleLine"}, {ids.SaleLineId}, {ids.UserId}, {now.AddMinutes(2)});

            INSERT INTO [BusinessOperations]
                ([OperationId], [StoreId], [OperationType], [RequestFingerprint], [Status],
                 [ResultReference], [CreatedAt], [CompletedAt])
            VALUES
                ({Guid.NewGuid()}, {ids.StoreId}, {"CompletePurchase"},
                 {new string('A', 64)}, {"Completed"}, {ids.PurchaseId}, {now}, {now});
            """);

        return ids;
    }

    private sealed record Slice3SeedIds(
        Guid UserId,
        Guid StoreId,
        Guid WarehouseId,
        Guid ProductId,
        Guid BalanceId,
        Guid SupplierId,
        Guid PurchaseId,
        Guid PurchaseLineId,
        Guid PurchasePaymentId,
        Guid SaleId,
        Guid SaleLineId,
        Guid SalePaymentId,
        Guid[] MovementIds);

    private sealed record MigrationTestCurrentUser(Guid UserId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
    }
}
