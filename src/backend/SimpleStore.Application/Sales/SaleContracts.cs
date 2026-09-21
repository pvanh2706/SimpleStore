namespace SimpleStore.Application.Sales;

public sealed record CompleteSaleLineCommand(Guid ProductId, decimal Quantity);

public sealed record SalePaymentCommand(decimal Amount, string Method);

public sealed record CompleteSaleCommand(
    Guid OperationId,
    Guid? CustomerId,
    IReadOnlyCollection<CompleteSaleLineCommand> Lines,
    IReadOnlyCollection<SalePaymentCommand> Payments);

public sealed record SaleLineResult(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string ProductUnit,
    decimal Quantity,
    decimal UnitSalePrice,
    decimal LineAmount,
    decimal UnitCostAtSale,
    string CostReliability);

public sealed record SalePaymentResult(
    Guid Id,
    decimal Amount,
    string Method,
    DateTimeOffset OccurredAt);

public sealed record SaleCustomerResult(Guid Id, string Name, string? Phone);

public sealed record SaleResult(
    Guid Id,
    string Status,
    string StoreName,
    Guid WarehouseId,
    SaleCustomerResult? Customer,
    string CashierDisplayName,
    IReadOnlyList<SaleLineResult> Lines,
    IReadOnlyList<SalePaymentResult> Payments,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset CompletedAt,
    bool WasAlreadyCompleted = false);

public sealed record SaleListItemResult(
    Guid Id,
    string Status,
    string? CustomerName,
    string CashierDisplayName,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTimeOffset CompletedAt);

public sealed record SaleListResult(
    IReadOnlyList<SaleListItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
