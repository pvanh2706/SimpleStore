# SimpleStore Design System v0.1

**Status:** Approved visual and UI direction — D-095, 2026-09-28; Today/explainability — D-096, Product Management — D-097, Purchase Management — D-098, Inventory Management — D-099, Debt Management — D-100, 2026-09-29. **UI redesign implementation:** NOT STARTED.

## Direction

**Modern Retail Utility + Friendly Local Commerce.** The application should feel modern, clean, quick and familiar to Vietnamese store owners and cashiers. Business tasks take precedence over decoration or a generic SaaS dashboard look. The intended feeling is **“Mở lên là biết phải làm gì.”** Use clear Vietnamese labels and keep the next useful action visible.

## Layout and hierarchy

- Design the POS for desktop first: left navigation sidebar, central workspace, and a right context/action panel when the workflow needs one.
- Put one obvious primary action in each workflow. Keep destructive actions separate. Keep important business actions in stable positions across themes and densities.
- Avoid a page full of generic dashboard cards. Use space to group work and make scanning fast.
- On tablet, adapt the product grid, allow navigation to collapse and keep checkout visible. On mobile, use a usable sequential flow; preserve the same business workflow rather than forcing the desktop columns side by side.

## Visual language

- Light is the default display mode. Use white or neutral surfaces, thin borders, very light shadows and clear spacing without making the working screen sparse.
- Use approximately 8–12 px corner radius. Keep typography legible, including long Vietnamese product names and supporting details.
- Make prices, totals, stock counts and stock status easy to scan. Use tabular numerals for aligned numeric data where appropriate.
- Warning states must convey a real condition, not decoration. Give empty, loading and error states explicit treatment. Favor keyboard and barcode-heavy cashier operation and short click paths; avoid unnecessary nested modals.

## Color and meaning

The default brand preset is **Emerald**. Emerald is the primary and positive-action color; Amber indicates warnings; Red indicates destructive actions and errors; Slate/Stone provide neutral surfaces and text. Brand customization must never replace warning or error meaning. Dark mode must preserve semantic meaning and legible contrast.

## Bounded appearance customization

Store branding includes a logo and display name, optional login/background branding where appropriate, and optional receipt branding. The approved initial preset direction is **Emerald, Ocean, Indigo, Terracotta, Slate, Dark POS**. Customization must preserve visual hierarchy and action meaning rather than allowing arbitrary component colors.

Use semantic tokens as an implementation contract rather than hard-coded component colors, for example:

```css
--brand-primary
--brand-primary-hover
--surface
--surface-muted
--border
--text
--text-muted
--warning
--danger
--radius
--density-space
```

Exact token values may be refined during future implementation; their semantic roles must remain stable. Display modes are **Light, Dark, System**. Density options are **Compact / Gọn**, **Standard / Tiêu chuẩn**, and **Comfortable / Thoải mái**. Density changes spacing and control density only: it must not move buttons, change workflow, hide business operations, reorder actions or create an unrelated layout.

## Approved screen directions

- [Sales screen](screens/sales-screen-v0.1.md): desktop POS, multiple working/held orders, compact discounts and a clear checkout hierarchy.
- [Appearance settings](screens/appearance-settings-v0.1.md): Owner-managed Store branding, presets, display mode, density and live previews.
- [Today screen](screens/today-screen-v0.1.md): operational overview with a clear KPI hierarchy, supporting evidence, inventory attention and a path to the next action.
- [Explainability Pattern](screens/explainability-pattern-v0.1.md): contextual explanations and source drill-down for important derived metrics and signals.
- [Product Management](screens/product-management-v0.1.md): scan-friendly list, focused detail/form/filter, future Category/Unit configuration direction, and clear Stock Adjustment, Stocktake and inventory-history interactions. Follow its linked screen specifications and both approved Product images.
- [Purchase Management](screens/purchase-management-v0.1.md): Purchase list/filter/detail, staged Draft creation and completion, payment/debt clarity and safe Void. Follow its linked specs and approved Purchase image.
- [Inventory Management](screens/inventory-management-v0.1.md): operational inventory overview, Product-level adjustment and Stocktake entry, C14 evidence, and future whole-inventory Stocktake/history direction. Follow its linked specs and approved Inventory image.
- [Debt Management](screens/debt-management-v0.1.md): distinct Customer collection and Supplier settlement, current outstanding with backend `asOf`, safe DebtPayment and explainability. Follow its linked specs and approved Debt image.
- [End-of-day Reporting](screens/end-of-day-v0.1.md): Store-local historical report, separate Revenue and Net Collected, ending debts, historical-cost Estimated Gross Profit with CostReliability, and contextual explanation. Follow its linked Detail, Explainability and future Close Day concept specs and approved EOD image; D-048 query-not-close remains authoritative.

## Explain important numbers, not every number

Explainability is especially useful for aggregates, estimates, derived KPIs, attention signals, accounting-like totals and figures affected materially by corrections or cutoffs. Raw facts generally need no formula explanation. Use a small **Vì sao?** / **Cách tính** action, keep the default screen calm, and place multi-section detail in a focused side panel. Explanations should state meaning, formula, included/excluded data, exact Store-local scope and a safe path to source records when supported. Preserve authorization, Store isolation, data reliability and existing backend semantics. Do not add decorative info icons throughout the interface.

These specifications capture approved direction, not completed software. Existing domain decisions remain authoritative. Held Sale persistence and discount rules need separate Product Owner technical/domain review before implementation. Today redesign and any new metric, date/shift control or drill-down capability likewise require separate implementation/technical review; D-096 does not alter approved Slice 6/C14 contracts. D-097 does not authorize Category/Unit schema, Product images, conversions or derived Product profit metrics without their own review. D-098 does not authorize Purchase discount, Purchase-level note, document-date editing or Purchase printing; the existing `Draft|Completed` lifecycle, payment/debt and Void contracts remain authoritative. D-099 does not authorize bulk Stocktake/Adjustment, global movement API or arbitrary low-stock thresholds; existing Product-level inventory and C14 semantics remain authoritative. D-100 does not authorize due dates/aging, direct debt edits, invoice allocation or an unreviewed generic history API; current DebtPayment and correction contracts remain authoritative. D-101 does not authorize Close Day/Reopen/locking, a historical debt-created or SaleCount metric, payment-method composition data, Category revenue reporting or unreviewed source-evidence APIs. D-048 remains authoritative: End-of-day is a query, not accounting close.
