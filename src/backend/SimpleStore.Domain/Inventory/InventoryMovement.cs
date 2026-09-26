namespace SimpleStore.Domain.Inventory;

public sealed class InventoryMovement
{
    private InventoryMovement()
    {
    }

    private InventoryMovement(
        Guid id,
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantityDelta,
        decimal inventoryValueDelta,
        decimal unitCost,
        InventoryMovementType movementType,
        string sourceType,
        Guid sourceId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        StoreId = storeId;
        WarehouseId = warehouseId;
        ProductId = productId;
        QuantityDelta = quantityDelta;
        InventoryValueDelta = inventoryValueDelta;
        UnitCost = unitCost;
        MovementType = movementType;
        SourceType = sourceType;
        SourceId = sourceId;
        PerformedByUserId = performedByUserId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public long LedgerSequence { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ProductId { get; private set; }

    public decimal QuantityDelta { get; private set; }

    public decimal InventoryValueDelta { get; private set; }

    public decimal UnitCost { get; private set; }

    public InventoryMovementType MovementType { get; private set; }

    public string SourceType { get; private set; } = string.Empty;

    public Guid SourceId { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public CostReliability? CostReliability { get; private set; }

    public string? Reason { get; private set; }

    public decimal? StocktakeExpectedQuantity { get; private set; }

    public decimal? StocktakeCountedQuantity { get; private set; }

    public static InventoryMovement CreateOpeningBalance(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        OpeningInventory openingInventory,
        string sourceType,
        Guid sourceId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        if (!openingInventory.HasStock)
        {
            throw new DomainRuleException(
                "opening-stock-required",
                "An opening movement requires quantity greater than zero.");
        }

        return new InventoryMovement(
            Guid.NewGuid(),
            storeId,
            warehouseId,
            productId,
            openingInventory.Quantity,
            openingInventory.InventoryValue,
            openingInventory.UnitCost!.Value,
            InventoryMovementType.OpeningBalance,
            sourceType,
            sourceId,
            performedByUserId,
            occurredAt);
    }

    public static InventoryMovement CreatePurchase(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantity,
        decimal inventoryValue,
        Guid purchaseLineId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        if (quantity <= 0 || inventoryValue < 0)
        {
            throw new DomainRuleException(
                "invalid-purchase-movement",
                "Purchase movement quantity and value are invalid.");
        }

        var effectiveUnitCost = Math.Round(
            inventoryValue / quantity,
            4,
            MidpointRounding.AwayFromZero);

        return new InventoryMovement(
            Guid.NewGuid(),
            storeId,
            warehouseId,
            productId,
            quantity,
            inventoryValue,
            effectiveUnitCost,
            InventoryMovementType.Purchase,
            "PurchaseLine",
            purchaseLineId,
            performedByUserId,
            occurredAt);
    }

    public static InventoryMovement CreateSale(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantity,
        decimal inventoryValue,
        decimal unitCost,
        Guid saleLineId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        if (quantity <= 0 || inventoryValue < 0 || unitCost < 0)
        {
            throw new DomainRuleException(
                "invalid-sale-movement",
                "Sale movement quantity and value are invalid.");
        }

        return new InventoryMovement(
            Guid.NewGuid(),
            storeId,
            warehouseId,
            productId,
            -quantity,
            -inventoryValue,
            unitCost,
            InventoryMovementType.Sale,
            "SaleLine",
            saleLineId,
            performedByUserId,
            occurredAt);
    }

    public static InventoryMovement CreateReturnRestock(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantity,
        decimal inventoryValue,
        decimal unitCost,
        Guid returnLineId,
        Guid performedByUserId,
        DateTimeOffset occurredAt) =>
        CreateInboundCorrection(
            storeId,
            warehouseId,
            productId,
            quantity,
            inventoryValue,
            unitCost,
            InventoryMovementType.ReturnRestock,
            "ReturnLine",
            returnLineId,
            performedByUserId,
            occurredAt);

    public static InventoryMovement CreateSaleVoid(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantity,
        decimal inventoryValue,
        decimal unitCost,
        Guid saleVoidId,
        Guid performedByUserId,
        DateTimeOffset occurredAt) =>
        CreateInboundCorrection(
            storeId,
            warehouseId,
            productId,
            quantity,
            inventoryValue,
            unitCost,
            InventoryMovementType.SaleVoid,
            "SaleVoid",
            saleVoidId,
            performedByUserId,
            occurredAt);

    public static InventoryMovement CreatePurchaseVoid(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantityDelta,
        decimal inventoryValueDelta,
        decimal unitCost,
        Guid purchaseVoidId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        if (quantityDelta >= 0 || inventoryValueDelta > 0 || unitCost < 0)
        {
            throw new DomainRuleException(
                "invalid-purchase-void-movement",
                "Purchase void movement deltas are invalid.");
        }

        return new InventoryMovement(
            Guid.NewGuid(),
            storeId,
            warehouseId,
            productId,
            quantityDelta,
            inventoryValueDelta,
            unitCost,
            InventoryMovementType.PurchaseVoid,
            "PurchaseVoid",
            purchaseVoidId,
            performedByUserId,
            occurredAt);
    }

    public static InventoryMovement CreateAdjustment(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantityDelta,
        decimal inventoryValueDelta,
        decimal unitCost,
        CostReliability costReliability,
        string reason,
        Guid adjustmentId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        var movement = CreateInventoryAdjustment(
            storeId,
            warehouseId,
            productId,
            quantityDelta,
            inventoryValueDelta,
            unitCost,
            InventoryMovementType.Adjustment,
            "StockAdjustment",
            adjustmentId,
            performedByUserId,
            occurredAt);
        movement.CostReliability = costReliability;
        movement.Reason = reason;
        return movement;
    }

    public static InventoryMovement CreateStocktakeAdjustment(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantityDelta,
        decimal inventoryValueDelta,
        decimal unitCost,
        CostReliability costReliability,
        string? note,
        decimal expectedQuantity,
        decimal countedQuantity,
        Guid stocktakeId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        var movement = CreateInventoryAdjustment(
            storeId,
            warehouseId,
            productId,
            quantityDelta,
            inventoryValueDelta,
            unitCost,
            InventoryMovementType.StocktakeAdjustment,
            "StocktakeResult",
            stocktakeId,
            performedByUserId,
            occurredAt);
        movement.CostReliability = costReliability;
        movement.Reason = note;
        movement.StocktakeExpectedQuantity = expectedQuantity;
        movement.StocktakeCountedQuantity = countedQuantity;
        return movement;
    }

    private static InventoryMovement CreateInventoryAdjustment(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantityDelta,
        decimal inventoryValueDelta,
        decimal unitCost,
        InventoryMovementType movementType,
        string sourceType,
        Guid sourceId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        if (quantityDelta == 0 || unitCost < 0)
        {
            throw new DomainRuleException(
                "invalid-inventory-adjustment-movement",
                "Inventory adjustment movement values are invalid.");
        }

        if ((quantityDelta > 0 && inventoryValueDelta < 0)
            || (quantityDelta < 0 && inventoryValueDelta > 0))
        {
            throw new DomainRuleException(
                "invalid-inventory-adjustment-value-direction",
                "Inventory adjustment quantity and value must have matching directions.");
        }

        return new InventoryMovement(
            Guid.NewGuid(),
            storeId,
            warehouseId,
            productId,
            quantityDelta,
            inventoryValueDelta,
            unitCost,
            movementType,
            sourceType,
            sourceId,
            performedByUserId,
            occurredAt);
    }

    private static InventoryMovement CreateInboundCorrection(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        decimal quantity,
        decimal inventoryValue,
        decimal unitCost,
        InventoryMovementType movementType,
        string sourceType,
        Guid sourceId,
        Guid performedByUserId,
        DateTimeOffset occurredAt)
    {
        if (quantity <= 0 || inventoryValue < 0 || unitCost < 0)
        {
            throw new DomainRuleException(
                "invalid-inbound-correction-movement",
                "Inbound correction movement quantity and value are invalid.");
        }

        return new InventoryMovement(
            Guid.NewGuid(),
            storeId,
            warehouseId,
            productId,
            quantity,
            inventoryValue,
            unitCost,
            movementType,
            sourceType,
            sourceId,
            performedByUserId,
            occurredAt);
    }
}
