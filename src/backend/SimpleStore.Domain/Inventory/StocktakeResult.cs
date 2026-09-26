namespace SimpleStore.Domain.Inventory;

public sealed class StocktakeResult
{
    public const int MaxNoteLength = 500;

    private StocktakeResult()
    {
    }

    private StocktakeResult(
        Guid id,
        Guid operationId,
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal expectedQuantity,
        byte[] expectedBalanceRowVersion,
        decimal countedQuantity,
        decimal difference,
        decimal? adjustmentUnitCost,
        decimal effectiveUnitCost,
        CostReliability costReliability,
        decimal inventoryValueDelta,
        string? note,
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
        ExpectedQuantity = expectedQuantity;
        ExpectedBalanceRowVersion = expectedBalanceRowVersion.ToArray();
        CountedQuantity = countedQuantity;
        Difference = difference;
        AdjustmentUnitCost = adjustmentUnitCost;
        EffectiveUnitCost = effectiveUnitCost;
        CostReliability = costReliability;
        InventoryValueDelta = inventoryValueDelta;
        Note = note;
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
    public decimal ExpectedQuantity { get; private set; }
    public byte[] ExpectedBalanceRowVersion { get; private set; } = [];
    public decimal CountedQuantity { get; private set; }
    public decimal Difference { get; private set; }
    public decimal? AdjustmentUnitCost { get; private set; }
    public decimal EffectiveUnitCost { get; private set; }
    public CostReliability CostReliability { get; private set; }
    public decimal InventoryValueDelta { get; private set; }
    public string? Note { get; private set; }
    public decimal InventoryValueBefore { get; private set; }
    public decimal InventoryValueAfter { get; private set; }
    public decimal AverageCostAfter { get; private set; }
    public bool HasAverageCostAfter { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? InventoryMovementId { get; private set; }

    public static StocktakeResult Create(
        Guid operationId,
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal expectedQuantity,
        byte[] expectedBalanceRowVersion,
        decimal countedQuantity,
        decimal? adjustmentUnitCost,
        InventoryAdjustmentCost? cost,
        string? note,
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

        if (expectedBalanceRowVersion.Length == 0)
        {
            throw new DomainRuleException("stocktake-revision-required", "Stocktake balance revision is required.");
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote?.Length > MaxNoteLength)
        {
            throw new DomainRuleException(
                "stocktake-note-too-long",
                $"Stocktake note must not exceed {MaxNoteLength} characters.");
        }

        var difference = countedQuantity - expectedQuantity;
        if (difference != 0 && cost is null)
        {
            throw new DomainRuleException("stocktake-cost-required", "Stocktake difference cost is required.");
        }

        return new StocktakeResult(
            Guid.NewGuid(), operationId, storeId, warehouseId, productId,
            expectedQuantity, expectedBalanceRowVersion, countedQuantity, difference,
            adjustmentUnitCost, cost?.UnitCost ?? 0, cost?.Reliability ?? CostReliability.Unavailable,
            cost?.InventoryValueDelta ?? 0, normalizedNote, inventoryValueBefore, inventoryValueAfter,
            averageCostAfter, hasAverageCostAfter, performedByUserId, occurredAt);
    }

    public void RecordMovement(Guid movementId)
    {
        if (Difference == 0 || movementId == Guid.Empty || InventoryMovementId.HasValue)
        {
            throw new DomainRuleException("invalid-stocktake-movement", "Stocktake movement is invalid.");
        }

        InventoryMovementId = movementId;
    }
}
