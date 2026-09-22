namespace SimpleStore.Application.Purchases;

public sealed record PurchaseLineCommand(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record PurchaseWriteCommand(
    Guid SupplierId,
    IReadOnlyCollection<PurchaseLineCommand> Lines);

public sealed record PurchasePaymentCommand(decimal Amount, string Method);

public sealed record CompletePurchaseCommand(
    Guid OperationId,
    IReadOnlyCollection<PurchasePaymentCommand> Payments);

public sealed record PurchaseLineResult(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductUnit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineAmount);

public sealed record PurchasePaymentResult(
    Guid Id,
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt);

public sealed record PurchaseVoidInfoResult(
    Guid Id,
    string Reason,
    Guid VoidedByUserId,
    DateTimeOffset VoidedAt);

public sealed record PurchaseResult(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string Status,
    IReadOnlyList<PurchaseLineResult> Lines,
    IReadOnlyList<PurchasePaymentResult> Payments,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    bool WasAlreadyCompleted = false,
    bool IsVoided = false,
    PurchaseVoidInfoResult? Void = null);

public sealed record PurchaseListItemResult(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string Status,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    bool IsVoided = false);

public sealed record PurchaseListResult(
    IReadOnlyList<PurchaseListItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record OperationStatusResult(
    Guid OperationId,
    string Status,
    string OperationType,
    Guid? ResultReference);
