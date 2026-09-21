using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Stores;
using SimpleStore.Domain.Suppliers;

namespace SimpleStore.Application.Abstractions;

public interface ISlice2Repository
{
    Task<Guid?> GetUserStoreIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken);

    Task<Supplier?> GetSupplierAsync(Guid storeId, Guid supplierId, CancellationToken cancellationToken);

    Task<decimal> GetSupplierOutstandingAsync(
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierSearchPage> SearchSuppliersAsync(
        Guid storeId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void AddSupplier(Supplier supplier);

    Task<Purchase?> GetPurchaseAsync(Guid storeId, Guid purchaseId, CancellationToken cancellationToken);

    Task<PurchaseSearchPage> SearchPurchasesAsync(
        Guid storeId,
        PurchaseStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Product>> GetProductsAsync(
        Guid storeId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Product>> GetProductsForUpdateAsync(
        Guid storeId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, InventoryBalance>> LockInventoryBalancesAsync(
        Guid storeId,
        Guid warehouseId,
        IReadOnlyCollection<Guid> orderedProductIds,
        CancellationToken cancellationToken);

    Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken);

    Task<BusinessOperation?> GetOperationAsync(
        Guid storeId,
        Guid operationId,
        CancellationToken cancellationToken);

    void AddPurchase(Purchase purchase);

    void AddPurchaseLine(PurchaseLine line);

    void RemovePurchaseLines(IReadOnlyCollection<PurchaseLine> lines);

    void AddPurchasePayment(PurchasePayment payment);

    void AddInventoryMovement(InventoryMovement movement);

    void AddBusinessOperation(BusinessOperation operation);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}

public sealed record SupplierSearchItem(Supplier Supplier, decimal OutstandingAmount);

public sealed record SupplierSearchPage(IReadOnlyList<SupplierSearchItem> Items, int TotalCount);

public sealed record PurchaseSearchItem(
    Purchase Purchase,
    string SupplierName,
    decimal PaidAmount);

public sealed record PurchaseSearchPage(IReadOnlyList<PurchaseSearchItem> Items, int TotalCount);
