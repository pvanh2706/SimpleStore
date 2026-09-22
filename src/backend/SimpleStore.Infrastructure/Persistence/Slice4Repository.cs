using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Corrections;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Returns;
using SimpleStore.Domain.Sales;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class Slice4Repository(ApplicationDbContext dbContext) : ISlice4Repository
{
    public Task<Sale?> GetSaleAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken) =>
        dbContext.Sales.Include(item => item.Lines).Include(item => item.Payments)
            .SingleOrDefaultAsync(item => item.StoreId == storeId && item.Id == saleId, cancellationToken);

    public async Task<IReadOnlyList<CustomerReturn>> GetReturnsForSaleAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken) =>
        await dbContext.Returns.Include(item => item.Lines).Include(item => item.RefundPayments)
            .Where(item => item.StoreId == storeId && item.OriginalSaleId == saleId)
            .OrderBy(item => item.CompletedAt).ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);

    public Task<CustomerReturn?> GetReturnAsync(Guid storeId, Guid returnId, CancellationToken cancellationToken) =>
        dbContext.Returns.Include(item => item.Lines).Include(item => item.RefundPayments)
            .SingleOrDefaultAsync(item => item.StoreId == storeId && item.Id == returnId, cancellationToken);

    public Task<SaleVoid?> GetSaleVoidAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken) =>
        dbContext.SaleVoids.SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.OriginalSaleId == saleId, cancellationToken);

    public Task<Purchase?> GetPurchaseAsync(Guid storeId, Guid purchaseId, CancellationToken cancellationToken) =>
        dbContext.Purchases.Include(item => item.Lines).Include(item => item.Payments)
            .SingleOrDefaultAsync(item => item.StoreId == storeId && item.Id == purchaseId, cancellationToken);

    public Task<PurchaseVoid?> GetPurchaseVoidAsync(Guid storeId, Guid purchaseId, CancellationToken cancellationToken) =>
        dbContext.PurchaseVoids.SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.OriginalPurchaseId == purchaseId, cancellationToken);

    public async Task<IReadOnlyList<PurchaseLineReversalBasis>> GetPurchaseReversalBasesAsync(
        Guid storeId, Guid purchaseId, CancellationToken cancellationToken) =>
        await dbContext.PurchaseLineReversalBases
            .Where(item => item.StoreId == storeId && item.PurchaseId == purchaseId)
            .ToArrayAsync(cancellationToken);

    public Task<InventoryMovement?> GetInventoryMovementAsync(Guid storeId, Guid movementId, CancellationToken cancellationToken) =>
        dbContext.InventoryMovements.SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.Id == movementId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, InventoryMovement>> GetSaleMovementsAsync(
        Guid storeId, IReadOnlyCollection<Guid> saleLineIds, CancellationToken cancellationToken) =>
        await dbContext.InventoryMovements
            .Where(item => item.StoreId == storeId
                && item.MovementType == InventoryMovementType.Sale
                && item.SourceType == "SaleLine"
                && saleLineIds.Contains(item.SourceId))
            .ToDictionaryAsync(item => item.SourceId, cancellationToken);

    public Task<bool> HasLaterInventoryMovementAsync(
        Guid storeId, Guid warehouseId, Guid productId, long sequence, CancellationToken cancellationToken) =>
        dbContext.InventoryMovements.AnyAsync(item =>
            item.StoreId == storeId && item.WarehouseId == warehouseId && item.ProductId == productId
            && item.LedgerSequence > sequence, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Product>> GetProductsForUpdateAsync(
        Guid storeId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken) =>
        await dbContext.Products.Where(item => item.StoreId == storeId && productIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, InventoryBalance>> LockInventoryBalancesAsync(
        Guid storeId, Guid warehouseId, IReadOnlyCollection<Guid> orderedProductIds, CancellationToken cancellationToken) =>
        InventoryBalanceLock.AcquireAsync(dbContext, storeId, warehouseId, orderedProductIds, cancellationToken);

    public Task<BusinessOperation?> GetOperationAsync(Guid storeId, Guid operationId, CancellationToken cancellationToken) =>
        dbContext.BusinessOperations.AsNoTracking().SingleOrDefaultAsync(
            item => item.StoreId == storeId && item.OperationId == operationId, cancellationToken);

    public Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken) =>
        ApplicationLock.AcquireBusinessOperationAsync(dbContext, operationId, cancellationToken);

    public Task AcquireSaleCorrectionLockAsync(Guid saleId, CancellationToken cancellationToken) =>
        ApplicationLock.AcquireSaleCorrectionAsync(dbContext, saleId, cancellationToken);

    public Task AcquirePurchaseCorrectionLockAsync(Guid purchaseId, CancellationToken cancellationToken) =>
        ApplicationLock.AcquirePurchaseCorrectionAsync(dbContext, purchaseId, cancellationToken);

    public void AddReturn(CustomerReturn customerReturn) => dbContext.Returns.Add(customerReturn);
    public void AddSaleVoid(SaleVoid saleVoid) => dbContext.SaleVoids.Add(saleVoid);
    public void AddPurchaseVoid(PurchaseVoid purchaseVoid) => dbContext.PurchaseVoids.Add(purchaseVoid);
    public void AddInventoryMovement(InventoryMovement movement) => dbContext.InventoryMovements.Add(movement);
    public void AddBusinessOperation(BusinessOperation operation) => dbContext.BusinessOperations.Add(operation);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
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
            throw new ApplicationConflictException("concurrent-update", "The data changed while this request was being processed. Reload and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new UniqueConstraintException("A unique database constraint was violated.", exception);
        }
    }
}
