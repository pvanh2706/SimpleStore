using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Domain.Debts;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class Slice5Repository(ApplicationDbContext dbContext) : ISlice5Repository
{
    public async Task<DebtPartyBalance?> GetCustomerDebtAsync(
        Guid storeId,
        Guid customerId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.AsNoTracking()
            .Where(item => item.StoreId == storeId && item.Id == customerId)
            .Select(item => new { item.Id, item.Name })
            .SingleOrDefaultAsync(cancellationToken);
        return customer is null
            ? null
            : new DebtPartyBalance(
                customer.Id,
                customer.Name,
                await GetCustomerOutstandingAsync(storeId, customer.Id, asOf, cancellationToken));
    }

    public async Task<DebtPartyBalance?> GetSupplierDebtAsync(
        Guid storeId,
        Guid supplierId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.AsNoTracking()
            .Where(item => item.StoreId == storeId && item.Id == supplierId)
            .Select(item => new { item.Id, item.Name })
            .SingleOrDefaultAsync(cancellationToken);
        return supplier is null
            ? null
            : new DebtPartyBalance(
                supplier.Id,
                supplier.Name,
                await GetSupplierOutstandingAsync(storeId, supplier.Id, asOf, cancellationToken));
    }

    public async Task<DebtPartyBalancePage> SearchCustomerDebtsAsync(
        Guid storeId,
        string? search,
        DateTimeOffset asOf,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Customers.AsNoTracking().Where(item => item.StoreId == storeId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(item => EF.Functions.Like(item.Name, pattern)
                || (item.Phone != null && EF.Functions.Like(item.Phone, pattern)));
        }

        var parties = await query.OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToArrayAsync(cancellationToken);
        var partyIds = parties.Select(item => item.Id).ToArray();
        var activeSales = await dbContext.Sales.AsNoTracking()
            .Where(sale => sale.StoreId == storeId
                && sale.CustomerId.HasValue
                && partyIds.Contains(sale.CustomerId.Value)
                && sale.CompletedAt < asOf
                && !dbContext.SaleVoids.Any(voided =>
                    voided.StoreId == storeId
                    && voided.OriginalSaleId == sale.Id
                    && voided.VoidedAt < asOf))
            .Select(sale => new { sale.Id, CustomerId = sale.CustomerId!.Value, sale.TotalAmount })
            .ToArrayAsync(cancellationToken);
        var saleIds = activeSales.Select(item => item.Id).ToArray();
        var salePayments = await dbContext.SalePayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && saleIds.Contains(payment.SaleId)
                && payment.OccurredAt < asOf)
            .GroupBy(payment => payment.SaleId)
            .Select(group => new { SaleId = group.Key, Amount = group.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.SaleId, item => item.Amount, cancellationToken);
        var returns = await dbContext.Returns.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && saleIds.Contains(item.OriginalSaleId)
                && item.CompletedAt < asOf)
            .Select(item => new { item.Id, item.OriginalSaleId, item.TotalReturnAmount })
            .ToArrayAsync(cancellationToken);
        var returnIds = returns.Select(item => item.Id).ToArray();
        var refunds = await dbContext.ReturnRefundPayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && returnIds.Contains(payment.ReturnId)
                && payment.OccurredAt < asOf)
            .GroupBy(payment => payment.ReturnId)
            .Select(group => new { ReturnId = group.Key, Amount = group.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.ReturnId, item => item.Amount, cancellationToken);
        var debtPayments = await dbContext.DebtPayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && payment.CustomerId.HasValue
                && partyIds.Contains(payment.CustomerId.Value)
                && payment.Purpose == DebtPaymentPurpose.CustomerDebtCollection
                && payment.OccurredAt < asOf)
            .GroupBy(payment => payment.CustomerId!.Value)
            .Select(group => new { CustomerId = group.Key, Amount = group.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.CustomerId, item => item.Amount, cancellationToken);
        var returnsBySale = returns.GroupBy(item => item.OriginalSaleId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Returned = group.Sum(item => item.TotalReturnAmount),
                    Refunded = group.Sum(item => refunds.GetValueOrDefault(item.Id))
                });
        var saleDebtByCustomer = activeSales.GroupBy(item => item.CustomerId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item =>
                {
                    var correction = returnsBySale.GetValueOrDefault(item.Id);
                    return item.TotalAmount
                        - salePayments.GetValueOrDefault(item.Id)
                        - (correction?.Returned ?? 0)
                        + (correction?.Refunded ?? 0);
                }));
        var balances = parties.Select(party => new DebtPartyBalance(
            party.Id,
            party.Name,
            saleDebtByCustomer.GetValueOrDefault(party.Id)
                - debtPayments.GetValueOrDefault(party.Id))).ToArray();
        return Page(balances, page, pageSize, "customer-debt-state-invalid");
    }

    public async Task<DebtPartyBalancePage> SearchSupplierDebtsAsync(
        Guid storeId,
        string? search,
        DateTimeOffset asOf,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Suppliers.AsNoTracking().Where(item => item.StoreId == storeId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(item => EF.Functions.Like(item.Name, pattern)
                || (item.Phone != null && EF.Functions.Like(item.Phone, pattern)));
        }

        var parties = await query.OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToArrayAsync(cancellationToken);
        var partyIds = parties.Select(item => item.Id).ToArray();
        var activePurchases = await dbContext.Purchases.AsNoTracking()
            .Where(purchase => purchase.StoreId == storeId
                && partyIds.Contains(purchase.SupplierId)
                && purchase.Status == PurchaseStatus.Completed
                && purchase.CompletedAt < asOf
                && !dbContext.PurchaseVoids.Any(voided =>
                    voided.StoreId == storeId
                    && voided.OriginalPurchaseId == purchase.Id
                    && voided.VoidedAt < asOf))
            .Select(purchase => new { purchase.Id, purchase.SupplierId, purchase.TotalAmount })
            .ToArrayAsync(cancellationToken);
        var purchaseIds = activePurchases.Select(item => item.Id).ToArray();
        var purchasePayments = await dbContext.PurchasePayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && purchaseIds.Contains(payment.PurchaseId)
                && payment.PaidAt < asOf)
            .GroupBy(payment => payment.PurchaseId)
            .Select(group => new { PurchaseId = group.Key, Amount = group.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.PurchaseId, item => item.Amount, cancellationToken);
        var debtPayments = await dbContext.DebtPayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && payment.SupplierId.HasValue
                && partyIds.Contains(payment.SupplierId.Value)
                && payment.Purpose == DebtPaymentPurpose.SupplierDebtSettlement
                && payment.OccurredAt < asOf)
            .GroupBy(payment => payment.SupplierId!.Value)
            .Select(group => new { SupplierId = group.Key, Amount = group.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.SupplierId, item => item.Amount, cancellationToken);
        var purchaseDebtBySupplier = activePurchases.GroupBy(item => item.SupplierId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.TotalAmount - purchasePayments.GetValueOrDefault(item.Id)));
        var balances = parties.Select(party => new DebtPartyBalance(
            party.Id,
            party.Name,
            purchaseDebtBySupplier.GetValueOrDefault(party.Id)
                - debtPayments.GetValueOrDefault(party.Id))).ToArray();
        return Page(balances, page, pageSize, "supplier-debt-state-invalid");
    }

    public Task<DebtPayment?> GetDebtPaymentAsync(
        Guid storeId,
        Guid debtPaymentId,
        CancellationToken cancellationToken) =>
        dbContext.DebtPayments.AsNoTracking().SingleOrDefaultAsync(
            payment => payment.StoreId == storeId && payment.Id == debtPaymentId,
            cancellationToken);

    public Task<BusinessOperation?> GetOperationAsync(
        Guid storeId,
        Guid operationId,
        CancellationToken cancellationToken) =>
        dbContext.BusinessOperations.AsNoTracking().SingleOrDefaultAsync(
            operation => operation.StoreId == storeId && operation.OperationId == operationId,
            cancellationToken);

    public Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken) =>
        ApplicationLock.AcquireBusinessOperationAsync(dbContext, operationId, cancellationToken);

    public Task AcquireCustomerDebtLockAsync(
        Guid storeId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        ApplicationLock.AcquireCustomerDebtAsync(dbContext, storeId, customerId, cancellationToken);

    public Task AcquireSupplierDebtLockAsync(
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        ApplicationLock.AcquireSupplierDebtAsync(dbContext, storeId, supplierId, cancellationToken);

    public void AddDebtPayment(DebtPayment payment) => dbContext.DebtPayments.Add(payment);
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

    private async Task<decimal> GetCustomerOutstandingAsync(
        Guid storeId,
        Guid customerId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var activeSales = await dbContext.Sales.AsNoTracking()
            .Where(sale => sale.StoreId == storeId
                && sale.CustomerId == customerId
                && sale.CompletedAt < asOf
                && !dbContext.SaleVoids.Any(voided =>
                    voided.StoreId == storeId
                    && voided.OriginalSaleId == sale.Id
                    && voided.VoidedAt < asOf))
            .Select(sale => new { sale.Id, sale.TotalAmount })
            .ToArrayAsync(cancellationToken);
        var saleIds = activeSales.Select(item => item.Id).ToArray();
        var salePayments = await dbContext.SalePayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && saleIds.Contains(payment.SaleId)
                && payment.OccurredAt < asOf)
            .SumAsync(payment => payment.Amount, cancellationToken);
        var returns = await dbContext.Returns.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && saleIds.Contains(item.OriginalSaleId)
                && item.CompletedAt < asOf)
            .Select(item => new { item.Id, item.TotalReturnAmount })
            .ToArrayAsync(cancellationToken);
        var returnIds = returns.Select(item => item.Id).ToArray();
        var refunds = await dbContext.ReturnRefundPayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && returnIds.Contains(payment.ReturnId)
                && payment.OccurredAt < asOf)
            .SumAsync(payment => payment.Amount, cancellationToken);
        var debtPayments = await dbContext.DebtPayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && payment.CustomerId == customerId
                && payment.Purpose == DebtPaymentPurpose.CustomerDebtCollection
                && payment.OccurredAt < asOf)
            .SumAsync(payment => payment.Amount, cancellationToken);
        return activeSales.Sum(item => item.TotalAmount)
            - salePayments
            - returns.Sum(item => item.TotalReturnAmount)
            + refunds
            - debtPayments;
    }

    private async Task<decimal> GetSupplierOutstandingAsync(
        Guid storeId,
        Guid supplierId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var activePurchases = await dbContext.Purchases.AsNoTracking()
            .Where(purchase => purchase.StoreId == storeId
                && purchase.SupplierId == supplierId
                && purchase.Status == PurchaseStatus.Completed
                && purchase.CompletedAt < asOf
                && !dbContext.PurchaseVoids.Any(voided =>
                    voided.StoreId == storeId
                    && voided.OriginalPurchaseId == purchase.Id
                    && voided.VoidedAt < asOf))
            .Select(purchase => new { purchase.Id, purchase.TotalAmount })
            .ToArrayAsync(cancellationToken);
        var purchaseIds = activePurchases.Select(item => item.Id).ToArray();
        var purchasePayments = await dbContext.PurchasePayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && purchaseIds.Contains(payment.PurchaseId)
                && payment.PaidAt < asOf)
            .SumAsync(payment => payment.Amount, cancellationToken);
        var debtPayments = await dbContext.DebtPayments.AsNoTracking()
            .Where(payment => payment.StoreId == storeId
                && payment.SupplierId == supplierId
                && payment.Purpose == DebtPaymentPurpose.SupplierDebtSettlement
                && payment.OccurredAt < asOf)
            .SumAsync(payment => payment.Amount, cancellationToken);
        return activePurchases.Sum(item => item.TotalAmount) - purchasePayments - debtPayments;
    }

    private static DebtPartyBalancePage Page(
        IReadOnlyCollection<DebtPartyBalance> balances,
        int page,
        int pageSize,
        string invalidCode)
    {
        if (balances.Any(item => item.OutstandingAmount < 0))
        {
            throw new ApplicationConflictException(
                invalidCode,
                "Debt history produces a negative outstanding balance.");
        }

        var outstanding = balances.Where(item => item.OutstandingAmount > 0).ToArray();
        var items = outstanding
            .OrderBy(item => item.PartyName)
            .ThenBy(item => item.PartyId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();
        return new DebtPartyBalancePage(items, outstanding.Length);
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
