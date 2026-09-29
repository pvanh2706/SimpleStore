# Inventory Management v0.1

**Status:** Product Owner-approved visual/product UX direction — D-099, 2026-09-29. **Dedicated Inventory UI redesign:** NOT STARTED.

![Approved Inventory Management visual and interaction reference](../references/inventory-management-v0.1.png)

## Purpose and precedence

The Inventory workspace should answer **“Trong kho hiện có gì, có vấn đề gì và tôi cần xử lý sản phẩm nào?”** It is an operational workspace, not a generic analytics dashboard. The image is the official visual/interaction reference; sample products, counts, costs, labels and controls do not establish new backend capabilities.

For future work, read in this order: (1) approved Product/Inventory/costing/C14 decisions and current contracts; (2) this overview and the relevant written spec — [Overview and Product detail](inventory-overview-v0.1.md), [Stocktake](inventory-stocktake-v0.1.md), [Movement History](inventory-history-v0.1.md), plus existing [Product Inventory Actions](product-inventory-actions-v0.1.md); (3) the [approved image](../references/inventory-management-v0.1.png). If the image conflicts with approved domain behavior, domain behavior wins. An unsupported pictured capability is visual/future direction only.

## Current capability boundary

Current code has one Main Warehouse, per-Product quantity on hand, InventoryValue, moving weighted average, `HasAverageCost`, CostReliability, immutable InventoryMovement, **one-Product** Stock Adjustment and Stocktake, expected revision and `stocktake-stale` protection, OperationId/idempotency, Owner-only mutation, Store `allowNegativeStock`, Purchase/Sale/Return/Void effects and deterministic C14 attention. Product-level movement history is available from Product detail. These contracts remain authoritative.

Current code does **not** establish a dedicated `/inventory` overview page/API, all-Product movement-list endpoint, whole-warehouse Stocktake session, bulk Stocktake submission, multi-Product Stock Adjustment, inventory-category summary API or configurable arbitrary low-stock threshold. D-099 approves their visual/product direction where specified, **not** their domain/backend/schema implementation. Each requires separate Product Owner domain/technical review before coding. Bulk/multi-Product Adjustment is **NOT APPROVED FOR IMPLEMENTATION** under D-099.

## Safety and access

Inventory corrections go through approved source transactions — Purchase, Sale, Return/Void, Adjustment or Stocktake — never direct edits to `InventoryBalance.QuantityOnHand`, InventoryValue or AverageCost. Preserve the immutable ledger, moving weighted average, D-087/D-088 costing, D-089 stale protection and historical `UnitCostAtSale` snapshots. Do not offer an editable “Giá vốn bình quân” field or retroactive revaluation. Adjustment and Stocktake mutation remain Owner-only; reads follow existing authorization and Store isolation. The Inventory screen must not change `allowNegativeStock` without a separately approved Operational Settings flow.

Design loading, no Products, no filtered results, list/detail/API failure, validation errors, uncertain idempotent Adjustment/Stocktake outcomes, `stocktake-stale`, permission failure and future global-history unavailability explicitly. Desktop favors summary, searchable table, focused filter and contextual detail; tablet adapts table/detail; mobile uses Inventory List → Product → Action/History. Minor presentation refinements for responsive behavior, spacing, typography, dimensions, keyboard use, accessibility and real content are allowed during later implementation, while action hierarchy, audit/costing/Stocktake safety, permissions and C14 semantics remain intact. Material workflow/capability changes require Product Owner review.
