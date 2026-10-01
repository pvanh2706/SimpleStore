# Technical Breakdown UI-B v0.1 — Sales Redesign

## Status and authority

**Technical Breakdown:** `APPROVED FOR IMPLEMENTATION — D-105`. **UI-B implementation:** `NOT STARTED`.

**Later status note (D-107, 2026-10-02):** UI-B has since been implemented against the D-106 visual reference; production capability cleanup/polish is scoped by [Sales Production Polish v0.1](sales-production-polish-v0.1.md) under [D-107](../../DECISIONS.md#d-107--sales-production-polish-scope-v01), with Pass 1 `APPROVED FOR IMPLEMENTATION / NOT STARTED` at that time. **Later status note (D-108, 2026-10-02):** Pass 1 is `APPROVED / COMPLETED — D-108`; Pass 2 is `APPROVED FOR IMPLEMENTATION / NOT STARTED`; Pass 3 is `NOT STARTED`. This breakdown remains the authoritative UI-B capability boundary.

Product Owner approved this breakdown on 2026-09-30 at reviewed HEAD `669cca8e2bd3f5e83a8cbcd1368d7455a2c58f9f`. This commit records documentation/governance only. UI-A remains `APPROVED / COMPLETED — D-104`; the UI Redesign Program remains `NOT COMPLETED`. Later UI-B implementation needs its own review and explicit Product Owner implementation approval.

Authority: D-095 [Design System v0.1](../design/simple-store-design-system-v0.1.md) and [Sales screen v0.1](../design/screens/sales-screen-v0.1.md), D-103 [staged UI Redesign Program](../design/ui-redesign-program-v0.1.md), D-104 [UI-A implementation approval](../../DECISIONS.md#d-104--ui-a-design-foundation--application-shell-implementation-approval), and all earlier approved Sale/Return/Void/Payment/Debt/Inventory/Auth semantics. Written Product Owner-approved behavior and current backend contracts prevail over mockups. Images express visual intent and do not authorize unsupported capabilities. D-095–D-104 semantics and boundaries remain unchanged.

## Scope and inspected baseline

Primary target: **`/sales/new`**, redesigned as a desktop-first POS workspace using the approved UI-A Emerald + Light + Standard semantic tokens and shared primitives. Allowed work is frontend presentation/layout, focused component refactoring, responsive UX, frontend tests, targeted Playwright smoke and minimal completion/receipt integration harmonization. `/sales` and `/sales/:id` may receive light harmonization only where needed for consistency and navigation continuity. Do not deeply redesign Sales history/detail, Return or Sale Void workflows.

Inspected responsibilities at the reviewed baseline:

| Existing area | Responsibility to preserve |
| --- | --- |
| `src/frontend/simplestore-web/src/views/SaleCheckoutView.vue` | Load operational settings; adapt existing Product/Customer APIs; complete Sale; check operation status; load authoritative Sale; coordinate completion, receipt and new Sale. |
| `src/frontend/simplestore-web/src/components/SaleCheckoutForm.vue` | One active cart, Customer selection, multiple payments, preview totals, validation, duplicate prevention, attempt snapshot and completion/recovery state. |
| `src/frontend/simplestore-web/src/components/SaleReceipt.vue` | Completed Sale receipt and print/reprint behavior; preserve the current 80mm layout. |
| UI-A shell, navigation, semantic styles and shared controls | Application navigation, Owner/Cashier visibility, auth gates, focus/accessibility baseline and print exclusion. |
| Existing checkout, receipt, router/auth tests and `tests/e2e/specs/ui-a-shell.spec.ts` | Regression baseline; extend relevant coverage during implementation without treating existing evidence as UI-B validation. |

No backend API, domain, authorization, database or migration changes are authorized. If implementation discovers a genuine backend/domain dependency, stop that portion and submit it for separate Product Owner review. Do not change backend contracts for UI convenience.

## Desktop POS workspace

UI-A's sidebar remains the navigation column. The Sales workspace uses its available width: product discovery in the center/main area and a persistent cart/checkout panel on the right. Do not apply a generic centered/max-width form layout to the POS workspace. Keep product search/selection spacious, checkout easy to see and the primary action in a stable location. Exact widths and breakpoints are implementation choices validated for usability.

The checkout panel groups current cart lines, Customer/payment controls and totals. Preserve one clear primary action before completion: **Hoàn tất bán hàng**. Unsupported controls from the image must not be added merely to reproduce its appearance.

## Product search and barcode

Preserve server-side Name/SKU/Barcode search, pagination and active Products only. The current adapter uses `/api/products` with `search`, `isActive=true`, `page` and `pageSize=20`; presentation refactoring must retain that supported behavior rather than filtering only an already loaded page.

Provide one clearly labelled search/barcode control and POS-friendly result cards or rows. Each result shows Product name, SKU, Unit, Sale Price, current quantity on hand and a quick Add action. Long Vietnamese Product names must remain readable. Treat loading, empty, error and pagination as first-class states with clear feedback and usable controls. Preserve keyboard and barcode-scanner-friendly entry/submission.

Category filters/tabs, Category master data, Product images and unsupported quick filters are outside UI-B. Do not show fake or disabled placeholders for them.

## Active cart and preview totals

Support **one active working order**. Show Product name, Unit price, directly editable quantity, line amount, remove action, total, paid and outstanding. Preserve duplicate Product protection and quantity validation; the current behavior prevents a second line for the same Product rather than creating duplicates. Keep numeric values easy to scan and preserve current preview calculations.

Frontend totals remain previews. Backend price, totals, stock and other business validations at CompleteSale remain authoritative. No unsupported custom price override or new frontend/backend calculation is authorized.

Held Sale, multiple simultaneous/working orders, **Đơn đang chờ**, **Giữ đơn**, Sale note, item discount and invoice discount are not implemented in UI-B. Do not simulate these through localStorage, frontend-only Sale persistence or inactive placeholder controls. **Đơn bán mới** after a confirmed completed Sale starts the next single working order; it does not introduce multiple-order or Held Sale capability.

## Customer and payment

Preserve **Cash** and **Transfer** as the only payment methods, with the current multiple-payment behavior. Each payment must be greater than zero; total payment must not exceed Sale total. Unpaid/outstanding Sale remains allowed under current rules, and any outstanding balance requires a Customer. Preserve Customer search/select, its server pagination and the current create-and-select flow using existing APIs. Customer presentation may be reorganized to fit the checkout panel.

Keep current payment persistence, Customer requirements, Debt semantics and accounting meaning. Outstanding is not a new payment method. Customer creation/selection and payment actions must obey the same transaction lock as cart edits while an attempt's outcome is ambiguous. Preserve the existing `AllowNegativeStock` warning and backend stock policy; the UI does not grant new negative-stock permissions or redefine costing.

## Complete Sale, recovery and completion

Keep business state ownership explicit throughout component extraction. One coordinator owns cart, Customer, payments, completion state and the immutable attempt snapshot. Presentation children receive data/locked state and emit user intentions; they must not create their own operation identity or independently mutate the attempt. Existing ownership may be retained or refactored into a focused owner without changing behavior.

The preserved attempt contains `operationId`, `customerId`, Product IDs/quantities and the Cash/Transfer payment amounts/methods. Generate OperationId for a new valid completion attempt, preserve its exact snapshot and use the authoritative `/api/sales/complete` contract. Preserve double-submit prevention and the existing `idle`, `completing`, `checking`, `retryable`, `completed` behavior; exact internal names may remain implementation choices.

| Outcome or action | Required behavior |
| --- | --- |
| CompleteSale is in flight or operation status is being checked | Block duplicate submission and lock cart/Customer/payment editing. |
| HTTP 408, 5xx, network/unknown failure or `operation-lock-timeout` | Treat as ambiguous; check the same OperationId through the existing operation-status lookup. |
| Operation is `Completed`, has type `CompleteSale` and has a result reference | Load that authoritative Sale before presenting recovered success. Do not infer completion from a local preview. |
| Status lookup or authoritative Sale load fails, or no confirmed completed result is available | Keep the exact attempt and locked cart/Customer/payments; offer retry of that same operation. No new OperationId or replacement attempt. |
| Retry after ambiguity | Submit the unchanged operation identity and snapshot, including its payment details. |
| Non-ambiguous business error | Preserve existing error handling and return to editable correction as appropriate; do not infer success or silently turn a conflict into a new completed operation. |

After confirmed success, show a clear Sale-completed state, authoritative transaction information, the receipt, **In hóa đơn** and **Đơn bán mới**. Recovered success follows the same completion presentation. Do not enable starting a replacement order as a way to bypass an ambiguous transaction.

## Printing

Printing remains independent of transaction completion. Print failure or cancellation does not change Sale status; an existing completed Sale remains reprintable. **In hóa đơn** must not become an equivalent primary action before CompleteSale.

Preserve receipt 80mm sizing and existing print layout. UI-A shell/sidebar/navigation and other non-receipt controls must be excluded from printing. Any necessary completion/receipt CSS adjustment must be narrow and covered by relevant receipt/print regressions; no printer configuration or architecture is added.

## Responsive and cashier accessibility

- **Desktop:** usable two-pane Product discovery + persistent checkout workspace beside the UI-A navigation sidebar.
- **Tablet:** adapt Product discovery and checkout to available space, keeping both usable without clipping the workspace.
- **Mobile:** present Product/cart/payment as a sequential workflow; do not compress desktop columns into two narrow columns.

Preserve keyboard use, barcode-scanner-friendly interaction, visible focus, adequate touch targets, readable long Vietnamese names and numeric scanability. Use meaningful warning/error text and semantic state presentation, never color alone. Maintain usable actions and focus as loading, validation, locked/retry and completed states change. Avoid nested modals in ordinary cashier flow. Display a shortcut such as F12 only if it is actually implemented and tested.

## Component architecture

`SaleCheckoutForm.vue` may be split into focused responsibilities such as `SaleProductBrowser`, `SaleCart`, `SaleCustomerPanel`, `SalePaymentPanel`, `SaleTotals`, `SaleCheckoutAction` and `SaleCompletionState`. These names are conceptual, not mandatory filenames. Prefer UI-A tokens/primitives and native controls where appropriate. Keep one clear owner for transaction state and exact retry snapshots; component boundaries must not weaken lock/recovery behavior.

Do not add a UI framework, separate design-system package or unnecessary generic abstractions. Shared components should have demonstrated reuse. Minimal history/detail harmonization must preserve Return/Void actions, current authorization and navigation continuity.

## Implementation sequence and gates

| Step | Work | Gate |
| --- | --- | --- |
| UI-B.1 — POS structural layout | Sales-specific full-width Product area and checkout panel using UI-A tokens. | Current Sale functionality remains reachable; desktop layout is usable. |
| UI-B.2 — Product browser | Search/barcode, result cards/rows, Add, loading/empty/error/pagination. | Search, server pagination and barcode behavior remain unchanged. |
| UI-B.3 — Cart and totals | Cart presentation, direct quantity editing, remove and totals hierarchy. | Quantity validation, duplicate prevention and previews remain unchanged. |
| UI-B.4 — Customer and payment | Customer debt UX, Cash/Transfer, multiple payments and outstanding presentation. | Customer requirement and no-overpayment semantics remain unchanged. |
| UI-B.5 — Completion/recovery | CompleteSale primary action, loading/locked/retry states, completion, receipt and new Sale. | OperationId, exact snapshot/retry and recovery invariants pass regression tests. |
| UI-B.6 — Responsive/accessibility/print | Desktop/tablet/mobile, keyboard/focus, long names and receipt 80mm. | Workspaces/actions remain usable, unclipped and accessible; shell stays excluded from print. |
| UI-B.7 — Full validation | Production frontend build, full frontend tests, relevant backend/CI regression and targeted UI-B Playwright. | Passing results and explicit evidence are ready for independent implementation review. |

## Testing expectations

Preserve and extend meaningful unit/component coverage for:

| Area | Required regression coverage |
| --- | --- |
| Product discovery | Text search, Barcode search, server pagination, Add Product, loading/empty/error states and duplicate prevention. |
| Cart and previews | Quantity changes/validation, line removal, line/total/paid/outstanding previews and negative-stock warning. |
| Customer/payment | Cash, Transfer, multiple payments, payment greater than zero, no overpayment, unpaid/outstanding, Customer required for debt, search/select and create-and-select Customer. |
| Transaction safety | Double submit, stable OperationId, HTTP 408 ambiguity, 5xx/network ambiguity, `operation-lock-timeout`, operation-status lookup, exact retry and locked cart/Customer/payments. |
| Recovery/error | Completed-operation recovery loads authoritative Sale; unresolved outcome retains the attempt; non-ambiguous business errors preserve existing behavior. |
| Completion/printing | Completed state, new Sale flow, receipt printing/reprint, print failure/cancel independence, receipt 80mm sizing and shell exclusion. |
| Authorization/accessibility | Current Owner/Cashier access, UI-A auth/navigation gates, keyboard/focus, meaningful labels and non-color-only feedback. |

Validate representative desktop, tablet and mobile viewports, including long Vietnamese names and no workspace clipping. Targeted UI-B Playwright should preferably exercise **Cashier login/session → Sales → Product search/barcode → Add → quantity/payment → complete → receipt → new Sale**, plus at least one responsive viewport. Preserve UI-A shell/print regression coverage where affected. Record whether smoke uses mocked contracts or a real API/database; do not claim backend execution from mocked coverage. Tests must not require backend changes purely for UI convenience.

Run frontend production build and full frontend tests, relevant backend/CI regression and targeted UI-B Playwright during implementation. CI must succeed. This documentation approval introduces no implementation and claims no UI-B test/build/E2E result; documentation/repository consistency validation is sufficient for this commit.

## Explicit exclusions and implementation approval gate

UI-B excludes Held Sale, multiple simultaneous orders, Đơn đang chờ/Giữ đơn, all discounts, Sale note, Category filters/tabs/master data, Product images, unsupported quick filters or custom price override, new payment methods/Debt semantics/backend calculations, frontend-authoritative Sale totals and frontend-only Sale persistence. It excludes backend APIs, domain/auth changes, database/migrations, Return/Void redesign and unrelated Product/Purchase/Settings redesign. Unsupported Sales capabilities remain pending separate Product Owner review.

Later Product Owner implementation approval requires all of the following: Sales materially reflects D-095 POS direction; usable desktop Product + checkout layout and responsive tablet/mobile; all current Sale capabilities preserved; no unsupported mockup capability implemented; Product search/barcode/pagination work; cart/payment/Customer/debt semantics unchanged; OperationId/recovery/exact retry unchanged; receipt 80mm and Owner/Cashier authorization unaffected; accessibility baseline met; frontend tests/build pass; CI succeeds; targeted UI-B Playwright evidence reviewed; and no backend/domain/database/migration changes unless separately approved.

D-105 approves this breakdown only. UI-B implementation remains **NOT STARTED**, UI-A remains **APPROVED / COMPLETED — D-104**, and the program remains **NOT COMPLETED**. PR-A/PR-B/PR-C, readiness blockers, M7, Pilot and C14 validation status remain unchanged. Later stages require their own breakdowns and Product Owner approvals.
