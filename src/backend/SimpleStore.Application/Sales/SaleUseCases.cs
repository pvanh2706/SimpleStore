using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Customers;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Sales;

namespace SimpleStore.Application.Sales;

public sealed class CompleteSaleUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice3Repository repository,
    TimeProvider timeProvider)
{
    public async Task<SaleResult> ExecuteAsync(
        CompleteSaleCommand command,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var payments = command.Payments.Select(ToDomainPayment).ToArray();
        var fingerprint = CreateFingerprint(command, payments);

        return await repository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                await repository.AcquireOperationLockAsync(command.OperationId, transactionCancellationToken);
                var existingOperation = await repository.GetOperationAsync(
                    storeId,
                    command.OperationId,
                    transactionCancellationToken);
                if (existingOperation is not null)
                {
                    if (existingOperation.OperationType != BusinessOperationTypes.CompleteSale
                        || !string.Equals(existingOperation.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                    {
                        throw new ApplicationConflictException(
                            "idempotency-key-reused",
                            "OperationId was already used for a different request.");
                    }

                    var existingSale = await GetRequiredSaleAsync(
                        storeId,
                        existingOperation.ResultReference!.Value,
                        transactionCancellationToken);
                    return await BuildResultAsync(existingSale, true, transactionCancellationToken);
                }

                if (command.Lines.Count == 0)
                {
                    throw new ApplicationValidationException(
                        "sale-lines-required",
                        "A sale requires at least one line.",
                        [new ValidationError(null, "lines", "sale-lines-required", "Add at least one product.")]);
                }

                var orderedProductIds = command.Lines
                    .Select(line => line.ProductId)
                    .Distinct()
                    .OrderBy(productId => productId)
                    .ToArray();
                var store = await repository.GetStoreAsync(storeId, transactionCancellationToken)
                    ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");
                var warehouse = await repository.GetMainWarehouseAsync(storeId, transactionCancellationToken)
                    ?? throw new ApplicationNotFoundException(
                        "main-warehouse-not-found",
                        "Main warehouse was not found.");
                var balances = await repository.LockInventoryBalancesAsync(
                    storeId,
                    warehouse.Id,
                    orderedProductIds,
                    transactionCancellationToken);
                if (balances.Count != orderedProductIds.Length)
                {
                    throw new ApplicationValidationException(
                        "sale-product-not-found",
                        "A sale product was not found in the current store.",
                        [new ValidationError(null, "productId", "sale-product-not-found", "Select products from the current store.")]);
                }

                var products = await repository.GetProductsForUpdateAsync(
                    storeId,
                    orderedProductIds,
                    transactionCancellationToken);
                if (products.Count != orderedProductIds.Length)
                {
                    throw new ApplicationValidationException(
                        "sale-product-not-found",
                        "A sale product was not found in the current store.",
                        [new ValidationError(null, "productId", "sale-product-not-found", "Select products from the current store.")]);
                }

                if (products.Values.Any(product => !product.IsActive))
                {
                    throw new ApplicationConflictException(
                        "sale-product-inactive",
                        "All products must be active when completing a sale.");
                }

                var requestedQuantities = command.Lines
                    .GroupBy(line => line.ProductId)
                    .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));
                var shortages = requestedQuantities
                    .Where(item => item.Value > balances[item.Key].QuantityOnHand)
                    .Select(item => new StockShortage(
                        item.Key,
                        item.Value - balances[item.Key].QuantityOnHand))
                    .ToArray();
                if (!store.AllowNegativeStock && shortages.Length > 0)
                {
                    throw new InsufficientStockException(shortages);
                }

                Customer? customer = null;
                if (command.CustomerId.HasValue)
                {
                    customer = await repository.GetCustomerAsync(
                        storeId,
                        command.CustomerId.Value,
                        transactionCancellationToken)
                        ?? throw new ApplicationNotFoundException("customer-not-found", "Customer was not found.");
                }

                var lineInputs = command.Lines.Select(line =>
                {
                    var product = products[line.ProductId];
                    var cost = balances[line.ProductId].ResolveSaleCost(product.ReferencePurchaseCost);
                    return new SaleLineInput(
                        product.Id,
                        product.Name,
                        product.Sku,
                        product.Unit,
                        line.Quantity,
                        product.SalePrice,
                        cost.UnitCost,
                        cost.Reliability);
                }).ToArray();
                var now = timeProvider.GetUtcNow();
                var sale = Sale.Complete(
                    storeId,
                    warehouse.Id,
                    customer?.Id,
                    userId,
                    lineInputs,
                    payments,
                    now);

                repository.AddSale(sale);
                foreach (var line in sale.Lines.OrderBy(line => line.ProductId))
                {
                    var inventoryValue = Math.Round(
                        line.Quantity * line.UnitCostAtSale,
                        2,
                        MidpointRounding.AwayFromZero);
                    balances[line.ProductId].IssueSale(line.Quantity, inventoryValue, now);
                    repository.AddInventoryMovement(InventoryMovement.CreateSale(
                        storeId,
                        warehouse.Id,
                        line.ProductId,
                        line.Quantity,
                        inventoryValue,
                        line.UnitCostAtSale,
                        line.Id,
                        userId,
                        now));
                }

                repository.AddBusinessOperation(BusinessOperation.CompleteSale(
                    command.OperationId,
                    storeId,
                    fingerprint,
                    sale.Id,
                    now));
                return await BuildResultAsync(sale, false, transactionCancellationToken, store, customer);
            },
            cancellationToken);
    }

    private async Task<Sale> GetRequiredSaleAsync(
        Guid storeId,
        Guid saleId,
        CancellationToken cancellationToken) =>
        await repository.GetSaleAsync(storeId, saleId, cancellationToken)
        ?? throw new ApplicationNotFoundException("sale-not-found", "Sale was not found.");

    private async Task<SaleResult> BuildResultAsync(
        Sale sale,
        bool wasAlreadyCompleted,
        CancellationToken cancellationToken,
        Domain.Stores.Store? store = null,
        Customer? customer = null)
    {
        store ??= await repository.GetStoreAsync(sale.StoreId, cancellationToken)
            ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");
        if (customer is null && sale.CustomerId.HasValue)
        {
            customer = await repository.GetCustomerAsync(sale.StoreId, sale.CustomerId.Value, cancellationToken)
                ?? throw new ApplicationNotFoundException("customer-not-found", "Customer was not found.");
        }

        var cashier = await repository.GetUserDisplayNameAsync(sale.CompletedByUserId, cancellationToken);
        return SaleUseCaseSupport.ToResult(sale, store.Name, customer, cashier, wasAlreadyCompleted);
    }

    private static SalePaymentInput ToDomainPayment(SalePaymentCommand command)
    {
        if (!Enum.TryParse<PaymentMethod>(command.Method, true, out var method))
        {
            throw new ApplicationValidationException(
                "invalid-payment-method",
                "Payment method is invalid.",
                [new ValidationError(null, "method", "invalid-payment-method", "Use Cash or Transfer.")]);
        }

        return new SalePaymentInput(command.Amount, method);
    }

    private static string CreateFingerprint(
        CompleteSaleCommand command,
        IReadOnlyCollection<SalePaymentInput> payments)
    {
        var lines = command.Lines
            .OrderBy(line => line.ProductId)
            .Select(line => string.Create(
                CultureInfo.InvariantCulture,
                $"{line.ProductId:N}:{line.Quantity:G29}"));
        var normalizedPayments = payments
            .OrderBy(payment => payment.Method)
            .ThenBy(payment => payment.Amount)
            .Select(payment => string.Create(
                CultureInfo.InvariantCulture,
                $"{payment.Method}:{payment.Amount:G29}"));
        var normalized = string.Create(
            CultureInfo.InvariantCulture,
            $"sale|customer:{command.CustomerId?.ToString("N") ?? "null"}|lines:{string.Join(';', lines)}|payments:{string.Join(';', normalizedPayments)}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}

public sealed class GetSaleUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice3Repository repository)
{
    public async Task<SaleResult> ExecuteAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var sale = await repository.GetSaleAsync(storeId, saleId, cancellationToken)
            ?? throw new ApplicationNotFoundException("sale-not-found", "Sale was not found.");
        var store = await repository.GetStoreAsync(storeId, cancellationToken)
            ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");
        var customer = sale.CustomerId.HasValue
            ? await repository.GetCustomerAsync(storeId, sale.CustomerId.Value, cancellationToken)
            : null;
        var cashier = await repository.GetUserDisplayNameAsync(sale.CompletedByUserId, cancellationToken);
        return SaleUseCaseSupport.ToResult(sale, store.Name, customer, cashier);
    }
}

