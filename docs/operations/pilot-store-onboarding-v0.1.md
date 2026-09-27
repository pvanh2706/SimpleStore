# Pilot Store Onboarding v0.1 — copy per real Store

Use this record only for an actual pilot Store and its approved operators. Keep credentials and customer/payment data out of the record. Each step needs an operator, UTC timestamp, result and safe evidence reference. A blank field means **PENDING**, not completion. Link incidents to [support runbook](support-runbook-v0.1.md); do not duplicate its incident/recovery instructions.

## Store and accountable people

| Field | Real pilot value |
|---|---|
| Safe Store ID and Store name | `<pending>` |
| Canonical IANA timezone (confirmed by Owner) | `<pending>` |
| Pilot Owner contact and source Product data owner | `<pending>` |
| Onboarding operator, reviewer, UTC window | `<pending>` |
| Support hours/contact, backup contact and escalation channel | `<pending>` |
| Incident record/log location and access owner | `<pending>` |

## Ordered execution record

| Step | Action and verification | Result / evidence / operator / UTC |
|---|---|---|
| 1. Release and recovery | Confirm reviewed [release candidate evidence](templates/pilot-release-evidence-template.md), deployed SHA/version, migration state, `/health/live`, `/health/ready`, initial backup freshness and actual restore-readiness decision. Record PR-B pending items. | PENDING |
| 2. First Owner | Authorized deployment operator runs one-shot `bootstrap-owner --email <approved-owner>` using a protected password prompt/STDIN, under the reviewed [deployment runbook](deployment-runbook-v0.1.md). Retain only non-secret result/normalized identity/version/time. Never paste the password. | PENDING |
| 3. Store initialization | Owner signs in, creates the real Store in `/setup` and verifies Store/Main Warehouse identity. Record the Store ID/name and timezone after setting `/settings/operations`. | PENDING |
| 4. Cashier lifecycle | Owner opens `/settings/users`, lists/creates only the intended Cashiers and gives one-time temporary credentials through an approved private channel; each Cashier changes password at first login. Rehearse Owner reset/disable and verify disabled sessions fail, using a test account if appropriate. Record safe IDs/results only. | PENDING |
| 5. Product source and CSV | Data owner signs off source catalog, units/SKUs, sale prices and deduplication. Download `/api/product-imports/template`; prepare UTF-8 CSV within the UI's 5 MB limit. Keep the source file in controlled storage, not in this record. | PENDING |
| 6. Validate/preview/confirm | Owner uses `/import`: upload, **Kiểm tra và xem trước**, resolve every validation error, compare preview/count/sample to source, then explicitly **Xác nhận nhập**. Verify imported count and Product list. Do not claim a preview wrote data. | PENDING |
| 7. Opening inventory | Data owner provides signed opening quantity and opening cost per Product/unit. Compare entered balance/value to physical count and approved source; record count, total differences and reconciliation references. Resolve mismatches before normal pilot Sale. | PENDING |
| 8. Negative-stock policy | Owner explicitly chooses allow/block negative stock in `/settings/operations`; verify persisted value. Explain the cost-estimate implications when enabled. | PENDING |
| 9. Print station | Record actual 80 mm printer model/interface, driver/version, roll width, browser/version and certified settings. Link [physical certification](printer-certification-v0.1.md); run a safe initial/reprint sanity check. | PENDING — HARDWARE NOT AVAILABLE |
| 10. Operator handoff | Confirm Owner/Cashier can reach the application, knows print retry, Return/Void permissions, support contact/hours, incident traceId capture, and escalation. Link [support runbook](support-runbook-v0.1.md). | PENDING |

Final Store onboarding result: `PENDING — NO PILOT STORE ONBOARDED`.
- Owner acknowledgement / operator / independent reviewer / UTC: `<pending>`.
- Unresolved gaps and safe incident references: `<pending>`.

An onboarding record cannot substitute for PR-B backup/deployment/operator evidence, 80 mm printer certification, PR-C Product Owner approval, or final M7 approval.
