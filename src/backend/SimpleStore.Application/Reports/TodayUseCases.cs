using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Domain;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Application.Reports;

public sealed record TodayContext(Guid StoreId, BusinessDateWindow Window);

public sealed class TodayContextResolver(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    TimeProvider timeProvider)
{
    public async Task<TodayContext> ResolveAsync(CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser, slice1Repository, cancellationToken);
        var store = await slice1Repository.GetStoreAsync(storeId, cancellationToken)
            ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(store.TimeZoneId, out var timeZone)
            || !timeZone.HasIanaId)
        {
            throw new DomainRuleException(
                "invalid-store-timezone",
                "Store timezone must be a valid canonical IANA timezone ID.");
        }

        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        var businessDate = DateOnly.FromDateTime(localNow.DateTime);
        return new TodayContext(storeId, BusinessDateWindow.Resolve(businessDate, store.TimeZoneId));
    }
}

public sealed class GetTodaySummaryUseCase(
    TodayContextResolver contextResolver,
    ISlice5BRepository financialRepository,
    ISlice6ARepository todayRepository)
{
    public async Task<TodaySummaryResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var context = await contextResolver.ResolveAsync(cancellationToken);
        var window = context.Window;
        var financialData = await financialRepository.GetEndOfDayAsync(
            context.StoreId, window.StartUtc, window.EndUtc, cancellationToken);
        var activity = await todayRepository.GetTodayActivityAsync(
            context.StoreId, window.StartUtc, window.EndUtc, cancellationToken);
        var financial = DailyFinancialProjection.Project(financialData);
        var debt = TodayProjection.CalculateDebtAndCount(activity);

        return new TodaySummaryResult(
            window.BusinessDate,
            window.TimeZoneId,
            window.StartUtc,
            window.EndUtc,
            financial.SalesRevenue,
            financial.Collected.NetAmount,
            financial.EstimatedGrossProfit,
            debt.SaleCount,
            debt.CustomerDebtCreated,
            debt.SupplierDebtCreated);
    }
}

public sealed class GetTodayExplanationUseCase(
    TodayContextResolver contextResolver,
    ISlice5BRepository financialRepository,
    ISlice6ARepository todayRepository)
{
    private static readonly HashSet<string> AllowedMetrics = new(
        [
            TodayMetricIds.Revenue,
            TodayMetricIds.Collected,
            TodayMetricIds.EstimatedGrossProfit,
            TodayMetricIds.SaleCount,
            TodayMetricIds.CustomerDebtCreated,
            TodayMetricIds.SupplierDebtCreated
        ],
        StringComparer.Ordinal);

    public async Task<TodayExplanationResult> ExecuteAsync(
        string metric,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!AllowedMetrics.Contains(metric))
        {
            throw new ApplicationValidationException(
                "invalid-today-metric",
                "Today explanation metric is invalid.",
                [new ValidationError(null, "metric", "invalid-today-metric", "Today explanation metric is invalid.")]);
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ApplicationValidationException(
                "invalid-pagination",
                "Pagination values are invalid.",
                [new ValidationError(null, "page", "invalid-pagination", "Page must be at least 1 and page size must be between 1 and 100.")]);
        }

        var context = await contextResolver.ResolveAsync(cancellationToken);
        var window = context.Window;
        var financialData = await financialRepository.GetEndOfDayAsync(
            context.StoreId, window.StartUtc, window.EndUtc, cancellationToken);
        var activity = await todayRepository.GetTodayActivityAsync(
            context.StoreId, window.StartUtc, window.EndUtc, cancellationToken);
        var financial = DailyFinancialProjection.Project(financialData);
        var debt = TodayProjection.CalculateDebtAndCount(activity);
        var allItems = TodayProjection.BuildEvidence(metric, activity, debt)
            .OrderByDescending(item => item.OccurredAt)
            .ThenBy(item => item.SourceId)
            .ToArray();
        var items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        var headline = metric switch
        {
            TodayMetricIds.Revenue => financial.SalesRevenue,
            TodayMetricIds.Collected => financial.Collected.NetAmount,
            TodayMetricIds.EstimatedGrossProfit => financial.EstimatedGrossProfit.Amount,
            TodayMetricIds.SaleCount => debt.SaleCount,
            TodayMetricIds.CustomerDebtCreated => debt.CustomerDebtCreated,
            TodayMetricIds.SupplierDebtCreated => debt.SupplierDebtCreated,
            _ => throw new InvalidOperationException("Validated metric was not handled.")
        };

        return new TodayExplanationResult(
            metric,
            headline,
            metric == TodayMetricIds.EstimatedGrossProfit
                ? financial.EstimatedGrossProfit.HistoricalCogs
                : null,
            metric == TodayMetricIds.EstimatedGrossProfit
                ? financial.EstimatedGrossProfit.CostReliability
                : null,
            page,
            pageSize,
            allItems.Length,
            allItems.Length == 0 ? 0 : (int)Math.Ceiling(allItems.Length / (double)pageSize),
            items);
    }
}

