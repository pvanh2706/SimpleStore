using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Experiments;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class Slice6BRepository(ApplicationDbContext dbContext) : ISlice6BRepository
{
    public async Task<IReadOnlyList<C14AttentionCandidateData>> GetAttentionCandidatesAsync(
        Guid storeId,
        Guid warehouseId,
        DateTimeOffset velocityStartUtc,
        DateTimeOffset velocityEndUtc,
        CancellationToken cancellationToken)
    {
        var products = await (
            from product in dbContext.Products.AsNoTracking()
            where product.StoreId == storeId && product.IsActive
            join balance in dbContext.InventoryBalances.AsNoTracking()
                    .Where(item => item.StoreId == storeId && item.WarehouseId == warehouseId)
                on new { product.StoreId, ProductId = product.Id }
                equals new { balance.StoreId, balance.ProductId }
                into balances
            from balance in balances.DefaultIfEmpty()
            select new
            {
                product.Id,
                product.Name,
                product.Sku,
                product.Unit,
                product.CreatedAt,
                CurrentStock = balance == null ? 0m : balance.QuantityOnHand
            })
            .ToArrayAsync(cancellationToken);

        var sales = await (
            from line in dbContext.SaleLines.AsNoTracking()
            join sale in dbContext.Sales.AsNoTracking() on line.SaleId equals sale.Id
            where sale.StoreId == storeId
                && sale.CompletedAt >= velocityStartUtc
                && sale.CompletedAt < velocityEndUtc
            group line by line.ProductId into grouped
            select new { ProductId = grouped.Key, Quantity = grouped.Sum(line => line.Quantity) })
            .ToDictionaryAsync(item => item.ProductId, item => item.Quantity, cancellationToken);

        var returns = await (
            from line in dbContext.ReturnLines.AsNoTracking()
            join customerReturn in dbContext.Returns.AsNoTracking()
                on line.ReturnId equals customerReturn.Id
            where customerReturn.StoreId == storeId
                && customerReturn.CompletedAt >= velocityStartUtc
                && customerReturn.CompletedAt < velocityEndUtc
            group line by line.ProductId into grouped
            select new { ProductId = grouped.Key, Quantity = grouped.Sum(line => line.Quantity) })
            .ToDictionaryAsync(item => item.ProductId, item => item.Quantity, cancellationToken);

        var voids = await (
            from voided in dbContext.SaleVoids.AsNoTracking()
            join line in dbContext.SaleLines.AsNoTracking()
                on voided.OriginalSaleId equals line.SaleId
            where voided.StoreId == storeId
                && voided.VoidedAt >= velocityStartUtc
                && voided.VoidedAt < velocityEndUtc
            group line by line.ProductId into grouped
            select new { ProductId = grouped.Key, Quantity = grouped.Sum(line => line.Quantity) })
            .ToDictionaryAsync(item => item.ProductId, item => item.Quantity, cancellationToken);

        return products.Select(product => new C14AttentionCandidateData(
                product.Id,
                product.Name,
                product.Name.Trim().ToUpperInvariant(),
                product.Sku,
                product.Unit,
                product.CreatedAt,
                product.CurrentStock,
                sales.GetValueOrDefault(product.Id),
                returns.GetValueOrDefault(product.Id),
                voids.GetValueOrDefault(product.Id)))
            .ToArray();
    }

    public async Task<IReadOnlyList<C14SourceActivity>> GetAttentionEvidenceAsync(
        Guid storeId,
        Guid productId,
        DateTimeOffset velocityStartUtc,
        DateTimeOffset velocityEndUtc,
        CancellationToken cancellationToken)
    {
        var sales = await (
            from line in dbContext.SaleLines.AsNoTracking()
            join sale in dbContext.Sales.AsNoTracking() on line.SaleId equals sale.Id
            where sale.StoreId == storeId && line.ProductId == productId
                && sale.CompletedAt >= velocityStartUtc && sale.CompletedAt < velocityEndUtc
            select new C14SourceActivity(
                "Sale", sale.Id, null, sale.CompletedAt, line.ProductId, line.Quantity, "Sale", sale.Id))
            .ToArrayAsync(cancellationToken);

        var returns = await (
            from line in dbContext.ReturnLines.AsNoTracking()
            join customerReturn in dbContext.Returns.AsNoTracking()
                on line.ReturnId equals customerReturn.Id
            where customerReturn.StoreId == storeId && line.ProductId == productId
                && customerReturn.CompletedAt >= velocityStartUtc
                && customerReturn.CompletedAt < velocityEndUtc
            select new C14SourceActivity(
                "Return", customerReturn.Id, customerReturn.OriginalSaleId,
                customerReturn.CompletedAt, line.ProductId, -line.Quantity,
                "Return", customerReturn.Id))
            .ToArrayAsync(cancellationToken);

        var voids = await (
            from voided in dbContext.SaleVoids.AsNoTracking()
            join line in dbContext.SaleLines.AsNoTracking()
                on voided.OriginalSaleId equals line.SaleId
            where voided.StoreId == storeId && line.ProductId == productId
                && voided.VoidedAt >= velocityStartUtc && voided.VoidedAt < velocityEndUtc
            select new C14SourceActivity(
                "SaleVoid", voided.Id, voided.OriginalSaleId,
                voided.VoidedAt, line.ProductId, -line.Quantity,
                "Sale", voided.OriginalSaleId))
            .ToArrayAsync(cancellationToken);

        return [.. sales, .. returns, .. voids];
    }

    public Task<bool> IsActiveProductAsync(
        Guid storeId,
        Guid productId,
        CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().AnyAsync(
            product => product.StoreId == storeId
                && product.Id == productId
                && product.IsActive,
            cancellationToken);

    public Task<C14ExperimentEvent?> GetExperimentEventAsync(
        Guid eventId,
        CancellationToken cancellationToken) =>
        dbContext.C14ExperimentEvents.SingleOrDefaultAsync(
            item => item.EventId == eventId,
            cancellationToken);

    public void AddExperimentEvent(C14ExperimentEvent experimentEvent) =>
        dbContext.C14ExperimentEvents.Add(experimentEvent);

    public async Task<T> ExecuteExperimentEventTransactionAsync<T>(
        Guid eventId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            await ApplicationLock.AcquireC14ExperimentEventAsync(
                dbContext, eventId, cancellationToken);
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
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new UniqueConstraintException(
                "A unique C14 experiment event constraint was violated.",
                exception);
        }
    }
}
