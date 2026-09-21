using Microsoft.EntityFrameworkCore;
using SimpleStore.Domain.Inventory;

namespace SimpleStore.Infrastructure.Persistence;

internal static class InventoryBalanceLock
{
    public static async Task<IReadOnlyDictionary<Guid, InventoryBalance>> AcquireAsync(
        ApplicationDbContext dbContext,
        Guid storeId,
        Guid warehouseId,
        IReadOnlyCollection<Guid> orderedProductIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, InventoryBalance>();
        foreach (var productId in orderedProductIds.OrderBy(id => id))
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
}
