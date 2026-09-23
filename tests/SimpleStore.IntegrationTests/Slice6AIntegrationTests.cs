using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Products;
using SimpleStore.Application.Reports;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice6AIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 9, 23, 3, 0, 0, TimeSpan.Zero);

    public Slice6AIntegrationTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.SetUtcNow(FixedUtcNow);
    }

    [Fact]
    public async Task TodayReusesEndOfDayFinancialsAndEnforcesOwnerStoreIsolation()
    {
        var context = await CreateOwnerContextAsync("Today shared financials");
        using var owner = context.Client;
        var product = await CreatePricedProductAsync(owner, 1_000, 10, 100);
        var sale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 1)], (1_000, "Cash"));

        var today = await owner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        var eod = await owner.GetFromJsonAsync<EndOfDayReportResult>(
            $"/api/reports/end-of-day?date={today!.BusinessDate:yyyy-MM-dd}");

        Assert.Equal(new DateOnly(2026, 9, 23), today.BusinessDate);
        Assert.Equal("Asia/Ho_Chi_Minh", today.TimeZoneId);
        Assert.Equal(eod!.SalesRevenue, today.SalesRevenue);
        Assert.Equal(eod.Collected.NetAmount, today.NetCollected);
        Assert.Equal(eod.EstimatedGrossProfit.HistoricalCogs,
            today.EstimatedGrossProfit.HistoricalCogs);
        Assert.Equal(eod.EstimatedGrossProfit.Amount, today.EstimatedGrossProfit.Amount);
        Assert.Equal(eod.EstimatedGrossProfit.CostReliability,
            today.EstimatedGrossProfit.CostReliability);
        Assert.Equal(1, today.SaleCount);

        foreach (var metric in new[]
                 {
                     TodayMetricIds.Revenue,
                     TodayMetricIds.Collected,
                     TodayMetricIds.EstimatedGrossProfit,
                     TodayMetricIds.SaleCount,
                     TodayMetricIds.CustomerDebtCreated,
                     TodayMetricIds.SupplierDebtCreated
                 })
        {
            var explanation = await owner.GetFromJsonAsync<TodayExplanationResult>(
                $"/api/today/explanations/{metric}?page=1&pageSize=100");
            Assert.NotNull(explanation);
            var sum = metric == TodayMetricIds.SaleCount
                ? explanation.Items.Sum(item => item.ContributionCount ?? 0)
                : explanation.Items.Sum(item => item.ContributionAmount ?? 0m);
            Assert.Equal(explanation.Headline, sum);
        }

        var revenue = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/revenue?pageSize=100");
        AssertNavigation(
            Assert.Single(revenue!.Items, item => item.SourceId == sale.Id),
            TodaySourceNavigationTypes.Sale,
            sale.Id);
        var collected = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/collected?pageSize=100");
        AssertNavigation(
            Assert.Single(collected!.Items, item => item.SourceType == "SalePayment"),
            TodaySourceNavigationTypes.Sale,
            sale.Id);
        var grossProfit = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/estimated-gross-profit?pageSize=100");
        AssertNavigation(
            Assert.Single(grossProfit!.Items, item => item.SourceType == "HistoricalCogs"),
            TodaySourceNavigationTypes.Sale,
            sale.Id);

        using var invalid = await owner.GetAsync("/api/today/explanations/not-a-metric");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("invalid-today-metric", await ReadCodeAsync(invalid));

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/api/today")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.GetAsync("/api/today/explanations/revenue")).StatusCode);

        using var anonymous = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/today")).StatusCode);

        var other = await CreateOwnerContextAsync("Other Today store");
        using var otherOwner = other.Client;
        var isolated = await otherOwner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(0, isolated!.SalesRevenue);
        Assert.Equal(0, isolated.SaleCount);
        var isolatedEvidence = await otherOwner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/revenue");
        Assert.Empty(isolatedEvidence!.Items);
    }

    [Fact]
    public async Task TodayUsesInjectedClockStoreTimezoneAndHalfOpenDstSafeWindow()
    {
        var context = await CreateOwnerContextAsync("Today clock");
        using var owner = context.Client;
        using var zone = await owner.PutWithAntiforgeryAsync(
            "/api/store/timezone", JsonContent.Create(new { timeZoneId = "America/New_York" }));
        zone.EnsureSuccessStatusCode();

        factory.SetUtcNow(new DateTimeOffset(2026, 3, 8, 6, 0, 0, TimeSpan.Zero));
        var spring = await owner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(new DateOnly(2026, 3, 8), spring!.BusinessDate);
        Assert.Equal(TimeSpan.FromHours(23), spring.EndUtc - spring.StartUtc);

        factory.SetUtcNow(new DateTimeOffset(2026, 11, 1, 5, 0, 0, TimeSpan.Zero));
        using var fallOwner = factory.CreateHttpsClient();
        await fallOwner.LoginAsync(context.Email, context.Password);
        var fall = await fallOwner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(new DateOnly(2026, 11, 1), fall!.BusinessDate);
        Assert.Equal(TimeSpan.FromHours(25), fall.EndUtc - fall.StartUtc);

        factory.SetUtcNow(FixedUtcNow);
        using var hcmOwner = factory.CreateHttpsClient();
        await hcmOwner.LoginAsync(context.Email, context.Password);
        using var hcmZone = await hcmOwner.PutWithAntiforgeryAsync(
            "/api/store/timezone", JsonContent.Create(new { timeZoneId = "Asia/Ho_Chi_Minh" }));
        hcmZone.EnsureSuccessStatusCode();
        var product = await CreatePricedProductAsync(hcmOwner, 100, 10, 10);
        var atStart = await hcmOwner.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 1)], (100, "Cash"));
        var atEnd = await hcmOwner.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 1)], (100, "Cash"));
        var beforeStart = await hcmOwner.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 1)], (100, "Cash"));
        var start = new DateTimeOffset(2026, 9, 22, 17, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 9, 23, 17, 0, 0, TimeSpan.Zero);
        await factory.WithDbContextAsync(async db =>
        {
            await SetSaleTimeAsync(db, atStart.Id, start);
            await SetSaleTimeAsync(db, atEnd.Id, end);
            await SetSaleTimeAsync(db, beforeStart.Id, start.AddTicks(-1));
            return 0;
        });

        var bounded = await hcmOwner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(100, bounded!.SalesRevenue);
        Assert.Equal(100, bounded.NetCollected);
        Assert.Equal(1, bounded.SaleCount);
    }

    [Fact]
    public async Task CustomerDebtCreatedUsesTransactionLocalReturnTotalAndIgnoresDebtPayments()
    {
        var context = await CreateOwnerContextAsync("Today customer debt");
        using var owner = context.Client;
        var product = await CreatePricedProductAsync(owner, 1_000, 20, 100);

        var case1 = await CreateCreditSaleAsync(owner, product.Id, 800, "Debt case 1");
        var case1Return = await owner.CreateReturnAsync(Guid.NewGuid(), case1.Sale.Id,
            [(Assert.Single(case1.Sale.Lines).Id, 0.1m, true)]);

        var case2 = await CreateCreditSaleAsync(owner, product.Id, 800, "Debt case 2");
        await owner.CreateReturnAsync(Guid.NewGuid(), case2.Sale.Id,
            [(Assert.Single(case2.Sale.Lines).Id, 0.5m, true)], "Cash");

        var case3 = await CreateCreditSaleAsync(owner, product.Id, 0, "Debt case 3");
        var case3DebtPayment = await owner.RecordCustomerDebtPaymentAsync(
            case3.CustomerId, Guid.NewGuid(), 1_000, 1_000, "Cash");
        var case3Return = await owner.CreateReturnAsync(Guid.NewGuid(), case3.Sale.Id,
            [(Assert.Single(case3.Sale.Lines).Id, 0.3m, true)], "Cash");

        var case4 = await CreateCreditSaleAsync(owner, product.Id, 0, "Debt case 4");
        await owner.CreateReturnAsync(Guid.NewGuid(), case4.Sale.Id,
            [(Assert.Single(case4.Sale.Lines).Id, 0.3m, true)]);
        await owner.RecordCustomerDebtPaymentAsync(
            case4.CustomerId, Guid.NewGuid(), 700, 700, "Transfer");

        var case5 = await CreateCreditSaleAsync(owner, product.Id, 0, "Debt case 5");
        var case5Line = Assert.Single(case5.Sale.Lines);
        await owner.CreateReturnAsync(Guid.NewGuid(), case5.Sale.Id, [(case5Line.Id, 0.2m, true)]);
        await owner.CreateReturnAsync(Guid.NewGuid(), case5.Sale.Id, [(case5Line.Id, 0.3m, true)]);

        var crossDay = await CreateCreditSaleAsync(owner, product.Id, 0, "Debt cross day");
        await owner.CreateReturnAsync(Guid.NewGuid(), crossDay.Sale.Id,
            [(Assert.Single(crossDay.Sale.Lines).Id, 0.3m, true)]);
        await factory.WithDbContextAsync(async db =>
        {
            await db.Sales.Where(item => item.Id == crossDay.Sale.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CompletedAt, FixedUtcNow.AddDays(-1)));
            return 0;
        });

        var today = await owner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(2_000, today!.CustomerDebtCreated);
        Assert.Equal(5, today.SaleCount);

        var explanation = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/customer-debt-created?page=1&pageSize=100");
        Assert.Equal(2_000, explanation!.Items.Sum(item => item.ContributionAmount));
        Assert.All(explanation.Items, item => Assert.Equal("Sale", item.SourceType));
        Assert.DoesNotContain(explanation.Items, item => item.SourceType == "CustomerDebtPayment");

        var beforePayment = Assert.Single(explanation.Items,
            item => item.SourceId == case3.Sale.Id).DebtContribution!;
        var afterPayment = Assert.Single(explanation.Items,
            item => item.SourceId == case4.Sale.Id).DebtContribution!;
        Assert.Equal(300, beforePayment.SameDayReturnObligationReduction);
        Assert.Equal(700, beforePayment.FinalContribution);
        Assert.Equal(300, afterPayment.SameDayReturnObligationReduction);
        Assert.Equal(700, afterPayment.FinalContribution);
        var directPayment = Assert.Single(explanation.Items,
            item => item.SourceId == case1.Sale.Id).DebtContribution!;
        Assert.Equal(200, directPayment.BaseDebt);
        Assert.Equal(100, directPayment.SameDayReturnObligationReduction);
        Assert.Equal(100, directPayment.FinalContribution);
        var floored = Assert.Single(explanation.Items,
            item => item.SourceId == case2.Sale.Id).DebtContribution!;
        Assert.Equal(200, floored.BaseDebt);
        Assert.Equal(500, floored.SameDayReturnObligationReduction);
        Assert.Equal(0, floored.FinalContribution);
        var multipleReturns = Assert.Single(explanation.Items,
            item => item.SourceId == case5.Sale.Id).DebtContribution!;
        Assert.Equal(500, multipleReturns.SameDayReturnObligationReduction);
        Assert.Equal(500, multipleReturns.FinalContribution);
        Assert.DoesNotContain(explanation.Items, item => item.SourceId == crossDay.Sale.Id);
        AssertNavigation(
            Assert.Single(explanation.Items, item => item.SourceId == case3.Sale.Id),
            TodaySourceNavigationTypes.Sale,
            case3.Sale.Id);

        var revenue = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/revenue?pageSize=100");
        AssertNavigation(
            Assert.Single(revenue!.Items, item => item.SourceId == case1Return.Id),
            TodaySourceNavigationTypes.Return,
            case1Return.Id);
        var collected = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/collected?pageSize=100");
        Assert.Null(Assert.Single(collected!.Items,
            item => item.SourceId == case3DebtPayment.Id).Navigation);
        AssertNavigation(
            Assert.Single(collected.Items,
                item => item.SourceType == "ActualCustomerRefund"
                    && item.RelatedSourceId == case3Return.Id),
            TodaySourceNavigationTypes.Return,
            case3Return.Id);
        var grossProfit = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/estimated-gross-profit?pageSize=100");
        AssertNavigation(
            Assert.Single(grossProfit!.Items,
                item => item.SourceType == "HistoricalCogs"
                    && item.SourceId == Assert.Single(case1Return.Lines).Id),
            TodaySourceNavigationTypes.Return,
            case1Return.Id);

        var page = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/customer-debt-created?page=1&pageSize=2");
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        using var invalidPage = await owner.GetAsync(
            "/api/today/explanations/customer-debt-created?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
        Assert.Equal("invalid-pagination", await ReadCodeAsync(invalidPage));

        factory.SetUtcNow(FixedUtcNow.AddDays(-1));
        using var priorDayOwner = factory.CreateHttpsClient();
        await priorDayOwner.LoginAsync(context.Email, context.Password);
        var priorDay = await priorDayOwner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(1_000, priorDay!.CustomerDebtCreated);
    }

    [Fact]
    public async Task SaleCountAndSupplierDebtRespectSameDayVoidWithoutCrossDayNegatives()
    {
        var context = await CreateOwnerContextAsync("Today count and supplier debt");
        using var owner = context.Client;
        var saleProduct = await CreatePricedProductAsync(owner, 100, 20, 10);

        await owner.CompleteSaleAsync(Guid.NewGuid(), null, [(saleProduct.Id, 1)], (100, "Cash"));
        var partial = await owner.CompleteSaleAsync(Guid.NewGuid(), null, [(saleProduct.Id, 1)], (100, "Cash"));
        var partialReturn = await owner.CreateReturnAsync(Guid.NewGuid(), partial.Id,
            [(Assert.Single(partial.Lines).Id, 0.5m, true)], "Cash");
        var full = await owner.CompleteSaleAsync(Guid.NewGuid(), null, [(saleProduct.Id, 1)], (100, "Cash"));
        await owner.CreateReturnAsync(Guid.NewGuid(), full.Id,
            [(Assert.Single(full.Lines).Id, 1m, true)], "Cash");
        var sameDayVoid = await owner.CompleteSaleAsync(Guid.NewGuid(), null, [(saleProduct.Id, 1)], (100, "Cash"));
        var sameDaySaleVoid = await owner.VoidSaleAsync(sameDayVoid.Id, Guid.NewGuid());
        var priorDayVoid = await owner.CompleteSaleAsync(Guid.NewGuid(), null, [(saleProduct.Id, 1)], (100, "Cash"));
        await factory.WithDbContextAsync(async db =>
        {
            await SetSaleTimeAsync(db, priorDayVoid.Id, FixedUtcNow.AddDays(-1));
            return 0;
        });
        await owner.VoidSaleAsync(priorDayVoid.Id, Guid.NewGuid());

        var supplierProduct1 = await CreatePricedProductAsync(owner, 10, 0, 10);
        var supplier1 = await owner.CreateSupplierAsync("Today supplier direct");
        var purchase1 = await owner.CreatePurchaseAsync(supplier1.Id, (supplierProduct1.Id, 1, 1_000));
        await owner.CompletePurchaseAsync(purchase1.Id, Guid.NewGuid(), (800, "Cash"));

        var supplierProduct2 = await CreatePricedProductAsync(owner, 10, 0, 10);
        var supplier2 = await owner.CreateSupplierAsync("Today supplier standalone");
        var purchase2 = await owner.CreatePurchaseAsync(supplier2.Id, (supplierProduct2.Id, 1, 1_000));
        await owner.CompletePurchaseAsync(purchase2.Id, Guid.NewGuid());
        await owner.RecordSupplierDebtPaymentAsync(supplier2.Id, Guid.NewGuid(), 1_000, 1_000);

        var supplierProduct3 = await CreatePricedProductAsync(owner, 10, 0, 10);
        var supplier3 = await owner.CreateSupplierAsync("Today supplier same-day void");
        var purchase3 = await owner.CreatePurchaseAsync(supplier3.Id, (supplierProduct3.Id, 1, 1_000));
        await owner.CompletePurchaseAsync(purchase3.Id, Guid.NewGuid());
        await owner.VoidPurchaseAsync(purchase3.Id, Guid.NewGuid());

        var supplierProduct4 = await CreatePricedProductAsync(owner, 10, 0, 10);
        var supplier4 = await owner.CreateSupplierAsync("Today supplier cross-day void");
        var purchase4 = await owner.CreatePurchaseAsync(supplier4.Id, (supplierProduct4.Id, 1, 1_000));
        await owner.CompletePurchaseAsync(purchase4.Id, Guid.NewGuid());
        await factory.WithDbContextAsync(async db =>
        {
            await db.Purchases.Where(item => item.Id == purchase4.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CompletedAt, FixedUtcNow.AddDays(-1)));
            return 0;
        });
        await owner.VoidPurchaseAsync(purchase4.Id, Guid.NewGuid());

        var today = await owner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(3, today!.SaleCount);
        Assert.Equal(1_200, today.SupplierDebtCreated);

        var count = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/sale-count?page=1&pageSize=100");
        Assert.Equal(3, count!.Items.Sum(item => item.ContributionCount));
        Assert.Contains(count.Items, item => item.SourceId == sameDayVoid.Id
            && item.ContributionCount == 0);
        Assert.DoesNotContain(count.Items, item => item.SourceId == priorDayVoid.Id);

        var supplierDebt = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/supplier-debt-created?page=1&pageSize=100");
        Assert.Equal(1_200, supplierDebt!.Items.Sum(item => item.ContributionAmount));
        Assert.Contains(supplierDebt.Items, item => item.SourceId == purchase3.Id
            && item.DebtContribution!.SameDayVoided
            && item.ContributionAmount == 0);
        Assert.DoesNotContain(supplierDebt.Items, item => item.SourceId == purchase4.Id);
        AssertNavigation(
            Assert.Single(supplierDebt.Items, item => item.SourceId == purchase1.Id),
            TodaySourceNavigationTypes.Purchase,
            purchase1.Id);

        var revenue = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/revenue?pageSize=100");
        AssertNavigation(
            Assert.Single(revenue!.Items, item => item.SourceId == sameDaySaleVoid.Id),
            TodaySourceNavigationTypes.Sale,
            sameDayVoid.Id);
        var grossProfit = await owner.GetFromJsonAsync<TodayExplanationResult>(
            "/api/today/explanations/estimated-gross-profit?pageSize=100");
        AssertNavigation(
            Assert.Single(grossProfit!.Items,
                item => item.SourceType == "HistoricalCogs"
                    && item.SourceId == Assert.Single(partialReturn.Lines).Id),
            TodaySourceNavigationTypes.Return,
            partialReturn.Id);
        AssertNavigation(
            Assert.Single(grossProfit.Items,
                item => item.SourceType == "HistoricalCogs"
                    && item.SourceId == Assert.Single(sameDayVoid.Lines).Id
                    && item.ContributionAmount > 0),
            TodaySourceNavigationTypes.Sale,
            sameDayVoid.Id);

        factory.SetUtcNow(FixedUtcNow.AddDays(-1));
        using var priorDayOwner = factory.CreateHttpsClient();
        await priorDayOwner.LoginAsync(context.Email, context.Password);
        var priorDay = await priorDayOwner.GetFromJsonAsync<TodaySummaryResult>("/api/today");
        Assert.Equal(1_000, priorDay!.SupplierDebtCreated);
    }

    private async Task<OwnerContext> CreateOwnerContextAsync(string storeName)
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        return new OwnerContext(client, store.Id, credentials.Email, credentials.Password);
    }

    private static async Task<ProductResult> CreatePricedProductAsync(
        HttpClient client,
        decimal salePrice,
        decimal openingQuantity,
        decimal openingCost)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/products",
            JsonContent.Create(new
            {
                sku = $"SL6A-{Guid.NewGuid():N}",
                name = "Stage 6A product",
                unit = "item",
                salePrice,
                referencePurchaseCost = openingCost,
                openingQuantity,
                openingCost = openingQuantity > 0 ? openingCost : (decimal?)null
            }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResult>())!;
    }

    private static async Task<CreditSaleContext> CreateCreditSaleAsync(
        HttpClient client,
        Guid productId,
        decimal directPayment,
        string customerName)
    {
        var customer = await client.CreateCustomerAsync(customerName, null);
        var sale = directPayment > 0
            ? await client.CompleteSaleAsync(
                Guid.NewGuid(), customer.Id, [(productId, 1)], (directPayment, "Cash"))
            : await client.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(productId, 1)]);
        return new CreditSaleContext(sale, customer.Id);
    }

    private static async Task SetSaleTimeAsync(
        SimpleStore.Infrastructure.Persistence.ApplicationDbContext db,
        Guid saleId,
        DateTimeOffset occurredAt)
    {
        await db.Sales.Where(item => item.Id == saleId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, occurredAt));
        await db.SalePayments.Where(item => item.SaleId == saleId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, occurredAt));
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("code").GetString();
    }

    private static void AssertNavigation(
        TodayEvidenceSourceResult source,
        string expectedType,
        Guid expectedId)
    {
        Assert.NotNull(source.Navigation);
        Assert.Equal(expectedType, source.Navigation.Type);
        Assert.Equal(expectedId, source.Navigation.Id);
    }

    private sealed record OwnerContext(
        HttpClient Client,
        Guid StoreId,
        string Email,
        string Password);
    private sealed record CreditSaleContext(SimpleStore.Application.Sales.SaleResult Sale, Guid CustomerId);
}
