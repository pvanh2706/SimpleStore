using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.ProductImports;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class Slice1Repository(ApplicationDbContext dbContext) : ISlice1Repository
{
    public async Task<Guid?> GetUserStoreIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.StoreId)
            .SingleAsync(cancellationToken);

    public async Task AssignUserToStoreAsync(
        Guid userId,
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleAsync(user => user.Id == userId, cancellationToken);
        user.AssignToStore(storeId);
    }

    public Task<Store?> GetStoreAsync(Guid storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.SingleOrDefaultAsync(store => store.Id == storeId, cancellationToken);

    public Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken) =>
        dbContext.Warehouses.SingleOrDefaultAsync(
            warehouse => warehouse.StoreId == storeId && warehouse.IsMain,
            cancellationToken);

    public void AddStore(Store store) => dbContext.Stores.Add(store);

    public void AddWarehouse(Warehouse warehouse) => dbContext.Warehouses.Add(warehouse);

    public Task<bool> ProductSkuExistsAsync(
        Guid storeId,
        string normalizedSku,
        Guid? excludingProductId,
        CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(
            product => product.StoreId == storeId
                && product.NormalizedSku == normalizedSku
                && (!excludingProductId.HasValue || product.Id != excludingProductId.Value),
            cancellationToken);

    public Task<bool> ProductBarcodeExistsAsync(
        Guid storeId,
        string normalizedBarcode,
        Guid? excludingProductId,
        CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(
            product => product.StoreId == storeId
                && product.NormalizedBarcode == normalizedBarcode
                && (!excludingProductId.HasValue || product.Id != excludingProductId.Value),
            cancellationToken);

    public Task<Product?> GetProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken) =>
        dbContext.Products.SingleOrDefaultAsync(
            product => product.StoreId == storeId && product.Id == productId,
            cancellationToken);

    public async Task<ProductSearchPage> SearchProductsAsync(
        Guid storeId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(product => product.StoreId == storeId);

        if (isActive.HasValue)
        {
            query = query.Where(product => product.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(product =>
                EF.Functions.Like(product.Name, pattern)
                || EF.Functions.Like(product.Sku, pattern)
                || (product.Barcode != null && EF.Functions.Like(product.Barcode, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await (
                from product in query
                join balance in dbContext.InventoryBalances.AsNoTracking()
                    on new { product.StoreId, ProductId = product.Id }
                    equals new { balance.StoreId, balance.ProductId }
                orderby product.Name, product.Id
                select new ProductSearchItem(product, balance.QuantityOnHand))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new ProductSearchPage(items, totalCount);
    }

    public void AddProduct(Product product) => dbContext.Products.Add(product);

    public void AddInventoryBalance(InventoryBalance balance) =>
        dbContext.InventoryBalances.Add(balance);

    public void AddInventoryMovement(InventoryMovement movement) =>
        dbContext.InventoryMovements.Add(movement);

    public Task<InventoryBalance?> GetInventoryBalanceAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken) =>
        dbContext.InventoryBalances
            .AsNoTracking()
            .SingleOrDefaultAsync(
                balance => balance.StoreId == storeId && balance.ProductId == productId,
                cancellationToken);

    public async Task<IReadOnlyList<InventoryMovement>> GetInventoryMovementsAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken) =>
        await dbContext.InventoryMovements
            .AsNoTracking()
            .Where(movement => movement.StoreId == storeId && movement.ProductId == productId)
            .OrderByDescending(movement => movement.OccurredAt)
            .ThenByDescending(movement => movement.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<ExistingProductIdentifiers> GetExistingProductIdentifiersAsync(
        Guid storeId,
        IReadOnlyCollection<string> normalizedSkus,
        IReadOnlyCollection<string> normalizedBarcodes,
        CancellationToken cancellationToken)
    {
        var skus = normalizedSkus.Count == 0
            ? []
            : await dbContext.Products
                .Where(product => product.StoreId == storeId
                    && normalizedSkus.Contains(product.NormalizedSku))
                .Select(product => product.NormalizedSku)
                .ToArrayAsync(cancellationToken);
        var barcodes = normalizedBarcodes.Count == 0
            ? []
            : await dbContext.Products
                .Where(product => product.StoreId == storeId
                    && product.NormalizedBarcode != null
                    && normalizedBarcodes.Contains(product.NormalizedBarcode))
                .Select(product => product.NormalizedBarcode!)
                .ToArrayAsync(cancellationToken);

        return new ExistingProductIdentifiers(
            skus.ToHashSet(StringComparer.Ordinal),
            barcodes.ToHashSet(StringComparer.Ordinal));
    }

    public void AddProductImport(ProductImport productImport) =>
        dbContext.ProductImports.Add(productImport);

    public Task<ProductImport?> GetProductImportAsync(
        Guid storeId,
        Guid importId,
        CancellationToken cancellationToken) =>
        dbContext.ProductImports
            .Include(productImport => productImport.Rows)
            .SingleOrDefaultAsync(
                productImport => productImport.StoreId == storeId && productImport.Id == importId,
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        SaveChangesCoreAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesCoreAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task SaveChangesCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApplicationConflictException(
                "concurrent-update",
                "The data changed while this request was being processed. Reload and try again.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new UniqueConstraintException(
                "A unique database constraint was violated.",
                exception);
        }
    }
}
