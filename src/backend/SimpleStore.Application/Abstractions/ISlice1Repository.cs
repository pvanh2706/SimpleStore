using SimpleStore.Domain.ProductImports;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Application.Abstractions;

public interface ISlice1Repository
{
    Task<Guid?> GetUserStoreIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AssignUserToStoreAsync(Guid userId, Guid storeId, CancellationToken cancellationToken);

    Task<Store?> GetStoreAsync(Guid storeId, CancellationToken cancellationToken);

    Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken);

    void AddStore(Store store);

    void AddWarehouse(Warehouse warehouse);

    Task<bool> ProductSkuExistsAsync(
        Guid storeId,
        string normalizedSku,
        Guid? excludingProductId,
        CancellationToken cancellationToken);

    Task<bool> ProductBarcodeExistsAsync(
        Guid storeId,
        string normalizedBarcode,
        Guid? excludingProductId,
        CancellationToken cancellationToken);

    Task<Product?> GetProductAsync(Guid storeId, Guid productId, CancellationToken cancellationToken);

    Task<ProductSearchPage> SearchProductsAsync(
        Guid storeId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void AddProduct(Product product);

    void AddInventoryBalance(InventoryBalance balance);

    void AddInventoryMovement(InventoryMovement movement);

    Task<InventoryBalance?> GetInventoryBalanceAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryMovement>> GetInventoryMovementsAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<ExistingProductIdentifiers> GetExistingProductIdentifiersAsync(
        Guid storeId,
        IReadOnlyCollection<string> normalizedSkus,
        IReadOnlyCollection<string> normalizedBarcodes,
        CancellationToken cancellationToken);

    void AddProductImport(ProductImport productImport);

    Task<ProductImport?> GetProductImportAsync(
        Guid storeId,
        Guid importId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}

public sealed record ProductSearchItem(Product Product, decimal QuantityOnHand);

public sealed record ProductSearchPage(
    IReadOnlyList<ProductSearchItem> Items,
    int TotalCount);

public sealed record ExistingProductIdentifiers(
    IReadOnlySet<string> NormalizedSkus,
    IReadOnlySet<string> NormalizedBarcodes);
