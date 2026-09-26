namespace SimpleStore.Domain.Inventory;

public sealed class StockAdjustment
{
    public const int MaxReasonLength = 500;

    private StockAdjustment()
    {
    }

    private StockAdjustment(
        Guid id,
        Guid operationId,
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantityDelta,
        decimal? adjustmentUnitCost,
        decimal effectiveUnitCost,
        CostReliability costReliability,
        decimal inventoryValueDelta,
        string reason,
        decimal quantityBefore,
        decimal quantityAfter,
        decimal inventoryValueBefore,
        decimal inventoryValueAfter,
        decimal averageCostAfter,
        bool hasAverageCostAfter,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        OperationId = operationId;
        StoreId = storeId;
        WarehouseId = warehouseId;
        ProductId = productId;
        QuantityDelta = quantityDelta;
        AdjustmentUnitCost = adjustmentUnitCost;
        EffectiveUnitCost = effectiveUnitCost;
        CostReliability = costReliability;
        InventoryValueDelta = inventoryValueDelta;
        Reason = reason;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        InventoryValueBefore = inventoryValueBefore;
        InventoryValueAfter = inventoryValueAfter;
        AverageCostAfter = averageCostAfter;
        HasAverageCostAfter = hasAverageCostAfter;
        PerformedByUserId = performedByUserId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal? AdjustmentUnitCost { get; private set; }
    public decimal EffectiveUnitCost { get; private set; }
    public CostReliability CostReliability { get; private set; }
    public decimal InventoryValueDelta { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public decimal QuantityBefore { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public decimal InventoryValueBefore { get; private set; }
    public decimal InventoryValueAfter { get; private set; }
    public decimal AverageCostAfter { get; private set; }
    public bool HasAverageCostAfter { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid InventoryMovementId { get; private set; }

    public static StockAdjustment Create(
        Guid operationId,
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantityDelta,
        decimal? adjustmentUnitCost,
        InventoryAdjustmentCost cost,
        string reason,
        decimal quantityBefore,
        decimal quantityAfter,
        decimal inventoryValueBefore,
        decimal inventoryValueAfter,
        decimal averageCostAfter,
        bool hasAverageCostAfter,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        if (operationId == Guid.Empty)
        {
            throw new DomainRuleException("operation-id-required", "OperationId is required.");
        }

        InventoryStoragePrecision.EnsureQuantity(
            quantityDelta,
            "invalid-adjustment-quantity-precision",
            "Adjustment quantity");
        if (adjustmentUnitCost.HasValue)
        {
            InventoryStoragePrecision.EnsureUnitCost(
                adjustmentUnitCost.Value,
                "invalid-adjustment-unit-cost-precision",
                "Adjustment unit cost");
        }

        var normalizedReason = reason?.Trim() ?? string.Empty;
        if (normalizedReason.Length is < 1 or > MaxReasonLength)
        {
            throw new DomainRuleException(
                "adjustment-reason-required",
                $"Adjustment reason is required and must not exceed {MaxReasonLength} characters.");
        }

        return new StockAdjustment(
            Guid.NewGuid(), operationId, storeId, warehouseId, productId, quantityDelta,
            adjustmentUnitCost, cost.UnitCost, cost.Reliability, cost.InventoryValueDelta,
            normalizedReason, quantityBefore, quantityAfter, inventoryValueBefore, inventoryValueAfter,
            averageCostAfter, hasAverageCostAfter, performedByUserId, occurredAt);
    }

    public void RecordMovement(Guid movementId)
    {
        if (movementId == Guid.Empty || InventoryMovementId != Guid.Empty)
        {
            throw new DomainRuleException("invalid-adjustment-movement", "Adjustment movement is invalid.");
        }

        InventoryMovementId = movementId;
    }
}