internal sealed record TodayDebtAndCountProjection(
    int SaleCount,
    decimal CustomerDebtCreated,
    decimal SupplierDebtCreated,
    IReadOnlyList<TodaySaleContribution> SaleContributions,
    IReadOnlyList<TodayPurchaseContribution> PurchaseContributions);

internal sealed record TodaySaleContribution(
    TodaySaleActivity Sale,
    decimal BaseDebt,
    decimal ReturnReduction,
    bool IsVoided,
    decimal FinalContribution);

internal sealed record TodayPurchaseContribution(
    TodayPurchaseActivity Purchase,
    decimal BaseDebt,
    bool IsVoided,
    decimal FinalContribution);

internal static class TodayProjection
{
    public static TodayDebtAndCountProjection CalculateDebtAndCount(TodayActivityData activity)
    {
        var returnTotals = activity.Returns
            .GroupBy(item => item.OriginalSaleId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.TotalReturnAmount));
        var voidedSaleIds = activity.SaleVoids.Select(item => item.OriginalSaleId).ToHashSet();
        var voidedPurchaseIds = activity.PurchaseVoids.Select(item => item.OriginalPurchaseId).ToHashSet();

        var sales = activity.Sales.Select(sale =>
        {
            var baseDebt = Math.Max(sale.TotalAmount - sale.DirectPayments, 0m);
            var returnReduction = returnTotals.GetValueOrDefault(sale.Id);
            var isVoided = voidedSaleIds.Contains(sale.Id);
            var final = isVoided ? 0m : Math.Max(baseDebt - returnReduction, 0m);
            return new TodaySaleContribution(sale, baseDebt, returnReduction, isVoided, final);
        }).ToArray();

        var purchases = activity.Purchases.Select(purchase =>
        {
            var baseDebt = Math.Max(purchase.TotalAmount - purchase.DirectPayments, 0m);
            var isVoided = voidedPurchaseIds.Contains(purchase.Id);
            return new TodayPurchaseContribution(
                purchase,
                baseDebt,
                isVoided,
                isVoided ? 0m : baseDebt);
        }).ToArray();

