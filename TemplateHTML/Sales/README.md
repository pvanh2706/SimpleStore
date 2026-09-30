# SimpleStore Sales Visual Reference — v1.0 Candidate

Pass 3 / final-polish candidate for Product Owner visual approval.

## Open

Open `index.html` in Chrome/Edge after extracting the ZIP. All product images are local under `assets/`; no CDN is required.

## Visual target

- Primary reference: `design-reference.png` (D-095)
- Target desktop viewport: **1536 × 1024**
- Goal: high visual fidelity in proportions, density, typography, spacing, surfaces, product cards, checkout hierarchy, totals and primary CTA.

## Important scope boundary

This prototype is a **visual source of truth only after Product Owner approval**. It is not permission to add business capability. Elements explicitly marked `VISUAL-ONLY` in `index.html` mirror D-095 but remain outside current UI-B production scope unless separately approved, including Held Sale/multiple working orders, discounts, Category master/filter, sale-note persistence and `Bán nợ` as a payment method.

Current transaction/debt/payment behavior remains governed by Product Owner decisions and D-105.

## Pass 3 changes

- fixed packaged asset paths so the HTML is self-contained with the included `assets/` folder;
- reduced product-card height and typographic weight to match the reference more closely;
- normalized product imagery using the approved-reference crops;
- tightened checkout spacing, cart rows, totals and CTA rhythm;
- removed the default visible debt-warning card so the normal-state screenshot stays closer to D-095 while preserving the conditional production rule in comments;
- corrected reference sample total to `37.000 đ` after the illustrative `-2.000 đ` item discount;
- retained explicit `VISUAL-ONLY` markers for capability not approved in UI-B.
