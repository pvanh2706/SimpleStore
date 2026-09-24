using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Products;
using SimpleStore.Application.Reports;
using SimpleStore.Domain.Experiments;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice6BIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 9, 23, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset VelocityStartUtc =
        new(2026, 9, 15, 17, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset VelocityEndUtc =
        new(2026, 9, 22, 17, 0, 0, TimeSpan.Zero);

    public Slice6BIntegrationTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.SetUtcNow(FixedUtcNow);
    }

    [Fact]
    public async Task AttentionUsesTransactionHistoryCoverageClassificationOrderingAndTypedEvidence()
    {
        var context = await CreateOwnerContextAsync("C14 classification");
        using var owner = context.Client;
        await owner.SetNegativeStockAsync(true);

        var negative = await CreateProductAsync(owner, "A-NEG", "Alpha negative", 0);
        var missing = await CreateProductAsync(owner, "B-MISSING", "Beta missing balance", 1);
        var partial = await CreateProductAsync(owner, "C-PARTIAL", "Gamma partial out", 1);
        var risk = await CreateProductAsync(owner, "Z-RISK", "Omega exact three", 20);
        var below = await CreateProductAsync(owner, "Y-BELOW", "Lower days of cover", 9);
        var above = await CreateProductAsync(owner, "NO-RISK", "No risk above threshold", 11);
        var preciseAbove = await CreateProductAsync(owner, "PRECISE", "Rounded display above", 10.001m);
        var inactive = await CreateProductAsync(owner, "INACTIVE", "Inactive signal", 1);
        var noEvidence = await CreateProductAsync(owner, "NO-EVIDENCE", "No evidence", 0);
        var openingOnly = await CreateProductAsync(owner, "OPENING", "Opening only", 5);
        var zeroNet = await CreateProductAsync(owner, "ZERO-NET", "Zero net", 1);
        var partialNegative = await CreateProductAsync(owner, "D-PARTIAL-NEG", "Delta partial negative", 0);

        var negativeSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(negative.Id, 1)], (12_000, "Cash"));
        var missingSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(missing.Id, 1)], (12_000, "Cash"));
        var partialSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(partial.Id, 1)], (12_000, "Cash"));
        var riskSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(risk.Id, 10)], (120_000, "Cash"));
        var restocked = await owner.CreateReturnAsync(
            Guid.NewGuid(), riskSale.Id,
            [(Assert.Single(riskSale.Lines).Id, 1, true)], "Cash");
        var notRestocked = await owner.CreateReturnAsync(
            Guid.NewGuid(), riskSale.Id,
            [(Assert.Single(riskSale.Lines).Id, 1, false)], "Cash");
        var voidedRiskSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(risk.Id, 1)], (12_000, "Cash"));
        var riskVoid = await owner.VoidSaleAsync(voidedRiskSale.Id, Guid.NewGuid());
        var excludedAtEnd = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(risk.Id, 2)], (24_000, "Cash"));
        var aboveSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(above.Id, 7)], (84_000, "Cash"));
        var belowSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(below.Id, 7)], (84_000, "Cash"));
        var preciseAboveSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(preciseAbove.Id, 7)], (84_000, "Cash"));
        var inactiveSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(inactive.Id, 1)], (12_000, "Cash"));
        var zeroNetSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(zeroNet.Id, 1)], (12_000, "Cash"));
        var zeroNetVoid = await owner.VoidSaleAsync(zeroNetSale.Id, Guid.NewGuid());
        var partialNegativeSale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(partialNegative.Id, 1)], (12_000, "Cash"));

        var supplier = await owner.CreateSupplierAsync("C14 excluded supplier");
        var excludedPurchase = await owner.CreatePurchaseAsync(
            supplier.Id, (openingOnly.Id, 2, 1_000));
        excludedPurchase = await owner.CompletePurchaseAsync(
            excludedPurchase.Id, Guid.NewGuid());
        await owner.VoidPurchaseAsync(excludedPurchase.Id, Guid.NewGuid());

        using (var deactivate = await owner.PostWithAntiforgeryAsync(
                   $"/api/products/{inactive.Id}/deactivate", JsonContent.Create(new { })))
        {
            Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        }

        await factory.WithDbContextAsync(async db =>
        {
            await db.Stores.Where(item => item.Id == context.StoreId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CreatedAt, VelocityStartUtc));
            var fullIds = new[]
            {
                negative.Id, missing.Id, risk.Id, below.Id, above.Id, preciseAbove.Id,
                inactive.Id, noEvidence.Id, openingOnly.Id, zeroNet.Id
            };
            await db.Products.Where(item => fullIds.Contains(item.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CreatedAt, VelocityStartUtc));
            await db.Products.Where(item => item.Id == partial.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CreatedAt, VelocityStartUtc.AddTicks(1)));
            await db.Products.Where(item => item.Id == partialNegative.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CreatedAt, VelocityStartUtc.AddTicks(1)));

            await SetSaleTimeAsync(db, negativeSale.Id, VelocityStartUtc);
            await SetSaleTimeAsync(db, missingSale.Id, VelocityStartUtc.AddHours(1));
            await SetSaleTimeAsync(db, partialSale.Id, VelocityStartUtc.AddDays(1));
            await SetSaleTimeAsync(db, riskSale.Id, VelocityStartUtc.AddDays(2));
            await SetReturnTimeAsync(db, restocked.Id, VelocityStartUtc.AddDays(3));
            await SetReturnTimeAsync(db, notRestocked.Id, VelocityStartUtc.AddDays(4));
            await SetSaleTimeAsync(db, voidedRiskSale.Id, VelocityStartUtc.AddTicks(-1));
            await db.SaleVoids.Where(item => item.Id == riskVoid.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.VoidedAt, VelocityStartUtc.AddDays(6)));
            await SetSaleTimeAsync(db, excludedAtEnd.Id, VelocityEndUtc);
            await SetSaleTimeAsync(db, aboveSale.Id, VelocityStartUtc.AddHours(2));
            await SetSaleTimeAsync(db, belowSale.Id, VelocityStartUtc.AddHours(2));
            await SetSaleTimeAsync(db, preciseAboveSale.Id, VelocityStartUtc.AddHours(2));
            await SetSaleTimeAsync(db, inactiveSale.Id, VelocityStartUtc.AddHours(3));
            await SetSaleTimeAsync(db, zeroNetSale.Id, VelocityStartUtc.AddHours(4));
            await db.SaleVoids.Where(item => item.Id == zeroNetVoid.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.VoidedAt, VelocityStartUtc.AddHours(5)));
            await SetSaleTimeAsync(db, partialNegativeSale.Id, VelocityStartUtc.AddHours(6));

            await db.InventoryBalances.Where(item => item.ProductId == risk.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.QuantityOnHand, 3m));
            await db.InventoryBalances.Where(item => item.ProductId == above.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.QuantityOnHand, 4m));
            await db.InventoryBalances.Where(item => item.ProductId == missing.Id)
                .ExecuteDeleteAsync();
            return 0;
        });

        var preview = await owner.GetFromJsonAsync<C14AttentionListResult>(
            "/api/today/attention?page=1&pageSize=3");
        Assert.NotNull(preview);
        Assert.Equal(7, preview.CompletedBusinessDays.Count);
        Assert.Equal(new DateOnly(2026, 9, 16), preview.CompletedBusinessDays[0].BusinessDate);
        Assert.Equal(new DateOnly(2026, 9, 22), preview.CompletedBusinessDays[^1].BusinessDate);
        Assert.Equal(VelocityStartUtc, preview.VelocityStartUtc);
        Assert.Equal(VelocityEndUtc, preview.VelocityEndUtc);
        Assert.Equal(6, preview.TotalAttentionCount);
        Assert.Equal(3, preview.Items.Count);
        Assert.Equal(
            [negative.Id, missing.Id, partialNegative.Id],
            preview.Items.Select(item => item.ProductId).ToArray());

        var full = await owner.GetFromJsonAsync<C14AttentionListResult>(
            "/api/today/attention?page=1&pageSize=100");
        Assert.NotNull(full);
        var exactThree = Assert.Single(full.Items, item => item.ProductId == risk.Id);
        Assert.Equal(C14AttentionKinds.LowStockRisk, exactThree.AttentionKind);
        Assert.Equal(7, exactThree.NetSoldQuantity);
        Assert.Equal(1, exactThree.AverageDailySales);
        Assert.Equal(3, exactThree.DaysOfCover);
        var orderedIds = full.Items.Select(item => item.ProductId).ToArray();
        Assert.True(Array.IndexOf(orderedIds, below.Id)
            < Array.IndexOf(orderedIds, risk.Id));
        Assert.Equal(C14HistoryCoverageTypes.PartialObservation,
            Assert.Single(full.Items, item => item.ProductId == partial.Id).HistoryCoverage);
        Assert.Null(Assert.Single(full.Items, item => item.ProductId == partial.Id).AverageDailySales);
        Assert.DoesNotContain(full.Items, item => item.ProductId == above.Id);
        Assert.DoesNotContain(full.Items, item => item.ProductId == preciseAbove.Id);
        Assert.DoesNotContain(full.Items, item => item.ProductId == inactive.Id);
        Assert.DoesNotContain(full.Items, item => item.ProductId == noEvidence.Id);
        Assert.DoesNotContain(full.Items, item => item.ProductId == openingOnly.Id);
        Assert.DoesNotContain(full.Items, item => item.ProductId == zeroNet.Id);

        var detail = await owner.GetFromJsonAsync<C14AttentionDetailResult>(
            $"/api/today/attention/{risk.Id}");
        Assert.NotNull(detail);
        Assert.Equal(detail.NetSoldQuantity, detail.Evidence.Sum(item => item.QuantityContribution));
        Assert.Contains(detail.Evidence, item => item.SourceType == C14SourceTypes.Sale
            && item.Navigation.Type == TodaySourceNavigationTypes.Sale);
        Assert.Contains(detail.Evidence, item => item.SourceType == C14SourceTypes.Return
            && item.SourceId == restocked.Id && item.Navigation.Type == TodaySourceNavigationTypes.Return);
        Assert.Contains(detail.Evidence, item => item.SourceType == C14SourceTypes.Return
            && item.SourceId == notRestocked.Id && item.Navigation.Type == TodaySourceNavigationTypes.Return);
        Assert.Contains(detail.Evidence, item => item.SourceType == C14SourceTypes.SaleVoid
            && item.SourceId == riskVoid.Id
            && item.Navigation.Id == voidedRiskSale.Id);
        Assert.DoesNotContain(detail.Evidence, item => item.SourceId == excludedAtEnd.Id);

        var other = await CreateOwnerContextAsync("Other C14 store");
        using var otherOwner = other.Client;
        var isolated = await otherOwner.GetFromJsonAsync<C14AttentionListResult>(
            "/api/today/attention");
        Assert.Empty(isolated!.Items);
        Assert.Equal(HttpStatusCode.NotFound,
            (await otherOwner.GetAsync($"/api/today/attention/{risk.Id}")).StatusCode);
    }

    [Fact]
    public async Task AttentionUsesDstSafeCompletedBusinessDayBoundaries()
    {
        factory.SetUtcNow(new DateTimeOffset(2026, 3, 9, 16, 0, 0, TimeSpan.Zero));
        var context = await CreateOwnerContextAsync("C14 DST");
        using var owner = context.Client;
        using var zone = await owner.PutWithAntiforgeryAsync(
            "/api/store/timezone", JsonContent.Create(new { timeZoneId = "America/New_York" }));
        zone.EnsureSuccessStatusCode();

        var result = await owner.GetFromJsonAsync<C14AttentionListResult>(
            "/api/today/attention");

        Assert.NotNull(result);
        Assert.Equal(7, result.CompletedBusinessDays.Count);
        Assert.Contains(result.CompletedBusinessDays,
            day => day.EndUtc - day.StartUtc == TimeSpan.FromHours(23));
        Assert.Equal(result.CompletedBusinessDays[0].StartUtc, result.VelocityStartUtc);
        Assert.Equal(result.CompletedBusinessDays[^1].EndUtc, result.VelocityEndUtc);

        factory.SetUtcNow(new DateTimeOffset(2026, 11, 2, 17, 0, 0, TimeSpan.Zero));
        var fallContext = await CreateOwnerContextAsync("C14 DST fall");
        using var fallOwner = fallContext.Client;
        using var fallZone = await fallOwner.PutWithAntiforgeryAsync(
            "/api/store/timezone", JsonContent.Create(new { timeZoneId = "America/New_York" }));
        fallZone.EnsureSuccessStatusCode();
        var fallResult = await fallOwner.GetFromJsonAsync<C14AttentionListResult>(
            "/api/today/attention");

        Assert.NotNull(fallResult);
        Assert.Equal(7, fallResult.CompletedBusinessDays.Count);
        Assert.Contains(fallResult.CompletedBusinessDays,
            day => day.EndUtc - day.StartUtc == TimeSpan.FromHours(25));
    }

    [Fact]
    public async Task ExperimentEventsAreServerDerivedAuthorizedStoreSafeAndIdempotent()
    {
        factory.SetUtcNow(FixedUtcNow);
        var context = await CreateOwnerContextAsync("C14 events");
        using var owner = context.Client;
        var product = await CreateProductAsync(owner, "EVENT", "Event product", 1);
        var otherProduct = await CreateProductAsync(owner, "EVENT-2", "Other event product", 1);
        var inactiveProduct = await CreateProductAsync(owner, "EVENT-OFF", "Inactive event product", 1);
        using (var deactivate = await owner.PostWithAntiforgeryAsync(
                   $"/api/products/{inactiveProduct.Id}/deactivate", JsonContent.Create(new { })))
        {
            Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        }
        var eventId = Guid.NewGuid();

        var first = await PostEventAsync(owner, eventId, C14ExperimentEventTypes.TodayOpened);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstResult = (await first.Content.ReadFromJsonAsync<C14ExperimentEventResult>())!;
        Assert.False(firstResult.WasAlreadyRecorded);
        Assert.Equal(new DateOnly(2026, 9, 23), firstResult.BusinessDate);
        Assert.Equal(FixedUtcNow, firstResult.OccurredAt);

        var retry = await PostEventAsync(owner, eventId, C14ExperimentEventTypes.TodayOpened);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.True((await retry.Content.ReadFromJsonAsync<C14ExperimentEventResult>())!.WasAlreadyRecorded);

        using var conflict = await PostEventAsync(
            owner, eventId, C14ExperimentEventTypes.SignalShown,
            product.Id, C14AttentionKinds.OutOfStock);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("c14-event-id-reused", await ReadCodeAsync(conflict));

        var productEventId = Guid.NewGuid();
        using var productEvent = await PostEventAsync(
            owner, productEventId, C14ExperimentEventTypes.SignalShown,
            product.Id, C14AttentionKinds.OutOfStock);
        Assert.Equal(HttpStatusCode.OK, productEvent.StatusCode);
        using var differentProduct = await PostEventAsync(
            owner, productEventId, C14ExperimentEventTypes.SignalShown,
            otherProduct.Id, C14AttentionKinds.OutOfStock);
        Assert.Equal(HttpStatusCode.Conflict, differentProduct.StatusCode);
        using var differentKind = await PostEventAsync(
            owner, productEventId, C14ExperimentEventTypes.SignalShown,
            product.Id, C14AttentionKinds.NegativeStock);
        Assert.Equal(HttpStatusCode.Conflict, differentKind.StatusCode);

        var concurrentId = Guid.NewGuid();
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => PostEventAsync(
            owner, concurrentId, C14ExperimentEventTypes.WhyOpened,
            product.Id, C14AttentionKinds.OutOfStock)));
        Assert.All(concurrent, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        foreach (var response in concurrent) response.Dispose();

        using var invalidType = await PostEventAsync(owner, Guid.NewGuid(), "GenericClick");
        Assert.Equal(HttpStatusCode.BadRequest, invalidType.StatusCode);
        Assert.Equal("invalid-c14-event-type", await ReadCodeAsync(invalidType));
        using var missingProduct = await PostEventAsync(
            owner, Guid.NewGuid(), C14ExperimentEventTypes.WhyOpened,
            null, C14AttentionKinds.OutOfStock);
        Assert.Equal(HttpStatusCode.BadRequest, missingProduct.StatusCode);
        using var inactive = await PostEventAsync(
            owner, Guid.NewGuid(), C14ExperimentEventTypes.PurchaseDraftStarted,
            inactiveProduct.Id, C14AttentionKinds.LowStockRisk);
        Assert.Equal(HttpStatusCode.NotFound, inactive.StatusCode);

        var other = await CreateOwnerContextAsync("Other event store");
        using var otherOwner = other.Client;
        using var crossStore = await PostEventAsync(
            otherOwner, Guid.NewGuid(), C14ExperimentEventTypes.WhyOpened,
            product.Id, C14AttentionKinds.OutOfStock);
        Assert.Equal(HttpStatusCode.NotFound, crossStore.StatusCode);

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        using var forbidden = await PostEventAsync(cashier, Guid.NewGuid(), C14ExperimentEventTypes.TodayOpened);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var anonymous = factory.CreateHttpsClient();
        using var unauthorized = await anonymous.PostAsync(
            "/api/experiments/c14/events",
            JsonContent.Create(new
            {
                eventId = Guid.NewGuid(),
                eventType = C14ExperimentEventTypes.TodayOpened,
                productId = (Guid?)null,
                attentionKind = (string?)null
            }));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await owner.PutWithAntiforgeryAsync(
                $"/api/experiments/c14/events/{eventId}",
                JsonContent.Create(new { eventType = C14ExperimentEventTypes.TodayOpened })))
            .StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await owner.DeleteAsync($"/api/experiments/c14/events/{eventId}"))
            .StatusCode);

        var persisted = await factory.WithDbContextAsync(async db => new
        {
            Count = await db.C14ExperimentEvents.CountAsync(item => item.EventId == eventId),
            ConcurrentCount = await db.C14ExperimentEvents.CountAsync(item => item.EventId == concurrentId),
            ProductCount = await db.Products.CountAsync(item => item.StoreId == context.StoreId)
        });
        Assert.Equal(1, persisted.Count);
        Assert.Equal(1, persisted.ConcurrentCount);
        Assert.Equal(3, persisted.ProductCount);
    }

    [Fact]
    public async Task AttentionEndpointsAreOwnerOnlyAndPaginationIsBounded()
    {
        factory.SetUtcNow(FixedUtcNow);
        var context = await CreateOwnerContextAsync("C14 authorization");
        using var owner = context.Client;
        var product = await CreateProductAsync(owner, "AUTH", "Authorization product", 1);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await owner.GetAsync("/api/today/attention?page=0&pageSize=20")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await owner.GetAsync("/api/today/attention?page=1&pageSize=101")).StatusCode);

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.GetAsync("/api/today/attention")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cashier.GetAsync($"/api/today/attention/{product.Id}")).StatusCode);

        using var anonymous = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/today/attention")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync($"/api/today/attention/{product.Id}")).StatusCode);
    }

    [Fact]
    public async Task StorePartialHistoryDoesNotProduceLowStockRisk()
    {
        factory.SetUtcNow(FixedUtcNow);
        var context = await CreateOwnerContextAsync("C14 partial store");
        using var owner = context.Client;
        var product = await CreateProductAsync(owner, "PARTIAL-STORE", "Partial store product", 10);
        var sale = await owner.CompleteSaleAsync(
            Guid.NewGuid(), null, [(product.Id, 7)], (84_000, "Cash"));

        await factory.WithDbContextAsync(async db =>
        {
            await db.Stores.Where(item => item.Id == context.StoreId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CreatedAt, VelocityStartUtc.AddTicks(1)));
            await db.Products.Where(item => item.Id == product.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.CreatedAt, VelocityStartUtc));
            await SetSaleTimeAsync(db, sale.Id, VelocityStartUtc);
            return 0;
        });

        var result = await owner.GetFromJsonAsync<C14AttentionListResult>(
            "/api/today/attention");
        Assert.NotNull(result);
        Assert.Equal(C14HistoryCoverageTypes.PartialObservation, result.EvaluationCoverage);
        Assert.DoesNotContain(result.Items, item => item.ProductId == product.Id);
    }

    private async Task<OwnerContext> CreateOwnerContextAsync(string storeName)
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        return new OwnerContext(client, store.Id);
    }

    private static Task<ProductResult> CreateProductAsync(
        HttpClient client,
        string sku,
        string name,
        decimal openingQuantity) =>
        client.CreateProductAsync(
            sku: sku,
            name: name,
            openingQuantity: openingQuantity,
            openingCost: 1_000);

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

    private static async Task SetReturnTimeAsync(
        SimpleStore.Infrastructure.Persistence.ApplicationDbContext db,
        Guid returnId,
        DateTimeOffset occurredAt)
    {
        await db.Returns.Where(item => item.Id == returnId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CompletedAt, occurredAt));
        await db.ReturnRefundPayments.Where(item => item.ReturnId == returnId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, occurredAt));
    }

    private static async Task<HttpResponseMessage> PostEventAsync(
        HttpClient client,
        Guid eventId,
        string eventType,
        Guid? productId = null,
        string? attentionKind = null) =>
        await client.PostWithAntiforgeryAsync(
            "/api/experiments/c14/events",
            JsonContent.Create(new { eventId, eventType, productId, attentionKind }));

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("code").GetString();
    }

    private sealed record OwnerContext(HttpClient Client, Guid StoreId);
}