public sealed class GetSalesUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice3Repository repository)
{
    public async Task<SaleListResult> ExecuteAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        PaginationGuard.Validate(page, pageSize);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var result = await repository.SearchSalesAsync(storeId, page, pageSize, cancellationToken);
        var items = result.Items.Select(item => new SaleListItemResult(
            item.Sale.Id,
            item.Sale.Status.ToString(),
            item.CustomerName,
            item.CashierDisplayName,
            item.Sale.TotalAmount,
            item.Sale.PaidAmount,
            item.Sale.OutstandingAmount,
            item.Sale.CompletedAt)).ToArray();
        return new SaleListResult(
            items,
            page,
            pageSize,
            result.TotalCount,
            result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize));
    }
}

internal static class SaleUseCaseSupport
{
    public static SaleResult ToResult(
        Sale sale,
        string storeName,
        Customer? customer,
        string cashierDisplayName,
        bool wasAlreadyCompleted = false) =>
        new(
            sale.Id,
            sale.Status.ToString(),
            storeName,
            sale.WarehouseId,
            customer is null ? null : new SaleCustomerResult(customer.Id, customer.Name, customer.Phone),
            cashierDisplayName,
            sale.Lines.Select(line => new SaleLineResult(
                line.Id,
                line.ProductId,
                line.ProductName,
                line.ProductSku,
                line.ProductUnit,
                line.Quantity,
                line.UnitSalePrice,
                line.LineAmount,
                line.UnitCostAtSale,
                line.CostReliability.ToString())).ToArray(),
            sale.Payments.Select(payment => new SalePaymentResult(
                payment.Id,
                payment.Amount,
                payment.Method.ToString(),
                payment.OccurredAt)).ToArray(),
            sale.TotalAmount,
            sale.PaidAmount,
            sale.OutstandingAmount,
            sale.CreatedAt,
            sale.CompletedAt,
            wasAlreadyCompleted);
}
