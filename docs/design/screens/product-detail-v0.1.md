# Product Detail v0.1

**Status:** Approved visual/product UX direction — D-097. **Redesign implementation:** NOT STARTED.

![Approved Product Detail reference](../references/product-list-detail-v0.1.png)

The contextual detail hierarchy may use **Tổng quan**, **Giá & chi phí**, **Tồn kho**, **Lịch sử** and **C14 / Cần chú ý**. Keep Product name, SKU, barcode, string unit and active state visible. Show sale price, current quantity on hand, inventory value, moving average cost and cost reliability context where the existing backend supports them. Owner actions such as edit or deactivate follow current permissions; historical records remain readable after deactivation.

## Cost and derived figures

Keep `ReferencePurchaseCost` (reference purchase cost), current balance `AverageCost`/`HasAverageCost`, and historical `SaleLine.UnitCostAtSale` distinct. A change to Product details or current average cost does not revalue old InventoryMovements or historical Sale cost snapshots. Use the [D-096 Explainability Pattern](explainability-pattern-v0.1.md) for inventory value, average cost or C14 evidence when useful. Mock cards labeled estimated Product profit or margin are **not** approved domain metrics; do not define `SalePrice − AverageCost` or a margin percentage as authoritative without separate Product Owner review.

## Future content shown in the image

- **Product images:** desirable future UX for recognition. There is no approved Product image-storage contract. Filesystem, SQL blob, cloud/CDN and resizing/compression architecture remain open for later technical review.
- **Category:** appears only after the separate one-level Category domain/technical review and implementation in [Category and Unit](product-category-unit-v0.1.md).
- **Supplier:** the displayed supplier name does not establish a fixed Product → Supplier relationship. A default/preferred supplier needs separate product/domain review; existing Supplier/Purchase contracts prevail.
- **In mã vạch** and **Nhân bản:** `FUTURE / REQUIRES SEPARATE REVIEW`; they are not implementation requirements under D-097.
- **C14/status:** use only approved deterministic active-Product attention and evidence. `NegativeStock` and `OutOfStock` are factual conditions subject to D-071; `LowStockRisk` requires its approved seven-completed-day sufficiency and threshold. Avoid arbitrary **Bình thường/Tồn kho thấp/Sắp hết hàng** formulas. C14 remains descriptive, not a validated prediction or replenishment recommendation.

Inventory history and adjustment/Stocktake interactions are described in [Inventory actions](product-inventory-actions-v0.1.md). On tablet the detail may become a full drawer/overlay; on mobile use List → Detail → Action. The image contains illustrative prices, costs, status labels and ratios, not new backend facts.
