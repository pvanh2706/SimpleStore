namespace SimpleStore.Application.Returns;

public sealed record ReturnLineCommand(Guid OriginalSaleLineId, decimal Quantity, bool? Restock);
public sealed record ReturnPreviewCommand(Guid OriginalSaleId, IReadOnlyCollection<ReturnLineCommand> Lines);
public sealed record CreateReturnCommand(
    Guid OperationId,
    Guid OriginalSaleId,
    IReadOnlyCollection<ReturnLineCommand> Lines,
    string? RefundMethod);

public sealed record ReturnLineResult(
    Guid Id,
    Guid OriginalSaleLineId,
    Guid ProductId,
    decimal Quantity,
    bool Restock,
    decimal UnitSalePriceBasis,
    decimal ReturnLineAmount,
    decimal UnitCostBasis,
    decimal RestockedInventoryValue);

public sealed record ReturnRefundPaymentResult(Guid Id, decimal Amount, string Method, DateTimeOffset OccurredAt);

public sealed record ReturnResult(
    Guid Id,
    Guid OriginalSaleId,
    string Status,
    IReadOnlyList<ReturnLineResult> Lines,
    IReadOnlyList<ReturnRefundPaymentResult> RefundPayments,
    decimal TotalReturnAmount,
    decimal RefundAmount,
    Guid CompletedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset CompletedAt,
    bool WasAlreadyCompleted = false);

public sealed record ReturnPreviewLineResult(
    Guid OriginalSaleLineId,
    Guid ProductId,
    decimal RequestedQuantity,
    bool Restock,
    decimal PreviouslyReturnedQuantity,
    decimal RemainingQuantityBefore,
    decimal ReturnLineAmount,
    decimal RestockedInventoryValue);

public sealed record ReturnPreviewResult(
    Guid OriginalSaleId,
    IReadOnlyList<ReturnPreviewLineResult> Lines,
    decimal CurrentReturnValue,
    decimal PreviousReturnedValue,
    decimal CumulativeReturnedValue,
    decimal NetSaleObligation,
    decimal NetCashHeld,
    decimal Outstanding,
    decimal RefundDueNow,
    bool RefundMethodRequired);

public sealed record ReturnContextLineResult(
    Guid SaleLineId,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string ProductUnit,
    decimal SoldQuantity,
    decimal PreviouslyReturnedQuantity,
    decimal ReturnableQuantity,
    decimal OriginalUnitSalePrice,
    decimal OriginalLineAmount);

public sealed record ReturnContextResult(
    Guid SaleId,
    bool IsVoided,
    bool HasReturns,
    decimal OriginalTotalAmount,
    decimal TotalReturnedAmount,
    decimal NetSaleAmount,
    decimal OriginalCollectedAmount,
    decimal TotalRefundedAmount,
    decimal NetCollectedAmount,
    decimal OutstandingAmount,
    IReadOnlyList<ReturnContextLineResult> Lines);
