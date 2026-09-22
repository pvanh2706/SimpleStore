namespace SimpleStore.Application.Corrections;

public sealed record VoidTransactionCommand(Guid OperationId, string Reason);

public sealed record SaleVoidResult(
    Guid Id,
    Guid OriginalSaleId,
    string Reason,
    Guid VoidedByUserId,
    DateTimeOffset VoidedAt,
    bool WasAlreadyCompleted = false);

public sealed record PurchaseVoidResult(
    Guid Id,
    Guid OriginalPurchaseId,
    string Reason,
    Guid VoidedByUserId,
    DateTimeOffset VoidedAt,
    bool WasAlreadyCompleted = false);
