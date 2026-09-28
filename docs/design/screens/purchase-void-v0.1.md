# Purchase Void interaction v0.1

**Status:** Approved visual/product UX direction — D-098. **Redesign implementation:** NOT STARTED. Existing safe Purchase Void domain/UI behavior remains authoritative under D-039/D-040/D-056 and Slice 4 approval.

![Approved Purchase Void confirmation reference](../references/purchase-management-v0.1.png)

Void is an **Owner-only**, explicit corrective action for a Completed Purchase. Keep it visually separate from ordinary detail actions. A focused destructive panel/modal names the Purchase, explains that reversal is conditional, requires a reason and offers a clear cancel action. The dialog must not promise that every old Purchase can be voided.

The backend may reject Void when Supplier aggregate debt would become negative, trustworthy reversal basis is unavailable/invalid, downstream inventory/cost dependency exists, Product reference-cost dependency makes reversal unsafe, the Purchase is already voided, or an idempotency key is reused with a different payload. Do not simplify Void into unconditional deletion, historical revaluation or costing replay. A multi-line Purchase reverses atomically or not at all. Preserve original Purchase lines/payments, typed PurchaseVoid/reversal evidence and exact safe balance restoration where approved.

Use a client-generated `OperationId` and preserve the exact immutable attempt for retry/recovery. When the result is uncertain, query operation/target state and retry the same attempt as permitted by D-040; do not create a new ID or invite double correction. Show typed conflict guidance and permission errors. After successful Void, retain the historical Purchase row with **Đã hủy**, reason, timestamp and actor where available. Do not create fake payment/refund records or hide the Purchase.

The [Detail spec](purchase-detail-v0.1.md) defines read-only historical presentation. A future implementation may refine modal dimensions, spacing and accessibility but must preserve these safeguards and the action hierarchy.
