using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class PilotReadinessRepository(ApplicationDbContext dbContext)
    : IPilotReadinessRepository
{
    public Task<Product?> GetProductAsync(Guid storeId, Guid productId, CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.Id == productId,
            cancellationToken);

    public Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken) =>
        dbContext.Warehouses.AsNoTracking().SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.IsMain,
            cancellationToken);

    public Task<InventoryBalance?> GetInventoryBalanceAsync(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        CancellationToken cancellationToken) =>
        dbContext.InventoryBalances.AsNoTracking().SingleOrDefaultAsync(
            item => item.StoreId == storeId
                && item.WarehouseId == warehouseId
                && item.ProductId == productId,
            cancellationToken);

    public Task<IReadOnlyDictionary<Guid, InventoryBalance>> LockInventoryBalancesAsync(
        Guid storeId,
        Guid warehouseId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        InventoryBalanceLock.AcquireAsync(dbContext, storeId, warehouseId, productIds, cancellationToken);

    public Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken) =>
        ApplicationLock.AcquireBusinessOperationAsync(dbContext, operationId, cancellationToken);

    public Task<BusinessOperation?> GetOperationAsync(
        Guid storeId,
        Guid operationId,
        CancellationToken cancellationToken) =>
        dbContext.BusinessOperations.AsNoTracking().SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.OperationId == operationId,
            cancellationToken);

    public Task<StockAdjustment?> GetStockAdjustmentAsync(
        Guid storeId,
        Guid adjustmentId,
        CancellationToken cancellationToken) =>
        dbContext.StockAdjustments.AsNoTracking().SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.Id == adjustmentId,
            cancellationToken);

    public Task<StocktakeResult?> GetStocktakeAsync(
        Guid storeId,
        Guid stocktakeId,
        CancellationToken cancellationToken) =>
        dbContext.StocktakeResults.AsNoTracking().SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.Id == stocktakeId,
            cancellationToken);

    public void AddStockAdjustment(StockAdjustment adjustment) => dbContext.StockAdjustments.Add(adjustment);
    public void AddStocktake(StocktakeResult stocktake) => dbContext.StocktakeResults.Add(stocktake);
    public void AddInventoryMovement(InventoryMovement movement) => dbContext.InventoryMovements.Add(movement);
    public void AddBusinessOperation(BusinessOperation operation) => dbContext.BusinessOperations.Add(operation);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
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
            throw new UniqueConstraintException("A unique database constraint was violated.", exception);
        }
    }
}
