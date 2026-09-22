using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Corrections;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;

namespace SimpleStore.Application.Purchases;

public sealed class CreatePurchaseUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository,
    TimeProvider timeProvider)
{
    public async Task<PurchaseResult> ExecuteAsync(
        PurchaseWriteCommand command,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var supplier = await PurchaseUseCaseSupport.GetActiveSupplierAsync(
            repository,
            storeId,
            command.SupplierId,
            cancellationToken);
        var products = await PurchaseUseCaseSupport.GetRequiredProductsAsync(
            repository,
            storeId,
            command.Lines,
            requireActive: false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var purchase = Purchase.CreateDraft(
            storeId,
            supplier.Id,
            userId,
            command.Lines.Select(PurchaseUseCaseSupport.ToDomainLine).ToArray(),
            now);
        repository.AddPurchase(purchase);
        await repository.SaveChangesAsync(cancellationToken);
        return PurchaseUseCaseSupport.ToResult(purchase, supplier.Name, products);
    }
}

public sealed class UpdatePurchaseUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository,
    TimeProvider timeProvider)
{
    public async Task<PurchaseResult> ExecuteAsync(
        Guid purchaseId,
        PurchaseWriteCommand command,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var purchase = await PurchaseUseCaseSupport.GetRequiredPurchaseAsync(
            repository,
            storeId,
            purchaseId,
            cancellationToken);
        if (purchase.Status == PurchaseStatus.Completed)
        {
            throw new ApplicationConflictException(
                "purchase-completed-immutable",
                "A completed purchase cannot be changed.");
        }

        var supplier = await PurchaseUseCaseSupport.GetActiveSupplierAsync(
            repository,
            storeId,
            command.SupplierId,
            cancellationToken);
        var products = await PurchaseUseCaseSupport.GetRequiredProductsAsync(
            repository,
            storeId,
            command.Lines,
            requireActive: false,
            cancellationToken);
        var previousLines = purchase.Lines.ToArray();
        purchase.ReplaceDraft(
            supplier.Id,
            command.Lines.Select(PurchaseUseCaseSupport.ToDomainLine).ToArray(),
            timeProvider.GetUtcNow());
        repository.RemovePurchaseLines(previousLines);
        foreach (var line in purchase.Lines)
        {
            repository.AddPurchaseLine(line);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return PurchaseUseCaseSupport.ToResult(purchase, supplier.Name, products);
    }
}

public sealed class GetPurchaseUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository)
{
    public async Task<PurchaseResult> ExecuteAsync(Guid purchaseId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var purchase = await PurchaseUseCaseSupport.GetRequiredPurchaseAsync(
            repository,
            storeId,
            purchaseId,
            cancellationToken);
        return await PurchaseUseCaseSupport.BuildResultAsync(
            repository,
            storeId,
            purchase,
            false,
            cancellationToken);
    }
}

public sealed class GetPurchasesUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository)
{
    public async Task<PurchaseListResult> ExecuteAsync(
        PurchaseStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        PaginationGuard.Validate(page, pageSize);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var result = await repository.SearchPurchasesAsync(
            storeId,
            status,
            page,
            pageSize,
            cancellationToken);
        var items = result.Items.Select(item => new PurchaseListItemResult(
            item.Purchase.Id,
            item.Purchase.SupplierId,
            item.SupplierName,
            item.Purchase.Status.ToString(),
            item.Purchase.TotalAmount,
            item.PaidAmount,
            item.IsVoided ? 0 : item.Purchase.TotalAmount - item.PaidAmount,
            item.Purchase.CreatedAt,
            item.Purchase.CompletedAt,
            item.IsVoided)).ToArray();
        return new PurchaseListResult(
            items,
            page,
            pageSize,
            result.TotalCount,
            result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize));
    }
}

