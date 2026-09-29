# UI Redesign Program v0.1

**Status:** Product Owner-approved staged frontend program — D-103, 2026-09-29; program **NOT COMPLETED**. **Implementation:** UI-A **APPROVED / COMPLETED — D-104**, at head `eb487be1c4290dad3e1ddd6fa039d2470dcaf495`; UI-B–UI-F **NOT STARTED**. This is not a new business/domain Slice and does not change current Slice, Pilot Readiness or C14 governance.

## Authority and stage rule

Use the approved D-095 [Design System](simple-store-design-system-v0.1.md), D-096 [Today/Explainability](screens/today-screen-v0.1.md), D-097 [Product](screens/product-management-v0.1.md), D-098 [Purchase](screens/purchase-management-v0.1.md), D-099 [Inventory](screens/inventory-management-v0.1.md), D-100 [Debt](screens/debt-management-v0.1.md), D-101 [End-of-day](screens/end-of-day-v0.1.md) and D-102 [Settings](screens/settings-v0.1.md). All earlier approved domain, backend and authorization decisions remain authoritative. The written behavior and existing supported contracts prevail over illustrative imagery. UI redesign is a staged frontend program, not permission to implement every pictured capability.

Each stage follows **Technical Breakdown approval → implementation → implementation review → fixes when needed → Product Owner stage approval → approval documentation → next stage**. D-103 approves only the [UI-A Technical Breakdown](../architecture/technical-breakdown-ui-a-v0.1.md) for later implementation. It does not approve implementation of UI-B–UI-F, multi-stage batching or completion of any stage. Review later stages separately before coding.

| Stage | Approved order and current-capability boundary |
| --- | --- |
| **UI-A — Design Foundation + Application Shell** | Semantic tokens, shared primitives with actual reuse, desktop sidebar/shell, responsive tablet/mobile navigation, centralized navigation presentation, accessibility baseline and compatibility with current screens. Do not redesign business-screen internals. Follow its approved technical breakdown. |
| **UI-B — Sales** | Redesign the currently supported Sale flow under D-095. Held Sale, discounts and other pending capabilities require separate approval; current transaction semantics remain unchanged. |
| **UI-C1 — Product** | Redesign supported Product capabilities first. Category/Unit master data, Product images, duplicate, barcode printing and unsupported derived metrics remain pending separate decisions/implementation contracts. |
| **UI-C2 — Purchase** | Redesign supported Purchase flows. No Purchase discount, unsupported notes/date editing, Purchase printing or new payment semantics. |
| **UI-D — Owner insight & financial workspaces** | Sequence **Today + Explainability → Debt → End-of-day**. Only backend-supported data/actions; no frontend-created financial semantics to mimic mockups. Preserve Owner/Cashier authorization and each written spec's current/future boundary. |
| **UI-E — Settings** | Implement D-102 with currently supported capabilities. No post-setup Store rename, printer architecture, unsupported auth lifecycle or operational settings. |
| **UI-F — Appearance** | Separate Store-level preset/mode/density/branding persistence may require new technical/domain contracts. Review persistence before implementation; do not assume UI-A already provides it. |

Dedicated global Inventory workspace, whole-inventory Stocktake, global Movement History, printer architecture, Held Sale, discounts, Category/Unit master data and unsupported report evidence/data are **future/separate review**, not automatically part of the implementation program. D-099 remains design direction for Inventory where dedicated capabilities are pending.

UI-A leaves the current workflows usable inside the new shell while business-screen internals remain pre-redesign. [D-104](../../DECISIONS.md#d-104--ui-a-design-foundation--application-shell-implementation-approval) records UI-A implementation approval and its reviewed CI/targeted Playwright evidence; D-095–D-103 semantics and boundaries remain unchanged. The next step is **Technical Breakdown UI-B — Sales redesign**. UI-B is **NOT STARTED** and requires separate Product Owner approval before implementation. UI-A approval does not complete the UI Redesign Program.
