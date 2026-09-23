namespace SimpleStore.Application.Reports;

public sealed record EndOfDayReportResult(
    DateOnly BusinessDate,
    string TimeZoneId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    decimal SalesRevenue,
    CollectedResult Collected,
    decimal CustomerOutstandingDebtAtEnd,
    SupplierPaymentsResult SupplierPayments,
    decimal SupplierOutstandingDebtAtEnd,
    EstimatedGrossProfitResult EstimatedGrossProfit);

public sealed record CollectedResult(
    decimal SalePayments,
    decimal CustomerDebtPayments,
    decimal CustomerRefunds,
    decimal NetAmount);

public sealed record SupplierPaymentsResult(
    decimal PurchasePayments,
    decimal SupplierDebtPayments,
    decimal TotalAmount);

public sealed record EstimatedGrossProfitResult(
    decimal NetSalesRevenue,
    decimal HistoricalCogs,
    decimal Amount,
    string CostReliability);
