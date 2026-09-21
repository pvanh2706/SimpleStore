using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Customers;
using SimpleStore.Application.Products;
using SimpleStore.Application.Purchases;
using SimpleStore.Application.Sales;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice3IntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task OwnerAndCashierCanReadPolicyButOnlyOwnerCanChangeItAndChangesAreAudited()
    {
        var context = await CreateOwnerContextAsync("Settings");
        using var owner = context.Client;
        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);

        var initial = await cashier.GetFromJsonAsync<StoreOperationalSettingsResult>(
            "/api/store/operational-settings");
        Assert.NotNull(initial);
        Assert.False(initial.AllowNegativeStock);

        using var forbidden = await cashier.PutWithAntiforgeryAsync(
            "/api/store/operational-settings/negative-stock",
            JsonContent.Create(new { allowNegativeStock = true }));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var updated = await owner.SetNegativeStockAsync(true);
        Assert.True(updated.AllowNegativeStock);
        var audit = await factory.WithDbContextAsync(dbContext =>
            dbContext.NegativeStockSettingAudits.SingleAsync(item => item.StoreId == context.StoreId));
        Assert.False(audit.OldValue);
        Assert.True(audit.NewValue);
    }

    [Fact]
    public async Task CustomersAreSearchableByOwnerAndCashierAndStoreScoped()
    {
        var contextA = await CreateOwnerContextAsync("Customers A");
        var contextB = await CreateOwnerContextAsync("Customers B");
        using var ownerA = contextA.Client;
        using var ownerB = contextB.Client;
        var customer = await ownerA.CreateCustomerAsync("Nguyen Van An", "0912345678");
        var cashierCredentials = await factory.CreateCashierAsync(contextA.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);

        var page = await cashier.GetFromJsonAsync<CustomerListResult>(
            "/api/customers?search=0912&page=1&pageSize=20");
        Assert.NotNull(page);
        Assert.Equal(customer.Id, Assert.Single(page.Items).Id);
        var cashierCreated = await cashier.CreateCustomerAsync("Cashier customer", null);
        Assert.NotEqual(Guid.Empty, cashierCreated.Id);
        using var crossStore = await ownerB.GetAsync($"/api/customers/{customer.Id}");
        Assert.Equal(HttpStatusCode.NotFound, crossStore.StatusCode);
    }

    [Fact]
    public async Task CashierCompletesAuthoritativeSaleWithPaymentsInventoryAndIdempotency()
    {
        var context = await CreateOwnerContextAsync("Sale completion");
        using var owner = context.Client;
        var product = await owner.CreateProductAsync(
            sku: $"SALE-{Guid.NewGuid():N}",
            name: "Coffee",
            openingQuantity: 10,
            openingCost: 8_000);
        var customer = await owner.CreateCustomerAsync("Credit customer");
        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        var operationId = Guid.NewGuid();

        var completed = await cashier.CompleteSaleAsync(
            operationId,
            customer.Id,
            [(product.Id, 2)],
            (10_000, "Cash"),
            (4_000, "Transfer"));
        var retry = await cashier.CompleteSaleAsync(
            operationId,
            customer.Id,
            [(product.Id, 2)],
            (10_000, "Cash"),
            (4_000, "Transfer"));

        Assert.Equal("Completed", completed.Status);
        Assert.Equal(24_000, completed.TotalAmount);
        Assert.Equal(14_000, completed.PaidAmount);
        Assert.Equal(10_000, completed.OutstandingAmount);
        Assert.Equal(12_000, Assert.Single(completed.Lines).UnitSalePrice);
        Assert.Equal(8_000, Assert.Single(completed.Lines).UnitCostAtSale);
        Assert.Equal("Reliable", Assert.Single(completed.Lines).CostReliability);
        Assert.True(retry.WasAlreadyCompleted);
        Assert.Equal(completed.Id, retry.Id);
        using var reused = await cashier.PostWithAntiforgeryAsync(
            "/api/sales/complete",
            Slice3HttpClient.SaleContent(
                operationId,
                customer.Id,
                [(product.Id, 1)],
                (10_000, "Cash")));
        Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
        Assert.Equal("idempotency-key-reused", await ReadCodeAsync(reused));

        var state = await factory.WithDbContextAsync(async dbContext => new
        {
            Balance = await dbContext.InventoryBalances.SingleAsync(item => item.ProductId == product.Id),
            Movement = await dbContext.InventoryMovements.SingleAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Sale),
            Payments = await dbContext.SalePayments.CountAsync(item => item.SaleId == completed.Id),
            Operations = await dbContext.BusinessOperations.CountAsync(item => item.OperationId == operationId)
        });
        Assert.Equal(8, state.Balance.QuantityOnHand);
        Assert.Equal(64_000, state.Balance.InventoryValue);
        Assert.Equal(-2, state.Movement.QuantityDelta);
        Assert.Equal(-16_000, state.Movement.InventoryValueDelta);
        Assert.Equal("SaleLine", state.Movement.SourceType);
        Assert.Equal(2, state.Payments);
        Assert.Equal(1, state.Operations);

        using (var update = await owner.PutWithAntiforgeryAsync(
            $"/api/products/{product.Id}",
            JsonContent.Create(new
            {
                sku = product.Sku,
                barcode = product.Barcode,
                name = product.Name,
                unit = product.Unit,
                salePrice = 99_000,
                referencePurchaseCost = product.ReferencePurchaseCost
            })))
        {
            update.EnsureSuccessStatusCode();
        }
        var historical = await cashier.GetFromJsonAsync<SaleResult>($"/api/sales/{completed.Id}");
        Assert.Equal(12_000, Assert.Single(historical!.Lines).UnitSalePrice);

        var operation = await cashier.GetFromJsonAsync<OperationStatusResult>(
            $"/api/operations/{operationId}");
        Assert.Equal(completed.Id, operation!.ResultReference);
    }

    [Fact]
    public async Task CreditAndCrossStoreReferencesAreRejectedWithoutPartialEffects()
    {
        var contextA = await CreateOwnerContextAsync("Validation A");
        var contextB = await CreateOwnerContextAsync("Validation B");
        using var clientA = contextA.Client;
        using var clientB = contextB.Client;
        var productA = await clientA.CreateProductAsync(name: "A", openingQuantity: 2, openingCost: 10);
        var productB = await clientB.CreateProductAsync(name: "B", openingQuantity: 2, openingCost: 10);
        var customerB = await clientB.CreateCustomerAsync("Store B customer");

        using var noCustomer = await clientA.PostWithAntiforgeryAsync(
            "/api/sales/complete",
            Slice3HttpClient.SaleContent(Guid.NewGuid(), null, [(productA.Id, 1)]));
        Assert.Equal(HttpStatusCode.BadRequest, noCustomer.StatusCode);
        Assert.Equal("customer-required-for-credit", await ReadCodeAsync(noCustomer));

        using var crossProduct = await clientA.PostWithAntiforgeryAsync(
            "/api/sales/complete",
            Slice3HttpClient.SaleContent(Guid.NewGuid(), null, [(productB.Id, 1)], (12_000, "Cash")));
        using var crossCustomer = await clientA.PostWithAntiforgeryAsync(
            "/api/sales/complete",
            Slice3HttpClient.SaleContent(Guid.NewGuid(), customerB.Id, [(productA.Id, 1)]));
        Assert.Equal(HttpStatusCode.BadRequest, crossProduct.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossCustomer.StatusCode);

        var state = await factory.WithDbContextAsync(async dbContext => new
        {
            Sales = await dbContext.Sales.CountAsync(item => item.StoreId == contextA.StoreId),
            Quantity = await dbContext.InventoryBalances.Where(item => item.ProductId == productA.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(0, state.Sales);
        Assert.Equal(2, state.Quantity);
    }

    [Fact]
    public async Task SaleDetailListAndOperationStatusAreStoreScoped()
    {
        var contextA = await CreateOwnerContextAsync("Sale scope A");
        var contextB = await CreateOwnerContextAsync("Sale scope B");
        using var clientA = contextA.Client;
        using var clientB = contextB.Client;
        var product = await clientA.CreateProductAsync(name: "Scoped", openingQuantity: 1, openingCost: 10);
        var operationId = Guid.NewGuid();
        var sale = await clientA.CompleteSaleAsync(
            operationId,
            null,
            [(product.Id, 1)],
            (12_000, "Cash"));

        using var saleResponse = await clientB.GetAsync($"/api/sales/{sale.Id}");
        using var operationResponse = await clientB.GetAsync($"/api/operations/{operationId}");
        var list = await clientB.GetFromJsonAsync<SaleListResult>("/api/sales?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.NotFound, saleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, operationResponse.StatusCode);
        Assert.Empty(list!.Items);
    }

    [Fact]
    public async Task NegativeStockOffReturnsStructuredShortagesAndRollsBackAllEffects()
    {
        var context = await CreateOwnerContextAsync("Shortage");
        using var client = context.Client;
        var productA = await client.CreateProductAsync(name: "A", openingQuantity: 1, openingCost: 10);
        var productB = await client.CreateProductAsync(name: "B", openingQuantity: 5, openingCost: 10);
        var operationId = Guid.NewGuid();

        using var response = await client.PostWithAntiforgeryAsync(
            "/api/sales/complete",
            Slice3HttpClient.SaleContent(
                operationId,
                null,
                [(productA.Id, 3), (productB.Id, 1)],
                (48_000, "Cash")));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal("insufficient-stock", body.RootElement.GetProperty("code").GetString());
        var shortage = Assert.Single(body.RootElement.GetProperty("shortages").EnumerateArray());
        Assert.Equal(productA.Id, shortage.GetProperty("productId").GetGuid());
        Assert.Equal(2, shortage.GetProperty("shortageQuantity").GetDecimal());
        var counts = await factory.WithDbContextAsync(async dbContext => new
        {
            Sales = await dbContext.Sales.CountAsync(item => item.StoreId == context.StoreId),
            Movements = await dbContext.InventoryMovements.CountAsync(item =>
                item.StoreId == context.StoreId && item.MovementType == InventoryMovementType.Sale),
            Operations = await dbContext.BusinessOperations.CountAsync(item => item.OperationId == operationId)
        });
        Assert.Equal(0, counts.Sales);
        Assert.Equal(0, counts.Movements);
        Assert.Equal(0, counts.Operations);
    }

    [Fact]
    public async Task NegativeStockUsesReferenceOrUnavailableCostWithoutRecomputingAverage()
    {
        var context = await CreateOwnerContextAsync("Negative cost");
        using var client = context.Client;
        await client.SetNegativeStockAsync(true);
        var withReference = await client.CreateProductAsync(name: "Reference only");
        var withoutReference = await client.CreateProductAsync(name: "No cost");
        using (var update = await client.PutWithAntiforgeryAsync(
            $"/api/products/{withReference.Id}",
            JsonContent.Create(new
            {
                sku = withReference.Sku,
                barcode = withReference.Barcode,
                name = withReference.Name,
                unit = withReference.Unit,
                salePrice = withReference.SalePrice,
                referencePurchaseCost = 0
            })))
        {
            update.EnsureSuccessStatusCode();
        }

        var sale = await client.CompleteSaleAsync(
            Guid.NewGuid(),
            null,
            [(withReference.Id, 1), (withoutReference.Id, 1)],
            (24_000, "Cash"));

        var estimated = sale.Lines.Single(line => line.ProductId == withReference.Id);
        var unavailable = sale.Lines.Single(line => line.ProductId == withoutReference.Id);
        Assert.Equal(0, estimated.UnitCostAtSale);
        Assert.Equal("Estimated", estimated.CostReliability);
        Assert.Equal(0, unavailable.UnitCostAtSale);
        Assert.Equal("Unavailable", unavailable.CostReliability);
        var balances = await factory.WithDbContextAsync(dbContext => dbContext.InventoryBalances
            .Where(item => item.ProductId == withReference.Id || item.ProductId == withoutReference.Id)
            .ToArrayAsync());
        Assert.All(balances, balance =>
        {
            Assert.Equal(-1, balance.QuantityOnHand);
            Assert.Equal(0, balance.InventoryValue);
            Assert.False(balance.HasAverageCost);
        });
    }

    [Fact]
    public async Task PurchaseResidualNegativeValueMarksAverageUnknownAndNextSaleUsesLatestReference()
    {
        var context = await CreateOwnerContextAsync("Residual valuation");
        using var client = context.Client;
        await client.SetNegativeStockAsync(true);
        var product = await client.CreateProductAsync(name: "Residual", openingQuantity: 10, openingCost: 10);
        await factory.WithDbContextAsync(async dbContext =>
        {
            var balance = await dbContext.InventoryBalances.SingleAsync(item => item.ProductId == product.Id);
            balance.IssueSale(20, 200, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
            return true;
        });
        var supplier = await client.CreateSupplierAsync();
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 11, 1));
        _ = await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());

        var sale = await client.CompleteSaleAsync(
            Guid.NewGuid(),
            null,
            [(product.Id, 1)],
            (12_000, "Cash"));
        var line = Assert.Single(sale.Lines);
        Assert.Equal(1, line.UnitCostAtSale);
        Assert.Equal("Estimated", line.CostReliability);
        var balanceAfterPurchaseAndSale = await factory.WithDbContextAsync(dbContext =>
            dbContext.InventoryBalances.SingleAsync(item => item.ProductId == product.Id));
        Assert.Equal(0, balanceAfterPurchaseAndSale.QuantityOnHand);
        Assert.Equal(-90, balanceAfterPurchaseAndSale.InventoryValue);
        Assert.False(balanceAfterPurchaseAndSale.HasAverageCost);
        Assert.Equal(10, balanceAfterPurchaseAndSale.AverageCost);
    }

    [Fact]
    public async Task PurchaseAtNegativeOrZeroQuantityPreservesKnownCostAndZeroValueCanReestablishReliableZero()
    {
        var context = await CreateOwnerContextAsync("Purchase edge transitions");
        using var client = context.Client;
        await client.SetNegativeStockAsync(true);
        var stillNegative = await client.CreateProductAsync(name: "Still negative", openingQuantity: 10, openingCost: 10);
        var exactlyZero = await client.CreateProductAsync(name: "Exactly zero", openingQuantity: 10, openingCost: 10);
        var zeroValue = await client.CreateProductAsync(name: "Zero value", openingQuantity: 1, openingCost: 10);
        await factory.WithDbContextAsync(async dbContext =>
        {
            (await dbContext.InventoryBalances.SingleAsync(item => item.ProductId == stillNegative.Id))
                .IssueSale(20, 200, DateTimeOffset.UtcNow);
            (await dbContext.InventoryBalances.SingleAsync(item => item.ProductId == exactlyZero.Id))
                .IssueSale(20, 200, DateTimeOffset.UtcNow);
            (await dbContext.InventoryBalances.SingleAsync(item => item.ProductId == zeroValue.Id))
                .IssueSale(2, 20, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
            return true;
        });
        var supplier = await client.CreateSupplierAsync();
        var purchase = await client.CreatePurchaseAsync(
            supplier.Id,
            (stillNegative.Id, 5, 10),
            (exactlyZero.Id, 10, 10),
            (zeroValue.Id, 2, 5));
        _ = await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());

        var balances = await factory.WithDbContextAsync(dbContext => dbContext.InventoryBalances
            .Where(item => item.ProductId == stillNegative.Id
                || item.ProductId == exactlyZero.Id
                || item.ProductId == zeroValue.Id)
            .ToDictionaryAsync(item => item.ProductId));
        Assert.Equal(-5, balances[stillNegative.Id].QuantityOnHand);
        Assert.Equal(-50, balances[stillNegative.Id].InventoryValue);
        Assert.Equal(10, balances[stillNegative.Id].AverageCost);
        Assert.True(balances[stillNegative.Id].HasAverageCost);
        Assert.Equal(0, balances[exactlyZero.Id].QuantityOnHand);
        Assert.Equal(0, balances[exactlyZero.Id].InventoryValue);
        Assert.Equal(10, balances[exactlyZero.Id].AverageCost);
        Assert.True(balances[exactlyZero.Id].HasAverageCost);
        Assert.Equal(1, balances[zeroValue.Id].QuantityOnHand);
        Assert.Equal(0, balances[zeroValue.Id].InventoryValue);
        Assert.Equal(0, balances[zeroValue.Id].AverageCost);
        Assert.True(balances[zeroValue.Id].HasAverageCost);

        var sale = await client.CompleteSaleAsync(
            Guid.NewGuid(),
            null,
            [(zeroValue.Id, 1)],
            (12_000, "Cash"));
        Assert.Equal(0, Assert.Single(sale.Lines).UnitCostAtSale);
        Assert.Equal("Reliable", Assert.Single(sale.Lines).CostReliability);
    }

    [Fact]
    public async Task ConcurrentSameOperationIdReturnsOneLogicalSale()
    {
        var context = await CreateOwnerContextAsync("Concurrent operation");
        using var setupClient = context.Client;
        var product = await setupClient.CreateProductAsync(name: "Idempotent sale", openingQuantity: 2, openingCost: 10);
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await clientB.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        var operationId = Guid.NewGuid();

        var results = await Task.WhenAll(
            clientA.CompleteSaleAsync(operationId, null, [(product.Id, 1)], (12_000, "Cash")),
            clientB.CompleteSaleAsync(operationId, null, [(product.Id, 1)], (12_000, "Cash")));

        Assert.Equal(results[0].Id, results[1].Id);
        Assert.Contains(results, result => result.WasAlreadyCompleted);
        var state = await factory.WithDbContextAsync(async dbContext => new
        {
            Sales = await dbContext.Sales.CountAsync(item => item.Id == results[0].Id),
            Payments = await dbContext.SalePayments.CountAsync(item => item.SaleId == results[0].Id),
            Movements = await dbContext.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Sale),
            Operations = await dbContext.BusinessOperations.CountAsync(item => item.OperationId == operationId)
        });
        Assert.Equal(1, state.Sales);
        Assert.Equal(1, state.Payments);
        Assert.Equal(1, state.Movements);
        Assert.Equal(1, state.Operations);
    }

    [Fact]
    public async Task ConcurrentSalesForLastUnitCommitOnlyOneLogicalSale()
    {
        var context = await CreateOwnerContextAsync("Concurrent sale");
        using var setupClient = context.Client;
        var product = await setupClient.CreateProductAsync(name: "Last unit", openingQuantity: 1, openingCost: 10);
        var credentials = context.Credentials;
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(credentials.Email, credentials.Password);
        await clientB.LoginAsync(credentials.Email, credentials.Password);

        var responses = await Task.WhenAll(
            clientA.PostWithAntiforgeryAsync(
                "/api/sales/complete",
                Slice3HttpClient.SaleContent(Guid.NewGuid(), null, [(product.Id, 1)], (12_000, "Cash"))),
            clientB.PostWithAntiforgeryAsync(
                "/api/sales/complete",
                Slice3HttpClient.SaleContent(Guid.NewGuid(), null, [(product.Id, 1)], (12_000, "Cash"))));
        using var responseA = responses[0];
        using var responseB = responses[1];
        Assert.Single(responses, response => response.IsSuccessStatusCode);
        var rejected = Assert.Single(responses, response => !response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);

        var state = await factory.WithDbContextAsync(async dbContext => new
        {
            Sales = await dbContext.Sales.CountAsync(item => item.StoreId == context.StoreId),
            Payments = await dbContext.SalePayments.CountAsync(item => item.StoreId == context.StoreId),
            Movements = await dbContext.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Sale),
            Quantity = await dbContext.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(1, state.Sales);
        Assert.Equal(1, state.Payments);
        Assert.Equal(1, state.Movements);
        Assert.Equal(0, state.Quantity);
    }

    [Fact]
    public async Task ConcurrentSaleAndPurchaseShareInventoryLockWithoutLostUpdate()
    {
        var context = await CreateOwnerContextAsync("Sale purchase concurrency");
        using var setupClient = context.Client;
        var product = await setupClient.CreateProductAsync(name: "Shared lock", openingQuantity: 10, openingCost: 10);
        var supplier = await setupClient.CreateSupplierAsync();
        var purchase = await setupClient.CreatePurchaseAsync(supplier.Id, (product.Id, 10, 20));
        using var saleClient = factory.CreateHttpsClient();
        using var purchaseClient = factory.CreateHttpsClient();
        await saleClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await purchaseClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);

        await Task.WhenAll(
            saleClient.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 5)], (60_000, "Cash")),
            purchaseClient.CompletePurchaseAsync(purchase.Id, Guid.NewGuid()));

        var state = await factory.WithDbContextAsync(async dbContext => new
        {
            Balance = await dbContext.InventoryBalances.SingleAsync(item => item.ProductId == product.Id),
            SaleMovements = await dbContext.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Sale),
            PurchaseMovements = await dbContext.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Purchase)
        });
        Assert.Equal(15, state.Balance.QuantityOnHand);
        Assert.True(state.Balance.InventoryValue is 225m or 250m);
        Assert.Equal(1, state.SaleMovements);
        Assert.Equal(1, state.PurchaseMovements);
    }

    private async Task<OwnerContext> CreateOwnerContextAsync(string storeName)
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        return new OwnerContext(client, store.Id, credentials);
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("code").GetString();
    }

    private sealed record OwnerContext(
        HttpClient Client,
        Guid StoreId,
        (string Email, string Password) Credentials);
}
