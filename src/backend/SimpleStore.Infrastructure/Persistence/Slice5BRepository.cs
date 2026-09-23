using Microsoft.EntityFrameworkCore;
using SimpleStore.Application.Abstractions;
using SimpleStore.Domain.Debts;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Purchases;

namespace SimpleStore.Infrastructure.Persistence;

public sealed class Slice5BRepository(ApplicationDbContext dbContext)
    : ISlice5BRepository, ISlice6ARepository
{
    public async Task<EndOfDayData> GetEndOfDayAsync(
        Guid storeId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var salesInWindow = dbContext.Sales.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.CompletedAt >= startUtc && item.CompletedAt < endUtc);
        var returnsInWindow = dbContext.Returns.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.CompletedAt >= startUtc && item.CompletedAt < endUtc);
        var voidsInWindow = dbContext.SaleVoids.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.VoidedAt >= startUtc && item.VoidedAt < endUtc);

        var completedSales = await salesInWindow.SumAsync(
            item => (decimal?)item.TotalAmount, cancellationToken) ?? 0m;
        var completedReturns = await returnsInWindow.SumAsync(
            item => (decimal?)item.TotalReturnAmount, cancellationToken) ?? 0m;
        var voidedSales = await (
            from voided in voidsInWindow
            join sale in dbContext.Sales.AsNoTracking() on voided.OriginalSaleId equals sale.Id
            select (decimal?)sale.TotalAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var saleCash = await SumSalePayments(storeId, startUtc, endUtc, PaymentMethod.Cash, cancellationToken);
        var saleTransfer = await SumSalePayments(storeId, startUtc, endUtc, PaymentMethod.Transfer, cancellationToken);
        var customerDebtCash = await SumDebtPayments(
            storeId, startUtc, endUtc, DebtPaymentPurpose.CustomerDebtCollection, PaymentMethod.Cash, cancellationToken);
        var customerDebtTransfer = await SumDebtPayments(
            storeId, startUtc, endUtc, DebtPaymentPurpose.CustomerDebtCollection, PaymentMethod.Transfer, cancellationToken);
        var refundCash = await SumRefunds(storeId, startUtc, endUtc, PaymentMethod.Cash, cancellationToken);
        var refundTransfer = await SumRefunds(storeId, startUtc, endUtc, PaymentMethod.Transfer, cancellationToken);
        var purchaseCash = await SumPurchasePayments(storeId, startUtc, endUtc, PaymentMethod.Cash, cancellationToken);
        var purchaseTransfer = await SumPurchasePayments(storeId, startUtc, endUtc, PaymentMethod.Transfer, cancellationToken);
        var supplierDebtCash = await SumDebtPayments(
            storeId, startUtc, endUtc, DebtPaymentPurpose.SupplierDebtSettlement, PaymentMethod.Cash, cancellationToken);
        var supplierDebtTransfer = await SumDebtPayments(
            storeId, startUtc, endUtc, DebtPaymentPurpose.SupplierDebtSettlement, PaymentMethod.Transfer, cancellationToken);

        var directLines = await (
            from line in dbContext.SaleLines.AsNoTracking()
            join sale in salesInWindow on line.SaleId equals sale.Id
            select new { line.Quantity, line.UnitCostAtSale, line.CostReliability })
            .ToArrayAsync(cancellationToken);
        var returnLines = await (
            from line in dbContext.ReturnLines.AsNoTracking()
            join customerReturn in returnsInWindow on line.ReturnId equals customerReturn.Id
            join original in dbContext.SaleLines.AsNoTracking()
                on line.OriginalSaleLineId equals original.Id
            select new { line.RestockedInventoryValue, original.CostReliability })
            .ToArrayAsync(cancellationToken);
        var voidedLines = await (
            from voided in voidsInWindow
            join line in dbContext.SaleLines.AsNoTracking()
                on voided.OriginalSaleId equals line.SaleId
            select new { line.Quantity, line.UnitCostAtSale, line.CostReliability })
            .ToArrayAsync(cancellationToken);

        var reliabilities = directLines.Select(item => item.CostReliability)
            .Concat(returnLines.Select(item => item.CostReliability))
            .Concat(voidedLines.Select(item => item.CostReliability));
        var reliability = reliabilities.DefaultIfEmpty(CostReliability.Reliable).Max();

        return new EndOfDayData(
            completedSales,
            completedReturns,
            voidedSales,
            saleCash,
            saleTransfer,
            customerDebtCash,
            customerDebtTransfer,
            refundCash,
            refundTransfer,
            purchaseCash,
            purchaseTransfer,
            supplierDebtCash,
            supplierDebtTransfer,
            await GetEndingCustomerDebt(storeId, endUtc, cancellationToken),
            await GetEndingSupplierDebt(storeId, endUtc, cancellationToken),
            directLines.Sum(item => Cost(item.Quantity, item.UnitCostAtSale)),
            returnLines.Sum(item => item.RestockedInventoryValue),
            voidedLines.Sum(item => Cost(item.Quantity, item.UnitCostAtSale)),
            reliability);
    }

    private Task<decimal> SumSalePayments(
        Guid storeId, DateTimeOffset start, DateTimeOffset end, PaymentMethod method,
        CancellationToken cancellationToken) =>
        Sum(dbContext.SalePayments.AsNoTracking().Where(item => item.StoreId == storeId
            && item.OccurredAt >= start && item.OccurredAt < end && item.Method == method)
            .Select(item => (decimal?)item.Amount), cancellationToken);

    private Task<decimal> SumRefunds(
        Guid storeId, DateTimeOffset start, DateTimeOffset end, PaymentMethod method,
        CancellationToken cancellationToken) =>
        Sum(dbContext.ReturnRefundPayments.AsNoTracking().Where(item => item.StoreId == storeId
            && item.OccurredAt >= start && item.OccurredAt < end && item.Method == method)
            .Select(item => (decimal?)item.Amount), cancellationToken);

    private Task<decimal> SumPurchasePayments(
        Guid storeId, DateTimeOffset start, DateTimeOffset end, PaymentMethod method,
        CancellationToken cancellationToken) =>
        Sum(dbContext.PurchasePayments.AsNoTracking().Where(item => item.StoreId == storeId
            && item.PaidAt >= start && item.PaidAt < end && item.Method == method)
            .Select(item => (decimal?)item.Amount), cancellationToken);

    private Task<decimal> SumDebtPayments(
        Guid storeId, DateTimeOffset start, DateTimeOffset end,
        DebtPaymentPurpose purpose, PaymentMethod method, CancellationToken cancellationToken) =>
        Sum(dbContext.DebtPayments.AsNoTracking().Where(item => item.StoreId == storeId
            && item.OccurredAt >= start && item.OccurredAt < end
            && item.Purpose == purpose && item.Method == method)
            .Select(item => (decimal?)item.Amount), cancellationToken);

    private async Task<decimal> GetEndingCustomerDebt(
        Guid storeId, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var activeSales = await dbContext.Sales.AsNoTracking()
            .Where(sale => sale.StoreId == storeId && sale.CustomerId.HasValue
                && sale.CompletedAt < end
                && !dbContext.SaleVoids.Any(voided => voided.StoreId == storeId
                    && voided.OriginalSaleId == sale.Id && voided.VoidedAt < end))
            .Select(sale => new { sale.Id, sale.TotalAmount })
            .ToArrayAsync(cancellationToken);
        var saleIds = activeSales.Select(item => item.Id).ToArray();
        var payments = await Sum(dbContext.SalePayments.AsNoTracking()
            .Where(item => item.StoreId == storeId && saleIds.Contains(item.SaleId)
                && item.OccurredAt < end)
            .Select(item => (decimal?)item.Amount), cancellationToken);
        var returns = await dbContext.Returns.AsNoTracking()
            .Where(item => item.StoreId == storeId && saleIds.Contains(item.OriginalSaleId)
                && item.CompletedAt < end)
            .Select(item => new { item.Id, item.TotalReturnAmount })
            .ToArrayAsync(cancellationToken);
        var returnIds = returns.Select(item => item.Id).ToArray();
        var refunds = await Sum(dbContext.ReturnRefundPayments.AsNoTracking()
            .Where(item => item.StoreId == storeId && returnIds.Contains(item.ReturnId)
                && item.OccurredAt < end)
            .Select(item => (decimal?)item.Amount), cancellationToken);
        var debtPayments = await Sum(dbContext.DebtPayments.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.Purpose == DebtPaymentPurpose.CustomerDebtCollection
                && item.OccurredAt < end)
            .Select(item => (decimal?)item.Amount), cancellationToken);
        return activeSales.Sum(item => item.TotalAmount)
            - payments - returns.Sum(item => item.TotalReturnAmount) + refunds - debtPayments;
    }

    private async Task<decimal> GetEndingSupplierDebt(
        Guid storeId, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var activePurchases = await dbContext.Purchases.AsNoTracking()
            .Where(purchase => purchase.StoreId == storeId
                && purchase.Status == PurchaseStatus.Completed
                && purchase.CompletedAt < end
                && !dbContext.PurchaseVoids.Any(voided => voided.StoreId == storeId
                    && voided.OriginalPurchaseId == purchase.Id && voided.VoidedAt < end))
            .Select(purchase => new { purchase.Id, purchase.TotalAmount })
            .ToArrayAsync(cancellationToken);
        var purchaseIds = activePurchases.Select(item => item.Id).ToArray();
        var payments = await Sum(dbContext.PurchasePayments.AsNoTracking()
            .Where(item => item.StoreId == storeId && purchaseIds.Contains(item.PurchaseId)
                && item.PaidAt < end)
            .Select(item => (decimal?)item.Amount), cancellationToken);
        var debtPayments = await Sum(dbContext.DebtPayments.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.Purpose == DebtPaymentPurpose.SupplierDebtSettlement
                && item.OccurredAt < end)
            .Select(item => (decimal?)item.Amount), cancellationToken);
        return activePurchases.Sum(item => item.TotalAmount) - payments - debtPayments;
    }

    private static async Task<decimal> Sum(
        IQueryable<decimal?> query,
        CancellationToken cancellationToken) =>
        await query.SumAsync(cancellationToken) ?? 0m;

    private static decimal Cost(decimal quantity, decimal unitCost) =>
        decimal.Round(quantity * unitCost, 2, MidpointRounding.AwayFromZero);

    public async Task<TodayActivityData> GetTodayActivityAsync(
        Guid storeId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var sales = await dbContext.Sales.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.CompletedAt >= startUtc && item.CompletedAt < endUtc)
            .Select(item => new TodaySaleActivity(
                item.Id,
                item.CompletedAt,
                item.TotalAmount,
                dbContext.SalePayments
                    .Where(payment => payment.StoreId == storeId && payment.SaleId == item.Id)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m))
            .ToArrayAsync(cancellationToken);

        var returns = await dbContext.Returns.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.CompletedAt >= startUtc && item.CompletedAt < endUtc)
            .Select(item => new TodayReturnActivity(
                item.Id,
                item.OriginalSaleId,
                item.CompletedAt,
                item.TotalReturnAmount))
            .ToArrayAsync(cancellationToken);

        var saleVoids = await (
            from voided in dbContext.SaleVoids.AsNoTracking()
            join sale in dbContext.Sales.AsNoTracking() on voided.OriginalSaleId equals sale.Id
            where voided.StoreId == storeId && sale.StoreId == storeId
                && voided.VoidedAt >= startUtc && voided.VoidedAt < endUtc
            select new TodaySaleVoidActivity(
                voided.Id,
                voided.OriginalSaleId,
                voided.VoidedAt,
                sale.TotalAmount))
            .ToArrayAsync(cancellationToken);

        var salePayments = await dbContext.SalePayments.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.OccurredAt >= startUtc && item.OccurredAt < endUtc)
            .Select(item => new TodayMoneyActivity(
                item.Id, item.SaleId, item.OccurredAt, item.Amount))
            .ToArrayAsync(cancellationToken);

        var customerDebtPayments = await dbContext.DebtPayments.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.Purpose == DebtPaymentPurpose.CustomerDebtCollection
                && item.OccurredAt >= startUtc && item.OccurredAt < endUtc)
            .Select(item => new TodayMoneyActivity(
                item.Id, item.CustomerId!.Value, item.OccurredAt, item.Amount))
            .ToArrayAsync(cancellationToken);

        var customerRefunds = await dbContext.ReturnRefundPayments.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.OccurredAt >= startUtc && item.OccurredAt < endUtc)
            .Select(item => new TodayMoneyActivity(
                item.Id, item.ReturnId, item.OccurredAt, item.Amount))
            .ToArrayAsync(cancellationToken);

        var purchases = await dbContext.Purchases.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.Status == PurchaseStatus.Completed
                && item.CompletedAt >= startUtc && item.CompletedAt < endUtc)
            .Select(item => new TodayPurchaseActivity(
                item.Id,
                item.CompletedAt!.Value,
                item.TotalAmount,
                dbContext.PurchasePayments
                    .Where(payment => payment.StoreId == storeId && payment.PurchaseId == item.Id)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m))
            .ToArrayAsync(cancellationToken);

        var purchasePayments = await dbContext.PurchasePayments.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.PaidAt >= startUtc && item.PaidAt < endUtc)
            .Select(item => new TodayMoneyActivity(
                item.Id, item.PurchaseId, item.PaidAt, item.Amount))
            .ToArrayAsync(cancellationToken);

        var purchaseVoids = await dbContext.PurchaseVoids.AsNoTracking()
            .Where(item => item.StoreId == storeId
                && item.VoidedAt >= startUtc && item.VoidedAt < endUtc)
            .Select(item => new TodayPurchaseVoidActivity(
                item.Id, item.OriginalPurchaseId, item.VoidedAt))
            .ToArrayAsync(cancellationToken);

        var directCogs = await (
            from line in dbContext.SaleLines.AsNoTracking()
            join sale in dbContext.Sales.AsNoTracking() on line.SaleId equals sale.Id
            where sale.StoreId == storeId
                && sale.CompletedAt >= startUtc && sale.CompletedAt < endUtc
            select new TodayCogsActivity(
                line.Id,
                sale.Id,
                sale.CompletedAt,
                "DirectSale",
                -Cost(line.Quantity, line.UnitCostAtSale),
                line.CostReliability))
            .ToArrayAsync(cancellationToken);

        var returnCogs = await (
            from line in dbContext.ReturnLines.AsNoTracking()
            join customerReturn in dbContext.Returns.AsNoTracking()
                on line.ReturnId equals customerReturn.Id
            join originalLine in dbContext.SaleLines.AsNoTracking()
                on line.OriginalSaleLineId equals originalLine.Id
            where customerReturn.StoreId == storeId
                && customerReturn.CompletedAt >= startUtc && customerReturn.CompletedAt < endUtc
            select new TodayCogsActivity(
                line.Id,
                customerReturn.Id,
                customerReturn.CompletedAt,
                "RestockedReturn",
                line.RestockedInventoryValue,
                originalLine.CostReliability))
            .ToArrayAsync(cancellationToken);

        var voidCogs = await (
            from voided in dbContext.SaleVoids.AsNoTracking()
            join line in dbContext.SaleLines.AsNoTracking()
                on voided.OriginalSaleId equals line.SaleId
            where voided.StoreId == storeId
                && voided.VoidedAt >= startUtc && voided.VoidedAt < endUtc
            select new TodayCogsActivity(
                line.Id,
                voided.OriginalSaleId,
                voided.VoidedAt,
                "VoidedSale",
                Cost(line.Quantity, line.UnitCostAtSale),
                line.CostReliability))
            .ToArrayAsync(cancellationToken);

        return new TodayActivityData(
            sales,
            returns,
            saleVoids,
            salePayments,
            customerDebtPayments,
            customerRefunds,
            purchases,
            purchasePayments,
            purchaseVoids,
            [.. directCogs, .. returnCogs, .. voidCogs]);
    }
}
