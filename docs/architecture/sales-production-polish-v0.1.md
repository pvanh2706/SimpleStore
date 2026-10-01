# Sales Production Polish v0.1

## Status and authority

**Scope:** `APPROVED — D-107`, 2026-10-02, at repository baseline `a89d693c8b6441eab2d380e5c643f2f3de6a33e5`. **Sales Polish Pass 1:** `APPROVED FOR IMPLEMENTATION / NOT STARTED`. **Pass 2 and Pass 3:** planned, `NOT STARTED`.

This document records documentation/governance only. It changes no Vue, backend, domain, database, API, auth, migration or test code, and it claims no implementation, validation or visual approval.

Authority, in order:

1. **Business and behavior:** approved Product Owner business/domain decisions and current backend contracts are authoritative. In particular, [D-105](../../DECISIONS.md#d-105--technical-breakdown-ui-b-sales-redesign-approval) and its [Technical Breakdown UI-B](technical-breakdown-ui-b-v0.1.md) remain the UI-B capability boundary.
2. **Visual:** the [D-106 Sales HTML Visual Reference v1.0](../design/approved/sales-html-reference-v1.0/index.html) remains the visual source of truth for Sales. Visual-only elements in that reference do **not** become production capability.
3. **Design direction:** D-095 [Design System](../design/simple-store-design-system-v0.1.md) and [Sales screen](../design/screens/sales-screen-v0.1.md), D-103 [UI Redesign Program](../design/ui-redesign-program-v0.1.md) and D-104 UI-A shell approval remain unchanged.

D-107 resolves how the current Sales implementation, which has ported much of the D-106 visual treatment, is cleaned up so that **live mode shows only production-supported capability** while **Demo / Visual Reference mode** keeps D-106 fidelity.

## Current implementation gap

Observed in the frontend at the baseline above. This list motivates Pass 1; it is not exhaustive, and Pass 1 must re-inspect the code before changing it.

| Area | Current live behavior | D-107 handling |
| --- | --- | --- |
| Category chips | `sales/catalog.ts` infers a Category from Product-name keywords and filters by it. | Not business data. Live: no Category filter until a real Category master/domain/API is approved and implemented. Demo: chips allowed. |
| Product images | `SaleCheckoutForm.vue` shows a sample image when a live Product's SKU and name match a demo Product. | Live: neutral placeholder; no Product → demo-image mapping. Demo: sample images allowed. |
| Stock badges | `sales/catalog.ts` labels `<= 2` as `Rất ít hàng` and `<= 5` as `Sắp hết hàng`. | Not authoritative rules. Live: factual `quantityOnHand`, plus factual zero/negative state. Demo: sample badges allowed. |
| Working orders | Browser-only order book with multiple orders, `Giữ đơn` and `Danh sách đơn đang chờ`. | Live: exactly one active working order; held/waiting controls hidden. Demo only. |
| Sale note | `Ghi chú đơn hàng` field, not persisted by any API. | Live: hidden. Demo only. |
| Discounts | Browser-only item discount preview; invoice-discount control. | Live: hidden. Demo only. |
| `Bán nợ` | Shown as a pay mode that records no payment. | Live: not a payment method; debt is the outstanding result. Demo only. |

## A — Shell and layout

Keep the current visual architecture: UI-A application shell, current sidebar/navigation, the Sales desktop POS workspace with the Product workspace on the left and the checkout panel on the right, and the responsive tablet/mobile behavior. Do not redesign the shell.

Sales Polish may refine only spacing, typography, control sizing, alignment, responsive behavior, focus/hover/disabled states and visual hierarchy. The geometry of `TemplateHTML/Sales` / D-106 remains the visual target.

## B — Product browser

Live mode shows only real capability:

- Name, SKU and Barcode search, with scanner-friendly input;
- server-side pagination over active Products only;
- quick Add;
- Product name, SKU, Unit, Sale Price and current stock quantity;
- loading, empty and error states.

**Category:** keyword-derived Category is not business data. Live mode uses no Category filter until a Category master/domain/API is separately approved and implemented. Demo mode may keep Category chips for D-106 fidelity.

**Product images:** live mode uses a neutral, design-consistent placeholder. Real images require a backend Product image capability under a separate decision. Demo mode may keep sample images. Do not map production Products to demo images by SKU, name or any other key.

## C — Cart and working order

Live production Sales supports **one active working order** with: cart lines, Product name, Unit Price, quantity editing, line removal, line amount, subtotal, total, duplicate-Product prevention and quantity validation.

Not production-enabled in this polish: multiple working orders, Held Sale, `Giữ đơn`, `Danh sách đơn đang chờ`, frontend-only held-order persistence, Sale note, item discount and invoice discount. Where these controls are needed to match the visual reference, they appear only in Demo / Visual Reference mode. Live mode hides them; it does not show fake or disabled placeholders.

The existing browser-only working-order implementation is not a production business capability.

## D — Customer

Keep the backend-supported behavior: default `Khách lẻ`, Customer search with server pagination, select Customer, and create-and-select Customer.

UX direction: when the Sale is fully paid, Customer is contextual/secondary and must not dominate visual attention. When an outstanding amount remains, Customer becomes an explicit business requirement and the UI explains why it is needed. Preserve **outstanding > 0 → Customer required**. Customer editing follows the transaction lock while a CompleteSale outcome is ambiguous.

## E — Payment and debt

Live payment methods are only `Tiền mặt` (Cash) and `Chuyển khoản` (Transfer). Preserve multiple payments, payment amount > 0, no overpayment, Cash/Transfer-only payloads, and backend-authoritative totals and payment validation.

In live mode `Bán nợ` is **not** a payment method. Debt is the result of:

> `Outstanding = Sale Total − Actual Payments`

Example: Sale total 100.000đ, Cash 60.000đ → outstanding 40.000đ; Customer required; the 40.000đ becomes Customer debt under existing authoritative semantics. With no actual payment, the outstanding amount may equal the whole Sale total where the existing backend/business contract allows it; Customer is still required. No payment named `Debt` is ever sent.

Demo mode may keep `Bán nợ` to match D-106, clearly as visual/demo-only.

## F — CompleteSale, recovery and receipt (critical invariant)

Transaction semantics do not change. Preserve:

- `OperationId` and the immutable attempt snapshot;
- double-submit prevention and exact request retry;
- ambiguity handling for HTTP 408, HTTP 5xx, network/unknown failures and `operation-lock-timeout`;
- operation-status lookup and authoritative Sale reload after a recovered success;
- cart, Customer and payment locked while the outcome is unresolved;
- a non-ambiguous business error returns correctly to the editable state;
- no new `OperationId` for an unresolved previous attempt.

Retryable UX should make the state explicit: a `Chưa xác định được kết quả` banner/state, visibly locked cart/Customer/payment, and the CTA `Thử lại đúng thao tác`.

Completion shows only a confirmed authoritative Sale: Sale summary, receipt, `In hóa đơn` and `Đơn bán mới`. Printing keeps the 80 mm receipt; print failure/cancel never affects the completed Sale; reprint stays possible; the application shell stays excluded from receipt printing.

## G — Demo / Visual Reference separation

Demo mode stays, for visual-fidelity review, comparison with D-106 and demonstrating future direction. It may contain visual-only elements: multiple orders, Held Sale, waiting orders, Categories, Product images, discounts, Sale note and `Bán nợ`.

Strict separation:

- **Demo:** browser-only; never mutates the real backend; never sends unsupported fields to production APIs; sample Product/Customer/payment/state clearly isolated.
- **Live:** uses only capabilities approved by backend/business decisions. Demo implementation must not be reused silently as production behavior.

## Stock status

The frontend thresholds `<= 2 → Rất ít hàng` and `<= 5 → Sắp hết hàng` are not authoritative business rules. Live Sales may always show factual `quantityOnHand`, and may state zero/negative stock clearly when based directly on inventory data. It must not invent configurable or business thresholds. C14 stock-attention semantics have their own decisions and are not redefined in Sales. Demo mode may keep sample badges.

## Implementation sequence

| Pass | Scope | Status |
| --- | --- | --- |
| **Pass 1** | B — Product browser, C — Cart, D — Customer, E — Payment. Make live mode production-supported only, keeping D-106 fidelity through Demo mode. | `APPROVED FOR IMPLEMENTATION / NOT STARTED` |
| **Pass 2** | A — Shell/layout refinement; F — Complete/recovery/completion/receipt refinement, without changing semantics. | Planned, `NOT STARTED` |
| **Pass 3** | G — Demo/live isolation hardening; regression cleanup; responsive/accessibility; visual-fidelity verification. | Planned, `NOT STARTED` |

Each pass needs its own implementation review before Sales Production Polish is considered complete. UI-B is not final production-approved or completed until a later Product Owner approval.

## Validation gates

Each pass is reviewed with evidence appropriate to its scope:

- Live mode exposes only the capabilities listed in B–F; no visual-only control is visible or reachable in live mode.
- Live requests carry only supported fields: Cash/Transfer payments, no `Debt` method, no note, discount, Category or held-order data.
- Demo mode never calls a mutating production API and stays isolated from live state.
- Customer-required-on-outstanding, no overpayment, positive payments and backend-authoritative totals are preserved.
- Section F invariants are covered by existing and updated frontend tests; no recovery behavior regresses.
- 80 mm receipt, reprint and shell print exclusion are preserved.
- Frontend production build and full frontend test suite pass; targeted Sales Playwright evidence is reviewed separately and is not presented as CI evidence unless CI actually runs it.
- Visual comparison against D-106 at the 1536 × 1024 target viewport, plus tablet/mobile checks.

## Explicit exclusions

D-107 does not authorize: backend, API, domain, database or migration changes; a Category master; Product image domain/storage; Held Sale backend; multiple production working orders; Sale note persistence; discount domain/calculation; a new Debt payment method; new stock thresholds; new C14 semantics; Return/Void redesign; or polish of unrelated screens. A genuine dependency on any of these stops that portion for a separate Product Owner decision.

## Implementation note

Pass 1 was implemented at `25de2b6c2866c8478f59da0aaae3e2f72d6ed952`. After Product Owner review, a hardening fix made full debt an explicit live action, `Ghi nợ toàn bộ`, outside the payment-method grid: opening `Nhập số tiền` without an amount is no longer a debt and cannot be completed, and a deliberate full debt sends `payments: []`. The fix also resets the RAM-only live working order when the authenticated session ends or changes user, and updated the real Slice 3/Slice 5 E2E. Pass 1 remains `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`; the status lines at the top record the D-107 approval-time state.

## Governance

UI-A remains `APPROVED / COMPLETED — D-104`. D-105 remains the authoritative UI-B capability breakdown, and D-106 remains the Sales visual source of truth. The UI Redesign Program remains `NOT COMPLETED`. PR-A/PR-B/PR-C, `PR-BLOCKER-01..07`, `M7 — NOT ACHIEVED`, `Pilot — NOT STARTED`, `Production readiness — NOT DECLARED` and `C14 value / willingness-to-pay — NOT VALIDATED` are unchanged.
