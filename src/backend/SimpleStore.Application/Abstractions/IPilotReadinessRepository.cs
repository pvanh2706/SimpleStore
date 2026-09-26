using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Application.Abstractions;

public interface IPilotReadinessRepository
{
    Task<Product?> GetProductAsync(Guid storeId, Guid productId, CancellationToken cancellationToken);
    Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken);
    Task<InventoryBalance?> GetInventoryBalanceAsync(Guid storeId, Guid warehouseId, Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, InventoryBalance>> LockInventoryBalancesAsync(Guid storeId, Guid warehouseId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken);
    Task<BusinessOperation?> GetOperationAsync(Guid storeId, Guid operationId, CancellationToken cancellationToken);
    Task<StockAdjustment?> GetStockAdjustmentAsync(Guid storeId, Guid adjustmentId, CancellationToken cancellationToken);
    Task<StocktakeResult?> GetStocktakeAsync(Guid storeId, Guid stocktakeId, CancellationToken cancellationToken);
    void AddStockAdjustment(StockAdjustment adjustment);
    void AddStocktake(StocktakeResult stocktake);
    void AddInventoryMovement(InventoryMovement movement);
    void AddBusinessOperation(BusinessOperation operation);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
