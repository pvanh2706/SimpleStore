namespace SimpleStore.Domain.Inventory;

public enum InventoryMovementType
{
    OpeningBalance = 1,
    Purchase = 2,
    Sale = 3,
    ReturnRestock = 4,
    SaleVoid = 5,
    PurchaseVoid = 6,
    Adjustment = 7,
    StocktakeAdjustment = 8
}
