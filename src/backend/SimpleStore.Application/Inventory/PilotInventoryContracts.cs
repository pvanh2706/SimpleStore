using SimpleStore.Domain.Inventory;

namespace SimpleStore.Application.Inventory;

public sealed record CreateStockAdjustmentCommand(
    Guid OperationId,
    Guid ProductId,
    decimal QuantityDelta,
    decimal? AdjustmentUnitCost,
    string Reason);

public sealed record StockAdjustmentResult(
    Guid Id,
    Guid OperationId,
    Guid ProductId,
    decimal QuantityDelta,
    decimal? AdjustmentUnitCost,
    decimal EffectiveUnitCost,
    string CostReliability,
    decimal InventoryValueDelta,
    string Reason,
    decimal QuantityBefore,
    decimal QuantityAfter,
    decimal InventoryValueBefore,
    decimal InventoryValueAfter,
    decimal AverageCostAfter,
    bool HasAverageCostAfter,
    Guid InventoryMovementId,
    DateTimeOffset OccurredAt,
    bool WasAlreadyCompleted);

public sealed record StocktakeContextResult(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string Unit,
    decimal ExpectedQuantity,
    string ExpectedRevision,
    bool HasAverageCost,
    decimal AverageCost);

public sealed record SubmitStocktakeCommand(
    Guid OperationId,
    Guid ProductId,
    decimal ExpectedQuantity,
    string ExpectedRevision,
    decimal CountedQuantity,
    decimal? AdjustmentUnitCost,
    string? Note);

public sealed record StocktakeResultDto(
    Guid Id,
    Guid OperationId,
    Guid ProductId,
    decimal ExpectedQuantity,
    string ExpectedRevision,
    decimal CountedQuantity,
    decimal Difference,
    decimal? AdjustmentUnitCost,
    decimal EffectiveUnitCost,
    string CostReliability,
    decimal InventoryValueDelta,
    string? Note,
    decimal InventoryValueBefore,
    decimal InventoryValueAfter,
    decimal AverageCostAfter,
    bool HasAverageCostAfter,
    Guid? InventoryMovementId,
    DateTimeOffset OccurredAt,
    bool WasAlreadyCompleted);
