# SimpleStore Sales HTML Visual Reference v1.0

**Status:** `APPROVED — D-106`, 2026-09-30.

This directory preserves the Product Owner-approved Sales HTML prototype as the visual source of truth for UI-B production alignment. The prototype files are retained from the approved `simplestore_sales_visual_reference_v3.zip` package. Candidate/Pass 3 wording embedded in the original `index.html` identifies the submitted package; D-106 records its final approved governance status.

**Source archive SHA-256:** `7d67dffd3e22390850acd6dddb4f6dd7c0bf4cd6c3ad0905eec27b4de4555a55`.

## Review locally

Open `index.html` directly in Chrome or Edge. The target desktop viewport is **1536 × 1024**. All Product images resolve locally from `assets/`; the reference requires no CDN, package install, build step, API or database.

Use these supporting images when comparing the implementation:

- `preview-1536x1024.png`: packaged rendering at the target viewport.
- `design-reference.png`: original D-095 Sales design inspiration.

## Authority order

1. **Business and behavior:** existing Product Owner decisions and supported contracts, especially D-095 and D-105, remain authoritative.
2. **Visual implementation:** this approved HTML prototype is the visual source of truth for UI-B.
3. **Original inspiration:** `design-reference.png` expresses the earlier design direction behind the prototype.

If the prototype conflicts with approved business/domain behavior, the approved behavior wins. If current Vue styling conflicts with this prototype, this prototype wins visually. D-106 does not approve the current Vue UI-B implementation; production alignment and a later Product Owner visual review are still required.

## VISUAL-ONLY capability boundary

The prototype deliberately retains visual elements that are outside the production capability boundary approved by D-105. Existing `VISUAL-ONLY` comments in `index.html` identify these examples, including:

- multiple working or held orders, `Giữ đơn` and the waiting-order list;
- Category filters;
- item and invoice discounts;
- Sale note persistence;
- `Bán nợ` presented as a payment method;
- displayed shortcuts such as `F12` where production behavior is not implemented and tested;
- any other Sales capability excluded by D-105.

These elements are visual guidance only. A production port must reuse only capabilities authorized by D-105 unless a later Product Owner decision explicitly expands scope. [D-107](../../../../DECISIONS.md#d-107--sales-production-polish-scope-v01) confirms they may appear only in the browser-only Demo / Visual Reference mode, never in live production Sales; see [Sales Production Polish v0.1](../../../architecture/sales-production-polish-v0.1.md). Current OperationId, exact retry/recovery, Customer debt, Cash/Transfer, pricing, stock, authorization and 80 mm receipt contracts remain unchanged.
