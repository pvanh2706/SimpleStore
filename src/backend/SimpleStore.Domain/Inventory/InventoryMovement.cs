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
}
