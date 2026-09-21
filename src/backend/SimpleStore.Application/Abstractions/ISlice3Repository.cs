using SimpleStore.Domain.Customers;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Products;
using SimpleStore.Domain.Sales;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Application.Abstractions;

public interface ISlice3Repository
{
    Task<Store?> GetStoreAsync(Guid storeId, CancellationToken cancellationToken);
    Task<Warehouse?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken);
    Task<string> GetUserDisplayNameAsync(Guid userId, CancellationToken cancellationToken);
    Task<Customer?> GetCustomerAsync(Guid storeId, Guid customerId, CancellationToken cancellationToken);
    Task<CustomerSearchPage> SearchCustomersAsync(
        Guid storeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    void AddCustomer(Customer customer);
    Task<Sale?> GetSaleAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken);
    Task<SaleSearchPage> SearchSalesAsync(
        Guid storeId,
        int page,
        int pageSize,
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
    void AddSale(Sale sale);
    void AddInventoryMovement(InventoryMovement movement);
    void AddBusinessOperation(BusinessOperation operation);
    void AddNegativeStockSettingAudit(NegativeStockSettingAudit audit);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}

public sealed record CustomerSearchPage(IReadOnlyList<Customer> Items, int TotalCount);

public sealed record SaleSearchItem(
    Sale Sale,
    string? CustomerName,
    string CashierDisplayName);

public sealed record SaleSearchPage(IReadOnlyList<SaleSearchItem> Items, int TotalCount);
