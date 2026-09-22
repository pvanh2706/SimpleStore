using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Products;
using SimpleStore.Application.Purchases;
using SimpleStore.Application.Returns;
using SimpleStore.Application.Sales;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain.Inventory;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice4IntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task MultipleReturnsUseHistoricalBasisObligationFirstRefundAndAuthoritativeProjection()
    {
        var context = await CreateOwnerContextAsync("Returns");
        using var client = context.Client;
        var product = await client.CreateProductAsync(name: "Return item", openingQuantity: 10, openingCost: 100);
        var customer = await client.CreateCustomerAsync("Return customer");
        var sale = await client.CompleteSaleAsync(
            Guid.NewGuid(), customer.Id, [(product.Id, 3)], (20_000, "Cash"));
        var lineId = Assert.Single(sale.Lines).Id;

        using (var previewResponse = await client.PostWithAntiforgeryAsync(
            "/api/returns/preview",
            JsonContent.Create(new
            {
                originalSaleId = sale.Id,
                lines = new[] { new { originalSaleLineId = lineId, quantity = 1m, restock = false } }
            })))
        {
            previewResponse.EnsureSuccessStatusCode();
            var preview = (await previewResponse.Content.ReadFromJsonAsync<ReturnPreviewResult>())!;
            Assert.Equal(12_000, preview.CurrentReturnValue);
            Assert.Equal(4_000, preview.Outstanding);
            Assert.Equal(0, preview.RefundDueNow);
        }

        var first = await client.CreateReturnAsync(
            Guid.NewGuid(), sale.Id, [(lineId, 1, false)]);
        var secondOperation = Guid.NewGuid();
        var second = await client.CreateReturnAsync(
            secondOperation, sale.Id, [(lineId, 2, true)], "Cash");
        var retry = await client.CreateReturnAsync(
            secondOperation, sale.Id, [(lineId, 2, true)], "Cash");

        Assert.Equal(12_000, first.TotalReturnAmount);
        Assert.Equal(0, first.RefundAmount);
        Assert.Equal(24_000, second.TotalReturnAmount);
        Assert.Equal(20_000, second.RefundAmount);
        Assert.True(retry.WasAlreadyCompleted);
        Assert.Equal(second.Id, retry.Id);

        var detail = await client.GetFromJsonAsync<SaleResult>($"/api/sales/{sale.Id}");
        Assert.NotNull(detail);
        Assert.Equal(36_000, detail.OriginalTotalAmount);
        Assert.Equal(36_000, detail.TotalReturnedAmount);
        Assert.Equal(0, detail.NetSaleAmount);
        Assert.Equal(20_000, detail.OriginalCollectedAmount);
        Assert.Equal(20_000, detail.TotalRefundedAmount);
        Assert.Equal(0, detail.NetCollectedAmount);
        Assert.Equal(0, detail.OutstandingAmount);
        Assert.Equal(2, detail.Returns!.Count);
        var list = await client.GetFromJsonAsync<SaleListResult>("/api/sales?page=1&pageSize=20");
        var listItem = Assert.Single(list!.Items, item => item.Id == sale.Id);
        Assert.Equal(36_000, listItem.TotalReturnedAmount);
        Assert.Equal(0, listItem.NetSaleAmount);
        Assert.Equal(0, listItem.OutstandingAmount);

        var state = await factory.WithDbContextAsync(async db => new
        {
            Balance = await db.InventoryBalances.SingleAsync(item => item.ProductId == product.Id),
            ReturnCount = await db.Returns.CountAsync(item => item.OriginalSaleId == sale.Id),
            RefundCount = await db.ReturnRefundPayments.CountAsync(payment =>
                db.Returns.Any(item => item.Id == payment.ReturnId && item.OriginalSaleId == sale.Id)),
            RestockMovements = await db.InventoryMovements.CountAsync(item =>
                item.MovementType == InventoryMovementType.ReturnRestock && item.ProductId == product.Id)
        });
        Assert.Equal(9, state.Balance.QuantityOnHand);
        Assert.Equal(900, state.Balance.InventoryValue);
        Assert.Equal(2, state.ReturnCount);
        Assert.Equal(1, state.RefundCount);
        Assert.Equal(1, state.RestockMovements);
    }

    [Fact]
    public async Task ReturnEnforcesAuthorizationStoreScopeCumulativeCapAndGlobalOperationId()
    {
        var context = await CreateOwnerContextAsync("Return security");
        var other = await CreateOwnerContextAsync("Other store");
        using var owner = context.Client;
        using var otherOwner = other.Client;
        var product = await owner.CreateProductAsync(openingQuantity: 2, openingCost: 10);
        var saleOperation = Guid.NewGuid();
        var sale = await owner.CompleteSaleAsync(
            saleOperation, null, [(product.Id, 1)], (12_000, "Cash"));
        var lineId = Assert.Single(sale.Lines).Id;

        using var crossStore = await otherOwner.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Cash"));
        Assert.Equal(HttpStatusCode.NotFound, crossStore.StatusCode);

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        using var forbidden = await cashier.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Cash"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var reused = await owner.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(saleOperation, sale.Id, [(lineId, 1, true)], "Cash"));
        Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
        Assert.Equal("idempotency-key-reused", await ReadCodeAsync(reused));

        await owner.CreateReturnAsync(Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Cash");
        using var overReturn = await owner.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Cash"));
        Assert.Equal(HttpStatusCode.Conflict, overReturn.StatusCode);
        Assert.Equal("return-quantity-exceeds-remaining", await ReadCodeAsync(overReturn));
    }

    [Fact]
    public async Task ConcurrentReturnsSerializeAndCommitOnlyOneCorrection()
    {
        var context = await CreateOwnerContextAsync("Return race");
        using var setup = context.Client;
        var product = await setup.CreateProductAsync(openingQuantity: 1, openingCost: 10);
        var sale = await setup.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 1)], (12_000, "Cash"));
        var lineId = Assert.Single(sale.Lines).Id;
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await clientB.LoginAsync(context.Credentials.Email, context.Credentials.Password);

        var responses = await Task.WhenAll(
            clientA.PostWithAntiforgeryAsync(
                "/api/returns", Slice4HttpClient.ReturnContent(Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Cash")),
            clientB.PostWithAntiforgeryAsync(
                "/api/returns", Slice4HttpClient.ReturnContent(Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Cash")));
        using var responseA = responses[0];
        using var responseB = responses[1];

        Assert.Equal(1, responses.Count(item => item.IsSuccessStatusCode));
        Assert.Equal(1, responses.Count(item => item.StatusCode == HttpStatusCode.Conflict));
        var state = await factory.WithDbContextAsync(async db => new
        {
            Returns = await db.Returns.CountAsync(item => item.OriginalSaleId == sale.Id),
            Restocks = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.ReturnRestock),
            Quantity = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(1, state.Returns);
        Assert.Equal(1, state.Restocks);
        Assert.Equal(1, state.Quantity);
    }

    [Fact]
    public async Task InvalidRefundIntentRollsBackAndTransferRefundIsRecordedExactly()
    {
        var context = await CreateOwnerContextAsync("Refund rollback");
        using var client = context.Client;
        var product = await client.CreateProductAsync(openingQuantity: 2, openingCost: 10);
        var sale = await client.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 2)], (24_000, "Cash"));
        var lineId = Assert.Single(sale.Lines).Id;

        using var missingMethod = await client.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(Guid.NewGuid(), sale.Id, [(lineId, 1, true)]));
        Assert.Equal(HttpStatusCode.BadRequest, missingMethod.StatusCode);
        Assert.Equal("refund-method-required", await ReadCodeAsync(missingMethod));
        var afterFailure = await factory.WithDbContextAsync(async db => new
        {
            Returns = await db.Returns.CountAsync(item => item.OriginalSaleId == sale.Id),
            Quantity = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(0, afterFailure.Returns);
        Assert.Equal(0, afterFailure.Quantity);

        var result = await client.CreateReturnAsync(
            Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Transfer");
        Assert.Equal(12_000, result.RefundAmount);
        Assert.Equal("Transfer", Assert.Single(result.RefundPayments).Method);

        using var voidRejected = await client.PostWithAntiforgeryAsync(
            $"/api/sales/{sale.Id}/void",
            JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Has return" }));
        Assert.Equal(HttpStatusCode.Conflict, voidRejected.StatusCode);
        Assert.Equal("sale-has-returns", await ReadCodeAsync(voidRejected));
    }

    [Fact]
    public async Task ReturnAndSaleVoidRaceCommitsExactlyOneCorrectionOutcome()
    {
        var context = await CreateOwnerContextAsync("Return void race");
        using var setup = context.Client;
        var product = await setup.CreateProductAsync(openingQuantity: 1, openingCost: 10);
        var sale = await setup.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 1)], (12_000, "Cash"));
        var lineId = Assert.Single(sale.Lines).Id;
        using var returnClient = factory.CreateHttpsClient();
        using var voidClient = factory.CreateHttpsClient();
        await returnClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await voidClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);

        var responses = await Task.WhenAll(
            returnClient.PostWithAntiforgeryAsync(
                "/api/returns", Slice4HttpClient.ReturnContent(Guid.NewGuid(), sale.Id, [(lineId, 1, true)], "Cash")),
            voidClient.PostWithAntiforgeryAsync(
                $"/api/sales/{sale.Id}/void",
                JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Race" })));
        using var returnResponse = responses[0];
        using var voidResponse = responses[1];
        Assert.Equal(1, responses.Count(item => item.IsSuccessStatusCode));
        Assert.Equal(1, responses.Count(item => item.StatusCode == HttpStatusCode.Conflict));

        var state = await factory.WithDbContextAsync(async db => new
        {
            Returns = await db.Returns.CountAsync(item => item.OriginalSaleId == sale.Id),
            Voids = await db.SaleVoids.CountAsync(item => item.OriginalSaleId == sale.Id),
            CorrectionMovements = await db.InventoryMovements.CountAsync(item => item.ProductId == product.Id
                && (item.MovementType == InventoryMovementType.ReturnRestock
                    || item.MovementType == InventoryMovementType.SaleVoid)),
            Quantity = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(1, state.Returns + state.Voids);
        Assert.Equal(1, state.CorrectionMovements);
        Assert.Equal(1, state.Quantity);
    }

    [Fact]
    public async Task SaleVoidRestoresHistoricalInventoryOnceAndRejectsReturn()
    {
        var context = await CreateOwnerContextAsync("Sale void");
        using var client = context.Client;
        var product = await client.CreateProductAsync(openingQuantity: 5, openingCost: 10);
        var sale = await client.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 2)], (24_000, "Cash"));
        var operationId = Guid.NewGuid();

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        using (var forbidden = await cashier.PostWithAntiforgeryAsync(
            $"/api/sales/{sale.Id}/void",
            JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Forbidden" })))
        {
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }

        var result = await client.VoidSaleAsync(sale.Id, operationId, "Duplicate checkout");
        var retry = await client.VoidSaleAsync(sale.Id, operationId, "Duplicate checkout");
        Assert.True(retry.WasAlreadyCompleted);
        Assert.Equal(result.Id, retry.Id);

        var detail = await client.GetFromJsonAsync<SaleResult>($"/api/sales/{sale.Id}");
        Assert.True(detail!.IsVoided);
        Assert.Equal("Duplicate checkout", detail.Void!.Reason);
        Assert.Equal(24_000, detail.OriginalTotalAmount);
        Assert.Equal(0, detail.NetSaleAmount);
        Assert.Equal(0, detail.NetCollectedAmount);
        Assert.Single(detail.Payments);

        using var rejected = await client.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(
                Guid.NewGuid(), sale.Id, [(Assert.Single(sale.Lines).Id, 1, true)], "Cash"));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal("sale-already-voided", await ReadCodeAsync(rejected));

        var state = await factory.WithDbContextAsync(async db => new
        {
            Balance = await db.InventoryBalances.SingleAsync(item => item.ProductId == product.Id),
            VoidMovements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.SaleVoid),
            Payments = await db.SalePayments.CountAsync(item => item.SaleId == sale.Id)
        });
        Assert.Equal(5, state.Balance.QuantityOnHand);
        Assert.Equal(50, state.Balance.InventoryValue);
        Assert.Equal(1, state.VoidMovements);
        Assert.Equal(1, state.Payments);
    }

    [Fact]
    public async Task SafePurchaseVoidRestoresExactStateAndExcludesSupplierOutstanding()
    {
        var context = await CreateOwnerContextAsync("Purchase void");
        using var client = context.Client;
        var product = await client.CreateProductAsync(name: "Before", openingQuantity: 10, openingCost: 10);
        var revisionBefore = product.ReferencePurchaseCostRevision;
        var supplier = await client.CreateSupplierAsync();
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 5, 20));
        var completed = await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid(), (40, "Cash"));

        var afterPurchase = await client.GetFromJsonAsync<ProductResult>($"/api/products/{product.Id}");
        Assert.Equal(revisionBefore + 1, afterPurchase!.ReferencePurchaseCostRevision);
        using (var rename = await client.PutWithAntiforgeryAsync(
            $"/api/products/{product.Id}",
            JsonContent.Create(new
            {
                sku = afterPurchase.Sku,
                barcode = afterPurchase.Barcode,
                name = "Renamed only",
                unit = afterPurchase.Unit,
                salePrice = afterPurchase.SalePrice,
                referencePurchaseCost = afterPurchase.ReferencePurchaseCost
            })))
        {
            rename.EnsureSuccessStatusCode();
        }

        var voidOperation = Guid.NewGuid();
        var purchaseVoid = await client.VoidPurchaseAsync(purchase.Id, voidOperation);
        var purchaseVoidRetry = await client.VoidPurchaseAsync(purchase.Id, voidOperation);
        Assert.True(purchaseVoidRetry.WasAlreadyCompleted);
        Assert.Equal(purchaseVoid.Id, purchaseVoidRetry.Id);
        using (var reused = await client.PostWithAntiforgeryAsync(
            $"/api/purchases/{purchase.Id}/void",
            JsonContent.Create(new { operationId = voidOperation, reason = "Different reason" })))
        {
            Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
            Assert.Equal("idempotency-key-reused", await ReadCodeAsync(reused));
        }

        var detail = await client.GetFromJsonAsync<PurchaseResult>($"/api/purchases/{purchase.Id}");
        var supplierDetail = await client.GetFromJsonAsync<SupplierResult>($"/api/suppliers/{supplier.Id}");
        var restoredProduct = await client.GetFromJsonAsync<ProductResult>($"/api/products/{product.Id}");
        Assert.True(detail!.IsVoided);
        Assert.Equal(0, detail.OutstandingAmount);
        Assert.Equal(40, detail.PaidAmount);
        Assert.Equal(0, supplierDetail!.OutstandingAmount);
        Assert.Equal(10, restoredProduct!.ReferencePurchaseCost);
        Assert.Equal(revisionBefore + 2, restoredProduct.ReferencePurchaseCostRevision);
        var list = await client.GetFromJsonAsync<PurchaseListResult>(
            "/api/purchases?page=1&pageSize=20");
        var listItem = Assert.Single(list!.Items, item => item.Id == purchase.Id);
        Assert.True(listItem.IsVoided);
        Assert.Equal(0, listItem.OutstandingAmount);

        var state = await factory.WithDbContextAsync(async db => new
        {
            Balance = await db.InventoryBalances.SingleAsync(item => item.ProductId == product.Id),
            Basis = await db.PurchaseLineReversalBases.CountAsync(item => item.PurchaseId == purchase.Id),
            VoidMovements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.PurchaseVoid),
            Payments = await db.PurchasePayments.CountAsync(item => item.PurchaseId == purchase.Id)
        });
        Assert.Equal(10, state.Balance.QuantityOnHand);
        Assert.Equal(100, state.Balance.InventoryValue);
        Assert.Equal(10, state.Balance.AverageCost);
        Assert.True(state.Balance.HasAverageCost);
        Assert.Equal(1, state.Basis);
        Assert.Equal(1, state.VoidMovements);
        Assert.Equal(1, state.Payments);
        Assert.Equal(100, completed.TotalAmount);
    }

    [Fact]
    public async Task PurchaseVoidRejectsDownstreamMovementAndReferenceCostAba()
    {
        var downstream = await CreateOwnerContextAsync("Purchase dependency");
        using (var client = downstream.Client)
        {
            var product = await client.CreateProductAsync(openingQuantity: 10, openingCost: 10);
            var supplier = await client.CreateSupplierAsync();
            var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 2, 20));
            await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
            await client.CompleteSaleAsync(Guid.NewGuid(), null, [(product.Id, 1)], (12_000, "Cash"));

            using var response = await client.PostWithAntiforgeryAsync(
                $"/api/purchases/{purchase.Id}/void",
                JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Try void" }));
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("purchase-void-downstream-inventory-dependency", await ReadCodeAsync(response));
        }

        var aba = await CreateOwnerContextAsync("Purchase ABA");
        using (var client = aba.Client)
        {
            var product = await client.CreateProductAsync(openingQuantity: 10, openingCost: 10);
            var supplier = await client.CreateSupplierAsync();
            var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 2, 20));
            await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
            await UpdateReferenceCostAsync(client, product.Id, 30);
            await UpdateReferenceCostAsync(client, product.Id, 20);

            using var response = await client.PostWithAntiforgeryAsync(
                $"/api/purchases/{purchase.Id}/void",
                JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "ABA" }));
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("purchase-void-reference-cost-dependency", await ReadCodeAsync(response));
        }
    }

    [Fact]
    public async Task OneUnsafeLineBlocksEntireMultiLinePurchaseVoid()
    {
        var context = await CreateOwnerContextAsync("Multi-line purchase void");
        using var client = context.Client;
        var productA = await client.CreateProductAsync(name: "A", openingQuantity: 10, openingCost: 10);
        var productB = await client.CreateProductAsync(name: "B", openingQuantity: 20, openingCost: 20);
        var supplier = await client.CreateSupplierAsync();
        var purchase = await client.CreatePurchaseAsync(
            supplier.Id, (productA.Id, 2, 30), (productB.Id, 3, 40));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        await client.CompleteSaleAsync(Guid.NewGuid(), null, [(productA.Id, 1)], (12_000, "Cash"));

        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/purchases/{purchase.Id}/void",
            JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "One unsafe line" }));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("purchase-void-downstream-inventory-dependency", await ReadCodeAsync(response));

        var state = await factory.WithDbContextAsync(async db => new
        {
            Voids = await db.PurchaseVoids.CountAsync(item => item.OriginalPurchaseId == purchase.Id),
            ProductBQuantity = await db.InventoryBalances.Where(item => item.ProductId == productB.Id)
                .Select(item => item.QuantityOnHand).SingleAsync(),
            VoidMovements = await db.InventoryMovements.CountAsync(item =>
                item.MovementType == InventoryMovementType.PurchaseVoid
                && (item.ProductId == productA.Id || item.ProductId == productB.Id))
        });
        Assert.Equal(0, state.Voids);
        Assert.Equal(23, state.ProductBQuantity);
        Assert.Equal(0, state.VoidMovements);
    }

    [Fact]
    public async Task PurchaseVoidRejectsLegacyPurchaseWithoutTrustworthyBasis()
    {
        var context = await CreateOwnerContextAsync("Legacy purchase");
        using var client = context.Client;
        var product = await client.CreateProductAsync(openingQuantity: 1, openingCost: 10);
        var supplier = await client.CreateSupplierAsync();
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 20));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        await factory.WithDbContextAsync(async db =>
        {
            var basis = await db.PurchaseLineReversalBases.SingleAsync(item => item.PurchaseId == purchase.Id);
            db.PurchaseLineReversalBases.Remove(basis);
            await db.SaveChangesAsync();
            return true;
        });

        using var response = await client.PostWithAntiforgeryAsync(
            $"/api/purchases/{purchase.Id}/void",
            JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Legacy" }));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("purchase-void-reversal-basis-unavailable", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task ConcurrentPurchaseVoidsCommitOnlyOneReversal()
    {
        var context = await CreateOwnerContextAsync("Purchase void race");
        using var setup = context.Client;
        var product = await setup.CreateProductAsync(openingQuantity: 4, openingCost: 10);
        var supplier = await setup.CreateSupplierAsync();
        var purchase = await setup.CreatePurchaseAsync(supplier.Id, (product.Id, 2, 20));
        await setup.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await clientB.LoginAsync(context.Credentials.Email, context.Credentials.Password);

        var responses = await Task.WhenAll(
            clientA.PostWithAntiforgeryAsync(
                $"/api/purchases/{purchase.Id}/void",
                JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "A" })),
            clientB.PostWithAntiforgeryAsync(
                $"/api/purchases/{purchase.Id}/void",
                JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "B" })));
        using var responseA = responses[0];
        using var responseB = responses[1];
        Assert.Equal(1, responses.Count(item => item.IsSuccessStatusCode));
        Assert.Equal(1, responses.Count(item => item.StatusCode == HttpStatusCode.Conflict));

        var state = await factory.WithDbContextAsync(async db => new
        {
            Voids = await db.PurchaseVoids.CountAsync(item => item.OriginalPurchaseId == purchase.Id),
            Movements = await db.InventoryMovements.CountAsync(item =>
                item.ProductId == product.Id && item.MovementType == InventoryMovementType.PurchaseVoid),
            Quantity = await db.InventoryBalances.Where(item => item.ProductId == product.Id)
                .Select(item => item.QuantityOnHand).SingleAsync()
        });
        Assert.Equal(1, state.Voids);
        Assert.Equal(1, state.Movements);
        Assert.Equal(4, state.Quantity);
    }

    [Fact]
    public async Task ConcurrentCrossTypeOperationIdHasOneWinnerAndDeterministicReuseConflict()
    {
        var context = await CreateOwnerContextAsync("Cross operation race");
        using var setup = context.Client;
        var product = await setup.CreateProductAsync(openingQuantity: 5, openingCost: 10);
        var supplier = await setup.CreateSupplierAsync();
        var purchase = await setup.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 20));
        using var purchaseClient = factory.CreateHttpsClient();
        using var saleClient = factory.CreateHttpsClient();
        await purchaseClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await saleClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        var operationId = Guid.NewGuid();

        var responses = await Task.WhenAll(
            purchaseClient.PostWithAntiforgeryAsync(
                $"/api/purchases/{purchase.Id}/complete",
                JsonContent.Create(new { operationId, payments = Array.Empty<object>() })),
            saleClient.PostWithAntiforgeryAsync(
                "/api/sales/complete",
                Slice3HttpClient.SaleContent(operationId, null, [(product.Id, 1)], (12_000, "Cash"))));
        using var purchaseResponse = responses[0];
        using var saleResponse = responses[1];
        Assert.Equal(1, responses.Count(item => item.IsSuccessStatusCode));
        var loser = Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("idempotency-key-reused", await ReadCodeAsync(loser));

        var operationCount = await factory.WithDbContextAsync(db =>
            db.BusinessOperations.CountAsync(item => item.OperationId == operationId));
        Assert.Equal(1, operationCount);
    }

    private async Task<OwnerContext> CreateOwnerContextAsync(string storeName)
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        return new OwnerContext(client, store.Id, credentials);
    }

    private static async Task UpdateReferenceCostAsync(HttpClient client, Guid productId, decimal cost)
    {
        var product = await client.GetFromJsonAsync<ProductResult>($"/api/products/{productId}");
        using var response = await client.PutWithAntiforgeryAsync(
            $"/api/products/{productId}",
            JsonContent.Create(new
            {
                sku = product!.Sku,
                barcode = product.Barcode,
                name = product.Name,
                unit = product.Unit,
                salePrice = product.SalePrice,
                referencePurchaseCost = cost
            }));
        response.EnsureSuccessStatusCode();
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
