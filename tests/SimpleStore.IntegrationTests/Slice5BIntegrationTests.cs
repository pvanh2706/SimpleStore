using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Products;
using SimpleStore.Application.Reports;
using SimpleStore.Application.Returns;
using SimpleStore.Domain.Inventory;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice5BIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task OwnerCanConfigureIanaTimezoneAndDstReportUsesIndependentMidnights()
    {
        var context = await CreateOwnerContextAsync("Timezone report");
        using var owner = context.Client;
        using var update = await owner.PutWithAntiforgeryAsync(
            "/api/store/timezone", JsonContent.Create(new { timeZoneId = "America/New_York" }));
        update.EnsureSuccessStatusCode();

        var spring = await owner.GetFromJsonAsync<EndOfDayReportResult>(
            "/api/reports/end-of-day?date=2026-03-08");
        var fall = await owner.GetFromJsonAsync<EndOfDayReportResult>(
            "/api/reports/end-of-day?date=2026-11-01");
        Assert.Equal("America/New_York", spring!.TimeZoneId);
        Assert.Equal(TimeSpan.FromHours(23), spring.EndUtc - spring.StartUtc);
        Assert.Equal(TimeSpan.FromHours(25), fall!.EndUtc - fall.StartUtc);

        using var invalidDate = await owner.GetAsync("/api/reports/end-of-day?date=03-08-2026");
        Assert.Equal(HttpStatusCode.BadRequest, invalidDate.StatusCode);
        Assert.Equal("invalid-business-date", await ReadCodeAsync(invalidDate));
        using var invalidZone = await owner.PutWithAntiforgeryAsync(
            "/api/store/timezone", JsonContent.Create(new { timeZoneId = "Eastern Standard Time" }));
        Assert.Equal(HttpStatusCode.BadRequest, invalidZone.StatusCode);
        Assert.Equal("invalid-store-timezone", await ReadCodeAsync(invalidZone));

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.GetAsync("/api/reports/end-of-day?date=2026-03-08")).StatusCode);
        using var forbiddenUpdate = await cashier.PutWithAntiforgeryAsync(
            "/api/store/timezone", JsonContent.Create(new { timeZoneId = "Asia/Ho_Chi_Minh" }));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdate.StatusCode);
    }

    [Fact]
    public async Task EndOfDayUsesEventDatesHistoricalCostsAndStrictEndingCutoff()
    {
        var context = await CreateOwnerContextAsync("EOD financial semantics");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 100, 10, 10);
        var customer = await client.CreateCustomerAsync("EOD customer", "0901000001");
        var sale = await client.CompleteSaleAsync(
            Guid.NewGuid(), customer.Id, [(product.Id, 2)], (50, "Cash"));
        await client.RecordCustomerDebtPaymentAsync(customer.Id, Guid.NewGuid(), 30, 150, "Transfer");
        await client.CreateReturnAsync(
            Guid.NewGuid(), sale.Id, [(Assert.Single(sale.Lines).Id, 1, true)]);

        var voidedSale = await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 1)], (100, "Cash"));
        await client.VoidSaleAsync(voidedSale.Id, Guid.NewGuid());

        var supplier = await client.CreateSupplierAsync("EOD supplier", "0902000002");
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid(), (20, "Transfer"));
        await client.RecordSupplierDebtPaymentAsync(supplier.Id, Guid.NewGuid(), 30, 80, "Cash");

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTimeOffset.UtcNow, "Asia/Ho_Chi_Minh").DateTime);
        var report = await client.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={localDate:yyyy-MM-dd}");

        Assert.NotNull(report);
        Assert.Equal(100, report.SalesRevenue);
        Assert.Equal(150, report.Collected.SalePayments);
        Assert.Equal(30, report.Collected.CustomerDebtPayments);
        Assert.Equal(0, report.Collected.CustomerRefunds);
        Assert.Equal(180, report.Collected.NetAmount);
        Assert.Equal(20, report.CustomerOutstandingDebtAtEnd);
        Assert.Equal(50, report.SupplierOutstandingDebtAtEnd);
        Assert.Equal(20, report.SupplierPayments.PurchasePayments);
        Assert.Equal(30, report.SupplierPayments.SupplierDebtPayments);
        Assert.Equal(50, report.SupplierPayments.TotalAmount);
        Assert.Equal(10, report.EstimatedGrossProfit.HistoricalCogs);
        Assert.Equal(90, report.EstimatedGrossProfit.Amount);
        Assert.Equal("Reliable", report.EstimatedGrossProfit.CostReliability);

        var debtList = await client.GetFromJsonAsync<JsonElement>("/api/customers/debts");
        Assert.Equal("0901000001", debtList.GetProperty("items")[0].GetProperty("phone").GetString());
    }

    [Fact]
    public async Task ReportIncludesStartExcludesEndUsesWorstReliabilityAndIsolatesStores()
    {
        var context = await CreateOwnerContextAsync("EOD boundaries");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 40, 5, 5);
        var atStart = await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 1)], (40, "Cash"));
        var atEnd = await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 1)], (40, "Cash"));
        var beforeStart = await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 1)], (40, "Cash"));
        var beforeEnd = await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 1)], (40, "Cash"));
        var date = new DateOnly(2026, 9, 23);
        var start = new DateTimeOffset(2026, 9, 22, 17, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 9, 23, 17, 0, 0, TimeSpan.Zero);
        await factory.WithDbContextAsync(async db =>
        {
            await db.Sales.Where(item => item.Id == atStart.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, start));
            await db.SalePayments.Where(item => item.SaleId == atStart.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, start));
            await db.SaleLines.Where(item => item.SaleId == atStart.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.CostReliability, CostReliability.Unavailable));
            await db.Sales.Where(item => item.Id == atEnd.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, end));
            await db.SalePayments.Where(item => item.SaleId == atEnd.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, end));
            await db.Sales.Where(item => item.Id == beforeStart.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, start.AddTicks(-1)));
            await db.SalePayments.Where(item => item.SaleId == beforeStart.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, start.AddTicks(-1)));
            await db.Sales.Where(item => item.Id == beforeEnd.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, end.AddTicks(-1)));
            await db.SalePayments.Where(item => item.SaleId == beforeEnd.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, end.AddTicks(-1)));
            return 0;
        });

        var report = await client.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={date:yyyy-MM-dd}");
        Assert.Equal(80, report!.SalesRevenue);
        Assert.Equal(80, report.Collected.NetAmount);
        Assert.Equal("Unavailable", report.EstimatedGrossProfit.CostReliability);

        var other = await CreateOwnerContextAsync("Other store");
        using var otherClient = other.Client;
        var isolated = await otherClient.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={date:yyyy-MM-dd}");
        Assert.Equal(0, isolated!.SalesRevenue);
        Assert.Equal(0, isolated.Collected.NetAmount);
    }

    [Fact]
    public async Task OldDebtPaymentsAreTodayFlowsWhileOldTransactionsAreNot()
    {
        var context = await CreateOwnerContextAsync("EOD old debt");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 100, 3, 10);
        var customer = await client.CreateCustomerAsync("Old customer debt");
        var sale = await client.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        var supplier = await client.CreateSupplierAsync("Old supplier debt");
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        var customerPayment = await client.RecordCustomerDebtPaymentAsync(
            customer.Id, Guid.NewGuid(), 40, 100, "Cash");
        var supplierPayment = await client.RecordSupplierDebtPaymentAsync(
            supplier.Id, Guid.NewGuid(), 30, 100, "Transfer");

        var date = new DateOnly(2026, 9, 23);
        var start = new DateTimeOffset(2026, 9, 22, 17, 0, 0, TimeSpan.Zero);
        await factory.WithDbContextAsync(async db =>
        {
            await db.Sales.Where(item => item.Id == sale.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, start.AddDays(-1)));
            await db.Purchases.Where(item => item.Id == purchase.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, start.AddDays(-1)));
            await db.DebtPayments.Where(item => item.Id == customerPayment.Id || item.Id == supplierPayment.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, start.AddHours(1)));
            return 0;
        });

        var report = await client.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={date:yyyy-MM-dd}");
        Assert.Equal(0, report!.SalesRevenue);
        Assert.Equal(0, report.Collected.SalePayments);
        Assert.Equal(40, report.Collected.CustomerDebtPayments);
        Assert.Equal(40, report.Collected.NetAmount);
        Assert.Equal(60, report.CustomerOutstandingDebtAtEnd);
        Assert.Equal(0, report.SupplierPayments.PurchasePayments);
        Assert.Equal(30, report.SupplierPayments.SupplierDebtPayments);
        Assert.Equal(70, report.SupplierOutstandingDebtAtEnd);

        var emptyDay = await client.GetFromJsonAsync<EndOfDayReportResult>(
            "/api/reports/end-of-day?date=2026-09-24");
        Assert.Equal(0, emptyDay!.SalesRevenue);
        Assert.Equal(0, emptyDay.Collected.NetAmount);
        Assert.Equal(0, emptyDay.SupplierPayments.TotalAmount);
        Assert.Equal(60, emptyDay.CustomerOutstandingDebtAtEnd);
        Assert.Equal(70, emptyDay.SupplierOutstandingDebtAtEnd);
    }

    [Fact]
    public async Task RefundNoRestockAndSafePurchaseVoidUseHistoricalMoneyAndCostSemantics()
    {
        var context = await CreateOwnerContextAsync("EOD corrections");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 100, 3, 10);
        var sale = await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 1)], (100, "Cash"));
        await client.CreateReturnAsync(
            Guid.NewGuid(), sale.Id, [(Assert.Single(sale.Lines).Id, 1, false)], "Cash");

        var supplier = await client.CreateSupplierAsync("Voided paid supplier");
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 12));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid(), (12, "Cash"));
        await client.VoidPurchaseAsync(purchase.Id, Guid.NewGuid());

        await factory.WithDbContextAsync(async db =>
        {
            await db.Products.Where(item => item.Id == product.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ReferencePurchaseCost, 999m));
            await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.AverageCost, 777m));
            return 0;
        });

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTimeOffset.UtcNow, "Asia/Ho_Chi_Minh").DateTime);
        var report = await client.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={localDate:yyyy-MM-dd}");
        Assert.Equal(0, report!.SalesRevenue);
        Assert.Equal(100, report.Collected.SalePayments);
        Assert.Equal(100, report.Collected.CustomerRefunds);
        Assert.Equal(0, report.Collected.NetAmount);
        Assert.Equal(10, report.EstimatedGrossProfit.HistoricalCogs);
        Assert.Equal(-10, report.EstimatedGrossProfit.Amount);
        Assert.Equal(12, report.SupplierPayments.PurchasePayments);
        Assert.Equal(12, report.SupplierPayments.TotalAmount);
        Assert.Equal(0, report.SupplierOutstandingDebtAtEnd);
    }

    [Fact]
    public async Task GrossProfitRoundsEachHistoricalLineAndAggregatesWorstReliability()
    {
        var context = await CreateOwnerContextAsync("EOD reliability");
        using var client = context.Client;
        using var setting = await client.PutWithAntiforgeryAsync(
            "/api/store/operational-settings/negative-stock",
            JsonContent.Create(new { allowNegativeStock = true }));
        setting.EnsureSuccessStatusCode();
        var estimatedProduct = await CreatePricedProductAsync(client, 30, 0, 10.005m);
        await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(estimatedProduct.Id, 0.333m)], (9.99m, "Cash"));

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTimeOffset.UtcNow, "Asia/Ho_Chi_Minh").DateTime);
        var estimated = await client.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={localDate:yyyy-MM-dd}");
        Assert.Equal(3.33m, estimated!.EstimatedGrossProfit.HistoricalCogs);
        Assert.Equal("Estimated", estimated.EstimatedGrossProfit.CostReliability);

        var unavailableProduct = await CreatePricedProductAsync(client, 1, 0, 0);
        await factory.WithDbContextAsync(async db =>
        {
            await db.Products.Where(item => item.Id == unavailableProduct.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.ReferencePurchaseCost, (decimal?)null));
            return 0;
        });
        await client.CompleteSaleAsync(
            Guid.NewGuid(), null, [(unavailableProduct.Id, 1)], (1, "Cash"));
        var unavailable = await client.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={localDate:yyyy-MM-dd}");
        Assert.Equal("Unavailable", unavailable!.EstimatedGrossProfit.CostReliability);
    }

    private async Task<OwnerContext> CreateOwnerContextAsync(string storeName)
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        return new OwnerContext(client, store.Id);
    }

    private static async Task<ProductResult> CreatePricedProductAsync(
        HttpClient client, decimal salePrice, decimal openingQuantity, decimal openingCost)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/products",
            JsonContent.Create(new
            {
                sku = $"SL5B-{Guid.NewGuid():N}",
                name = "Stage 5B product",
                unit = "item",
                salePrice,
                referencePurchaseCost = openingCost,
                openingQuantity,
                openingCost = openingQuantity > 0 ? openingCost : (decimal?)null
            }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResult>())!;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("code").GetString();
    }

    private sealed record OwnerContext(HttpClient Client, Guid StoreId);
}
