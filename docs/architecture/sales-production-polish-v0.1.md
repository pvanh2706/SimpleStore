# Sales Production Polish v0.1

## Status and authority

**Scope:** `APPROVED — D-107`, 2026-10-02, at repository baseline `a89d693c8b6441eab2d380e5c643f2f3de6a33e5`. **Sales Polish Pass 1:** `APPROVED FOR IMPLEMENTATION / NOT STARTED`. **Pass 2 and Pass 3:** planned, `NOT STARTED`.

**Current status (D-108, 2026-10-02):** Pass 1 `APPROVED / COMPLETED — D-108` at implementation head `1af1fc781ef4cebf3e861c9eb211343e726e68f7`; Pass 2 `APPROVED FOR IMPLEMENTATION / NOT STARTED`; Pass 3 `NOT STARTED`. The status line above records the D-107 approval-time state; see [Implementation status](#implementation-status).

**Current status (Pass 2 implementation, 2026-10-02):** Pass 2 `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`; Pass 1 remains `APPROVED / COMPLETED — D-108`; Pass 3 `NOT STARTED`.

**Current status (D-109, 2026-10-02):** Pass 1 `APPROVED / COMPLETED — D-108`; Pass 2 `APPROVED / COMPLETED — D-109` at implementation head `cada9daf6ea8bbfba4864c7495a53073b25b793b`; Pass 3 `APPROVED FOR IMPLEMENTATION / NOT STARTED`. The status lines above record earlier states.

**Current status (Pass 3 implementation, 2026-10-02):** Pass 3 `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`; Pass 1 remains `APPROVED / COMPLETED — D-108`; Pass 2 remains `APPROVED / COMPLETED — D-109`. UI-B is not final completed.

The D-107 scope approval is documentation/governance only. It changes no Vue, backend, domain, database, API, auth, migration or test code, and it claims no implementation, validation or visual approval; implementation approvals are recorded separately (Pass 1: D-108).

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

| Pass | Scope | Status at D-107 approval | Current status |
| --- | --- | --- | --- |
| **Pass 1** | B — Product browser, C — Cart, D — Customer, E — Payment. Make live mode production-supported only, keeping D-106 fidelity through Demo mode. | `APPROVED FOR IMPLEMENTATION / NOT STARTED` | `APPROVED / COMPLETED — D-108` |
| **Pass 2** | A — Shell/layout refinement; F — Complete/recovery/completion/receipt refinement, without changing semantics. | Planned, `NOT STARTED` | `APPROVED / COMPLETED — D-109` (approved for implementation at D-108) |
| **Pass 3** | G — Demo/live isolation hardening; regression cleanup; responsive/accessibility; visual-fidelity verification. | Planned, `NOT STARTED` | `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW` (approved for implementation at D-109) |

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

## Implementation status

**Pass 1 — `APPROVED / COMPLETED — D-108`** (Product Owner, 2026-10-02).

- **Implementation baseline:** `25de2b6c2866c8478f59da0aaae3e2f72d6ed952` — live Sales reduced to production-supported B–E capability; Demo / Visual Reference mode keeps D-106 fidelity.
- **Hardening baseline (approved implementation head):** `1af1fc781ef4cebf3e861c9eb211343e726e68f7` — after Product Owner review: full debt is the explicit live action `Ghi nợ toàn bộ`, outside the payment-method grid, requiring a Customer and sending `payments: []`; payment intent (`full-payment`, `explicit-payments`, `full-debt`) is separated from the `Nhập số tiền` UI state, so opening `Nhập số tiền` without an amount is not a debt and cannot be completed; the RAM-only live working order survives same-session route changes and is reset when the authenticated session ends or the identity changes; real Slice 3 follows the UI-B search/checkout and real Slice 5 verifies full debt via `payments: []`. No `Debt` payment method is sent; debt remains `Outstanding = Total − Actual Payments`. Section F CompleteSale semantics are unchanged.
- **Evidence:** GitHub Actions CI #87 `SUCCESS` at the hardening baseline (frontend production build and test suite; backend restore, build and tests). `tests/e2e/specs/ui-b-sales.spec.ts` covers Sales UI behavior against browser-level mocked APIs and is not real backend/database evidence. The updated real `tests/e2e/real-specs/slice3.spec.ts` and `slice5.spec.ts` were run locally with the existing temporary LocalDB real-E2E flow; CI does not run them, and no raw run log or artifact is committed.
- **Moved to Pass 2 (non-blocking):** a lightweight live checkout `Lịch sử bán hàng` shortcut (history remains reachable from the sidebar `Đơn bán` and after Sale completion), and `Thêm khách` nowrap on desktop (visual-only wrapping).

**Pass 2 — `APPROVED / COMPLETED — D-109`** (Product Owner, 2026-10-02; approved for implementation at D-108). Frontend only.

- **Implementation baseline:** `b5d16df1e3384710175d8cffc40fae466690a0a7`, on top of `2098c071d99ecd2137ec0dfbd57832b148a8a126`.
- **Review-hardening baseline (approved implementation head):** `cada9daf6ea8bbfba4864c7495a53073b25b793b` — the unresolved CompleteSale attempt is kept across navigation (see below). CI #90 `SUCCESS` at this head; CI #89 `SUCCESS` at the implementation baseline. CI does not run Playwright.

The notes below were written at implementation and review time; D-109 approves them.

- **A — Shell/layout:** the UI-A shell, POS split and D-106 geometry are kept. `+ Thêm khách` is one line (icon and label, content-sized column; the Customer dropdown anchors to the whole Customer row). A quiet live `Lịch sử bán hàng` shortcut sits in the checkout header, stacked above `Xóa đơn` where the Demo keeps its kebab, away from the Complete CTA; it is disabled while a CompleteSale outcome is unresolved. Payment spacing no longer relies on negative margins; long Product names clamp to two lines in the cart, long Unit/stock labels truncate on cards, large totals wrap instead of overflowing, and the quantity stepper fits six digits. On desktop the checkout stays within the viewport: a long cart scrolls inside it so the Complete CTA stays reachable. Tablet keeps the three-column search toolbar and two-column Customer/payment rows; phones get a two-row search toolbar, side-by-side Cash/Transfer, a full-row create-Customer panel and 44–48 px touch targets.
- **F — Complete/recovery/completion/receipt:** one transaction status at a time: the CTA reads `Đang xác nhận…` or `Đang kiểm tra kết quả…` (one spinner, `aria-busy`). An unresolved outcome shows a calm `Chưa xác định được kết quả` block (no new Sale, no edits, the previous attempt is kept; `SimpleStore sẽ dùng lại đúng mã thao tác trước đó để tránh tạo đơn trùng.`) above `Thử lại đúng thao tác`, scrolled into view. Locked controls are tinted rather than faded, the header chip reads `Đã khóa`, the Product area and section labels show a lock, and the Customer pickers cannot open. A non-ambiguous rejection shows `Chưa hoàn tất đơn bán` with the server message right above the CTA, with every control editable and no lock residue. The completion screen orders success → Sale id/time → Total → Paid (per method) → Outstanding (with the Customer it is recorded for) → Customer → `In hóa đơn` → `Đơn bán mới` (primary) and `Lịch sử bán hàng` (secondary link), beside an 80 mm-like receipt preview (about 76 mm printable width). A Sale returned as already completed says so. `In hóa đơn` calls the same receipt print boundary; print failure/cancel keeps the Sale and allows reprint; physical 80 mm print CSS is unchanged.
- **Unchanged:** OperationId, immutable attempt snapshot, exact retry, double-submit prevention, HTTP 408/5xx/network/unknown and `operation-lock-timeout` ambiguity, operation-status lookup, authoritative Sale reload, the lock while unresolved, no new OperationId while unresolved, non-ambiguous error → editable correction, payment intents and `payments: []` full debt. Demo / Visual Reference mode keeps its order rail, Category chips, images, note/discount visuals and three pay modes.
- **Evidence:** frontend production build and full frontend unit suite pass, with new tests for the shortcut, `Thêm khách`, busy/checking states, the unresolved lock, correctable rejection and the completion screen. Router-level unit tests prove that a refused navigation keeps the same OperationId and that the exact retry resends the identical snapshot. Mocked-API `tests/e2e/specs/ui-b-sales.spec.ts` (browser-level mocks, not real backend/database evidence) covers desktop 1536 × 1024 empty/cart/outstanding/full-debt/unresolved/rejected/long-cart/completed states, refused sidebar/brand/Back/logout/drawer navigation during an unresolved outcome, tablet 820 × 900 and mobile 390 × 844 checkout-to-completion without horizontal overflow, print and Demo. Real Slice 3 and Slice 5 were run locally on temporary LocalDB databases, unchanged, and passed; CI does not run Playwright. Screenshots are local review artifacts and are not committed.
- **Unresolved attempt kept across navigation (Product Owner review fix):** while CompleteSale is `completing`, `checking` or `retryable`, the live checkout stays mounted so its exact attempt (OperationId and snapshot) is never dropped. A router guard registered before the auth guard refuses every in-app navigation away from `/sales/new` (sidebar, topbar, mobile drawer, the history shortcut, programmatic pushes and browser Back within the app), and sign-out is refused before the logout request is sent. The checkout then shows `Đơn bán đang chờ xác định kết quả.` / `Hãy kiểm tra kết quả hoặc thử lại đúng thao tác trước khi rời màn bán hàng để tránh tạo đơn trùng.` above the transaction state and retry CTA. A `beforeunload` warning (the browser's own text) is registered only in those states, for reload, tab close or leaving the app. Nothing is persisted. A non-ambiguous rejection, a confirmed Sale (including after an exact retry or status lookup) or an ended session (HTTP 401) releases the guard; an ended session cannot retry the attempt, and the RAM order resets with it as before.
- **Observation for review (not changed by Pass 2):** mocked `tests/e2e/specs/ui-a-shell.spec.ts` cases for Owner `Chức năng khác → Đơn bán` navigation and the tablet Products page width also fail at the D-108 baseline; they do not involve the Sales checkout.

**Pass 3 — `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`** (approved for implementation at D-109; implemented on top of `7991a38b0abca0726ca7e9ac008576225eac60b9`). Frontend and tests only; no business capability added.

- **G — Demo/Live isolation, findings and fixes:** (1) The Demo checkout was handed the live `completeSale`, `checkOperation` and `loadSale` callbacks; only its `previewOnly` early return kept it from submitting. It is now bound to an explicit browser-only `demoCheckout` (`sales/demo.ts`): sample Product/Customer search, a created Customer kept in the tab, and CompleteSale/Sale reads refused locally as a non-ambiguous `sales-demo-only` error. No Demo path reaches the API. (2) Switching to Demo could hide a live CompleteSale whose outcome was unknown. Switching is now refused like leaving (topbar button, mobile checkbox and a synchronous guard in the live checkout), shows `Đơn bán đang chờ xác định kết quả.`, and is allowed again once the outcome is known. (3) The live payment snapshot copied payment objects with a spread; it now picks `amount` and `method` explicitly. (4) The quantity input id lacked the Demo suffix; ids are unique while Live and Demo are both mounted. Verified unchanged: the RAM-only `liveOrderBook` is used by live only; each Demo session starts a fresh sample order book; sample Products are never mutated; Category, images, stock thresholds, discounts, note and `Bán nợ` remain Demo-only.
- **Payload proof:** the richest valid live order (Customer, several Products, Cash plus Transfer) and a deliberate full debt serialize to exactly `operationId`, `customerId`, `lines[{productId, quantity}]` and `payments[{amount, method: Cash|Transfer}]` (`payments: []` for full debt), after a Live → Demo → Live round trip that leaves the live cart, Customer, payment intent, payments, attempt, search text and results untouched.
- **Accessibility:** the full Sale is completable by keyboard. Escape closes the Customer pickers and returns focus to their summary; selecting or creating a Customer returns focus to the Customer control; when an unresolved outcome or a rejection leaves focus lost, it returns to the CTA; completion focuses the success heading; `Đơn bán mới` focuses Product search. One screen-reader result count replaces an aria-live Product grid; the search box has a clearer focus ring; Demo images are decorative. Status/alert roles, `aria-busy`, `aria-pressed`, `aria-disabled` and labels were reviewed and kept.
- **Responsive and touch:** on tablet and phone, Add, quantity −/+, payment methods, Customer, `+ Thêm khách`, `Ghi nợ toàn bộ` and Complete are at least 44 px; remove, `Nhập số tiền` and the history shortcut are at least 40 px. On tablet the header utilities sit side by side, and 360 px phones keep `Chuyển khoản` on one line. Verified at 1536 × 1024, 820 × 900, 390 × 844 and 360 × 740 with no horizontal overflow.
- **Visual fidelity:** Demo matches the D-106 reference at 1536 × 1024 except for the approved one-line `+ Thêm khách` (D-109) and a slightly wider quantity stepper. Live differs from D-106 only where D-105/D-107 remove visual-only capability: rail, waiting list, `Giữ đơn`, Category chips, images, note, discounts and `Bán nợ`.
- **Evidence:** production build and full frontend unit suite pass, with new isolation, payload, Demo-toggle, id, focus and announcement tests. Mocked-API `tests/e2e/specs/ui-b-sales.spec.ts` covers keyboard completion, Demo with zero API calls, the Demo toggle refused during an unresolved Sale, exact payload keys, touch targets and the 360 px phone (browser-level mocks, not real backend/database evidence). Real Slice 3 and Slice 5 were run locally, unchanged, on temporary LocalDB databases and passed; CI does not run Playwright. Screenshots are local review artifacts and are not committed.
- **Observations outside Pass 3:** the mocked `ui-a-shell.spec.ts` failures noted above predate D-108 and are unchanged. The mobile header `Menu` button is the shared UI-A `AppButton` at 40 px and is not changed by a Sales pass.
- **Known limits by design:** leaving the browser itself (reload, tab close, Back past the first page) relies on the browser's `beforeunload` prompt, and a forced session end still drops an unresolved attempt. The working order is RAM-only, and no persistence is authorized.

Pass 3 needs Product Owner review; D-110 is reserved for the final UI-B implementation approval. UI-B is not final production-approved or completed until then.

## Governance

UI-A remains `APPROVED / COMPLETED — D-104`. D-105 remains the authoritative UI-B capability breakdown, and D-106 remains the Sales visual source of truth. D-107 remains the Sales Production Polish scope authority; D-108 approves only Pass 1 and D-109 only Pass 2. The UI Redesign Program remains `NOT COMPLETED`. PR-A/PR-B/PR-C, `PR-BLOCKER-01..07`, `M7 — NOT ACHIEVED`, `Pilot — NOT STARTED`, `Production readiness — NOT DECLARED` and `C14 value / willingness-to-pay — NOT VALIDATED` are unchanged.
