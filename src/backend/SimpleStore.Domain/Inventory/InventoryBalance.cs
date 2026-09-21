namespace SimpleStore.Domain.Inventory;

public sealed class InventoryBalance
{
    private InventoryBalance()
    {
    }

    private InventoryBalance(
        Guid id,
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        OpeningInventory openingInventory,
        DateTimeOffset updatedAt)
    {
        Id = id;
        StoreId = storeId;
        WarehouseId = warehouseId;
        ProductId = productId;
        QuantityOnHand = openingInventory.Quantity;
        InventoryValue = openingInventory.InventoryValue;
        AverageCost = openingInventory.AverageCost;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ProductId { get; private set; }

    public decimal QuantityOnHand { get; private set; }

    public decimal InventoryValue { get; private set; }

    public decimal AverageCost { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static InventoryBalance Create(
        Guid storeId,
        Guid warehouseId,
        Guid productId,
        OpeningInventory openingInventory,
        DateTimeOffset updatedAt) =>
        new(Guid.NewGuid(), storeId, warehouseId, productId, openingInventory, updatedAt);

    public void ReceivePurchase(decimal quantity, decimal inventoryValue, DateTimeOffset updatedAt)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("invalid-purchase-quantity", "Purchase quantity must be greater than zero.");
        }

        if (inventoryValue < 0)
        {
            throw new DomainRuleException("invalid-purchase-value", "Purchase inventory value cannot be negative.");
        }

        var newQuantity = QuantityOnHand + quantity;
        var newValue = InventoryValue + inventoryValue;
        QuantityOnHand = newQuantity;
        InventoryValue = newValue;
        AverageCost = Math.Round(newValue / newQuantity, 4, MidpointRounding.AwayFromZero);
        UpdatedAt = updatedAt;
    }
}
