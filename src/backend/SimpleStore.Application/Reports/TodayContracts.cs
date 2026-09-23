namespace SimpleStore.Application.Reports;

public sealed record TodaySummaryResult(
    DateOnly BusinessDate,
    string TimeZoneId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    decimal SalesRevenue,
    decimal NetCollected,
    EstimatedGrossProfitResult EstimatedGrossProfit,
    int SaleCount,
    decimal CustomerDebtCreated,
    decimal SupplierDebtCreated);

public sealed record TodayExplanationResult(
    string Metric,
    decimal Headline,
    decimal? HistoricalCogs,
    string? CostReliability,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<TodayEvidenceSourceResult> Items);

public sealed record TodayEvidenceSourceResult(
    string SourceType,
    Guid SourceId,
    Guid? RelatedSourceId,
    DateTimeOffset OccurredAt,
    decimal? ContributionAmount,
    int? ContributionCount,
    string Title,
    TodayDebtContributionResult? DebtContribution,
    TodaySourceNavigationResult? Navigation);

public sealed record TodaySourceNavigationResult(string Type, Guid Id);

public static class TodaySourceNavigationTypes
{
    public const string Sale = "Sale";
    public const string Return = "Return";
    public const string Purchase = "Purchase";
}

public sealed record TodayDebtContributionResult(
    decimal OriginalTotal,
    decimal DirectPayments,
    decimal BaseDebt,
    decimal SameDayReturnObligationReduction,
    bool SameDayVoided,
    decimal FinalContribution);

public static class TodayMetricIds
{
    public const string Revenue = "revenue";
    public const string Collected = "collected";
    public const string EstimatedGrossProfit = "estimated-gross-profit";
    public const string SaleCount = "sale-count";
    public const string CustomerDebtCreated = "customer-debt-created";
    public const string SupplierDebtCreated = "supplier-debt-created";
}
