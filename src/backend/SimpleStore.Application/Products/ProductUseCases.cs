using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;

namespace SimpleStore.Application.Products;

public sealed class CreateProductUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository,
    TimeProvider timeProvider)
{
    public async Task<ProductResult> ExecuteAsync(
        ProductWriteCommand command,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        var sku = await ProductUseCaseSupport.ResolveSkuAsync(
            command.Sku,
            storeId,
            repository,
            cancellationToken);
        var openingInventory = OpeningInventory.Create(command.OpeningQuantity, command.OpeningCost);
        var referenceCost = command.ReferencePurchaseCost ?? command.OpeningCost;
        var now = timeProvider.GetUtcNow();
        var product = Product.Create(
            storeId,
            sku,
            command.Barcode,
            command.Name,
            command.Unit,
            command.SalePrice,
            referenceCost,
            now);

        await ProductUseCaseSupport.EnsureIdentifiersAvailableAsync(
            repository,
            storeId,
            product,
            null,
            cancellationToken);

        return await repository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                await ProductUseCaseSupport.EnsureIdentifiersAvailableAsync(
                    repository,
                    storeId,
                    product,
                    null,
                    transactionCancellationToken);

                var warehouse = await ProductUseCaseSupport.GetMainWarehouseAsync(
                    repository,
                    storeId,
                    transactionCancellationToken);
                var balance = InventoryBalance.Create(
                    storeId,
                    warehouse.Id,
                    product.Id,
                    openingInventory,
                    now);

                repository.AddProduct(product);
                repository.AddInventoryBalance(balance);

                if (openingInventory.HasStock)
                {
                    repository.AddInventoryMovement(InventoryMovement.CreateOpeningBalance(
                        storeId,
                        warehouse.Id,
                        product.Id,
                        openingInventory,
                        "Product",
                        product.Id,
                        userId,
                        now));
                }

                return ProductUseCaseSupport.ToResult(product, balance);
            },
            cancellationToken);
    }
}

public sealed class UpdateProductUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository,
    TimeProvider timeProvider)
{
    public async Task<ProductResult> ExecuteAsync(
        Guid productId,
        ProductWriteCommand command,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        var product = await ProductUseCaseSupport.GetProductAsync(
            repository,
            storeId,
            productId,
            cancellationToken);
        var sku = string.IsNullOrWhiteSpace(command.Sku) ? product.Sku : command.Sku;

        product.Update(
            sku!,
            command.Barcode,
            command.Name,
            command.Unit,
            command.SalePrice,
            command.ReferencePurchaseCost,
            timeProvider.GetUtcNow());

        await ProductUseCaseSupport.EnsureIdentifiersAvailableAsync(
            repository,
            storeId,
            product,
            product.Id,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var balance = await repository.GetInventoryBalanceAsync(storeId, product.Id, cancellationToken)
            ?? throw new ApplicationNotFoundException(
                "inventory-balance-not-found",
                "Inventory balance was not found.");
        return ProductUseCaseSupport.ToResult(product, balance);
    }
}

public sealed class DeactivateProductUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(Guid productId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        var product = await ProductUseCaseSupport.GetProductAsync(
            repository,
            storeId,
            productId,
            cancellationToken);

        product.Deactivate(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetProductUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository)
{
    public async Task<ProductResult> ExecuteAsync(Guid productId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        var product = await ProductUseCaseSupport.GetProductAsync(
            repository,
            storeId,
            productId,
            cancellationToken);
        var balance = await repository.GetInventoryBalanceAsync(storeId, product.Id, cancellationToken)
            ?? throw new ApplicationNotFoundException(
                "inventory-balance-not-found",
                "Inventory balance was not found.");

        return ProductUseCaseSupport.ToResult(product, balance);
    }
}

public sealed class GetProductsUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository)
{
    public async Task<ProductListResult> ExecuteAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ApplicationValidationException(
                "invalid-pagination",
                "Pagination values are invalid.",
                [new ValidationError(null, "page", "invalid-pagination", "Page must be at least 1 and page size must be between 1 and 100.")]);
        }

        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        var result = await repository.SearchProductsAsync(
            storeId,
            search?.Trim(),
            isActive,
            page,
            pageSize,
            cancellationToken);

        var items = result.Items
            .Select(item => new ProductListItemResult(
                item.Product.Id,
                item.Product.Sku,
                item.Product.Barcode,
                item.Product.Name,
                item.Product.Unit,
                item.Product.SalePrice,
                item.Product.IsActive,
                item.QuantityOnHand))
            .ToArray();
        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)pageSize);

        return new ProductListResult(items, page, pageSize, result.TotalCount, totalPages);
    }
}

