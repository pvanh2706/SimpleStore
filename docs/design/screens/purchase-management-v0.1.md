# Purchase Management v0.1

**Status:** Product Owner-approved visual/product UX direction — D-098, 2026-09-29. **Purchase UI redesign:** NOT STARTED.

![Approved Purchase Management visual and interaction reference](../references/purchase-management-v0.1.png)

The approved image covers Purchase List/Filter/Detail, staged Create Purchase, Edit Draft, payment/debt presentation, Void and loading/empty/error states. It is the official visual/interaction reference, but sample dates, amounts, status labels and controls do not create domain fields or API capabilities.

## Read before implementation

1. Approved Purchase/Inventory/Debt domain decisions, especially D-018–D-021, D-039–D-040, D-043–D-047 and D-056, plus current code contracts.
2. This overview and the relevant written spec: [List and Filter](purchase-list-v0.1.md), [Create/Edit Form](purchase-form-v0.1.md), [Detail](purchase-detail-v0.1.md), [Void](purchase-void-v0.1.md).
3. The [approved Purchase image](../references/purchase-management-v0.1.png).

Domain behavior wins if the image differs. Unsupported pictured capabilities remain illustrative/future direction. D-098 is a design/product-direction approval, not backend, schema, domain or UI implementation completion.

## Authoritative Purchase model

The lifecycle is `Draft → Completed` (D-018); `isVoided` and Void evidence are separate. Supplier, Purchase lines, Product quantity/unit price/line amount, total, a list of `PurchasePayment` entries (`Cash` or `Transfer`), paid/outstanding amounts and the immutable inventory/cost effects of completion already exist. Only Draft can be edited; Completed Purchase is historical evidence. Completion and Void use their approved idempotency, correction and recovery contracts. Supplier debt is derived from transaction and actual payment history, with no invoice allocation/FIFO.

Payment descriptions such as **Chưa thanh toán**, **Thanh toán một phần** and **Đã thanh toán** may be display labels for a non-voided Completed Purchase using authoritative total, original paid and outstanding amounts. They are **not** persisted Purchase lifecycle values. A voided Purchase stays visibly distinct. Do not present unpaid debt as a fake `Credit` payment method.

The image's Purchase discount, Purchase-level note, editable document date, structured Supplier address and **In phiếu** are not established by the current Purchase/Supplier contracts; they need separate review as described in the linked specs. Existing Sale receipt printing does not approve Purchase printing.

## Working principles

The table and forms should make Supplier, lines, total, original payments and remaining obligation easy to scan with tabular numerals. Keep one clear primary action per step and separate destructive Void. Show loading, empty list, no filter match, Supplier/Product search failure, validation, Draft save failure, uncertain completion, typed Void conflict and permission failure as explicit states. Preserve Owner-only Purchase/Supplier mutation, Store isolation and backend authority.

Desktop favors an efficient list and readable detail; tablet can use a full panel/stepper; mobile uses a sequential flow instead of squeezed desktop tables. During later implementation, small changes to spacing, typography, widths, modal/drawer dimensions, responsive behavior, keyboard navigation and accessibility are allowed. They must preserve the approved logical workflow, action hierarchy, lifecycle, payment/debt semantics and Void safeguards. Material capability or workflow changes require Product Owner review.
