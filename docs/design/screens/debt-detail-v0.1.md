# Debt Party Detail v0.1

**Status:** Approved visual/product UX direction — D-100. **Redesign implementation:** NOT STARTED.

![Approved Customer and Supplier debt detail reference](../references/debt-management-v0.1.png)

Debt-party detail may show party identity, phone, current outstanding, exact backend `asOf`, recent contributing transactions **where supported**, standalone DebtPayment history, [explanation](debt-explainability-v0.1.md) and one direction-specific primary action: **+ Thu tiền** for Customer, **+ Trả tiền** for Supplier. Keep the current balance prominent and distinguish it from historical document totals or Today new-debt-created metrics. Customer and Supplier authorization remain D-051/D-052, with Store isolation on every source record.

Current Customer contract contains name and optional phone; Supplier has name, optional phone/note and active state. The pictured structured address, Customer group/type and party classifications are `FUTURE / REQUIRES SEPARATE REVIEW`. Do not turn Supplier note into a structured address or add Customer metadata by inference from the image.

The history direction helps users see which approved events changed debt. Customer side may involve Completed Sale obligation and original payment, Return obligation reduction/actual refund, Sale Void and standalone Customer DebtPayment. Supplier side may involve Completed Purchase obligation and original payment, Purchase Void and standalone Supplier DebtPayment. The mockup's transaction rows, running balances and document identifiers are illustrative. A complete generic per-party ledger API is **not** currently established; before building one, review source events, pagination/order, current versus historical `asOf`, Return/Void signs, reconciliation, source navigation, authorization and performance. Never imply that a standalone payment was allocated to a specific invoice because it appears beside it chronologically.

Where safe typed references exist, source drill-down may lead to Sale, Return, Purchase or Void/correction evidence. Do not parse localized descriptions for navigation. Show no outstanding debt, no accessible history, loading, missing party, permission denial and API failure as distinct states. Avoid a negative “running debt” display from sample rows unless an authoritative reconciled ledger model explicitly supports its meaning; aggregate debt is non-negative under D-056.