public sealed class GetInventoryBalanceUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository)
{
    public async Task<InventoryBalanceResult> ExecuteAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        _ = await ProductUseCaseSupport.GetProductAsync(
            repository,
            storeId,
            productId,
            cancellationToken);
        var balance = await repository.GetInventoryBalanceAsync(storeId, productId, cancellationToken)
            ?? throw new ApplicationNotFoundException(
                "inventory-balance-not-found",
                "Inventory balance was not found.");

        return new InventoryBalanceResult(
            balance.ProductId,
            balance.WarehouseId,
            balance.QuantityOnHand,
            balance.InventoryValue,
            balance.AverageCost,
            balance.HasAverageCost,
            balance.UpdatedAt);
    }
}

public sealed class GetInventoryMovementsUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository)
{
    public async Task<IReadOnlyList<InventoryMovementResult>> ExecuteAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        _ = await ProductUseCaseSupport.GetProductAsync(
            repository,
            storeId,
            productId,
            cancellationToken);
        var movements = await repository.GetInventoryMovementsAsync(
            storeId,
            productId,
            cancellationToken);

        return movements
            .Select(movement => new InventoryMovementResult(
                movement.Id,
                movement.MovementType.ToString(),
                movement.QuantityDelta,
                movement.InventoryValueDelta,
                movement.UnitCost,
                movement.SourceType,
                movement.SourceId,
                movement.PerformedByUserId,
                movement.OccurredAt,
                movement.CostReliability?.ToString(),
                movement.Reason,
                movement.StocktakeExpectedQuantity,
                movement.StocktakeCountedQuantity))
            .ToArray();
    }
}

internal static class ProductUseCaseSupport
{
    public static async Task<string> ResolveSkuAsync(
        string? requestedSku,
        Guid storeId,
        ISlice1Repository repository,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedSku))
        {
            return requestedSku.Trim();
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var generatedSku = $"SP-{Guid.NewGuid():N}"[..11].ToUpperInvariant();
            if (!await repository.ProductSkuExistsAsync(
                    storeId,
                    Product.NormalizeIdentifier(generatedSku),
                    null,
                    cancellationToken))
            {
                return generatedSku;
            }
        }

        throw new ApplicationConflictException(
            "sku-generation-failed",
            "Could not generate a unique SKU. Try again.");
    }

    public static async Task EnsureIdentifiersAvailableAsync(
        ISlice1Repository repository,
        Guid storeId,
        Product product,
        Guid? excludingProductId,
        CancellationToken cancellationToken)
    {
        if (await repository.ProductSkuExistsAsync(
                storeId,
                product.NormalizedSku,
                excludingProductId,
                cancellationToken))
        {
            throw new ApplicationConflictException(
                "duplicate-sku",
                "SKU already exists in this store.");
        }

        if (product.NormalizedBarcode is not null
            && await repository.ProductBarcodeExistsAsync(
                storeId,
                product.NormalizedBarcode,
                excludingProductId,
                cancellationToken))
        {
            throw new ApplicationConflictException(
                "duplicate-barcode",
                "Barcode already exists in this store.");
        }
    }

    public static async Task<Product> GetProductAsync(
        ISlice1Repository repository,
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken) =>
        await repository.GetProductAsync(storeId, productId, cancellationToken)
        ?? throw new ApplicationNotFoundException("product-not-found", "Product was not found.");

    public static async Task<Domain.Stores.Warehouse> GetMainWarehouseAsync(
        ISlice1Repository repository,
        Guid storeId,
        CancellationToken cancellationToken) =>
        await repository.GetMainWarehouseAsync(storeId, cancellationToken)
        ?? throw new ApplicationNotFoundException(
            "main-warehouse-not-found",
            "Main warehouse was not found.");

    public static ProductResult ToResult(Product product, InventoryBalance balance) =>
        new(
            product.Id,
            product.Sku,
            product.Barcode,
            product.Name,
            product.Unit,
            product.SalePrice,
            product.ReferencePurchaseCost,
            product.IsActive,
            balance.QuantityOnHand,
            balance.InventoryValue,
            balance.AverageCost,
            balance.HasAverageCost,
            product.ReferencePurchaseCostRevision,
            product.CreatedAt,
            product.UpdatedAt);
}
