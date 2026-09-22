using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice5MigrationUpgradeTests
{
    private const string Slice4TerminalMigration = "20260922021501_ImplementSlice4BackendCorrections";

    [Fact]
    public async Task ExistingSlice4StoreAndBusinessDataUpgradeWithCanonicalTimezone()
    {
        var connectionString = CustomWebApplicationFactory.CreateIsolatedConnectionString(
            "SimpleStoreSlice5MigrationTests");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            var migrator = db.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(Slice4TerminalMigration);
            var userId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var supplierId = Guid.NewGuid();
            var purchaseId = Guid.NewGuid();
            var purchaseLineId = Guid.NewGuid();
            var purchasePaymentId = Guid.NewGuid();
            var saleId = Guid.NewGuid();
            var saleLineId = Guid.NewGuid();
            var salePaymentId = Guid.NewGuid();
            var returnId = Guid.NewGuid();
            var returnLineId = Guid.NewGuid();
            var refundPaymentId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [AspNetUsers]
                    ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
                     [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [StoreId])
                VALUES
                    ({userId}, {"slice5-owner@example.test"}, {"SLICE5-OWNER@EXAMPLE.TEST"},
                     {"slice5-owner@example.test"}, {"SLICE5-OWNER@EXAMPLE.TEST"}, {true},
                     {false}, {false}, {true}, {0}, NULL);

                INSERT INTO [Stores] ([Id], [OwnerUserId], [Name], [CreatedAt], [AllowNegativeStock])
                VALUES ({storeId}, {userId}, {"Existing Slice 4 Store"}, {now}, {false});

                UPDATE [AspNetUsers] SET [StoreId] = {storeId} WHERE [Id] = {userId};

                INSERT INTO [Customers] ([Id], [StoreId], [Name], [Phone], [CreatedAt], [UpdatedAt])
                VALUES ({customerId}, {storeId}, {"Existing customer"}, NULL, {now}, {now});

                INSERT INTO [Warehouses] ([Id], [StoreId], [Name], [IsMain], [CreatedAt])
                VALUES ({warehouseId}, {storeId}, {"Main Warehouse"}, {true}, {now});

                INSERT INTO [Products]
                    ([Id], [StoreId], [Sku], [NormalizedSku], [Name], [Unit], [SalePrice],
                     [ReferencePurchaseCost], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                    ({productId}, {storeId}, {"MIGRATION-5A"}, {"MIGRATION-5A"}, {"Existing product"},
                     {"item"}, {100m}, {60m}, {true}, {now}, {now});

                INSERT INTO [Suppliers] ([Id], [StoreId], [Name], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES ({supplierId}, {storeId}, {"Existing supplier"}, {true}, {now}, {now});

                INSERT INTO [Purchases]
                    ([Id], [StoreId], [SupplierId], [Status], [CreatedByUserId], [CreatedAt], [UpdatedAt],
                     [CompletedAt], [TotalAmount])
                VALUES
                    ({purchaseId}, {storeId}, {supplierId}, {"Completed"}, {userId}, {now}, {now}, {now}, {75m});

                INSERT INTO [PurchaseLines]
                    ([Id], [StoreId], [PurchaseId], [ProductId], [Quantity], [UnitPrice], [LineAmount])
                VALUES ({purchaseLineId}, {storeId}, {purchaseId}, {productId}, {1m}, {75m}, {75m});

                INSERT INTO [PurchasePayments]
                    ([Id], [StoreId], [PurchaseId], [Amount], [Method], [PaidAt], [PerformedByUserId])
                VALUES ({purchasePaymentId}, {storeId}, {purchaseId}, {25m}, {"Cash"}, {now}, {userId});

                INSERT INTO [Sales]
                    ([Id], [StoreId], [WarehouseId], [CustomerId], [CompletedByUserId], [Status], [CompletedAt],
                     [TotalAmount], [CreatedAt])
                VALUES
                    ({saleId}, {storeId}, {warehouseId}, {customerId}, {userId}, {"Completed"}, {now}, {100m}, {now});

                INSERT INTO [SaleLines]
                    ([Id], [StoreId], [SaleId], [ProductId], [ProductName], [ProductSku], [ProductUnit],
                     [Quantity], [UnitSalePrice], [LineAmount], [UnitCostAtSale], [CostReliability])
                VALUES
                    ({saleLineId}, {storeId}, {saleId}, {productId}, {"Existing product"}, {"MIGRATION-5A"},
                     {"item"}, {1m}, {100m}, {100m}, {60m}, {"Reliable"});

                INSERT INTO [SalePayments]
                    ([Id], [StoreId], [SaleId], [Amount], [Method], [OccurredAt], [PerformedByUserId])
                VALUES ({salePaymentId}, {storeId}, {saleId}, {80m}, {"Cash"}, {now}, {userId});

                INSERT INTO [Returns]
                    ([Id], [StoreId], [OriginalSaleId], [Status], [TotalReturnAmount], [RefundAmount],
                     [CompletedByUserId], [CreatedAt], [CompletedAt])
                VALUES
                    ({returnId}, {storeId}, {saleId}, {"Completed"}, {20m}, {10m}, {userId}, {now}, {now});

                INSERT INTO [ReturnLines]
                    ([Id], [StoreId], [ReturnId], [OriginalSaleLineId], [ProductId], [Quantity], [Restock],
                     [UnitSalePriceBasis], [ReturnLineAmount], [UnitCostBasis], [RestockedInventoryValue])
                VALUES
                    ({returnLineId}, {storeId}, {returnId}, {saleLineId}, {productId}, {0.2m}, {false},
                     {100m}, {20m}, {60m}, {0m});

                INSERT INTO [ReturnRefundPayments]
                    ([Id], [StoreId], [ReturnId], [Amount], [Method], [OccurredAt], [PerformedByUserId])
                VALUES ({refundPaymentId}, {storeId}, {returnId}, {10m}, {"Cash"}, {now}, {userId});
                """);

            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();

            var store = await db.Stores.SingleAsync(item => item.Id == storeId);
            Assert.Equal("Asia/Ho_Chi_Minh", store.TimeZoneId);
            Assert.Equal(1, await db.Customers.CountAsync(item => item.Id == customerId));
            Assert.Equal(1, await db.Purchases.CountAsync(item => item.Id == purchaseId));
            Assert.Equal(1, await db.PurchaseLines.CountAsync(item => item.Id == purchaseLineId));
            Assert.Equal(25m, await db.PurchasePayments
                .Where(item => item.Id == purchasePaymentId)
                .Select(item => item.Amount)
                .SingleAsync());
            Assert.Equal(1, await db.Sales.CountAsync(item => item.Id == saleId));
            Assert.Equal(1, await db.SaleLines.CountAsync(item => item.Id == saleLineId));
            Assert.Equal(80m, await db.SalePayments
                .Where(item => item.Id == salePaymentId)
                .Select(item => item.Amount)
                .SingleAsync());
            Assert.Equal(20m, await db.Returns
                .Where(item => item.Id == returnId)
                .Select(item => item.TotalReturnAmount)
                .SingleAsync());
            Assert.Equal(1, await db.ReturnLines.CountAsync(item => item.Id == returnLineId));
            Assert.Equal(10m, await db.ReturnRefundPayments
                .Where(item => item.Id == refundPaymentId)
                .Select(item => item.Amount)
                .SingleAsync());
            Assert.Equal(0, await db.DebtPayments.CountAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
