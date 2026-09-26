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
        HasAverageCost = openingInventory.HasStock;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ProductId { get; private set; }

    public decimal QuantityOnHand { get; private set; }

    public decimal InventoryValue { get; private set; }

    public decimal AverageCost { get; private set; }

    public bool HasAverageCost { get; private set; }

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
        => ReceiveInbound(quantity, inventoryValue, updatedAt);

    public void ReceiveInbound(decimal quantity, decimal inventoryValue, DateTimeOffset updatedAt)
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
        if (newQuantity > 0 && newValue >= 0)
        {
            AverageCost = Math.Round(newValue / newQuantity, 4, MidpointRounding.AwayFromZero);
            HasAverageCost = true;
        }
        else if (newQuantity > 0)
        {
            HasAverageCost = false;
        }

        UpdatedAt = updatedAt;
    }

    public void RestoreExact(
        decimal quantity,
        decimal inventoryValue,
        decimal averageCost,
        bool hasAverageCost,
        DateTimeOffset updatedAt)
    {
        if (averageCost < 0)
        {
            throw new DomainRuleException(
                "invalid-average-cost",
                "Average cost cannot be negative.");
        }

        QuantityOnHand = quantity;
        InventoryValue = inventoryValue;
        AverageCost = averageCost;
        HasAverageCost = hasAverageCost;
        UpdatedAt = updatedAt;
    }

    public SaleCost ResolveSaleCost(decimal? referencePurchaseCost)
    {
        if (HasAverageCost)
        {
            return new SaleCost(AverageCost, CostReliability.Reliable);
        }

        return referencePurchaseCost.HasValue
            ? new SaleCost(referencePurchaseCost.Value, CostReliability.Estimated)
            : new SaleCost(0, CostReliability.Unavailable);
    }

    public void IssueSale(decimal quantity, decimal inventoryValue, DateTimeOffset updatedAt)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("invalid-sale-quantity", "Sale quantity must be greater than zero.");
        }

        if (inventoryValue < 0)
        {
            throw new DomainRuleException("invalid-sale-value", "Sale inventory value cannot be negative.");
        }

        QuantityOnHand -= quantity;
        InventoryValue -= inventoryValue;
        UpdatedAt = updatedAt;
    }

    public void ApplyInventoryAdjustment(
        decimal quantityDelta,
        decimal inventoryValueDelta,
        decimal effectiveUnitCost,
        bool establishesReliableBasis,
        DateTimeOffset updatedAt)
    {
        if (quantityDelta == 0)
        {
            throw new DomainRuleException(
                "adjustment-quantity-required",
                "Adjustment quantity must not be zero.");
        }

        if (effectiveUnitCost < 0)
        {
            throw new DomainRuleException(
                "invalid-adjustment-unit-cost",
                "Adjustment unit cost cannot be negative.");
        }

        var wasReliable = HasAverageCost;
        QuantityOnHand += quantityDelta;
        InventoryValue += inventoryValueDelta;

        if (quantityDelta > 0 && establishesReliableBasis)
        {
            AverageCost = effectiveUnitCost;
            HasAverageCost = true;
        }
        else if (!wasReliable)
        {
            HasAverageCost = false;
        }

        UpdatedAt = updatedAt;
    }
}

public sealed record SaleCost(decimal UnitCost, CostReliability Reliability);
