namespace SimpleStore.Application.Reports;

public static class C14HistoryCoverageTypes
{
    public const string FullSevenCompletedDays = "FullSevenCompletedDays";
    public const string PartialObservation = "PartialObservation";
}

public static class C14RecentSalesEvidenceTypes
{
    public const string PositiveNetSold = "PositiveNetSold";
    public const string NoPositiveNetSold = "NoPositiveNetSold";
}

public static class C14RiskEvaluationTypes
{
    public const string Eligible = "Eligible";
    public const string InsufficientFullHistory = "InsufficientFullHistory";
    public const string NoPositiveSalesEvidence = "NoPositiveSalesEvidence";
}

public static class C14SourceTypes
{
    public const string Sale = "Sale";
    public const string Return = "Return";
    public const string SaleVoid = "SaleVoid";
}

public sealed record C14BusinessDateResult(
    DateOnly BusinessDate,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc);

public sealed record C14AttentionItemResult(
    Guid ProductId,
    string ProductName,
    string Sku,
    string Unit,
    string AttentionKind,
    decimal CurrentStock,
    decimal NetSoldQuantity,
    decimal? AverageDailySales,
    decimal? DaysOfCover,
    string HistoryCoverage,
    string RecentSalesEvidence,
    string RiskEvaluation);

public sealed record C14AttentionListResult(
    DateOnly BusinessDate,
    string TimeZoneId,
    DateTimeOffset VelocityStartUtc,
    DateTimeOffset VelocityEndUtc,
    IReadOnlyList<C14BusinessDateResult> CompletedBusinessDays,
    string EvaluationCoverage,
    int TotalAttentionCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<C14AttentionItemResult> Items);

public sealed record C14FormulaInputsResult(
    decimal SaleQuantity,
    decimal ReturnQuantity,
    decimal SaleVoidQuantity,
    int Denominator);

public sealed record C14SourceEvidenceResult(
    string SourceType,
    Guid SourceId,
    Guid? RelatedAggregateId,
    DateTimeOffset OccurredAt,
    DateOnly BusinessDate,
    Guid ProductId,
    decimal QuantityContribution,
    TodaySourceNavigationResult Navigation);

public sealed record C14AttentionDetailResult(
    Guid ProductId,
    string ProductName,
    string Sku,
    string Unit,
    string AttentionKind,
    decimal CurrentStock,
    DateOnly BusinessDate,
    string TimeZoneId,
    DateTimeOffset VelocityStartUtc,
    DateTimeOffset VelocityEndUtc,
    IReadOnlyList<C14BusinessDateResult> CompletedBusinessDays,
    decimal NetSoldQuantity,
    decimal? AverageDailySales,
    decimal? DaysOfCover,
    string HistoryCoverage,
    string RecentSalesEvidence,
    string RiskEvaluation,
    C14FormulaInputsResult FormulaInputs,
    IReadOnlyList<C14SourceEvidenceResult> Evidence);

public sealed record RecordC14ExperimentEventCommand(
    Guid EventId,
    string EventType,
    Guid? ProductId,
    string? AttentionKind);

public sealed record C14ExperimentEventResult(
    Guid EventId,
    string EventType,
    Guid? ProductId,
    string? AttentionKind,
    DateOnly BusinessDate,
    DateTimeOffset OccurredAt,
    bool WasAlreadyRecorded);