public sealed class CompletePurchaseUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository,
    ISlice5Repository debtRepository,
    TimeProvider timeProvider)
{
    public async Task<PurchaseResult> ExecuteAsync(
        Guid purchaseId,
        CompletePurchaseCommand command,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var payments = command.Payments.Select(PurchaseUseCaseSupport.ToDomainPayment).ToArray();
        var fingerprint = CreateFingerprint(purchaseId, payments);

        return await repository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                await repository.AcquireOperationLockAsync(
                    command.OperationId,
                    transactionCancellationToken);
                var existingOperation = await repository.GetOperationAsync(
                    storeId,
                    command.OperationId,
                    transactionCancellationToken);
                if (existingOperation is not null)
                {
                    if (existingOperation.OperationType != BusinessOperationTypes.CompletePurchase
                        || !string.Equals(
                            existingOperation.RequestFingerprint,
                            fingerprint,
                            StringComparison.Ordinal))
                    {
                        throw new ApplicationConflictException(
                            "idempotency-key-reused",
                            "OperationId was already used for a different request.");
                    }

                    var completedPurchase = await PurchaseUseCaseSupport.GetRequiredPurchaseAsync(
                        repository,
                        storeId,
                        existingOperation.ResultReference!.Value,
                        transactionCancellationToken);
                    return await PurchaseUseCaseSupport.BuildResultAsync(
                        repository,
                        storeId,
                        completedPurchase,
                        true,
                        transactionCancellationToken);
                }

                var purchase = await PurchaseUseCaseSupport.GetRequiredPurchaseAsync(
                    repository,
                    storeId,
                    purchaseId,
                    transactionCancellationToken);
                if (purchase.Status == PurchaseStatus.Completed)
                {
                    throw new ApplicationConflictException(
                        "purchase-already-completed",
                        "Purchase is already completed.");
                }

                await debtRepository.AcquireSupplierDebtLockAsync(
                    storeId,
                    purchase.SupplierId,
                    transactionCancellationToken);

                var supplier = await PurchaseUseCaseSupport.GetActiveSupplierAsync(
                    repository,
                    storeId,
                    purchase.SupplierId,
                    transactionCancellationToken);
                var lineCommands = purchase.Lines.Select(line => new PurchaseLineCommand(
                    line.ProductId,
                    line.Quantity,
                    line.UnitPrice)).ToArray();
                _ = await PurchaseUseCaseSupport.GetRequiredProductsAsync(
                    repository,
                    storeId,
                    lineCommands,
                    requireActive: true,
                    transactionCancellationToken);
                var warehouse = await repository.GetMainWarehouseAsync(
                    storeId,
                    transactionCancellationToken)
                    ?? throw new ApplicationNotFoundException(
                        "main-warehouse-not-found",
                        "Main warehouse was not found.");
                var orderedProductIds = purchase.Lines
                    .Select(line => line.ProductId)
                    .OrderBy(productId => productId)
                    .ToArray();
                var balances = await repository.LockInventoryBalancesAsync(
                    storeId,
                    warehouse.Id,
                    orderedProductIds,
                    transactionCancellationToken);
                if (balances.Count != orderedProductIds.Length)
                {
                    throw new ApplicationConflictException(
                        "inventory-balance-missing",
                        "An inventory balance is missing for a purchase product.");
                }

                var products = await repository.GetProductsForUpdateAsync(
                    storeId,
                    orderedProductIds,
                    transactionCancellationToken);
                PurchaseUseCaseSupport.EnsureProductsValid(
                    products,
                    orderedProductIds,
                    requireActive: true);

                var now = timeProvider.GetUtcNow();
                purchase.Complete(payments, userId, now);
                foreach (var payment in purchase.Payments)
                {
                    repository.AddPurchasePayment(payment);
                }

                foreach (var line in purchase.Lines.OrderBy(line => line.ProductId))
                {
                    var balance = balances[line.ProductId];
                    var product = products[line.ProductId];
                    var quantityBefore = balance.QuantityOnHand;
                    var inventoryValueBefore = balance.InventoryValue;
                    var averageCostBefore = balance.AverageCost;
                    var hasAverageCostBefore = balance.HasAverageCost;
                    var referencePurchaseCostBefore = product.ReferencePurchaseCost;
                    var movement = InventoryMovement.CreatePurchase(
                        storeId,
                        warehouse.Id,
                        line.ProductId,
                        line.Quantity,
                        line.LineAmount,
                        line.Id,
                        userId,
                        now);
                    balance.ReceivePurchase(line.Quantity, line.LineAmount, now);
                    product.UpdateReferencePurchaseCost(line.UnitPrice, now);
                    repository.AddInventoryMovement(movement);
                    repository.AddPurchaseLineReversalBasis(PurchaseLineReversalBasis.Capture(
                        storeId,
                        purchase.Id,
                        line.Id,
                        line.ProductId,
                        warehouse.Id,
                        quantityBefore,
                        inventoryValueBefore,
                        averageCostBefore,
                        hasAverageCostBefore,
                        referencePurchaseCostBefore,
                        line.UnitPrice,
                        product.ReferencePurchaseCostRevision,
                        movement.Id,
                        now));
                }

                repository.AddBusinessOperation(BusinessOperation.CompletePurchase(
                    command.OperationId,
                    storeId,
                    fingerprint,
                    purchase.Id,
                    now));
                return PurchaseUseCaseSupport.ToResult(purchase, supplier.Name, products);
            },
            cancellationToken);
    }

    private static string CreateFingerprint(
        Guid purchaseId,
        IReadOnlyCollection<PurchasePaymentInput> payments)
    {
        var normalizedPayments = payments
            .OrderBy(payment => payment.Method)
            .ThenBy(payment => payment.Amount)
            .Select(payment => string.Create(
                CultureInfo.InvariantCulture,
                $"{payment.Method}:{payment.Amount:G29}"));
        var normalized = $"purchase:{purchaseId:N}|payments:{string.Join(';', normalizedPayments)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}

public sealed class GetOperationStatusUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice2Repository repository)
{
    public async Task<OperationStatusResult?> ExecuteAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            slice1Repository,
            cancellationToken);
        var operation = await repository.GetOperationAsync(storeId, operationId, cancellationToken);
        return operation is null
            ? null
            : new OperationStatusResult(
                operation.OperationId,
                operation.Status.ToString(),
                operation.OperationType,
                operation.ResultReference);
    }
}

