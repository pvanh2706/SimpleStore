# Purchase Detail v0.1

**Status:** Approved visual/product UX direction — D-098. **Redesign implementation:** NOT STARTED.

![Approved Purchase Detail reference](../references/purchase-management-v0.1.png)

Use **Tổng quan**, **Sản phẩm**, **Thanh toán** and **Lịch sử/correction evidence** where supported. Show Supplier, `Draft|Completed` lifecycle, separate Void state, created/completed times, Product lines and price/quantity snapshots, total, original Purchase payments, original paid amount and applicable outstanding contribution. A Completed Purchase is read-only historical evidence; it cannot be edited or hard-deleted. Draft has the supported edit/complete actions. Preserve Store isolation and Owner-only Purchase access.

## Payment and debt wording

Clearly distinguish **Tổng tiền**, **Đã thanh toán trong phiếu** and **Còn nợ theo phiếu** from the **current aggregate Supplier outstanding debt**. The Purchase API's `paidAmount` is the sum of original `PurchasePayment` rows. Its `outstandingAmount` is the Purchase's original total minus original payments (or zero after Void); standalone `SupplierDebtPayment` changes aggregate Supplier debt through separate history and is not allocated to a particular Purchase under D-047. Do not claim a Purchase's outstanding field is current invoice-specific debt after standalone repayments. Use the [D-096 Explainability Pattern](explainability-pattern-v0.1.md) when these figures might be confused.

Completed Purchase details keep original lines/payments visible after correction. If voided, show **Đã hủy**, reason, time, actor where available and linked Void evidence; do not rewrite original payments or invent a refund transaction. Inventory balances, moving average, Product reference purchase cost and supplier obligation may be affected by completion/Void, but cannot be directly edited from history.

The pictured **In phiếu** is `FUTURE / REQUIRES SEPARATE REVIEW`; Purchase printing is not an approved requirement under D-098 and is separate from 80 mm Sale receipt printing. The pictured Purchase note and editable document date are also future/review-only because current Purchase contracts do not contain those fields. Show loading, not-found/permission, read failure and recovery states intentionally. The [Void spec](purchase-void-v0.1.md) governs the destructive correction action.
