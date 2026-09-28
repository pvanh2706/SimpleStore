# Product Inventory Actions and History v0.1

**Status:** Approved visual/product UX direction — D-097. **Redesign implementation:** NOT STARTED. Existing PR-A Stock Adjustment and Stocktake implementation remains approved under D-093.

![Approved Product inventory action states](../references/product-management-states-v0.1.png)

## Stock Adjustment

Use a focused modal/drawer with visible Product identity, signed quantity delta and a required reason. Explain that a positive delta increases stock and a negative delta decreases it; show the resulting quantity before confirmation where safely available. Ask for `AdjustmentUnitCost` only when the approved costing contract requires it. Preserve Owner-only mutation, Store/Main Warehouse scope, `OperationId` idempotency, immutable StockAdjustment source and typed `Adjustment` InventoryMovement, moving weighted average, InventoryValue and `CostReliability` (D-087, D-093). Positive adjustment without reliable current cost needs explicit adjustment unit cost; never silently fall back to `ReferencePurchaseCost`. Negative adjustment follows the approved reliability/cost resolution and cannot retroactively revalue earlier records.

## Stocktake / Kiểm kho

Present a three-step flow: **Nhập số đếm → Xem chênh lệch → Xác nhận**. Show Product, expected/current quantity, counted quantity, difference, note and a cost input only where required. Stocktake uses its own immutable StocktakeResult and typed `StocktakeAdjustment` movement for nonzero differences; it is not a generic Adjustment in disguise. The cost of every difference follows D-087/D-088. A zero-difference result does not create a fake movement. Reject invalid negative counted quantities according to the existing contract.

Preserve D-089 stale protection: submission carries expected balance revision, and if inventory changed after context load, show the typed `409 stocktake-stale` conflict with safe expected/current information. The Owner must refresh/recount and submit a new attempt; never silently apply the old difference or overwrite a newer movement. Handle uncertain idempotent-operation outcomes with recovery/retry guidance instead of implying the operation failed or succeeded without evidence.

## Immutable Inventory Movement History

Show Product identity, a paginated/scrollable movement list, quantity delta, unit cost, timestamp, actor, source/reason, and balance/resulting context where the backend supports it. Date-range and movement-type filters are visual direction only where server support exists. Preserve existing movement concepts: `OpeningBalance`, `Purchase`, `Sale`, `ReturnRestock`, `SaleVoid`, `PurchaseVoid`, `Adjustment` and `StocktakeAdjustment`. Movement cost/reliability and source links should be understandable through the [D-096 Explainability Pattern](explainability-pattern-v0.1.md) when derived values need it. The ledger is immutable: do not add edit/delete actions to historical movements.

All mutation actions are Owner-only under existing contracts; read visibility follows current authorization and Store isolation. Explicitly design loading, no movements, API error, validation error, stale Stocktake and uncertain retry states. On mobile use a sequential Product → Inventory Action/History flow rather than compressing the desktop layout.