        return new TodayDebtAndCountProjection(
            sales.Count(item => !item.IsVoided),
            sales.Sum(item => item.FinalContribution),
            purchases.Sum(item => item.FinalContribution),
            sales,
            purchases);
    }

    public static IReadOnlyList<TodayEvidenceSourceResult> BuildEvidence(
        string metric,
        TodayActivityData activity,
        TodayDebtAndCountProjection debt) => metric switch
        {
            TodayMetricIds.Revenue => RevenueEvidence(activity),
            TodayMetricIds.Collected => CollectedEvidence(activity),
            TodayMetricIds.EstimatedGrossProfit => GrossProfitEvidence(activity),
            TodayMetricIds.SaleCount => SaleCountEvidence(debt),
            TodayMetricIds.CustomerDebtCreated => CustomerDebtEvidence(debt),
            TodayMetricIds.SupplierDebtCreated => SupplierDebtEvidence(debt),
            _ => throw new InvalidOperationException("Validated metric was not handled.")
        };

    private static TodayEvidenceSourceResult[] RevenueEvidence(TodayActivityData activity) =>
        [
            .. activity.Sales.Select(item => Evidence(
                "Sale", item.Id, null, item.CompletedAt, item.TotalAmount, null, "Đơn bán hoàn tất")),
            .. activity.Returns.Select(item => Evidence(
                "CustomerReturn", item.Id, item.OriginalSaleId, item.CompletedAt,
                -item.TotalReturnAmount, null, "Trả hàng")),
            .. activity.SaleVoids.Select(item => Evidence(
                "SaleVoid", item.Id, item.OriginalSaleId, item.VoidedAt,
                -item.OriginalSaleAmount, null, "Hủy đơn bán"))
        ];

    private static TodayEvidenceSourceResult[] CollectedEvidence(TodayActivityData activity) =>
        [
            .. activity.SalePayments.Select(item => Evidence(
                "SalePayment", item.Id, item.RelatedSourceId, item.OccurredAt,
                item.Amount, null, "Thanh toán đơn bán")),
            .. activity.CustomerDebtPayments.Select(item => Evidence(
                "CustomerDebtPayment", item.Id, item.RelatedSourceId, item.OccurredAt,
                item.Amount, null, "Thu nợ khách hàng")),
            .. activity.CustomerRefunds.Select(item => Evidence(
                "ActualCustomerRefund", item.Id, item.RelatedSourceId, item.OccurredAt,
                -item.Amount, null, "Hoàn tiền khách hàng"))
        ];

    private static TodayEvidenceSourceResult[] GrossProfitEvidence(TodayActivityData activity) =>
        [
            .. RevenueEvidence(activity),
            .. activity.Cogs.Select(item => Evidence(
                "HistoricalCogs", item.Id, item.RelatedSourceId, item.OccurredAt,
                item.GrossProfitContribution, null, item.Kind switch
                {
                    "DirectSale" => "Giá vốn lịch sử đơn bán",
                    "RestockedReturn" => "Hoàn nhập giá vốn lịch sử",
                    "VoidedSale" => "Đảo giá vốn lịch sử khi hủy",
                    _ => "Giá vốn lịch sử"
                }))
        ];

    private static TodayEvidenceSourceResult[] SaleCountEvidence(
        TodayDebtAndCountProjection debt) => debt.SaleContributions
        .Select(item => Evidence(
            "Sale",
            item.Sale.Id,
            null,
            item.Sale.CompletedAt,
            null,
            item.IsVoided ? 0 : 1,
            item.IsVoided ? "Đơn bán bị hủy cùng ngày" : "Đơn bán được tính"))
        .ToArray();

    private static TodayEvidenceSourceResult[] CustomerDebtEvidence(
        TodayDebtAndCountProjection debt) => debt.SaleContributions
        .Select(item => Evidence(
            "Sale",
            item.Sale.Id,
            null,
            item.Sale.CompletedAt,
            item.FinalContribution,
            null,
            "Nghĩa vụ nợ mới từ đơn bán",
            new TodayDebtContributionResult(
                item.Sale.TotalAmount,
                item.Sale.DirectPayments,
                item.BaseDebt,
                item.ReturnReduction,
                item.IsVoided,
                item.FinalContribution)))
        .ToArray();

    private static TodayEvidenceSourceResult[] SupplierDebtEvidence(
        TodayDebtAndCountProjection debt) => debt.PurchaseContributions
        .Select(item => Evidence(
            "Purchase",
            item.Purchase.Id,
            null,
            item.Purchase.CompletedAt,
            item.FinalContribution,
            null,
            "Nghĩa vụ nợ mới từ phiếu nhập",
            new TodayDebtContributionResult(
                item.Purchase.TotalAmount,
                item.Purchase.DirectPayments,
                item.BaseDebt,
                0m,
                item.IsVoided,
                item.FinalContribution)))
        .ToArray();

    private static TodayEvidenceSourceResult Evidence(
        string sourceType,
        Guid sourceId,
        Guid? relatedSourceId,
        DateTimeOffset occurredAt,
        decimal? amount,
        int? count,
        string title,
        TodayDebtContributionResult? debt = null) =>
        new(sourceType, sourceId, relatedSourceId, occurredAt, amount, count, title, debt);
}
