# SimpleStore Design System v0.1

**Status:** Approved visual and UI direction — D-095, 2026-09-28. **Implementation:** NOT STARTED.

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

These specifications capture approved direction, not completed software. Existing domain decisions remain authoritative. Held Sale persistence and discount rules need separate Product Owner technical/domain review before implementation.
