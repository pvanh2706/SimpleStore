using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Products;
using SimpleStore.Application.Purchases;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice2IntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SupplierCrudLiteIsStoreScopedAndOwnerOnly()
    {
        var (ownerClient, storeId, _) = await CreateOwnerContextAsync("Supplier A");
        var (otherClient, _, _) = await CreateOwnerContextAsync("Supplier B");
        using (ownerClient)
        using (otherClient)
        {
            var supplier = await ownerClient.CreateSupplierAsync("Công ty An", "0909123456");
            using var update = await ownerClient.PutWithAntiforgeryAsync(
                $"/api/suppliers/{supplier.Id}",
                JsonContent.Create(new { name = "Công ty An mới", phone = "0911000000", note = "Ưu tiên" }));
            update.EnsureSuccessStatusCode();

            var page = await ownerClient.GetFromJsonAsync<SupplierListResult>(
                "/api/suppliers?search=0911&isActive=true&page=1&pageSize=20");
            Assert.NotNull(page);
            Assert.Single(page.Items);
            Assert.Equal("Công ty An mới", page.Items[0].Name);

            var crossStore = await otherClient.GetAsync($"/api/suppliers/{supplier.Id}");
            Assert.Equal(HttpStatusCode.NotFound, crossStore.StatusCode);

            var cashierCredentials = await factory.CreateCashierAsync(storeId);
            using var cashierClient = factory.CreateHttpsClient();
            await cashierClient.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
            using var cashierRead = await cashierClient.GetAsync("/api/suppliers");
            using var cashierWrite = await cashierClient.PostWithAntiforgeryAsync(
                "/api/suppliers",
                JsonContent.Create(new { name = "Không được tạo" }));
            using var cashierPurchase = await cashierClient.PostWithAntiforgeryAsync(
                "/api/purchases",
                PurchaseContent(supplier.Id));
            Assert.Equal(HttpStatusCode.Forbidden, cashierRead.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, cashierWrite.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, cashierPurchase.StatusCode);

            using var deactivate = await ownerClient.PostWithAntiforgeryAsync(
                $"/api/suppliers/{supplier.Id}/deactivate",
                JsonContent.Create(new { }));
            Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
            var detail = await ownerClient.GetFromJsonAsync<SupplierResult>($"/api/suppliers/{supplier.Id}");
            Assert.NotNull(detail);
            Assert.False(detail.IsActive);
        }
    }

    [Fact]
    public async Task DraftCanBeCreatedAndEditedButDuplicateAndCrossStoreReferencesAreRejected()
    {
        var (client, _, _) = await CreateOwnerContextAsync("Draft A");
        var (otherClient, _, _) = await CreateOwnerContextAsync("Draft B");
        using (client)
        using (otherClient)
        {
            var supplier = await client.CreateSupplierAsync();
            var productA = await client.CreateProductAsync(name: "A");
            var productB = await client.CreateProductAsync(name: "B");
            var otherSupplier = await otherClient.CreateSupplierAsync("NCC Store B");
            var otherProduct = await otherClient.CreateProductAsync(name: "Product Store B");

            var draft = await client.CreatePurchaseAsync(supplier.Id, (productA.Id, 1.005m, 1m));
            Assert.Equal("Draft", draft.Status);
            Assert.Equal(1.01m, draft.TotalAmount);

            using var update = await client.PutWithAntiforgeryAsync(
                $"/api/purchases/{draft.Id}",
                PurchaseContent(supplier.Id, (productB.Id, 3, 20)));
            update.EnsureSuccessStatusCode();
            var edited = await update.Content.ReadFromJsonAsync<PurchaseResult>();
            Assert.NotNull(edited);
            Assert.Equal(productB.Id, Assert.Single(edited.Lines).ProductId);

            using var duplicate = await client.PostWithAntiforgeryAsync(
                "/api/purchases",
                PurchaseContent(supplier.Id, (productA.Id, 1, 10), (productA.Id, 2, 20)));
            Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
            Assert.Equal("duplicate-purchase-product", await ReadCodeAsync(duplicate));

            using var crossSupplier = await client.PostWithAntiforgeryAsync(
                "/api/purchases",
                PurchaseContent(otherSupplier.Id, (productA.Id, 1, 10)));
            using var crossProduct = await client.PostWithAntiforgeryAsync(
                "/api/purchases",
                PurchaseContent(supplier.Id, (otherProduct.Id, 1, 10)));
            Assert.Equal(HttpStatusCode.NotFound, crossSupplier.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, crossProduct.StatusCode);
        }
    }

    [Fact]
    public async Task CompleteWithoutPaymentUpdatesInventoryCostMovementAndFullDebt()
    {
        var (client, _, _) = await CreateOwnerContextAsync("Complete no payment");
        using (client)
        {
            var supplier = await client.CreateSupplierAsync();
            var product = await client.CreateProductAsync(
                name: "Cà phê",
                openingQuantity: 10,
                openingCost: 10_000);
            var draft = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 20, 13_000));

            var completed = await client.CompletePurchaseAsync(draft.Id, Guid.NewGuid());

            Assert.Equal("Completed", completed.Status);
            Assert.Equal(260_000, completed.TotalAmount);
            Assert.Equal(0, completed.PaidAmount);
            Assert.Equal(260_000, completed.OutstandingAmount);
            Assert.Empty(completed.Payments);

            var updatedProduct = await client.GetFromJsonAsync<ProductResult>($"/api/products/{product.Id}");
            Assert.NotNull(updatedProduct);
            Assert.Equal(30, updatedProduct.QuantityOnHand);
            Assert.Equal(360_000, updatedProduct.InventoryValue);
            Assert.Equal(12_000, updatedProduct.AverageCost);
            Assert.Equal(13_000, updatedProduct.ReferencePurchaseCost);

            var movements = await client.GetFromJsonAsync<InventoryMovementResult[]>(
                $"/api/products/{product.Id}/movements");
            Assert.NotNull(movements);
            var purchaseMovement = Assert.Single(movements, movement => movement.Type == "Purchase");
            Assert.Equal(20, purchaseMovement.QuantityDelta);
            Assert.Equal(260_000, purchaseMovement.InventoryValueDelta);
            Assert.Equal(Assert.Single(completed.Lines).Id, purchaseMovement.SourceId);

            var supplierDetail = await client.GetFromJsonAsync<SupplierResult>(
                $"/api/suppliers/{supplier.Id}");
            Assert.NotNull(supplierDetail);
            Assert.Equal(260_000, supplierDetail.OutstandingAmount);
        }
    }

    [Fact]
    public async Task PaymentsSupportOneOrManyAndRejectOverpayment()
    {
        var (client, _, _) = await CreateOwnerContextAsync("Payments");
        using (client)
        {
            var supplier = await client.CreateSupplierAsync();
            var product = await client.CreateProductAsync();
            var onePaymentDraft = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 2, 100));
            var onePayment = await client.CompletePurchaseAsync(
                onePaymentDraft.Id,
                Guid.NewGuid(),
                (50, "Cash"));
            Assert.Equal(50, onePayment.PaidAmount);
            Assert.Equal(150, onePayment.OutstandingAmount);

            var manyPaymentDraft = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 3, 100));
            var manyPayments = await client.CompletePurchaseAsync(
                manyPaymentDraft.Id,
                Guid.NewGuid(),
                (75, "Cash"),
                (125, "Transfer"));
            Assert.Equal(200, manyPayments.PaidAmount);
            Assert.Equal(100, manyPayments.OutstandingAmount);
            Assert.Equal(2, manyPayments.Payments.Count);

            var overpaymentDraft = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
            using var overpayment = await client.PostWithAntiforgeryAsync(
                $"/api/purchases/{overpaymentDraft.Id}/complete",
                CompletionContent(Guid.NewGuid(), (100.01m, "Cash")));
            Assert.Equal(HttpStatusCode.BadRequest, overpayment.StatusCode);
            Assert.Equal("purchase-overpayment", await ReadCodeAsync(overpayment));
        }
    }

    [Fact]
    public async Task CompletedPurchaseIsImmutableAndFailureRollsBackEveryEffect()
    {
        var (client, storeId, _) = await CreateOwnerContextAsync("Rollback");
        using (client)
        {
            var supplier = await client.CreateSupplierAsync();
            var productA = await client.CreateProductAsync(name: "Active", openingQuantity: 1, openingCost: 10);
            var productB = await client.CreateProductAsync(name: "Deactivate", openingQuantity: 1, openingCost: 10);
            var draft = await client.CreatePurchaseAsync(
                supplier.Id,
                (productA.Id, 1, 20),
                (productB.Id, 1, 30));
            using var deactivate = await client.PostWithAntiforgeryAsync(
                $"/api/products/{productB.Id}/deactivate",
                JsonContent.Create(new { }));
            deactivate.EnsureSuccessStatusCode();

            using var failed = await client.PostWithAntiforgeryAsync(
                $"/api/purchases/{draft.Id}/complete",
                CompletionContent(Guid.NewGuid(), (10, "Cash")));
            Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);

            var databaseState = await factory.WithDbContextAsync(async dbContext => new
            {
                Status = await dbContext.Purchases.Where(item => item.Id == draft.Id)
                    .Select(item => item.Status).SingleAsync(),
                Payments = await dbContext.PurchasePayments.CountAsync(item => item.PurchaseId == draft.Id),
                Movements = await dbContext.InventoryMovements.CountAsync(item =>
                    item.StoreId == storeId && item.SourceType == "PurchaseLine"),
                QuantityA = await dbContext.InventoryBalances.Where(item => item.ProductId == productA.Id)
                    .Select(item => item.QuantityOnHand).SingleAsync()
            });
            Assert.Equal(PurchaseStatus.Draft, databaseState.Status);
            Assert.Equal(0, databaseState.Payments);
            Assert.Equal(0, databaseState.Movements);
            Assert.Equal(1, databaseState.QuantityA);

            var successful = await client.CreatePurchaseAsync(supplier.Id, (productA.Id, 1, 20));
            _ = await client.CompletePurchaseAsync(successful.Id, Guid.NewGuid());
            using var editCompleted = await client.PutWithAntiforgeryAsync(
                $"/api/purchases/{successful.Id}",
                PurchaseContent(supplier.Id, (productA.Id, 2, 20)));
            Assert.Equal(HttpStatusCode.Conflict, editCompleted.StatusCode);
            Assert.Equal("purchase-completed-immutable", await ReadCodeAsync(editCompleted));
        }
    }

    [Fact]
    public async Task IdempotentRetryReturnsSameResultAndDifferentFingerprintConflicts()
    {
        var (client, _, _) = await CreateOwnerContextAsync("Idempotency");
        using (client)
        {
            var supplier = await client.CreateSupplierAsync();
            var product = await client.CreateProductAsync();
            var draft = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 2, 100));
            var operationId = Guid.NewGuid();

            var first = await client.CompletePurchaseAsync(draft.Id, operationId, (50, "Cash"));
            var retry = await client.CompletePurchaseAsync(draft.Id, operationId, (50, "Cash"));
            Assert.False(first.WasAlreadyCompleted);
            Assert.True(retry.WasAlreadyCompleted);
            Assert.Equal(first.Id, retry.Id);

            using var conflict = await client.PostWithAntiforgeryAsync(
                $"/api/purchases/{draft.Id}/complete",
                CompletionContent(operationId, (60, "Cash")));
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("idempotency-key-reused", await ReadCodeAsync(conflict));

            var operation = await client.GetFromJsonAsync<OperationStatusResult>(
                $"/api/operations/{operationId}");
            Assert.NotNull(operation);
            Assert.Equal("Completed", operation.Status);
            Assert.Equal(draft.Id, operation.ResultReference);

            var counts = await factory.WithDbContextAsync(async dbContext => new
            {
                Payments = await dbContext.PurchasePayments.CountAsync(item => item.PurchaseId == draft.Id),
                Movements = await dbContext.InventoryMovements.CountAsync(item =>
                    item.SourceType == "PurchaseLine"
                    && dbContext.PurchaseLines.Where(line => line.PurchaseId == draft.Id)
                        .Select(line => line.Id).Contains(item.SourceId)),
                Operations = await dbContext.BusinessOperations.CountAsync(item =>
                    item.OperationId == operationId)
            });
            Assert.Equal(1, counts.Payments);
            Assert.Equal(1, counts.Movements);
            Assert.Equal(1, counts.Operations);
        }
    }

    [Fact]
    public async Task ConcurrentPurchasesSerializeBalanceUpdatesWithoutLostUpdate()
    {
        var credentials = await factory.CreateOwnerAsync();
        using var setupClient = factory.CreateHttpsClient();
        await setupClient.LoginAsync(credentials.Email, credentials.Password);
        _ = await setupClient.InitializeStoreAsync("Concurrency");
        var supplier = await setupClient.CreateSupplierAsync();
        var product = await setupClient.CreateProductAsync(
            name: "Concurrent product",
            openingQuantity: 10,
            openingCost: 10_000);
        var purchaseA = await setupClient.CreatePurchaseAsync(supplier.Id, (product.Id, 10, 20_000));
        var purchaseB = await setupClient.CreatePurchaseAsync(supplier.Id, (product.Id, 10, 30_000));

        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(credentials.Email, credentials.Password);
        await clientB.LoginAsync(credentials.Email, credentials.Password);
        var results = await Task.WhenAll(
            clientA.CompletePurchaseAsync(purchaseA.Id, Guid.NewGuid()),
            clientB.CompletePurchaseAsync(purchaseB.Id, Guid.NewGuid()));

        Assert.All(results, result => Assert.Equal("Completed", result.Status));
        var balance = await setupClient.GetFromJsonAsync<ProductResult>($"/api/products/{product.Id}");
        Assert.NotNull(balance);
        Assert.Equal(30, balance.QuantityOnHand);
        Assert.Equal(600_000, balance.InventoryValue);
        Assert.Equal(20_000, balance.AverageCost);
        var movementCount = await factory.WithDbContextAsync(dbContext =>
            dbContext.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Purchase));
        Assert.Equal(2, movementCount);
    }

    [Fact]
    public async Task ConcurrentSameOperationIdProducesOneLogicalCompletion()
    {
        var credentials = await factory.CreateOwnerAsync();
        using var setupClient = factory.CreateHttpsClient();
        await setupClient.LoginAsync(credentials.Email, credentials.Password);
        _ = await setupClient.InitializeStoreAsync("Operation concurrency");
        var supplier = await setupClient.CreateSupplierAsync();
        var product = await setupClient.CreateProductAsync();
        var purchase = await setupClient.CreatePurchaseAsync(supplier.Id, (product.Id, 5, 100));
        var operationId = Guid.NewGuid();

        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(credentials.Email, credentials.Password);
        await clientB.LoginAsync(credentials.Email, credentials.Password);
        var results = await Task.WhenAll(
            clientA.CompletePurchaseAsync(purchase.Id, operationId, (100, "Cash")),
            clientB.CompletePurchaseAsync(purchase.Id, operationId, (100, "Cash")));

        Assert.Equal(purchase.Id, results[0].Id);
        Assert.Equal(purchase.Id, results[1].Id);
        Assert.Contains(results, result => result.WasAlreadyCompleted);
        var counts = await factory.WithDbContextAsync(async dbContext => new
        {
            Operations = await dbContext.BusinessOperations.CountAsync(item => item.OperationId == operationId),
            Payments = await dbContext.PurchasePayments.CountAsync(item => item.PurchaseId == purchase.Id),
            Movements = await dbContext.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.Purchase),
            Quantity = await dbContext.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(1, counts.Operations);
        Assert.Equal(1, counts.Payments);
        Assert.Equal(1, counts.Movements);
        Assert.Equal(5, counts.Quantity);
    }

    private async Task<(HttpClient Client, Guid StoreId, string Email)> CreateOwnerContextAsync(
        string storeName)
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        return (client, store.Id, credentials.Email);
    }

    private static JsonContent PurchaseContent(
        Guid supplierId,
        params (Guid ProductId, decimal Quantity, decimal UnitPrice)[] lines) =>
        JsonContent.Create(new
        {
            supplierId,
            lines = lines.Select(line => new
            {
                productId = line.ProductId,
                quantity = line.Quantity,
                unitPrice = line.UnitPrice
            })
        });

    private static JsonContent CompletionContent(
        Guid operationId,
        params (decimal Amount, string Method)[] payments) =>
        JsonContent.Create(new
        {
            operationId,
            payments = payments.Select(payment => new
            {
                amount = payment.Amount,
                method = payment.Method
            })
        });

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("code").GetString();
    }
}
