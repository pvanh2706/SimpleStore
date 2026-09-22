using SimpleStore.Domain.Corrections;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Returns;
using SimpleStore.Domain.Sales;

namespace SimpleStore.Application.Abstractions;

public interface ISlice4Repository
{
    Task<Sale?> GetSaleAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerReturn>> GetReturnsForSaleAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken);
    Task<CustomerReturn?> GetReturnAsync(Guid storeId, Guid returnId, CancellationToken cancellationToken);
    Task<SaleVoid?> GetSaleVoidAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken);
    Task<Purchase?> GetPurchaseAsync(Guid storeId, Guid purchaseId, CancellationToken cancellationToken);
    Task<PurchaseVoid?> GetPurchaseVoidAsync(Guid storeId, Guid purchaseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PurchaseLineReversalBasis>> GetPurchaseReversalBasesAsync(Guid storeId, Guid purchaseId, CancellationToken cancellationToken);
    Task<InventoryMovement?> GetInventoryMovementAsync(Guid storeId, Guid movementId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, InventoryMovement>> GetSaleMovementsAsync(Guid storeId, IReadOnlyCollection<Guid> saleLineIds, CancellationToken cancellationToken);
    Task<bool> HasLaterInventoryMovementAsync(Guid storeId, Guid warehouseId, Guid productId, long sequence, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, Product>> GetProductsForUpdateAsync(Guid storeId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, InventoryBalance>> LockInventoryBalancesAsync(Guid storeId, Guid warehouseId, IReadOnlyCollection<Guid> orderedProductIds, CancellationToken cancellationToken);
    Task<BusinessOperation?> GetOperationAsync(Guid storeId, Guid operationId, CancellationToken cancellationToken);
    Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken);
    Task AcquireSaleCorrectionLockAsync(Guid saleId, CancellationToken cancellationToken);
    Task AcquirePurchaseCorrectionLockAsync(Guid purchaseId, CancellationToken cancellationToken);
    void AddReturn(CustomerReturn customerReturn);
    void AddSaleVoid(SaleVoid saleVoid);
    void AddPurchaseVoid(PurchaseVoid purchaseVoid);
    void AddInventoryMovement(InventoryMovement movement);
    void AddBusinessOperation(BusinessOperation operation);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
