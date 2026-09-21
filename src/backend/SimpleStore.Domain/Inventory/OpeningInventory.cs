namespace SimpleStore.Domain.Inventory;

public sealed record OpeningInventory
{
    private OpeningInventory(decimal quantity, decimal? unitCost)
    {
        Quantity = quantity;
        UnitCost = unitCost;
        InventoryValue = quantity * (unitCost ?? 0);
    }

    public decimal Quantity { get; }

    public decimal? UnitCost { get; }

    public decimal InventoryValue { get; }

    public decimal AverageCost => Quantity > 0 ? UnitCost!.Value : 0;

    public bool HasStock => Quantity > 0;

    public static OpeningInventory Create(decimal quantity, decimal? unitCost)
    {
        if (quantity < 0)
        {
            throw new DomainRuleException(
                "invalid-opening-quantity",
                "Opening quantity cannot be negative.");
        }

        if (unitCost < 0)
        {
            throw new DomainRuleException(
                "invalid-opening-cost",
                "Opening cost cannot be negative.");
        }

        if (quantity > 0 && unitCost is null)
        {
            throw new DomainRuleException(
                "opening-cost-required",
                "Opening cost is required when opening quantity is greater than zero.");
        }

        return new OpeningInventory(quantity, unitCost);
    }
}
