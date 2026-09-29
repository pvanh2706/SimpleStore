# Debt Explainability v0.1

**Status:** Approved visual/product UX direction — D-100, reusing the [D-096 Explainability Pattern](explainability-pattern-v0.1.md). **Redesign implementation:** NOT STARTED.

![Approved debt explanation reference](../references/debt-management-v0.1.png)

An explanation for current Customer or Supplier outstanding should answer: **Còn nợ bao nhiêu? Số này tính đến lúc nào? Giao dịch nào tăng/giảm nợ? Thu/trả nợ ảnh hưởng thế nào? Return/Void ảnh hưởng thế nào? Xem dữ liệu nguồn ở đâu?** Show the authoritative backend `asOf` where it materially improves trust. Current debt uses committed history, while historical/as-of reports use their approved cutoff semantics; do not replace backend `asOf` with browser-local now or silently mix current and historical scopes.

Use the conceptual description:

```text
Công nợ hiện tại
= Nghĩa vụ phát sinh hợp lệ
- Các khoản thanh toán làm giảm nghĩa vụ
- Các điều chỉnh nghĩa vụ hợp lệ
```

Each term must be defined against typed authoritative events. The explanation is **not** a second calculation engine and must not flatten all behavior into `Tổng phát sinh − Đã thanh toán` without defining direct payments, standalone DebtPayments, Return/Void effects, refunds and time scope. Customer side includes Sale obligation after original payments, aggregate standalone collections, Return obligation reduction, Sale Void and actual refund implications. Supplier side includes Purchase obligation after original payments, standalone settlements and Purchase Void. A Customer DebtPayment is not Revenue; a Supplier DebtPayment does not change Purchase cost/value. Payments remain party-level, not FIFO/invoice allocated (D-043–D-047, D-056).

When a safe backend reference exists, drill down to Sale, Return, Purchase or correction evidence with authorization and Store isolation. A future full per-party ledger must reconcile to the authoritative balance and define event sign, pagination/order and cutoff before it can power the detailed panel. Avoid a link that promises unavailable source data. Source navigation must use typed references, not parsed Vietnamese text.

The image's example amounts, “tổng phát sinh/đã thanh toán”, overdue counts, running balances and dated rows are not expected test values or approved debt formulas. Due date, aging and overdue explanation are **NOT APPROVED FOR IMPLEMENTATION** under D-100 and require separate domain review. Keep the default workspace calm: use a short **Giải thích công nợ** action and a focused contextual panel for the full explanation.
