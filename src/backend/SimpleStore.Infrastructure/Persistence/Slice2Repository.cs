using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Stores;
using SimpleStore.Domain.Suppliers;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class Slice2Repository(ApplicationDbContext dbContext) : ISlice2Repository
{
    public async Task<Guid?> GetUserStoreIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.StoreId)
            .SingleAsync(cancellationToken);

    public Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken) =>
        dbContext.Warehouses.SingleOrDefaultAsync(
            warehouse => warehouse.StoreId == storeId && warehouse.IsMain,
            cancellationToken);

    public Task<Supplier?> GetSupplierAsync(
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        dbContext.Suppliers.SingleOrDefaultAsync(
            supplier => supplier.StoreId == storeId && supplier.Id == supplierId,
            cancellationToken);

    public async Task<decimal> GetSupplierOutstandingAsync(
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        await dbContext.Purchases
            .Where(purchase => purchase.StoreId == storeId
                && purchase.SupplierId == supplierId
                && purchase.Status == PurchaseStatus.Completed)
            .SumAsync(
                purchase => purchase.TotalAmount
                    - purchase.Payments.Sum(payment => payment.Amount),
                cancellationToken);

    public async Task<SupplierSearchPage> SearchSuppliersAsync(
        Guid storeId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Suppliers.AsNoTracking().Where(supplier => supplier.StoreId == storeId);
        if (isActive.HasValue)
        {
            query = query.Where(supplier => supplier.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(supplier =>
                EF.Functions.Like(supplier.Name, pattern)
                || (supplier.Phone != null && EF.Functions.Like(supplier.Phone, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(supplier => new SupplierSearchItem(
                supplier,
                dbContext.Purchases
                    .Where(purchase => purchase.StoreId == storeId
                        && purchase.SupplierId == supplier.Id
                        && purchase.Status == PurchaseStatus.Completed)
                    .Sum(purchase => purchase.TotalAmount
                        - purchase.Payments.Sum(payment => payment.Amount))))
            .ToArrayAsync(cancellationToken);
        return new SupplierSearchPage(items, totalCount);
    }

    public void AddSupplier(Supplier supplier) => dbContext.Suppliers.Add(supplier);

    public Task<Purchase?> GetPurchaseAsync(
        Guid storeId,
        Guid purchaseId,
        CancellationToken cancellationToken) =>
        dbContext.Purchases
            .Include(purchase => purchase.Lines)
            .Include(purchase => purchase.Payments)
            .SingleOrDefaultAsync(
                purchase => purchase.StoreId == storeId && purchase.Id == purchaseId,
                cancellationToken);

    public async Task<PurchaseSearchPage> SearchPurchasesAsync(
        Guid storeId,
        PurchaseStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Purchases.AsNoTracking().Where(purchase => purchase.StoreId == storeId);
        if (status.HasValue)
        {
            query = query.Where(purchase => purchase.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await (
                from purchase in query
                join supplier in dbContext.Suppliers.AsNoTracking()
                    on new { purchase.StoreId, purchase.SupplierId }
                    equals new { supplier.StoreId, SupplierId = supplier.Id }
                orderby purchase.CreatedAt descending, purchase.Id descending
                select new PurchaseSearchItem(
                    purchase,
                    supplier.Name,
                    purchase.Payments.Sum(payment => payment.Amount)))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new PurchaseSearchPage(items, totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, Product>> GetProductsAsync(
        Guid storeId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .Where(product => product.StoreId == storeId && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Product>> GetProductsForUpdateAsync(
        Guid storeId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        await dbContext.Products
            .Where(product => product.StoreId == storeId && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, InventoryBalance>> LockInventoryBalancesAsync(
        Guid storeId,
        Guid warehouseId,
        IReadOnlyCollection<Guid> orderedProductIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, InventoryBalance>();
        foreach (var productId in orderedProductIds)
        {
            var balance = await dbContext.InventoryBalances
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM [InventoryBalances] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [StoreId] = {storeId}
                      AND [WarehouseId] = {warehouseId}
                      AND [ProductId] = {productId}
                    """)
                .SingleOrDefaultAsync(cancellationToken);
            if (balance is not null)
            {
                result.Add(productId, balance);
            }
        }

        return result;
    }

    public async Task AcquireOperationLockAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException("An active database transaction is required.");
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            SELECT @result;
            """;
        command.Parameters.Add(new SqlParameter(
            "@resource",
            SqlDbType.NVarChar,
            255)
        {
            Value = $"SimpleStore:CompletePurchase:{operationId:N}"
        });
        var result = Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken),
            System.Globalization.CultureInfo.InvariantCulture);
        if (result < 0)
        {
            throw new ApplicationConflictException(
                "operation-lock-timeout",
                "The operation is already being processed. Check its status and retry.");
        }
    }

    public Task<BusinessOperation?> GetOperationAsync(
        Guid storeId,
        Guid operationId,
        CancellationToken cancellationToken) =>
        dbContext.BusinessOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                operation => operation.StoreId == storeId
                    && operation.OperationId == operationId,
                cancellationToken);

    public void AddPurchase(Purchase purchase) => dbContext.Purchases.Add(purchase);

    public void AddPurchaseLine(PurchaseLine line) => dbContext.PurchaseLines.Add(line);

    public void RemovePurchaseLines(IReadOnlyCollection<PurchaseLine> lines) =>
        dbContext.PurchaseLines.RemoveRange(lines);

    public void AddPurchasePayment(PurchasePayment payment) =>
        dbContext.PurchasePayments.Add(payment);

    public void AddInventoryMovement(InventoryMovement movement) =>
        dbContext.InventoryMovements.Add(movement);

    public void AddBusinessOperation(BusinessOperation operation) =>
        dbContext.BusinessOperations.Add(operation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        SaveChangesCoreAsync(cancellationToken);

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
