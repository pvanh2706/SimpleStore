# End-of-day Explainability v0.1

**Status:** Approved visual/product UX direction — D-101, reusing [D-096 Explainability Pattern](explainability-pattern-v0.1.md). **Redesign implementation:** NOT STARTED.

![Approved End-of-day explanation reference](../references/end-of-day-v0.1.png)

A compact **Cách tính** action may open a focused panel for Revenue, Net Collected, both ending debts, Supplier Payments, Historical COGS and Estimated Gross Profit. For each, answer: what the number means; its authoritative formula; the Store-local interval or historical cutoff; included and excluded events; safe source navigation where available; and any reliability or API limitation. Use backend `businessDate`, `timeZoneId`, `startUtc` and `endUtc` to display **Từ** and **Đến trước** in the Store timezone. Browser-local now is not the boundary.

| Metric | Explanation that preserves current semantics |
| --- | --- |
| Revenue | Economic value from Completed Sales with Return and Sale Void corrections by approved event dates. Payments and debt creation do not create Revenue. Describe the backend projection; do not independently recalculate it in the panel. |
| Net Collected | `Sale Payments + Customer Debt Payments − Actual Customer Refunds`. Standalone debt collection is actual money received, not Revenue; debt creation is not Collected. |
| Customer/Supplier debt at end | Aggregate balance at the strict historical `endUtc` cutoff. It is not current debt, new debt created during the selected date, Sale/Purchase value or a cash flow. Explain contributing obligations, payments and corrections without implying invoice allocation. |
| Supplier Payments | `Purchase Payments + Supplier Debt Payments`, actual money out. A Supplier DebtPayment does not change Purchase cost/value. |
| Historical COGS | SaleLine cost snapshots with approved Return/Void adjustments; not a recomputation using current Product average cost. |
| Estimated Gross Profit | `Net Sales Revenue − Historical COGS`, with `CostReliability` visible as `Reliable`, `Estimated` or `Unavailable`. Describe uncertain cost plainly. Excludes salary, rent, electricity, depreciation, tax and other operating expenses; not net/accounting profit. |

Explanations describe the authoritative backend calculation and must not become a second calculation engine. Headline and details should reconcile to the response. If typed evidence is not exposed, state that the report is an aggregate and omit any dead **Xem giao dịch** link; do not imply a partial page proves the whole total. Future drill-down must use typed source IDs with authorization and Store isolation, not localized descriptions. Revenue and money movement remain distinct even if their sample mock values happen to be close.

The image's example equation, sample rows, method chart, Category bars, invoice count, **Công nợ phát sinh** and “đã đóng” marker are illustrative. The first three need separate API/semantic or Category review as specified in [Detail](end-of-day-detail-v0.1.md); a historical debt-created metric cannot be silently inferred from ending debt. A close marker has no current domain state under D-048. State unavailable evidence and CostReliability limitations openly rather than hiding them in hover-only text.
