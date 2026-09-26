namespace SimpleStore.Domain.Inventory;

public static class InventoryAdjustmentCostResolver
{
    public static InventoryAdjustmentCost Resolve(
        InventoryBalance balance,
        decimal quantityDelta,
        decimal? adjustmentUnitCost,
        decimal? referencePurchaseCost)
    {
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

        if (quantityDelta == 0)
        {
            throw new DomainRuleException(
                "adjustment-quantity-required",
                "Adjustment quantity must not be zero.");
        }

        if (adjustmentUnitCost < 0)
        {
            throw new DomainRuleException(
                "invalid-adjustment-unit-cost",
                "Adjustment unit cost cannot be negative.");
        }

        if (quantityDelta > 0)
        {
            if (balance.HasAverageCost)
            {
                if (adjustmentUnitCost.HasValue)
                {
                    throw new DomainRuleException(
                        "adjustment-unit-cost-not-allowed",
                        "Adjustment unit cost is not accepted when the balance has a reliable average cost.");
                }

                return Create(
                    quantityDelta,
                    balance.AverageCost,
                    CostReliability.Reliable,
                    establishesReliableBasis: false);
            }

            if (!adjustmentUnitCost.HasValue)
            {
                throw new DomainRuleException(
                    "adjustment-unit-cost-required",
                    "Adjustment unit cost is required when the current cost basis is not reliable.");
            }

            var cleanZeroBalance = balance.QuantityOnHand == 0 && balance.InventoryValue == 0;
            return Create(
                quantityDelta,
                adjustmentUnitCost.Value,
                CostReliability.Reliable,
                cleanZeroBalance);
        }

        if (adjustmentUnitCost.HasValue)
        {
            throw new DomainRuleException(
                "adjustment-unit-cost-not-allowed",
                "Adjustment unit cost is only accepted for a positive adjustment without reliable cost.");
        }

        var resolved = balance.ResolveSaleCost(referencePurchaseCost);
        return Create(quantityDelta, resolved.UnitCost, resolved.Reliability, false);
    }

    private static InventoryAdjustmentCost Create(
        decimal quantityDelta,
        decimal unitCost,
        CostReliability reliability,
        bool establishesReliableBasis)
    {
        decimal inventoryValueDelta;
        try
        {
            inventoryValueDelta = Math.Round(
                quantityDelta * unitCost,
                2,
                MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            throw new DomainRuleException(
                "invalid-adjustment-inventory-value",
                "Adjustment inventory value is outside the supported range.");
        }

        InventoryStoragePrecision.EnsureInventoryValue(
            inventoryValueDelta,
            "invalid-adjustment-inventory-value",
            "Adjustment inventory value");
        return new InventoryAdjustmentCost(
            unitCost,
            inventoryValueDelta,
            reliability,
            establishesReliableBasis);
    }
}

public sealed record InventoryAdjustmentCost(
    decimal UnitCost,
    decimal InventoryValueDelta,
    CostReliability Reliability,
    bool EstablishesReliableBasis);
