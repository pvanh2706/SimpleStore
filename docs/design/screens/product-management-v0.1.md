# Product Management v0.1

**Status:** Product Owner-approved visual/product UX direction — D-097, 2026-09-29. **Product UI redesign:** NOT STARTED. **Category/Unit master-data implementation:** NOT STARTED; separate domain/technical review required.

Approved visual references: [Product List and Detail](../references/product-list-detail-v0.1.png) and [Product management states](../references/product-management-states-v0.1.png). These are official references, but their data, controls and calculations are illustrative. They do not amend existing Product/Inventory contracts.

## Scope and reading order

1. Approved Product/Inventory domain decisions and existing code contracts.
2. This Product Management specification and the relevant written screen specification below.
3. The approved Product reference images.

If an image conflicts with domain behavior, domain behavior wins. If it shows an unapproved capability, treat that capability as illustrative/future direction pending separate review. Do not infer backend, schema or UI implementation completion from D-097.

| Written specification | Approved direction |
| --- | --- |
| [Product List](product-list-v0.1.md) | List, search, filter, row actions and pagination |
| [Product Detail](product-detail-v0.1.md) | Product identity, prices/costs, inventory context, history and C14 |
| [Product Form](product-form-v0.1.md) | Compact Add/Edit Product interaction |
| [Category and Unit](product-category-unit-v0.1.md) | Future one-level Category and reusable Unit master-data direction |
| [Inventory actions](product-inventory-actions-v0.1.md) | Stock Adjustment, Stocktake and immutable movement history |

## Current contract and boundaries

The current implementation already has Product ID, SKU, barcode, name, a string `Unit`, sale price, reference purchase cost, active state, quantity on hand, inventory value, moving average cost/`HasAverageCost`, timestamps, create/edit/search, CSV import, immutable InventoryMovement, Stock Adjustment, Stocktake, deactivation and C14 Product evidence. D-097 does not reopen their approved semantics.

The mockups also show Category, reusable Unit management, Product images, a fixed Supplier field, margin/profit cards, stock-status labels, barcode printing, duplicate Product and package conversion. Their implementation status and review gates are specified in the linked documents. Do not invent relationships, algorithms or storage from a picture.

Owner may mutate Products, adjust stock and submit Stocktake; future Category/Unit configuration is Owner-managed. Cashier visibility/actions remain governed by existing authorization. All views and actions preserve Store isolation. Destructive/deactivation actions are separated from primary actions; historical Product and inventory records are never hard-deleted or rewritten.

Design loading, empty Product list, no search results, API error, validation error, inactive Product, stale Stocktake and uncertain idempotent-operation outcome explicitly. Desktop favors a scan-friendly list plus contextual detail; tablet may use a drawer/full overlay; mobile follows List → Detail → Action rather than squeezing every desktop column onto the screen. Use the [D-096 Explainability Pattern](explainability-pattern-v0.1.md) for important derived costs, values and C14 signals, not for raw identity fields.
