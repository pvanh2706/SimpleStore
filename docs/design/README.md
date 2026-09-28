# SimpleStore Design System v0.1

**Status:** Visual direction approved by Product Owner in [D-095](../../DECISIONS.md#d-095--simplestore-visual-direction-v01-approval) on 2026-09-28, with Today/explainability in [D-096](../../DECISIONS.md#d-096--today-screen--explainability-pattern-v01-approval) and Product Management in [D-097](../../DECISIONS.md#d-097--product-management-design-v01-approval) on 2026-09-29. UI redesign implementation: **NOT STARTED**.

Before future UI work, review in this order:

1. [Design system v0.1](simple-store-design-system-v0.1.md).
2. The relevant written screen spec: [Sales](screens/sales-screen-v0.1.md), [Appearance settings](screens/appearance-settings-v0.1.md), [Today](screens/today-screen-v0.1.md), [Product Management](screens/product-management-v0.1.md) and its linked Product specs, plus the reusable [Explainability Pattern](screens/explainability-pattern-v0.1.md) where applicable.
3. Its approved image: [Sales](references/sales-screen-v0.1.png), [Appearance settings](references/appearance-settings-v0.1.png), [Today](references/today-screen-v0.1.png), [Today explanation](references/today-explainability-v0.1.png), [Product List/Detail](references/product-list-detail-v0.1.png), or [Product states](references/product-management-states-v0.1.png).

The images define **visual intent**. The written Product Owner-approved specs define behavior and invariants. If an image conflicts with written business behavior, the written behavior wins. Do not reinterpret the approved design from scratch without a new Product Owner decision. Mock prices, dates, controls and content in the images are examples, not production data or a new domain contract.

The current Vue UI predates Design System v0.1 and its Today/Product extensions and has not been assessed as matching these references. A future redesign needs separately controlled implementation stages and review. D-095–D-097 do not approve or complete that redesign. Existing Sale, Return, Product, Inventory, Slice 5/6 and Pilot Readiness decisions remain authoritative; Held Sale, discounts and Product Category/Unit master-data changes require separate technical/domain review before coding.
