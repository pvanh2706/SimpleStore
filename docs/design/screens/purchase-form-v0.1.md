# Create and Edit Purchase v0.1

**Status:** Approved visual/product UX direction — D-098. **Redesign implementation:** NOT STARTED.

![Approved staged Purchase form reference](../references/purchase-management-v0.1.png)

The logical flow is **Nhà cung cấp → Sản phẩm → Thanh toán → Xác nhận**. A future implementation may use a stepper, full page or drawer, provided that sequence and the final action's meaning remain clear. Creating/saving line details first produces an editable **Draft**; moving between steps does not complete Purchase or post inventory. Completion is a separate authoritative operation.

## 1. Nhà cung cấp

Select an active Supplier. Show name, phone and current Supplier outstanding amount where available, with clear labels. Supplier management navigation follows existing Owner-only permissions. The current Supplier contract has no structured address; the address in the image is illustrative. Do not establish a fixed Product → Supplier relationship from this flow.

## 2. Sản phẩm

Search active Products by supported name/SKU/barcode identifiers. Each line shows Product, its current unit snapshot/string, decimal quantity, **Giá nhập** (transaction unit price) and calculated line amount. One Product may appear only once in a Purchase (D-020); prevent and explain duplicate selection. Quantity and money precision/rounding follow D-019: quantity `decimal(18,3)`, unit price/line amount/money `decimal(18,2)`, line rounding away from zero and total as sum of lines. The frontend amount is a preview; backend is authoritative. **Giá nhập** is not Product sale price, reference purchase cost or current moving average cost. No multi-unit/package conversion is approved here.

## 3. Thanh toán

Choices **Thanh toán toàn bộ**, **Thanh toán một phần** and **Chưa thanh toán** are UX helpers for composing completion input, not Purchase statuses. Completion accepts zero or more original `PurchasePayment` entries with explicit amount and supported `Cash`/`Transfer` method. For partial payment, show the amount/method and remaining Supplier obligation; for no payment, initial paid amount is zero and the full obligation remains. Do not add a generic `Credit` payment method. The current model and frontend can hold a list of payments; the mockup does not require a new split-payment/orchestration engine. Technical review should pin down the exact UI when multiple entries are presented. Prevent overpayment under the backend's authoritative rules.

The mockup's **Giảm giá** line is illustrative only. Purchase-level or line discounts, discount accounting, `DiscountAmount` and `DiscountPercent` are **NOT approved for implementation under D-098** and require separate Product Owner domain review.

## 4. Xác nhận and finality

Review Supplier, Product lines, quantities, Purchase prices, authoritative total, initial payment entries/methods and resulting Supplier debt before completion. The primary action must communicate finality, for example **Hoàn tất phiếu nhập**; a label that looks like merely saving a Draft is insufficient. Completion remains responsible for immutable InventoryMovements, balances, moving average, reference purchase cost and debt effects. Preserve `OperationId` idempotency and exact-attempt recovery when an outcome is uncertain.

Only Draft can be edited, including Supplier and lines. Preserve selected Supplier/Products even if they are not on the first search page. Completed Purchase remains read-only; corrections use the approved [Void interaction](purchase-void-v0.1.md). Purchase-level note and editable document/business date shown in the image are `FUTURE / REQUIRES SEPARATE REVIEW`; current Purchase write/complete contracts contain neither. Supplier `Note` is a separate Supplier field. An explicit Purchase date could affect inventory, costing, debt, Today/End-of-day and corrections and cannot be inferred from the mockup.

Design validation, Supplier/Product search errors, Draft save failure, uncertain completion and immutable retry/recovery states explicitly. Backend timestamps and Store-local reporting windows remain authoritative.
