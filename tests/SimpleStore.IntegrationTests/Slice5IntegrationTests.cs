using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Corrections;
using SimpleStore.Application.Debts;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Products;
using SimpleStore.Application.Purchases;
using SimpleStore.Application.Returns;
using SimpleStore.Application.Sales;
using SimpleStore.Application.Stores;
using SimpleStore.Infrastructure.Persistence;
using Xunit;

namespace SimpleStore.IntegrationTests;

public sealed class Slice5IntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CustomerDebtOwnerAndCashierReadPayRetryAndRecoverAtomically()
    {
        var context = await CreateOwnerContextAsync("Customer debt");
        using var owner = context.Client;
        var product = await CreatePricedProductAsync(owner, 100, 10);
        var customer = await owner.CreateCustomerAsync("Debt customer");
        await owner.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)], (20, "Cash"));

        var initial = await owner.GetCustomerDebtAsync(customer.Id);
        Assert.Equal(80, initial.OutstandingAmount);
        var list = await owner.GetFromJsonAsync<DebtBalanceListResult>("/api/customers/debts?page=1&pageSize=20");
        Assert.Equal(customer.Id, Assert.Single(list!.Items).PartyId);

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        Assert.Equal(80, (await cashier.GetCustomerDebtAsync(customer.Id)).OutstandingAmount);

        var operationId = Guid.NewGuid();
        var payment = await cashier.RecordCustomerDebtPaymentAsync(
            customer.Id, operationId, 30, 80, "Transfer", "  morning  ");
        var retry = await cashier.RecordCustomerDebtPaymentAsync(
            customer.Id, operationId, 30, 80, "Transfer", "morning");
        Assert.Equal(payment.Id, retry.Id);
        Assert.True(retry.WasAlreadyRecorded);
        Assert.Equal("morning", payment.Note);
        Assert.Equal(50, (await owner.GetCustomerDebtAsync(customer.Id)).OutstandingAmount);

        var operation = await owner.GetFromJsonAsync<JsonElement>($"/api/operations/{operationId}");
        Assert.Equal("RecordCustomerDebtPayment", operation.GetProperty("operationType").GetString());
        Assert.Equal(payment.Id, operation.GetProperty("resultReference").GetGuid());

        using var changedNote = await cashier.PostWithAntiforgeryAsync(
            $"/api/customers/{customer.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(operationId, 30, 80, "Transfer", "different"));
        Assert.Equal(HttpStatusCode.Conflict, changedNote.StatusCode);
        Assert.Equal("idempotency-key-reused", await ReadCodeAsync(changedNote));

        var persisted = await factory.WithDbContextAsync(async db => new
        {
            Payments = await db.DebtPayments.CountAsync(item => item.OperationId == operationId),
            Operations = await db.BusinessOperations.CountAsync(item => item.OperationId == operationId)
        });
        Assert.Equal(1, persisted.Payments);
        Assert.Equal(1, persisted.Operations);
    }

    [Fact]
    public async Task SupplierDebtIsOwnerOnlyAndDoesNotMutatePurchaseValue()
    {
        var context = await CreateOwnerContextAsync("Supplier debt");
        using var owner = context.Client;
        var product = await CreatePricedProductAsync(owner, 100, 10);
        var supplier = await owner.CreateSupplierAsync("Debt supplier");
        var purchase = await owner.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
        await owner.CompletePurchaseAsync(purchase.Id, Guid.NewGuid(), (20, "Cash"));

        Assert.Equal(80, (await owner.GetSupplierDebtAsync(supplier.Id)).OutstandingAmount);
        var supplierOperationId = Guid.NewGuid();
        var payment = await owner.RecordSupplierDebtPaymentAsync(
            supplier.Id, supplierOperationId, 30, 80, "Transfer", " settlement ");
        var retry = await owner.RecordSupplierDebtPaymentAsync(
            supplier.Id, supplierOperationId, 30, 80, "Transfer", "settlement");
        Assert.Equal(payment.Id, retry.Id);
        Assert.True(retry.WasAlreadyRecorded);
        Assert.Equal("MoneyOut", payment.Direction);
        Assert.Equal(50, (await owner.GetSupplierDebtAsync(supplier.Id)).OutstandingAmount);
        var list = await owner.GetFromJsonAsync<DebtBalanceListResult>(
            "/api/suppliers/debts?search=Debt%20supplier&page=1&pageSize=1");
        Assert.Equal(supplier.Id, Assert.Single(list!.Items).PartyId);

        using var reused = await owner.PostWithAntiforgeryAsync(
            $"/api/suppliers/{supplier.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(supplierOperationId, 30, 80, "Transfer", "changed"));
        Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
        Assert.Equal("idempotency-key-reused", await ReadCodeAsync(reused));

        var cashierCredentials = await factory.CreateCashierAsync(context.StoreId);
        using var cashier = factory.CreateHttpsClient();
        await cashier.LoginAsync(cashierCredentials.Email, cashierCredentials.Password);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync($"/api/suppliers/{supplier.Id}/debt")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync($"/api/operations/{supplierOperationId}")).StatusCode);
        using var forbiddenPayment = await cashier.PostWithAntiforgeryAsync(
            $"/api/suppliers/{supplier.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 10, 50));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPayment.StatusCode);

        var persistedTotal = await factory.WithDbContextAsync(db => db.Purchases
            .Where(item => item.Id == purchase.Id)
            .Select(item => item.TotalAmount)
            .SingleAsync());
        Assert.Equal(100, persistedTotal);
    }

    [Fact]
    public async Task ConcurrentDifferentCustomerPaymentsCannotOverpayAndStaleBalanceReturnsLatest()
    {
        var context = await CreateOwnerContextAsync("Debt concurrency");
        using var setup = context.Client;
        var product = await CreatePricedProductAsync(setup, 100, 10);
        var customer = await setup.CreateCustomerAsync("Concurrent customer");
        await setup.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await clientB.LoginAsync(context.Credentials.Email, context.Credentials.Password);

        var responses = await Task.WhenAll(
            clientA.PostWithAntiforgeryAsync(
                $"/api/customers/{customer.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 100)),
            clientB.PostWithAntiforgeryAsync(
                $"/api/customers/{customer.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 100)));
        using var first = responses[0];
        using var second = responses[1];
        Assert.Equal(1, responses.Count(item => item.IsSuccessStatusCode));
        var conflict = Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("customer-debt-changed", await ReadCodeAsync(conflict));
        Assert.Equal(20, await ReadDecimalExtensionAsync(conflict, "latestOutstandingAmount"));
        Assert.Equal(20, (await setup.GetCustomerDebtAsync(customer.Id)).OutstandingAmount);
        Assert.Equal(1, await factory.WithDbContextAsync(db =>
            db.DebtPayments.CountAsync(item => item.CustomerId == customer.Id)));
    }

    [Fact]
    public async Task ConcurrentSameOperationIsOneCustomerPaymentAndSupplierPaymentsAlsoSerialize()
    {
        var context = await CreateOwnerContextAsync("Debt idempotency concurrency");
        using var setup = context.Client;
        var product = await CreatePricedProductAsync(setup, 100, 10);
        var customer = await setup.CreateCustomerAsync("Same operation customer");
        await setup.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        using var clientA = factory.CreateHttpsClient();
        using var clientB = factory.CreateHttpsClient();
        await clientA.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await clientB.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        var operationId = Guid.NewGuid();
        var sameOperationResponses = await Task.WhenAll(
            clientA.PostWithAntiforgeryAsync(
                $"/api/customers/{customer.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(operationId, 30, 100, note: " same ")),
            clientB.PostWithAntiforgeryAsync(
                $"/api/customers/{customer.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(operationId, 30, 100, note: "same")));
        Assert.All(sameOperationResponses, response => Assert.True(response.IsSuccessStatusCode));
        var sameResults = await Task.WhenAll(sameOperationResponses.Select(response =>
            response.Content.ReadFromJsonAsync<DebtPaymentResult>()));
        Assert.Equal(sameResults[0]!.Id, sameResults[1]!.Id);
        Assert.Equal(1, sameResults.Count(item => item!.WasAlreadyRecorded));
        foreach (var response in sameOperationResponses) response.Dispose();

        var supplier = await setup.CreateSupplierAsync("Concurrent supplier");
        var purchase = await setup.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
        await setup.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        var supplierResponses = await Task.WhenAll(
            clientA.PostWithAntiforgeryAsync(
                $"/api/suppliers/{supplier.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 100)),
            clientB.PostWithAntiforgeryAsync(
                $"/api/suppliers/{supplier.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 100)));
        Assert.Equal(1, supplierResponses.Count(response => response.IsSuccessStatusCode));
        Assert.Equal(1, supplierResponses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in supplierResponses) response.Dispose();
        Assert.Equal(20, (await setup.GetSupplierDebtAsync(supplier.Id)).OutstandingAmount);
    }

    [Fact]
    public async Task AggregateReturnUsesCustomerDebtOnceAndRejectsStalePreview()
    {
        var context = await CreateOwnerContextAsync("Aggregate return");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 50, 20);

        await AssertReturnCaseAsync(client, product.Id, 30, 20, 0);
        await AssertReturnCaseAsync(client, product.Id, 50, 0, 0);
        await AssertReturnCaseAsync(client, product.Id, 0, 50, 0);

        var customer = await client.CreateCustomerAsync("Aggregate one hundred");
        var saleToReturn = await client.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        await client.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        var preview = await PreviewAsync(client, saleToReturn.Id, Assert.Single(saleToReturn.Lines).Id, 1);
        Assert.Equal(100, preview.CurrentAggregateCustomerDebt);
        Assert.Equal(50, preview.DebtReduction);
        Assert.Equal(0, preview.RequiredActualRefund);
        await CompleteFromPreviewAsync(client, saleToReturn.Id, Assert.Single(saleToReturn.Lines).Id, preview);
        Assert.Equal(50, (await client.GetCustomerDebtAsync(customer.Id)).OutstandingAmount);

        var paidCustomer = await client.CreateCustomerAsync("Prior debt payment");
        var paidSale = await client.CompleteSaleAsync(Guid.NewGuid(), paidCustomer.Id, [(product.Id, 2)]);
        await client.RecordCustomerDebtPaymentAsync(paidCustomer.Id, Guid.NewGuid(), 80, 100);
        var paidPreview = await PreviewAsync(client, paidSale.Id, Assert.Single(paidSale.Lines).Id, 1);
        Assert.Equal(20, paidPreview.CurrentAggregateCustomerDebt);
        Assert.Equal(20, paidPreview.DebtReduction);
        Assert.Equal(30, paidPreview.RequiredActualRefund);
        var paidReturn = await CompleteFromPreviewAsync(
            client, paidSale.Id, Assert.Single(paidSale.Lines).Id, paidPreview, "Cash");
        Assert.Equal(30, paidReturn.RefundAmount);
        Assert.Single(paidReturn.RefundPayments);
        Assert.Equal(0, (await client.GetCustomerDebtAsync(paidCustomer.Id)).OutstandingAmount);

        var staleCustomer = await client.CreateCustomerAsync("Stale preview");
        var staleSale = await client.CompleteSaleAsync(Guid.NewGuid(), staleCustomer.Id, [(product.Id, 2)]);
        var stalePreview = await PreviewAsync(client, staleSale.Id, Assert.Single(staleSale.Lines).Id, 1);
        await client.RecordCustomerDebtPaymentAsync(staleCustomer.Id, Guid.NewGuid(), 80, 100);
        using var staleResponse = await client.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(
                Guid.NewGuid(), staleSale.Id, [(Assert.Single(staleSale.Lines).Id, 1, false)], null,
                stalePreview.CurrentAggregateCustomerDebt, stalePreview.RequiredActualRefund));
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        Assert.Equal("return-refund-requirement-changed", await ReadCodeAsync(staleResponse));
        Assert.Equal(30, await ReadDecimalExtensionAsync(staleResponse, "requiredActualRefund"));

        var returnFirstCustomer = await client.CreateCustomerAsync("Return first");
        var returnFirstSale = await client.CompleteSaleAsync(
            Guid.NewGuid(), returnFirstCustomer.Id, [(product.Id, 2)]);
        var returnFirstLineId = Assert.Single(returnFirstSale.Lines).Id;
        var returnFirstPreview = await PreviewAsync(client, returnFirstSale.Id, returnFirstLineId, 1);
        await CompleteFromPreviewAsync(client, returnFirstSale.Id, returnFirstLineId, returnFirstPreview);
        using var rejectedPayment = await client.PostWithAntiforgeryAsync(
            $"/api/customers/{returnFirstCustomer.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 50));
        Assert.Equal(HttpStatusCode.Conflict, rejectedPayment.StatusCode);
        Assert.Equal("debt-payment-exceeds-outstanding", await ReadCodeAsync(rejectedPayment));
        Assert.Equal(50, (await client.GetCustomerDebtAsync(returnFirstCustomer.Id)).OutstandingAmount);
    }

    [Fact]
    public async Task SaleAndPurchaseVoidRejectNegativeAggregateDebtWithoutPartialEffects()
    {
        var context = await CreateOwnerContextAsync("Void debt guards");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 100, 10);
        var customer = await client.CreateCustomerAsync("Void customer");
        var sale = await client.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        await client.RecordCustomerDebtPaymentAsync(customer.Id, Guid.NewGuid(), 80, 100);
        using var saleVoid = await client.PostWithAntiforgeryAsync(
            $"/api/sales/{sale.Id}/void",
            JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Cannot remove obligation" }));
        Assert.Equal(HttpStatusCode.Conflict, saleVoid.StatusCode);
        Assert.Equal("customer-debt-would-become-negative", await ReadCodeAsync(saleVoid));

        var supplier = await client.CreateSupplierAsync("Void supplier");
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        await client.RecordSupplierDebtPaymentAsync(supplier.Id, Guid.NewGuid(), 80, 100);
        using var purchaseVoid = await client.PostWithAntiforgeryAsync(
            $"/api/purchases/{purchase.Id}/void",
            JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Cannot remove obligation" }));
        Assert.Equal(HttpStatusCode.Conflict, purchaseVoid.StatusCode);
        Assert.Equal("supplier-debt-would-become-negative", await ReadCodeAsync(purchaseVoid));

        var state = await factory.WithDbContextAsync(async db => new
        {
            SaleVoids = await db.SaleVoids.CountAsync(item => item.OriginalSaleId == sale.Id),
            PurchaseVoids = await db.PurchaseVoids.CountAsync(item => item.OriginalPurchaseId == purchase.Id),
            SaleVoidMovements = await db.InventoryMovements.CountAsync(item =>
                item.StoreId == context.StoreId && item.SourceType == "SaleVoid"),
            PurchaseVoidMovements = await db.InventoryMovements.CountAsync(item =>
                item.StoreId == context.StoreId && item.SourceType == "PurchaseVoid")
        });
        Assert.Equal(0, state.SaleVoids);
        Assert.Equal(0, state.PurchaseVoids);
        Assert.Equal(0, state.SaleVoidMovements);
        Assert.Equal(0, state.PurchaseVoidMovements);

        var allowedProduct = await CreatePricedProductAsync(client, 100, 2);
        var allowedCustomer = await client.CreateCustomerAsync("Allowed void customer");
        var allowedSale = await client.CompleteSaleAsync(
            Guid.NewGuid(), allowedCustomer.Id, [(allowedProduct.Id, 1)]);
        var allowedSaleVoid = await client.VoidSaleAsync(
            allowedSale.Id, Guid.NewGuid(), "Remove unsettled sale");
        Assert.Equal(allowedSale.Id, allowedSaleVoid.OriginalSaleId);
        Assert.Equal(0, (await client.GetCustomerDebtAsync(allowedCustomer.Id)).OutstandingAmount);

        var allowedSupplier = await client.CreateSupplierAsync("Allowed void supplier");
        var allowedPurchase = await client.CreatePurchaseAsync(
            allowedSupplier.Id, (allowedProduct.Id, 1, 100));
        await client.CompletePurchaseAsync(allowedPurchase.Id, Guid.NewGuid());
        var allowedPurchaseVoid = await client.VoidPurchaseAsync(
            allowedPurchase.Id, Guid.NewGuid(), "Remove unsettled purchase");
        Assert.Equal(allowedPurchase.Id, allowedPurchaseVoid.OriginalPurchaseId);
        Assert.Equal(0, (await client.GetSupplierDebtAsync(allowedSupplier.Id)).OutstandingAmount);
    }

    [Fact]
    public async Task DebtEndpointsAreStoreScopedAndValidatePaymentInput()
    {
        var first = await CreateOwnerContextAsync("Debt store A");
        var second = await CreateOwnerContextAsync("Debt store B");
        using var owner = first.Client;
        using var otherOwner = second.Client;
        var customer = await owner.CreateCustomerAsync("Scoped customer");
        var supplier = await owner.CreateSupplierAsync("Scoped supplier");

        Assert.Equal(HttpStatusCode.NotFound, (await otherOwner.GetAsync($"/api/customers/{customer.Id}/debt")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherOwner.GetAsync($"/api/suppliers/{supplier.Id}/debt")).StatusCode);
        using var crossStoreCustomerPayment = await otherOwner.PostWithAntiforgeryAsync(
            $"/api/customers/{customer.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 1, 0));
        Assert.Equal(HttpStatusCode.NotFound, crossStoreCustomerPayment.StatusCode);
        Assert.Equal("customer-not-found", await ReadCodeAsync(crossStoreCustomerPayment));
        using var crossStoreSupplierPayment = await otherOwner.PostWithAntiforgeryAsync(
            $"/api/suppliers/{supplier.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 1, 0));
        Assert.Equal(HttpStatusCode.NotFound, crossStoreSupplierPayment.StatusCode);
        Assert.Equal("supplier-not-found", await ReadCodeAsync(crossStoreSupplierPayment));

        using var precision = await owner.PostWithAntiforgeryAsync(
            $"/api/customers/{customer.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 1.001m, 0));
        Assert.Equal(HttpStatusCode.BadRequest, precision.StatusCode);
        Assert.Equal("invalid-payment-precision", await ReadCodeAsync(precision));

        using var longNote = await owner.PostWithAntiforgeryAsync(
            $"/api/customers/{customer.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 1, 0, note: new string('x', 251)));
        Assert.Equal(HttpStatusCode.BadRequest, longNote.StatusCode);
        Assert.Equal("invalid-note-length", await ReadCodeAsync(longNote));
    }

    [Fact]
    public async Task DebtPaymentDatabaseConstraintsRejectInvalidPartyShapeAndCrossStoreParty()
    {
        var first = await CreateOwnerContextAsync("Debt constraints A");
        var second = await CreateOwnerContextAsync("Debt constraints B");
        using var firstClient = first.Client;
        using var secondClient = second.Client;
        var customer = await firstClient.CreateCustomerAsync("Constraint customer");

        await factory.WithDbContextAsync(async db =>
        {
            var firstUserId = await db.Users
                .Where(item => item.Email == first.Credentials.Email)
                .Select(item => item.Id)
                .SingleAsync();
            var now = DateTimeOffset.UtcNow;
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [DebtPayments]
                    ([Id], [StoreId], [OperationId], [Direction], [Purpose], [CustomerId], [SupplierId],
                     [Amount], [Method], [Note], [OccurredAt], [PerformedByUserId])
                VALUES
                    ({Guid.NewGuid()}, {first.StoreId}, {Guid.NewGuid()}, {"MoneyIn"},
                     {"CustomerDebtCollection"}, NULL, NULL, {1m}, {"Cash"}, NULL, {now}, {firstUserId});
                """));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [DebtPayments]
                    ([Id], [StoreId], [OperationId], [Direction], [Purpose], [CustomerId], [SupplierId],
                     [Amount], [Method], [Note], [OccurredAt], [PerformedByUserId])
                VALUES
                    ({Guid.NewGuid()}, {second.StoreId}, {Guid.NewGuid()}, {"MoneyIn"},
                     {"CustomerDebtCollection"}, {customer.Id}, NULL, {1m}, {"Cash"}, NULL, {now}, {firstUserId});
                """));
            return true;
        });
    }

    [Fact]
    public async Task HistoricalCutoffAndNoOutstandingOrOverpaymentAreAuthoritative()
    {
        var context = await CreateOwnerContextAsync("Historical debt");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 100, 5);
        var customer = await client.CreateCustomerAsync("Historical customer");
        await client.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        var payment = await client.RecordCustomerDebtPaymentAsync(
            customer.Id, Guid.NewGuid(), 40, 100);
        var supplier = await client.CreateSupplierAsync("Historical supplier");
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        var supplierPayment = await client.RecordSupplierDebtPaymentAsync(
            supplier.Id, Guid.NewGuid(), 40, 100);

        var historical = await factory.WithDbContextAsync(async db =>
        {
            var repository = new Slice5Repository(db);
            var beforePayment = await repository.GetCustomerDebtAsOfAsync(
                context.StoreId, customer.Id, payment.OccurredAt, CancellationToken.None);
            var afterPayment = await repository.GetCustomerDebtAsOfAsync(
                context.StoreId, customer.Id, payment.OccurredAt.AddTicks(1), CancellationToken.None);
            var currentCustomer = await repository.GetCurrentCustomerDebtAsync(
                context.StoreId, customer.Id, CancellationToken.None);
            var supplierAtPayment = await repository.GetSupplierDebtAsOfAsync(
                context.StoreId, supplier.Id, supplierPayment.OccurredAt, CancellationToken.None);
            var supplierAfterPayment = await repository.GetSupplierDebtAsOfAsync(
                context.StoreId, supplier.Id, supplierPayment.OccurredAt.AddTicks(1), CancellationToken.None);
            var currentSupplier = await repository.GetCurrentSupplierDebtAsync(
                context.StoreId, supplier.Id, CancellationToken.None);
            return (
                beforePayment,
                afterPayment,
                currentCustomer,
                supplierAtPayment,
                supplierAfterPayment,
                currentSupplier);
        });
        Assert.Equal(100, historical.beforePayment!.OutstandingAmount);
        Assert.Equal(60, historical.afterPayment!.OutstandingAmount);
        Assert.Equal(60, historical.currentCustomer!.OutstandingAmount);
        Assert.Equal(100, historical.supplierAtPayment!.OutstandingAmount);
        Assert.Equal(60, historical.supplierAfterPayment!.OutstandingAmount);
        Assert.Equal(60, historical.currentSupplier!.OutstandingAmount);

        using var overpayment = await client.PostWithAntiforgeryAsync(
            $"/api/customers/{customer.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 61, 60));
        Assert.Equal(HttpStatusCode.Conflict, overpayment.StatusCode);
        Assert.Equal("debt-payment-exceeds-outstanding", await ReadCodeAsync(overpayment));

        await client.RecordCustomerDebtPaymentAsync(customer.Id, Guid.NewGuid(), 60, 60);
        using var noDebt = await client.PostWithAntiforgeryAsync(
            $"/api/customers/{customer.Id}/debt-payments",
            Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 1, 0));
        Assert.Equal(HttpStatusCode.Conflict, noDebt.StatusCode);
        Assert.Equal("customer-has-no-outstanding-debt", await ReadCodeAsync(noDebt));
    }

    [Fact]
    public async Task EqualTimestampCurrentDebtProtectsReturnAndVoidMutations()
    {
        var context = await CreateOwnerContextAsync("Equal timestamp debt");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 50, 10);

        var returnCustomer = await client.CreateCustomerAsync("Equal timestamp return");
        var returnSale = await client.CompleteSaleAsync(
            Guid.NewGuid(), returnCustomer.Id, [(product.Id, 2)]);
        var returnPayment = await client.RecordCustomerDebtPaymentAsync(
            returnCustomer.Id, Guid.NewGuid(), 80, 100);

        var voidCustomer = await client.CreateCustomerAsync("Equal timestamp sale void");
        var voidSale = await client.CompleteSaleAsync(
            Guid.NewGuid(), voidCustomer.Id, [(product.Id, 2)]);
        var voidPayment = await client.RecordCustomerDebtPaymentAsync(
            voidCustomer.Id, Guid.NewGuid(), 80, 100);

        var supplier = await client.CreateSupplierAsync("Equal timestamp purchase void");
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 2, 50));
        await client.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        var supplierPayment = await client.RecordSupplierDebtPaymentAsync(
            supplier.Id, Guid.NewGuid(), 80, 100);

        var timestamp = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var userId = await factory.WithDbContextAsync(db => db.Users
            .Where(item => item.Email == context.Credentials.Email)
            .Select(item => item.Id)
            .SingleAsync());
        await factory.WithDbContextAsync(async db =>
        {
            var paymentIds = new[] { returnPayment.Id, voidPayment.Id, supplierPayment.Id };
            await db.DebtPayments
                .Where(item => paymentIds.Contains(item.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.OccurredAt, timestamp));
            db.ChangeTracker.Clear();

            var currentUser = new FixedCurrentUser(userId);
            var slice1 = new Slice1Repository(db);
            var slice4 = new Slice4Repository(db);
            var slice5 = new Slice5Repository(db);
            var fixedTime = new FixedTimeProvider(timestamp);
            Assert.Equal(20, (await slice5.GetCurrentCustomerDebtAsync(
                context.StoreId, returnCustomer.Id, CancellationToken.None))!.OutstandingAmount);
            Assert.Equal(20, (await slice5.GetCurrentSupplierDebtAsync(
                context.StoreId, supplier.Id, CancellationToken.None))!.OutstandingAmount);

            var returnUseCase = new CreateReturnUseCase(
                currentUser, slice1, slice4, slice5, fixedTime);
            var completedReturn = await returnUseCase.ExecuteAsync(
                new CreateReturnCommand(
                    Guid.NewGuid(),
                    returnSale.Id,
                    [new ReturnLineCommand(Assert.Single(returnSale.Lines).Id, 1, false)],
                    "Cash",
                    20,
                    30),
                CancellationToken.None);
            Assert.Equal(30, completedReturn.RefundAmount);
            Assert.Equal(timestamp, completedReturn.CompletedAt);
            Assert.Equal(timestamp, Assert.Single(completedReturn.RefundPayments).OccurredAt);
            Assert.Equal(0, (await slice5.GetCurrentCustomerDebtAsync(
                context.StoreId, returnCustomer.Id, CancellationToken.None))!.OutstandingAmount);

            var saleVoidUseCase = new VoidSaleUseCase(
                currentUser, slice1, slice4, slice5, fixedTime);
            var saleVoidConflict = await Assert.ThrowsAsync<ApplicationConflictException>(() =>
                saleVoidUseCase.ExecuteAsync(
                    voidSale.Id,
                    new VoidTransactionCommand(Guid.NewGuid(), "Equal timestamp guard"),
                    CancellationToken.None));
            Assert.Equal("customer-debt-would-become-negative", saleVoidConflict.Code);

            var purchaseVoidUseCase = new VoidPurchaseUseCase(
                currentUser, slice1, slice4, slice5, fixedTime);
            var purchaseVoidConflict = await Assert.ThrowsAsync<ApplicationConflictException>(() =>
                purchaseVoidUseCase.ExecuteAsync(
                    purchase.Id,
                    new VoidTransactionCommand(Guid.NewGuid(), "Equal timestamp guard"),
                    CancellationToken.None));
            Assert.Equal("supplier-debt-would-become-negative", purchaseVoidConflict.Code);

            Assert.Equal(0, await db.SaleVoids.CountAsync(item => item.OriginalSaleId == voidSale.Id));
            Assert.Equal(0, await db.PurchaseVoids.CountAsync(item => item.OriginalPurchaseId == purchase.Id));
            return true;
        });
    }

    [Fact]
    public async Task DebtPaymentOperationLockTimeoutCommitsNothingAndExactRetryRecovers()
    {
        var context = await CreateOwnerContextAsync("Debt lock timeout");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 100, 2);
        var customer = await client.CreateCustomerAsync("Timeout customer");
        await client.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 1)]);
        var operationId = Guid.NewGuid();
        var resource = $"SimpleStore:BusinessOperation:{operationId:N}";

        using var timeoutResponse = await factory.WithDbContextAsync(async db =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                EXEC sys.sp_getapplock
                    @Resource = {resource},
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 0;
                """);
            var response = await client.PostWithAntiforgeryAsync(
                $"/api/customers/{customer.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(operationId, 40, 100));
            await transaction.RollbackAsync();
            return response;
        });
        Assert.Equal(HttpStatusCode.Conflict, timeoutResponse.StatusCode);
        Assert.Equal("operation-lock-timeout", await ReadCodeAsync(timeoutResponse));
        Assert.Equal(0, await factory.WithDbContextAsync(db =>
            db.DebtPayments.CountAsync(item => item.OperationId == operationId)));

        var recovered = await client.RecordCustomerDebtPaymentAsync(
            customer.Id, operationId, 40, 100);
        Assert.Equal(60, recovered.OutstandingAfter);
        Assert.False(recovered.WasAlreadyRecorded);
    }

    [Fact]
    public async Task DebtPaymentsAndVoidsSerializePerPartyWithoutNegativeDebtOrPartialEffects()
    {
        var context = await CreateOwnerContextAsync("Debt correction races");
        using var setup = context.Client;
        var saleProduct = await CreatePricedProductAsync(setup, 100, 2);
        var customer = await setup.CreateCustomerAsync("Race customer");
        var sale = await setup.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(saleProduct.Id, 1)]);
        using var customerPaymentClient = factory.CreateHttpsClient();
        using var saleVoidClient = factory.CreateHttpsClient();
        await customerPaymentClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await saleVoidClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        var customerResponses = await Task.WhenAll(
            customerPaymentClient.PostWithAntiforgeryAsync(
                $"/api/customers/{customer.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 100)),
            saleVoidClient.PostWithAntiforgeryAsync(
                $"/api/sales/{sale.Id}/void",
                JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Concurrent void" })));
        Assert.Equal(1, customerResponses.Count(response => response.IsSuccessStatusCode));
        Assert.Equal(1, customerResponses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in customerResponses) response.Dispose();
        Assert.True((await setup.GetCustomerDebtAsync(customer.Id)).OutstandingAmount >= 0);

        var purchaseProduct = await CreatePricedProductAsync(setup, 100, 2);
        var supplier = await setup.CreateSupplierAsync("Race supplier");
        var purchase = await setup.CreatePurchaseAsync(supplier.Id, (purchaseProduct.Id, 1, 100));
        await setup.CompletePurchaseAsync(purchase.Id, Guid.NewGuid());
        using var supplierPaymentClient = factory.CreateHttpsClient();
        using var purchaseVoidClient = factory.CreateHttpsClient();
        await supplierPaymentClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await purchaseVoidClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        var supplierResponses = await Task.WhenAll(
            supplierPaymentClient.PostWithAntiforgeryAsync(
                $"/api/suppliers/{supplier.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 100)),
            purchaseVoidClient.PostWithAntiforgeryAsync(
                $"/api/purchases/{purchase.Id}/void",
                JsonContent.Create(new { operationId = Guid.NewGuid(), reason = "Concurrent void" })));
        Assert.Equal(1, supplierResponses.Count(response => response.IsSuccessStatusCode));
        Assert.Equal(1, supplierResponses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in supplierResponses) response.Dispose();
        Assert.True((await setup.GetSupplierDebtAsync(supplier.Id)).OutstandingAmount >= 0);
    }

    [Fact]
    public async Task CustomerDebtPaymentAndReturnSerializeAndHonorPreviewIntention()
    {
        var context = await CreateOwnerContextAsync("Debt return race");
        using var setup = context.Client;
        var product = await CreatePricedProductAsync(setup, 50, 4);
        var customer = await setup.CreateCustomerAsync("Return race customer");
        var sale = await setup.CompleteSaleAsync(Guid.NewGuid(), customer.Id, [(product.Id, 2)]);
        var lineId = Assert.Single(sale.Lines).Id;
        var preview = await PreviewAsync(setup, sale.Id, lineId, 1);
        using var paymentClient = factory.CreateHttpsClient();
        using var returnClient = factory.CreateHttpsClient();
        await paymentClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await returnClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);

        var responses = await Task.WhenAll(
            paymentClient.PostWithAntiforgeryAsync(
                $"/api/customers/{customer.Id}/debt-payments",
                Slice5HttpClient.DebtPaymentContent(Guid.NewGuid(), 80, 100)),
            returnClient.PostWithAntiforgeryAsync(
                "/api/returns",
                Slice4HttpClient.ReturnContent(
                    Guid.NewGuid(), sale.Id, [(lineId, 1, false)], null,
                    preview.CurrentAggregateCustomerDebt, preview.RequiredActualRefund)));
        Assert.Equal(1, responses.Count(response => response.IsSuccessStatusCode));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in responses) response.Dispose();
        var finalDebt = (await setup.GetCustomerDebtAsync(customer.Id)).OutstandingAmount;
        Assert.True(finalDebt is 20m or 50m);
        Assert.True(finalDebt >= 0);
    }

    [Fact]
    public async Task CompleteSaleAndPurchaseParticipateInPartyDebtLocks()
    {
        var context = await CreateOwnerContextAsync("Completion debt locks");
        using var client = context.Client;
        var product = await CreatePricedProductAsync(client, 100, 3);
        var customer = await client.CreateCustomerAsync("Locked completion customer");
        var supplier = await client.CreateSupplierAsync("Locked completion supplier");
        var purchase = await client.CreatePurchaseAsync(supplier.Id, (product.Id, 1, 100));
        var saleOperationId = Guid.NewGuid();
        var purchaseOperationId = Guid.NewGuid();
        using var saleClient = factory.CreateHttpsClient();
        using var purchaseClient = factory.CreateHttpsClient();
        await saleClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);
        await purchaseClient.LoginAsync(context.Credentials.Email, context.Credentials.Password);

        var responses = await factory.WithDbContextAsync(async db =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await AcquireTestLockAsync(
                db,
                $"SimpleStore:CustomerDebt:{context.StoreId:N}:{customer.Id:N}");
            await AcquireTestLockAsync(
                db,
                $"SimpleStore:SupplierDebt:{context.StoreId:N}:{supplier.Id:N}");
            var lockedResponses = await Task.WhenAll(
                saleClient.PostWithAntiforgeryAsync(
                    "/api/sales/complete",
                    Slice3HttpClient.SaleContent(
                        saleOperationId, customer.Id, [(product.Id, 1)])),
                purchaseClient.PostWithAntiforgeryAsync(
                    $"/api/purchases/{purchase.Id}/complete",
                    JsonContent.Create(new { operationId = purchaseOperationId, payments = Array.Empty<object>() })));
            await transaction.RollbackAsync();
            return lockedResponses;
        });

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Conflict, response.StatusCode));
        foreach (var response in responses)
        {
            Assert.Equal("operation-lock-timeout", await ReadCodeAsync(response));
            response.Dispose();
        }
        Assert.Equal(0, await factory.WithDbContextAsync(db =>
            db.BusinessOperations.CountAsync(item =>
                item.OperationId == saleOperationId || item.OperationId == purchaseOperationId)));

        await client.CompleteSaleAsync(
            saleOperationId, customer.Id, [(product.Id, 1)]);
        await client.CompletePurchaseAsync(purchase.Id, purchaseOperationId);
        Assert.Equal(100, (await client.GetCustomerDebtAsync(customer.Id)).OutstandingAmount);
        Assert.Equal(100, (await client.GetSupplierDebtAsync(supplier.Id)).OutstandingAmount);
    }

    private static async Task AssertReturnCaseAsync(
        HttpClient client,
        Guid productId,
        decimal startingDebt,
        decimal expectedRefund,
        decimal expectedEndingDebt)
    {
        var customer = await client.CreateCustomerAsync($"Return debt {startingDebt}");
        var sale = await client.CompleteSaleAsync(
            Guid.NewGuid(), customer.Id, [(productId, 1)], startingDebt == 50 ? [] : [(50 - startingDebt, "Cash")]);
        var lineId = Assert.Single(sale.Lines).Id;
        var preview = await PreviewAsync(client, sale.Id, lineId, 1);
        Assert.Equal(startingDebt, preview.CurrentAggregateCustomerDebt);
        Assert.Equal(Math.Min(50, startingDebt), preview.DebtReduction);
        Assert.Equal(expectedRefund, preview.RequiredActualRefund);
        var result = await CompleteFromPreviewAsync(
            client, sale.Id, lineId, preview, expectedRefund > 0 ? "Cash" : null);
        Assert.Equal(expectedRefund, result.RefundAmount);
        Assert.Equal(expectedRefund > 0 ? 1 : 0, result.RefundPayments.Count);
        Assert.Equal(expectedEndingDebt, (await client.GetCustomerDebtAsync(customer.Id)).OutstandingAmount);
    }

    private static async Task<ReturnPreviewResult> PreviewAsync(
        HttpClient client,
        Guid saleId,
        Guid lineId,
        decimal quantity)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/returns/preview",
            JsonContent.Create(new
            {
                originalSaleId = saleId,
                lines = new[] { new { originalSaleLineId = lineId, quantity, restock = false } }
            }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReturnPreviewResult>())!;
    }

    private static async Task<ReturnResult> CompleteFromPreviewAsync(
        HttpClient client,
        Guid saleId,
        Guid lineId,
        ReturnPreviewResult preview,
        string? refundMethod = null)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/returns",
            Slice4HttpClient.ReturnContent(
                Guid.NewGuid(), saleId, [(lineId, 1, false)], refundMethod,
                preview.CurrentAggregateCustomerDebt, preview.RequiredActualRefund));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ReturnResult>())!;
    }

    private static async Task<ProductResult> CreatePricedProductAsync(
        HttpClient client,
        decimal salePrice,
        decimal openingQuantity)
    {
        using var response = await client.PostWithAntiforgeryAsync(
            "/api/products",
            JsonContent.Create(new
            {
                sku = $"SL5-{Guid.NewGuid():N}",
                name = "Slice 5 product",
                unit = "item",
                salePrice,
                referencePurchaseCost = 1,
                openingQuantity,
                openingCost = 1
            }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResult>())!;
    }

    private static Task<int> AcquireTestLockAsync(ApplicationDbContext db, string resource) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            EXEC sys.sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 0;
            """);

    private async Task<OwnerContext> CreateOwnerContextAsync(string storeName)
    {
        var credentials = await factory.CreateOwnerAsync();
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(credentials.Email, credentials.Password);
        var store = await client.InitializeStoreAsync(storeName);
        Assert.Equal("Asia/Ho_Chi_Minh", store.TimeZoneId);
        return new OwnerContext(client, store.Id, credentials);
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("code").GetString();
    }

    private static async Task<decimal> ReadDecimalExtensionAsync(HttpResponseMessage response, string name)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty(name).GetDecimal();
    }

    private sealed record OwnerContext(
        HttpClient Client,
        Guid StoreId,
        (string Email, string Password) Credentials);

    private sealed record FixedCurrentUser(Guid UserId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
