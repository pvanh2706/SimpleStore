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
        MovementType = InventoryMovementType.OpeningBalance;
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
            sourceType,
            sourceId,
            performedByUserId,
            occurredAt);
    }
}