internal static class PurchaseUseCaseSupport
{
    public static PurchaseLineInput ToDomainLine(PurchaseLineCommand command) =>
        new(command.ProductId, command.Quantity, command.UnitPrice);

    public static PurchasePaymentInput ToDomainPayment(PurchasePaymentCommand command)
    {
        if (!Enum.TryParse<PaymentMethod>(command.Method, true, out var method))
        {
            throw new ApplicationValidationException(
                "invalid-payment-method",
                "Payment method is invalid.",
                [new ValidationError(null, "method", "invalid-payment-method", "Use Cash or Transfer.")]);
        }

        return new PurchasePaymentInput(command.Amount, method);
    }

    public static async Task<Purchase> GetRequiredPurchaseAsync(
        ISlice2Repository repository,
        Guid storeId,
        Guid purchaseId,
        CancellationToken cancellationToken) =>
        await repository.GetPurchaseAsync(storeId, purchaseId, cancellationToken)
        ?? throw new ApplicationNotFoundException("purchase-not-found", "Purchase was not found.");

    public static async Task<Domain.Suppliers.Supplier> GetActiveSupplierAsync(
        ISlice2Repository repository,
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var supplier = await SupplierUseCaseSupport.GetRequiredAsync(
            repository,
            storeId,
            supplierId,
            cancellationToken);
        if (!supplier.IsActive)
        {
            throw new ApplicationConflictException(
                "supplier-inactive",
                "The supplier is inactive.");
        }

        return supplier;
    }

    public static async Task<IReadOnlyDictionary<Guid, Product>> GetRequiredProductsAsync(
        ISlice2Repository repository,
        Guid storeId,
        IReadOnlyCollection<PurchaseLineCommand> lines,
        bool requireActive,
        CancellationToken cancellationToken)
    {
        var productIds = lines.Select(line => line.ProductId).Distinct().ToArray();
        var products = await repository.GetProductsAsync(storeId, productIds, cancellationToken);
        EnsureProductsValid(products, productIds, requireActive);
        return products;
    }

    public static void EnsureProductsValid(
        IReadOnlyDictionary<Guid, Product> products,
        IReadOnlyCollection<Guid> productIds,
        bool requireActive)
    {
        if (products.Count != productIds.Count)
        {
            throw new ApplicationValidationException(
                "purchase-product-not-found",
                "A purchase product was not found in the current store.",
                [new ValidationError(null, "productId", "purchase-product-not-found", "Select products from the current store.")]);
        }

        if (requireActive && products.Values.Any(product => !product.IsActive))
        {
            throw new ApplicationConflictException(
                "purchase-product-inactive",
                "All products must be active when completing a purchase.");
        }
    }

    public static async Task<PurchaseResult> BuildResultAsync(
        ISlice2Repository repository,
        Guid storeId,
        Purchase purchase,
        bool wasAlreadyCompleted,
        CancellationToken cancellationToken)
    {
        var supplier = await SupplierUseCaseSupport.GetRequiredAsync(
            repository,
            storeId,
            purchase.SupplierId,
            cancellationToken);
        var products = await repository.GetProductsAsync(
            storeId,
            purchase.Lines.Select(line => line.ProductId).ToArray(),
            cancellationToken);
        var purchaseVoid = await repository.GetPurchaseVoidAsync(storeId, purchase.Id, cancellationToken);
        return ToResult(purchase, supplier.Name, products, wasAlreadyCompleted, purchaseVoid);
    }

    public static PurchaseResult ToResult(
        Purchase purchase,
        string supplierName,
        IReadOnlyDictionary<Guid, Product> products,
        bool wasAlreadyCompleted = false,
        PurchaseVoid? purchaseVoid = null) =>
        new(
            purchase.Id,
            purchase.SupplierId,
            supplierName,
            purchase.Status.ToString(),
            purchase.Lines.Select(line => new PurchaseLineResult(
                line.Id,
                line.ProductId,
                products[line.ProductId].Name,
                products[line.ProductId].Unit,
                line.Quantity,
                line.UnitPrice,
                line.LineAmount)).ToArray(),
            purchase.Payments.Select(payment => new PurchasePaymentResult(
                payment.Id,
                payment.Amount,
                payment.Method.ToString(),
                payment.PaidAt)).ToArray(),
            purchase.TotalAmount,
            purchase.PaidAmount,
            purchaseVoid is null ? purchase.OutstandingAmount : 0,
            purchase.CreatedAt,
            purchase.UpdatedAt,
            purchase.CompletedAt,
            wasAlreadyCompleted,
            purchaseVoid is not null,
            purchaseVoid is null ? null : new PurchaseVoidInfoResult(
                purchaseVoid.Id,
                purchaseVoid.Reason,
                purchaseVoid.VoidedByUserId,
                purchaseVoid.VoidedAt));
}
