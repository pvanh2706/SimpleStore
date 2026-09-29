# Inventory Overview and Product Detail v0.1

**Status:** Approved visual/product UX direction — D-099. **Dedicated Inventory UI/API implementation:** NOT STARTED; separate review required.

![Approved Inventory overview and detail reference](../references/inventory-management-v0.1.png)

## Overview and factual state

Use compact headline facts, fast Product search, important inventory attention, a scan-friendly table and direct safe actions. Possible summary cards include active Product count, zero-stock Product count, negative-stock Product count and Products with approved C14 attention. Keep definitions and denominator explicit. **Hết hàng** may describe a factual `QuantityOnHand == 0`, and **Tồn âm** a factual `QuantityOnHand < 0`, when consistent with the authoritative balance. These stock facts are distinct from C14 `OutOfStock`/`NegativeStock` attention: C14 also requires the approved recent positive net-sales evidence and active-Product rules. Do not equate a factual zero/negative count with the number of C14 signals.

**Sắp hết hàng** must use approved C14 `LowStockRisk` semantics where appropriate, not an invented threshold. The pictured **Tồn kho thấp** is **not** an approved standalone algorithm (for example, `quantity <= 10`); a separate threshold would need Product Owner review. No C14 signal does not prove “healthy inventory.” Prefer narrow wording such as **Không có tín hiệu cần chú ý theo C14 hiện tại** over a blanket **Bình thường** claim.

## Search, filter and list

Search by Product name, SKU or barcode using backend-supported behavior; keep barcode/keyboard interaction fast. A focused filter panel may eventually offer active/inactive, zero stock, negative stock, C14 attention kind, quantity range, inventory-value range, Category and Unit. Expose only approved data/API filters when implemented. Category and reusable Unit remain D-097 design direction with technical/domain implementation pending; current Product Unit is a string.

The desktop table may show Product, SKU, unit, current quantity, moving average cost **when reliable**, inventory value, factual/approved attention state and update context where the backend supports it. Use tabular numerals and keep long Vietnamese Product names readable. Avoid packing every Product field into each row. The current Product list API supplies search, `isActive` and paging but does not by itself provide an Inventory overview/filter/summary API.

## Product inventory detail and actions

Present Product identity, SKU, barcode, unit, active state, current Main Warehouse quantity, InventoryValue, AverageCost/`HasAverageCost`, sale price and reference purchase cost where useful, latest inventory facts, Product-level movement history and C14 evidence. Keep `ReferencePurchaseCost`, current `AverageCost` and historical Sale `UnitCostAtSale` distinct. Reuse the [D-096 Explainability Pattern](explainability-pattern-v0.1.md) for InventoryValue/AverageCost: explain current balance, cost reliability, update time and source movements where available, without implying historical revaluation.

The Inventory overview and Product detail may both lead to **one-Product** Stock Adjustment or Stocktake. The focused form follows [Product Inventory Actions](product-inventory-actions-v0.1.md): signed quantity delta, required reason, cost only when D-087 requires it, safe before/after context, Owner-only mutation and immutable movement. The image's multiple selection/whole-warehouse appearance does **not** approve one multi-Product Adjustment. Such an operation needs separate atomicity, idempotency, partial-failure, per-Product costing and audit/source decisions.

Show approved C14 `NegativeStock`, `OutOfStock` and `LowStockRisk` with **Vì sao?** and typed evidence: active Products only; current Main Warehouse balance; seven completed Store-local business days for velocity; D-071 history sufficiency; positive net-sales evidence; deterministic thresholds/order. C14 is descriptive, not AI forecasting, automatic reorder advice or validated business value. Do not change Store `allowNegativeStock` from this screen under D-099.
