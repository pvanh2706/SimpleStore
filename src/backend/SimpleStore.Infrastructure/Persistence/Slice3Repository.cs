using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Sales;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class Slice3Repository(ApplicationDbContext dbContext) : ISlice3Repository
{
    public Task<Store?> GetStoreAsync(Guid storeId, CancellationToken cancellationToken) =>
        dbContext.Stores.SingleOrDefaultAsync(store => store.Id == storeId, cancellationToken);

    public Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken) =>
        dbContext.Warehouses.SingleOrDefaultAsync(
            warehouse => warehouse.StoreId == storeId && warehouse.IsMain,
            cancellationToken);

    public async Task<string> GetUserDisplayNameAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.Email ?? user.UserName ?? user.Id.ToString())
            .SingleAsync(cancellationToken);

    public Task<Customer?> GetCustomerAsync(
        Guid storeId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        dbContext.Customers.SingleOrDefaultAsync(
            customer => customer.StoreId == storeId && customer.Id == customerId,
            cancellationToken);

    public async Task<CustomerSearchPage> SearchCustomersAsync(
        Guid storeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Customers.AsNoTracking().Where(customer => customer.StoreId == storeId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(customer =>
                EF.Functions.Like(customer.Name, pattern)
                || (customer.Phone != null && EF.Functions.Like(customer.Phone, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(customer => customer.Name)
            .ThenBy(customer => customer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return new CustomerSearchPage(items, totalCount);
    }

    public void AddCustomer(Customer customer) => dbContext.Customers.Add(customer);

    public Task<Sale?> GetSaleAsync(
        Guid storeId,
        Guid saleId,
        CancellationToken cancellationToken) =>
        dbContext.Sales
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .SingleOrDefaultAsync(
                sale => sale.StoreId == storeId && sale.Id == saleId,
                cancellationToken);

    public async Task<SaleSearchPage> SearchSalesAsync(
        Guid storeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Sales.AsNoTracking().Where(sale => sale.StoreId == storeId);
        var totalCount = await query.CountAsync(cancellationToken);
        var sales = await query
            .Include(sale => sale.Payments)
            .OrderByDescending(sale => sale.CompletedAt)
            .ThenByDescending(sale => sale.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        var customerIds = sales.Where(sale => sale.CustomerId.HasValue)
            .Select(sale => sale.CustomerId!.Value)
            .Distinct()
            .ToArray();
        var customerNames = await dbContext.Customers.AsNoTracking()
            .Where(customer => customer.StoreId == storeId && customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken);
        var userIds = sales.Select(sale => sale.CompletedByUserId).Distinct().ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.Email ?? user.UserName ?? user.Id.ToString(),
                cancellationToken);
        var saleIds = sales.Select(sale => sale.Id).ToArray();
        var returnAmounts = await dbContext.Returns.AsNoTracking()
            .Where(item => item.StoreId == storeId && saleIds.Contains(item.OriginalSaleId))
            .GroupBy(item => item.OriginalSaleId)
            .Select(group => new
            {
                SaleId = group.Key,
                Returned = group.Sum(item => item.TotalReturnAmount),
                Refunded = group.SelectMany(item => item.RefundPayments).Sum(payment => payment.Amount)
            })
            .ToDictionaryAsync(item => item.SaleId, cancellationToken);
        var voidedSaleIds = await dbContext.SaleVoids.AsNoTracking()
            .Where(item => item.StoreId == storeId && saleIds.Contains(item.OriginalSaleId))
            .Select(item => item.OriginalSaleId)
            .ToHashSetAsync(cancellationToken);
        return new SaleSearchPage(
            sales.Select(sale => new SaleSearchItem(
                sale,
                sale.CustomerId.HasValue ? customerNames[sale.CustomerId.Value] : null,
                users[sale.CompletedByUserId],
                returnAmounts.TryGetValue(sale.Id, out var amounts) ? amounts.Returned : 0,
                returnAmounts.TryGetValue(sale.Id, out amounts) ? amounts.Refunded : 0,
                voidedSaleIds.Contains(sale.Id))).ToArray(),
            totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, Product>> GetProductsForUpdateAsync(
        Guid storeId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        await dbContext.Products
            .Where(product => product.StoreId == storeId && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, InventoryBalance>> LockInventoryBalancesAsync(
        Guid storeId,
        Guid warehouseId,
        IReadOnlyCollection<Guid> orderedProductIds,
        CancellationToken cancellationToken) =>
        InventoryBalanceLock.AcquireAsync(
            dbContext,
            storeId,
            warehouseId,
            orderedProductIds,
            cancellationToken);

    public Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken) =>
        ApplicationLock.AcquireBusinessOperationAsync(dbContext, operationId, cancellationToken);

    public Task<BusinessOperation?> GetOperationAsync(
        Guid storeId,
        Guid operationId,
        CancellationToken cancellationToken) =>
        dbContext.BusinessOperations.AsNoTracking().SingleOrDefaultAsync(
            operation => operation.StoreId == storeId && operation.OperationId == operationId,
            cancellationToken);

    public void AddSale(Sale sale) => dbContext.Sales.Add(sale);
    public void AddInventoryMovement(InventoryMovement movement) =>
        dbContext.InventoryMovements.Add(movement);
    public void AddBusinessOperation(BusinessOperation operation) =>
        dbContext.BusinessOperations.Add(operation);
    public void AddNegativeStockSettingAudit(NegativeStockSettingAudit audit) =>
        dbContext.NegativeStockSettingAudits.Add(audit);
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
            throw new UniqueConstraintException("A unique database constraint was violated.", exception);
        }
    }
}
