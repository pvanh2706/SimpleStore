namespace SimpleStore.Domain.Inventory;

public static class InventoryStoragePrecision
{
    public const decimal MaxQuantity = 999_999_999_999_999.999m;
    public const decimal MaxUnitCost = 99_999_999_999_999.9999m;
    public const decimal MaxInventoryValue = 9_999_999_999_999_999.99m;

    public static void EnsureQuantity(decimal value, string code, string fieldName) =>
        EnsureExact(value, 3, MaxQuantity, code, fieldName, "decimal(18,3)");

    public static void EnsureUnitCost(decimal value, string code, string fieldName) =>
        EnsureExact(value, 4, MaxUnitCost, code, fieldName, "decimal(18,4)");

    public static void EnsureInventoryValue(decimal value, string code, string fieldName) =>
        EnsureExact(value, 2, MaxInventoryValue, code, fieldName, "decimal(18,2)");

    private static void EnsureExact(
        decimal value,
        int scale,
        decimal maximumMagnitude,
        string code,
        string fieldName,
        string storageType)
    {
        if (value < -maximumMagnitude
            || value > maximumMagnitude
            || decimal.Round(value, scale, MidpointRounding.ToEven) != value)
        {
            throw new DomainRuleException(
                code,
                $"{fieldName} must fit {storageType} exactly without rounding.");
        }
    }
}
