# Add and Edit Product v0.1

**Status:** Approved visual/product UX direction — D-097. **Redesign implementation:** NOT STARTED.

![Approved Add/Edit Product state reference](../references/product-management-states-v0.1.png)

Use a compact form with clear groups rather than a long undifferentiated field list. **Thông tin cơ bản** contains Product name, SKU, barcode, current string Unit and Category **only when implemented**. **Giá và tồn kho** contains sale price, reference purchase cost, and opening quantity/unit cost where applicable to Product creation. **Status** shows active/inactive state where the current workflow supports it. Mark required fields, preserve meaningful validation and keep **Lưu** as the obvious primary action.

The current Product command stores `Unit` as a string; a reusable Unit selector is future direction and requires the review described in [Category and Unit](product-category-unit-v0.1.md). Category is not yet established in the Product domain/schema. The mockup's optional image, Supplier, package quantity and margin concepts do not add fields to the approved contract by themselves.

Opening inventory must continue through the existing approved OpeningBalance/inventory ledger and costing rules; it must not silently set stock by directly editing a balance. Editing a Product must not rewrite immutable InventoryMovements, existing Sale lines or historical `UnitCostAtSale` snapshots. Do not turn Add/Edit into a Stock Adjustment substitute. Where the form exposes a cost input, label reference purchase cost and opening unit cost distinctly; neither is automatically current average cost.

Only an authorized Owner may mutate the Product. Show loading, server validation, duplicate SKU/barcode, save failure and uncertain outcome clearly; prevent accidental duplicate submissions. Keep the form usable as a drawer/full overlay on tablet and a sequential screen on mobile. Future storage/API changes require separate technical/domain review, not D-097 alone.
