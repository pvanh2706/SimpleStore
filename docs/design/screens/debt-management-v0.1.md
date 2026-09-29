# Debt Management v0.1

**Status:** Product Owner-approved visual/product UX direction — D-100, 2026-09-29. **Debt UI redesign:** NOT STARTED.

![Approved Debt Management visual and interaction reference](../references/debt-management-v0.1.png)

The approved image gives a unified **Công nợ** workspace with distinct **Khách hàng** and **Nhà cung cấp** modes. It is an official visual/interaction reference; example amounts, transactions, dates, overdue labels and fields are illustrative, not new domain or API contracts.

## Reading order and scope

For future implementation, review: (1) approved Sale/Purchase/Debt/Return/Void decisions and current contracts; (2) this overview and the applicable [List/Filter](debt-list-v0.1.md), [Party Detail](debt-detail-v0.1.md), [Debt Payment](debt-payment-v0.1.md) and [Debt Explainability](debt-explainability-v0.1.md) specs; (3) the [approved image](../references/debt-management-v0.1.png). Domain behavior wins on conflict. Unsupported pictured capabilities remain future direction pending separate review.

The workspace answers **“Ai đang còn nợ / mình đang nợ ai, bao nhiêu và cần xem gì tiếp theo?”** Customer mode uses **Thu tiền** (money in); Supplier mode uses **Trả tiền** (money out). Do not collapse them into an ambiguous generic “Thanh toán” action. Customer debt read/collection is permitted for Owner and Cashier under D-051. Supplier debt read/settlement is Owner-only under D-052. Every route and operation preserves Store isolation.

## Current authority

Debt is derived from committed Sale/Purchase, actual payments and Return/Void/correction history; it is never an editable balance field (D-043). Current debt APIs provide Customer/Supplier outstanding lists and a per-party balance with `outstandingAmount` and backend `asOf`; lists support search and paging. Standalone DebtPayment supports `Cash`/`Transfer`, optional immutable note, `occurredAt`, actor, before/after outstanding, client OperationId, idempotent exact retry and expected-outstanding concurrency protection. Aggregate debt cannot go negative. Customer collection is actual money received, **not new Revenue**; Supplier settlement is actual money paid and **does not change Purchase value/cost**. Neither is FIFO/invoice allocated (D-044–D-047, D-055–D-056).

The current backend does not establish due date, payment terms, overdue/aging rules, historical total-incurred/total-paid aggregates or a complete generic per-party debt ledger matching every mock row. D-100 approves presentation direction only. Due/overdue logic is **NOT APPROVED FOR IMPLEMENTATION / REQUIRES SEPARATE DOMAIN REVIEW**. Detailed generic history is **DESIGN DIRECTION APPROVED / API + TECHNICAL IMPLEMENTATION PENDING WHERE NOT CURRENTLY EXPOSED**. No `Customer.OutstandingAmount`, `Supplier.OutstandingAmount` or `DebtBalance` direct-edit control may be added.

Design no debt, no search match, loading, API failure, missing party, zero outstanding, validation, concurrent balance change, ambiguous operation, permission denial and safe retry explicitly. The current `DebtManagementView.vue` predates D-100. Later implementation may refine spacing, typography, table/drawer size, responsive behavior, keyboard use, accessibility and wording clarity; it must preserve money direction, non-allocation, non-negative debt, Return/Void effects, idempotency/concurrency safeguards and permissions. Material behavior changes require Product Owner review.
