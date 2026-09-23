using SimpleStore.Domain.Inventory;

namespace SimpleStore.Application.Abstractions;

public interface ISlice6ARepository
{
    Task<TodayActivityData> GetTodayActivityAsync(
        Guid storeId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);
}

public sealed record TodayActivityData(
    IReadOnlyList<TodaySaleActivity> Sales,
    IReadOnlyList<TodayReturnActivity> Returns,
    IReadOnlyList<TodaySaleVoidActivity> SaleVoids,
    IReadOnlyList<TodayMoneyActivity> SalePayments,
    IReadOnlyList<TodayMoneyActivity> CustomerDebtPayments,
    IReadOnlyList<TodayMoneyActivity> CustomerRefunds,
    IReadOnlyList<TodayPurchaseActivity> Purchases,
    IReadOnlyList<TodayMoneyActivity> PurchasePayments,
    IReadOnlyList<TodayPurchaseVoidActivity> PurchaseVoids,
    IReadOnlyList<TodayCogsActivity> Cogs);

public sealed record TodaySaleActivity(
    Guid Id,
    DateTimeOffset CompletedAt,
    decimal TotalAmount,
    decimal DirectPayments);

public sealed record TodayReturnActivity(
    Guid Id,
    Guid OriginalSaleId,
    DateTimeOffset CompletedAt,
    decimal TotalReturnAmount);

public sealed record TodaySaleVoidActivity(
    Guid Id,
    Guid OriginalSaleId,
    DateTimeOffset VoidedAt,
    decimal OriginalSaleAmount);

public sealed record TodayPurchaseActivity(
    Guid Id,
    DateTimeOffset CompletedAt,
    decimal TotalAmount,
    decimal DirectPayments);

public sealed record TodayPurchaseVoidActivity(
    Guid Id,
    Guid OriginalPurchaseId,
    DateTimeOffset VoidedAt);

public sealed record TodayMoneyActivity(
    Guid Id,
    Guid RelatedSourceId,
    DateTimeOffset OccurredAt,
    decimal Amount);

public sealed record TodayCogsActivity(
    Guid Id,
    Guid RelatedSourceId,
    DateTimeOffset OccurredAt,
    string Kind,
    decimal GrossProfitContribution,
    CostReliability CostReliability);
