using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.ProductImports;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;

namespace SimpleStore.Application.ProductImports;

public sealed class ValidateProductImportUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository,
    TimeProvider timeProvider)
{
    public async Task<ProductImportValidationResult> ExecuteAsync(
        byte[] fileContent,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);
        var parsedImport = CsvProductImportParser.Parse(fileContent);
        var errors = parsedImport.Errors.ToList();

        if (parsedImport.Rows.Count > 0)
        {
            var normalizedSkus = parsedImport.Rows
                .Select(row => Product.NormalizeIdentifier(row.Sku))
                .ToHashSet(StringComparer.Ordinal);
            var normalizedBarcodes = parsedImport.Rows
                .Where(row => row.Barcode is not null)
                .Select(row => Product.NormalizeIdentifier(row.Barcode!))
                .ToHashSet(StringComparer.Ordinal);
            var existing = await repository.GetExistingProductIdentifiersAsync(
                storeId,
                normalizedSkus,
                normalizedBarcodes,
                cancellationToken);

            foreach (var row in parsedImport.Rows)
            {
                if (existing.NormalizedSkus.Contains(Product.NormalizeIdentifier(row.Sku)))
                {
                    errors.Add(new ValidationError(
                        row.RowNumber,
                        "SKU",
                        "duplicate-sku",
                        "SKU already exists in this store."));
                }

                if (row.Barcode is not null
                    && existing.NormalizedBarcodes.Contains(Product.NormalizeIdentifier(row.Barcode)))
                {
                    errors.Add(new ValidationError(
                        row.RowNumber,
                        "Barcode",
                        "duplicate-barcode",
                        "Barcode already exists in this store."));
                }
            }
        }

        var previewRows = parsedImport.Rows.Select(ToPreviewRow).ToArray();
        if (errors.Count > 0)
        {
            return new ProductImportValidationResult(null, false, previewRows, errors);
        }

        var importRows = parsedImport.Rows
            .Select(row => ProductImportRow.Create(
                row.RowNumber,
                row.Sku,
                row.Barcode,
                row.Name,
                row.Unit,
                row.SalePrice,
                row.OpeningCost,
                row.OpeningQuantity))
            .ToArray();
        var productImport = ProductImport.CreateValidated(
            storeId,
            userId,
            timeProvider.GetUtcNow(),
            importRows);

        repository.AddProductImport(productImport);
        await repository.SaveChangesAsync(cancellationToken);

        return new ProductImportValidationResult(productImport.Id, true, previewRows, []);
    }

    private static ProductImportPreviewRow ToPreviewRow(ParsedImportRow row) =>
        new(
            row.RowNumber,
            row.Sku,
            row.Barcode,
            row.Name,
            row.Unit,
            row.SalePrice,
            row.OpeningCost,
            row.OpeningQuantity);
}

public sealed class ConfirmProductImportUseCase(
    ICurrentUser currentUser,
    ISlice1Repository repository,
    TimeProvider timeProvider)
{
    public async Task<ProductImportConfirmResult> ExecuteAsync(
        Guid importId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser,
            repository,
            cancellationToken);

        return await repository.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var productImport = await repository.GetProductImportAsync(
                    storeId,
                    importId,
                    transactionCancellationToken)
                    ?? throw new ApplicationNotFoundException(
                        "product-import-not-found",
                        "Validated product import was not found.");

                if (productImport.Status == ProductImportStatus.Completed)
                {
                    return new ProductImportConfirmResult(
                        productImport.Id,
                        productImport.ImportedProductCount,
                        true);
                }

                var normalizedSkus = productImport.Rows
                    .Select(row => Product.NormalizeIdentifier(row.Sku))
                    .ToHashSet(StringComparer.Ordinal);
                var normalizedBarcodes = productImport.Rows
                    .Where(row => row.Barcode is not null)
                    .Select(row => Product.NormalizeIdentifier(row.Barcode!))
                    .ToHashSet(StringComparer.Ordinal);
                var existing = await repository.GetExistingProductIdentifiersAsync(
                    storeId,
                    normalizedSkus,
                    normalizedBarcodes,
                    transactionCancellationToken);
                var errors = BuildExistingIdentifierErrors(productImport, existing);
                if (errors.Count > 0)
                {
                    throw new ApplicationValidationException(
                        "import-no-longer-valid",
                        "Import data conflicts with products created after preview.",
                        errors);
                }

                var warehouse = await repository.GetMainWarehouseAsync(
                    storeId,
                    transactionCancellationToken)
                    ?? throw new ApplicationNotFoundException(
                        "main-warehouse-not-found",
                        "Main warehouse was not found.");
                var now = timeProvider.GetUtcNow();

                foreach (var row in productImport.Rows.OrderBy(row => row.RowNumber))
                {
                    var openingInventory = OpeningInventory.Create(
                        row.OpeningQuantity,
                        row.OpeningCost);
                    var product = Product.Create(
                        storeId,
                        row.Sku,
                        row.Barcode,
                        row.Name,
                        row.Unit,
                        row.SalePrice,
                        row.OpeningCost,
                        now);
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
                            "ProductImport",
                            productImport.Id,
                            userId,
                            now));
                    }
                }

                productImport.MarkCompleted(productImport.Rows.Count, now);
                return new ProductImportConfirmResult(
                    productImport.Id,
                    productImport.Rows.Count,
                    false);
            },
            cancellationToken);
    }

    private static List<ValidationError> BuildExistingIdentifierErrors(
        ProductImport productImport,
        ExistingProductIdentifiers existing)
    {
        var errors = new List<ValidationError>();
        foreach (var row in productImport.Rows)
        {
            if (existing.NormalizedSkus.Contains(Product.NormalizeIdentifier(row.Sku)))
            {
                errors.Add(new ValidationError(
                    row.RowNumber,
                    "SKU",
                    "duplicate-sku",
                    "SKU already exists in this store."));
            }

            if (row.Barcode is not null
                && existing.NormalizedBarcodes.Contains(Product.NormalizeIdentifier(row.Barcode)))
            {
                errors.Add(new ValidationError(
                    row.RowNumber,
                    "Barcode",
                    "duplicate-barcode",
                    "Barcode already exists in this store."));
            }
        }

        return errors;
    }
}
