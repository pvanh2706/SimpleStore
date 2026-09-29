# Inventory Movement History v0.1

**Status:** Product-level history exists. Whole-inventory history is **DESIGN DIRECTION APPROVED — D-099 / API + TECHNICAL IMPLEMENTATION NOT STARTED**.

![Approved Inventory Movement History reference](../references/inventory-management-v0.1.png)

Current Product detail exposes immutable movements for one Product. The approved future whole-inventory workspace would support audit and trust across Products. Potential columns are timestamp, Product, movement type, signed quantity delta, unit cost, inventory-value delta, source/reason and actor, with resulting balance context only where an authoritative backend field supports it. Preserve these existing movement types: `OpeningBalance`, `Purchase`, `Sale`, `ReturnRestock`, `SaleVoid`, `PurchaseVoid`, `Adjustment`, `StocktakeAdjustment`. No edit/delete action belongs on historical movements.

There is no established global/all-Product InventoryMovement list endpoint. Before coding it, separately review Store/Main Warehouse scope, stable pagination/order, Product/date/type filters, source navigation, performance/indexes and authorization. The picture's date range, movement-type selector and pagination do not create server support on their own.

Where supported, typed source references may navigate safely to Purchase, Sale, Return, Product or correction evidence. Keep Store isolation and route authorization. Never parse localized display text to decide navigation. Use the [D-096 Explainability Pattern](explainability-pattern-v0.1.md) for balance/value and CostReliability when users need source evidence. Distinguish missing/partial history from “no movements”; show loading, empty and API-failure states explicitly.

Inventory corrections must continue through approved source transactions. The history view is read-only and must never expose direct edits to `InventoryBalance.QuantityOnHand`, InventoryValue or AverageCost or suggest retroactive revaluation of historical Sale cost snapshots.
