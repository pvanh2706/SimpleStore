# Technical Breakdown Pilot Readiness v0.1

## Trạng thái

`APPROVED FOR IMPLEMENTATION — D-092`

Implementation baseline đã inspect: `94dfe79086df63a499d85e3a0c1c9e3e259f22a6`; Product Owner reviewed/approved baseline: `5e5720659cee1fd400001d2a2f7c5b750e0483b6`. GitHub Actions CI #49 / `36037545751` tại approved baseline là `SUCCESS`; backend và frontend đều `SUCCESS`. Đây là approval-baseline evidence, không phải PR-A implementation evidence.

Tài liệu này chuyển D-077–D-091 và [Pilot Readiness v0.1](../product/pilot-readiness-v0.1.md) thành implementation stages, contracts, operational artifacts, evidence và review gates được Product Owner approve tại D-092. PR-A hiện là `APPROVED / COMPLETED — D-093`; PR-B là `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW`; PR-C vẫn `APPROVED FOR IMPLEMENTATION / NOT STARTED`. M7 vẫn `NOT ACHIEVED`, pilot vẫn `NOT STARTED` và application chưa được tuyên bố production-ready.

## 1. Mục tiêu và nguyên tắc

Technical Breakdown phải:

- mô tả phần cần code, API/UI, schema/migration, scripts, runbooks, server configuration và manual evidence;
- đóng được PR-BLOCKER-01..07 bằng evidence kiểm chứng được;
- giữ nguyên behavior Slice 0–6 và toàn bộ approved C14 semantics;
- ưu tiên một deployable application + một SQL Server database trên Windows Server/IIS;
- không dùng document/code existence thay cho completion evidence;
- áp dụng chính xác các Product Owner decisions D-085–D-091 đã resolve PR-Q1–PR-Q7, approval D-092 và explicit PR-A approval D-093; không suy diễn PR-B/PR-C complete hoặc M7 achieved.

Trình tự implementation/review đã approve tại D-092:

`PR-A implementation + separate review/approval → PR-B implementation + separate review/approval → PR-C implementation + separate review/approval → final M7 readiness review/approval`

## 2. Baseline implementation findings

### 2.1 Identity, authorization và tenancy

- ASP.NET Core Identity dùng `ApplicationUser : IdentityUser<Guid>`, role đóng `Owner | Cashier`, unique email và cookie auth.
- Auth cookie `__Host-SimpleStore.Auth` là `HttpOnly`, `Secure`, `SameSite=Lax`, lifetime 8 giờ; antiforgery cookie là `HttpOnly`, `Secure`, `SameSite=Strict`, request header `X-CSRF-TOKEN`.
- Backend controllers enforce authentication/roles; Store-scoped use cases derive Store through current user. `ApplicationUser.StoreId` nullable và `AssignToStore` từ chối chuyển user sang Store khác.
- `DevelopmentOwnerSeeder` chỉ chạy khi environment là Development. Production hiện không có Owner bootstrap hoặc Owner-driven Cashier lifecycle.
- Login hiện dựa vào `PasswordSignInAsync(..., lockoutOnFailure: true)`, nhưng chưa có application-level enabled/disabled state, forced credential-change state, account audit, Cashier management API/UI hoặc immediate rejection of an already-issued cookie after disable.
- Owner được bootstrap trước Store; Owner sau đó gọi existing Store initialization để tạo Store + Main Warehouse và gắn account vào Store.

### 2.2 Inventory ledger/costing

- `InventoryBalance` giữ `QuantityOnHand`, `InventoryValue`, `AverageCost`, `HasAverageCost`, `UpdatedAt` và SQL `rowversion`.
- Mutating transaction flows đã dùng serializable transaction, application lock cho `OperationId` và `UPDLOCK, HOLDLOCK` trên balance theo ordered Product IDs.
- `InventoryMovement` là immutable ledger evidence với Store/Warehouse/Product, quantity/value delta, unit cost, typed `InventoryMovementType`, source identity, actor, time và monotonic `LedgerSequence`.
- Closed movement types hiện tại: `OpeningBalance`, `Purchase`, `Sale`, `ReturnRestock`, `SaleVoid`, `PurchaseVoid`; chưa có Adjustment/Stocktake.
- Product inventory history hiện trả movement/value/source/time nhưng chưa resolve Adjustment/Stocktake reason hoặc actor presentation.
- Moving Weighted Average, sale cost snapshot, Return/Void reversal và negative-stock semantics đã được approve ở các slice trước; Pilot Readiness không được retroactively revalue historical movements hoặc SaleLine cost snapshots.

### 2.3 Operations baseline

- `ILogger`, global exception handler, ProblemDetails và response `traceId` đã có; persistent searchable sink, rotation/retention và complete trace-to-log enrichment chưa có.
- Anonymous `/health` hiện chỉ dùng empty `AddHealthChecks()` nên chứng minh process up, chưa chứng minh SQL Server reachable.
- CI đã chạy backend restore/Release build/tests và frontend frozen install/build/tests; real Playwright E2E vẫn là evidence local/release riêng.
- Vue build tạo static SPA; production hosting/publish integration và Windows/IIS deployment runbook chưa có.
- Browser `window.print()` đã implement; completed Sale độc lập với print và reprint dùng stored Sale, nhưng chưa có target paper/printer certification.

## 3. Approved staging và blocker closure

| Stage | Trạng thái hiện tại | Trọng tâm | Blocker/gate đóng khi có reviewed evidence |
|---|---|---|---|
| PR-A — Functional pilot blockers | `APPROVED / COMPLETED — D-093` | Production account provisioning; Stock Adjustment; Stocktake | PR-BLOCKER-01, PR-BLOCKER-02 `CLOSED — D-093` |
| PR-B — Operational safety | `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW` | Deployment, configuration/secrets, version, backup/restore, logs, health/readiness, support | PR-BLOCKER-03, PR-BLOCKER-04, PR-BLOCKER-05 remain open pending sufficient reviewed environment evidence |
| PR-C — Pilot certification and release gate | `APPROVED FOR IMPLEMENTATION / NOT STARTED` | Printer certification, formal release evidence, onboarding/support plan, validation execution plan | PR-BLOCKER-06, PR-BLOCKER-07, Pilot operations gate, Validation readiness gate |

Stage order được approve tại D-092. PR-A và mapped blockers PR-BLOCKER-01/02 được Product Owner approve/close tại D-093. PR-B implementation tồn tại tại `1284487939b26a7370b499d48d760e9335b011cc`, nhưng Windows Server/IIS và pilot-infrastructure backup/schedule/operator evidence chưa đủ nên stage chưa được approve/complete. PR-C vẫn `APPROVED FOR IMPLEMENTATION / NOT STARTED`. PR-B và PR-C vẫn cần separate Product Owner review/approval; PR-BLOCKER-03..07 và các later gates còn mở. M7 vẫn là separate final Product Owner gate sau cả ba stage.

## 4. Stage PR-A — Production account provisioning

### 4.1 First Owner bootstrap boundary

First Owner bootstrap phải tách khỏi day-to-day account management:

- không bật `DevelopmentOwnerSeeder` trong Production;
- không có anonymous public registration endpoint;
- không dùng manual SQL/Identity table edits như normal workflow;
- bootstrap tạo đúng một Owner account chưa có Store, tạo/verify closed Owner role, rồi existing Store initialization tiếp tục tạo Store/Main Warehouse;
- command phải refuse unsafe overwrite, normalize email, apply Identity password policy, avoid credential logging và return non-secret success/failure output;
- execution audit tối thiểu ghi timestamp, normalized account identity, result và deployed version/SHA vào secure operational log; không ghi password.

Theo D-085, bootstrap là một explicit one-shot admin CLI/command do authorized deployment operator chạy, reuse Identity/EF infrastructure và đọc password từ protected prompt/ephemeral secret. Command chỉ tạo Owner đầu tiên, từ chối overwrite hoặc Owner thứ hai có identity khác và chỉ cho phép exact retry của cùng normalized identity theo contract an toàn. Không có HTTP bootstrap surface, startup auto-seeding, public registration hay manual SQL normal workflow. Operational evidence chỉ ghi non-secret timestamp, normalized identity, result và deployed version/SHA.

### 4.2 Application and Identity boundary

Proposed structure:

- Application contracts/use cases định nghĩa account lifecycle intent, Store/role checks và typed results/errors.
- Infrastructure adapter encapsulates `UserManager`, `RoleManager`, password validation, security-stamp update và Identity persistence.
- Owner bootstrap host gọi riêng infrastructure/application bootstrap service; API không expose bootstrap endpoint.
- Cashier lifecycle always resolves acting Owner's Store server-side. Request không được chọn arbitrary StoreId hoặc role.
- User remains assigned to at most one Store; Cashier creation sets exactly current Owner Store and exactly Cashier role.

### 4.3 Cashier API/UI proposal

Owner-only endpoints:

- `GET /api/users/cashiers` — list Store-scoped Cashiers with id, email, enabled state, mandatory credential-change state, created/updated timestamps.
- `POST /api/users/cashiers` — create Cashier in current Store with normalized email, D-086 temporary password and mandatory-change state.
- `POST /api/users/cashiers/{cashierId}/disable` — idempotently disable only a Cashier in current Store; update security stamp and invalidate active sessions.
- `POST /api/users/cashiers/{cashierId}/credentials/reset` — issue a temporary password under D-086, set mandatory-change state, invalidate prior sessions/security stamp and expose the temporary credential only through the approved one-time handoff UX; never expose hashes or log plaintext credential material.
- `POST /api/auth/change-password` — verify current/temporary credential, change password, clear mandatory-change state and refresh sign-in.

Frontend proposal:

- Owner-only `/settings/users` route with Cashier list, create, disable and reset actions.
- confirmation for disable/reset; no role editor; no invitation/email-delivery subsystem.
- router and backend restrict a Cashier with mandatory-change state to password-change, logout and minimal session/auth state until completed; every other business API is denied.

### 4.4 Disabled-user behavior

Technical recommendation derived from D-078:

- add explicit enabled/disabled state rather than overload temporary Identity lockout;
- login returns the same generic invalid-credentials response for missing, disabled or bad-password users to avoid account enumeration;
- disabling updates security stamp and cookie validation checks enabled state on every authenticated request (or an equivalently immediate server-side validation), rejecting/signing out existing sessions;
- authorization remains backend authoritative; frontend state is convenience only;
- disabled users remain in historical actor foreign keys and audit evidence; no hard delete.

### 4.5 Account audit

Add immutable `AccountLifecycleAudit` (name subject to implementation naming) for `CashierCreated`, `CashierDisabled`, `CredentialReset`, and `CredentialChanged`. Minimum fields: Id, StoreId, TargetUserId, Action, PerformedByUserId, OccurredAt; no password/token/credential material. Bootstrap has separate secure operational audit because no Store/actor may exist yet.

### 4.6 Account tests

- bootstrap command absent from normal HTTP surface and production seeder remains disabled;
- bootstrap success, duplicate retry/existing different Owner refusal, password-policy failure and no secret leakage;
- Owner creates only Cashier for own Store; Cashier/anonymous forbidden;
- duplicate normalized email conflict is deterministic;
- disabled Cashier cannot login and an existing session is rejected;
- reset obeys D-086 temporary-password + mandatory-change semantics and invalidates prior credentials/session;
- Store A Owner cannot list/mutate Store B Cashier;
- audit rows are exact, immutable and contain no secret.

## 5. Stage PR-A — Stock Adjustment

### 5.1 Proposed aggregate and contract

Add immutable `StockAdjustment` aggregate as the business source, not a direct balance edit. Proposed fields:

- Id/OperationId;
- StoreId/MainWarehouseId/ProductId;
- QuantityDelta (non-zero, precision consistent with inventory);
- costing input/snapshot fields required by D-087;
- InventoryValueDelta and effective UnitCost snapshot;
- normalized required Reason;
- PerformedByUserId/OccurredAt;
- resulting InventoryMovementId;
- request fingerprint or equivalent immutable identity for retry conflict detection.

Proposed Owner-only endpoint:

`POST /api/inventory/adjustments`

Request includes `operationId`, `productId`, `quantityDelta`, `reason`, and optional `adjustmentUnitCost` only where D-087 requires it for a positive adjustment without reliable cost basis. Store/Warehouse/actor/time are server-derived. Response returns adjustment identity, before/after quantity/value/cost/reliability state and movement identity.

### 5.2 Atomic/idempotent algorithm

Inside one serializable transaction:

1. acquire application lock on `OperationId`;
2. load existing Store-scoped operation and compare normalized fingerprint;
3. resolve active Product + Main Warehouse in current Store;
4. acquire balance `UPDLOCK, HOLDLOCK`;
5. apply approved cost policy and negative-stock policy;
6. create immutable StockAdjustment source;
7. add exactly one typed `InventoryMovementType.Adjustment` referencing `StockAdjustment`;
8. mutate `InventoryBalance` quantity/value/cost atomically;
9. add/complete `BusinessOperationTypes.AdjustStock` recovery record;
10. commit; retry with same identity returns same result, different payload conflicts.

No path updates/deletes historical movement or writes `InventoryBalance` without source evidence.

### 5.3 Costing boundary — approved D-087 contract

Example baseline: quantity `10`, average cost `20,000`, value `200,000`; found `+2` units.

Apply D-087 exactly:

- when `HasAverageCost = true`, positive adjustment uses current authoritative average cost; the example adds value `40,000`, yielding quantity `12`, value `240,000`, average `20,000`;
- when positive adjustment has no reliable cost basis, Owner must provide explicit `Adjustment Unit Cost`; never fall back to `ReferencePurchaseCost`. The explicit cost is authoritative only for this movement and does not retroactively revalue history;
- explicit known cost does not automatically make the entire balance reliable. Only a clean zero-balance initialized by the positive adjustment with explicit known cost may establish a reliable basis;
- negative adjustment with `HasAverageCost = true` removes value at current average cost and snapshots `Reliable`;
- negative adjustment without reliable average snapshots `ReferencePurchaseCost` as `Estimated` when available, otherwise cost/value delta `0` as `Unavailable`;
- negative adjustment never promotes balance reliability. Preserve Moving Weighted Average, `InventoryValue`, `HasAverageCost`, `CostReliability`, negative-stock semantics and immutable movement cost/reliability snapshots;
- historical `SaleLine` cost snapshots and prior movements never change. If an abnormal invariant cannot be safely derived from D-087, implementation must raise a new Product Owner question rather than invent a rule.

### 5.4 Explainability

Product inventory history contract should expose typed source `Adjustment`, reason, actor identity suitable for audit, occurred time, quantity/value delta and unit-cost/reliability presentation. Backend resolves source; frontend must not parse arbitrary source strings to infer behavior.

## 6. Stage PR-A — Stocktake

### 6.1 Minimal model

Stocktake is a separate immutable counting result, not an Adjustment form label. Minimal single-Product `StocktakeResult` aggregate:

- Id/OperationId;
- StoreId/MainWarehouseId/ProductId;
- ExpectedQuantity snapshot and ExpectedBalanceRowVersion/revision;
- CountedQuantity;
- Difference = Counted − Expected;
- costing snapshot/input according to D-087/D-088;
- optional bounded Note; reason/source label identifying stocktake;
- PerformedByUserId/OccurredAt;
- optional InventoryMovementId for non-zero difference.

Proposed flow/endpoints:

- `GET /api/inventory/stocktakes/context/{productId}` — current Product, balance quantity and opaque rowversion/revision.
- `POST /api/inventory/stocktakes` — `operationId`, `productId`, expected quantity/revision, counted quantity, optional note and only approved cost input.
- optional `GET /api/inventory/stocktakes/{id}` for recovery/audit; operation recovery remains available by OperationId.

### 6.2 Concurrency-safe submit

Within the same transaction/lock pattern as Adjustment, reload and lock the balance, then compare request expected revision/quantity with current state. Never apply a difference calculated from a stale expected quantity to a newer balance.

Per D-089, return typed `409 stocktake-stale` with safe expected/current quantity and revision information, then require refresh, recount and a new submission. Holding a database lock while a human counts, silently recalculating/applying the old difference, or overwriting a newer movement is prohibited. An exact completed `OperationId` retry remains idempotent under D-014; reuse with changed payload conflicts.

### 6.3 Movement and zero-difference behavior

- Non-zero difference creates exactly one `InventoryMovementType.StocktakeAdjustment` referencing StocktakeResult and atomically updates balance.
- Zero difference persists the StocktakeResult/audit but creates no zero-value fake movement and leaves balance unchanged.
- Same OperationId + same normalized payload returns the existing result; reuse for another payload conflicts.
- Product detail history explains count, expected, difference, note, actor/time and typed source.

### 6.4 Costing boundary — approved D-088 contract

Stocktake difference reuses D-087 exactly: upward difference uses reliable current average cost or requires explicit Adjustment Unit Cost when reliable basis is absent; downward difference uses the same `Reliable` / `Estimated` / `Unavailable` chain. Stocktake remains a distinct immutable `StocktakeResult` and `StocktakeAdjustment` source/movement for explainability. No deferred value reconciliation is allowed.

## 7. Movement types and inventory migration contract

Add closed enum values only if PR-A is approved:

- `Adjustment`;
- `StocktakeAdjustment`.

Continue typed constructors with exact source identities `StockAdjustment` and `StocktakeResult`; do not introduce arbitrary user-selected movement/source types. Reason/note lives on immutable source aggregate and is projected into history. This extends the inventory ledger; it does not create a generic accounting ledger.

Expected PR-A schema candidates:

- `AspNetUsers`: enabled state plus D-086 mandatory credential-change state/timestamps;
- immutable `AccountLifecycleAudits`;
- `StockAdjustments` with Store/Product/Warehouse/actor FKs, unique Store + OperationId, reason/cost/result evidence and indexes for Product/time;
- `StocktakeResults` with expected/count/difference/revision/cost/movement evidence and equivalent FKs/indexes;
- possible BusinessOperation type additions require code but not a new generic operation schema;
- movement enum is stored as string, so new closed values may not require column shape change; no historical row rewrite.

Migration requirements:

- clean database migration;
- upgrade from current D-076/Pilot Readiness baseline schema;
- existing Identity, Store, InventoryBalance, InventoryMovement and transaction data preserved;
- deterministic defaults/backfill for any new required user state;
- rollback/data-recovery plan reviewed; operational setup is not placed in EF migrations.

## 8. Stage PR-B — Deployment topology and artifacts

### 8.1 Responsibility split

| Kind | Proposed work |
|---|---|
| Application code | Serve SPA or support chosen single-site route; DB readiness; safe version metadata; persistent structured logging/correlation. |
| Build/release scripts | Frozen frontend build, backend publish, copy/package SPA, inject version metadata, checksum/archive artifact, migration bundle if chosen. |
| Runbooks | Deploy, migrate, smoke, rollback/recovery, backup/restore, support. |
| Server/manual configuration | Windows/IIS roles, Hosting Bundle, HTTPS certificate/binding, app-pool identity/ACL, SQL connectivity, environment/secrets, scheduled tasks/SQL Agent. |

### 8.2 Production-like Windows/IIS procedure

Proposed runbook sequence:

1. provision supported Windows Server, IIS/ASP.NET Core Module via matching .NET Hosting Bundle/runtime, SQL Server connectivity and least-privilege service/database identities;
2. build Vue with frozen lockfile on build machine/CI; Node is not installed on production;
3. `dotnet publish` backend Release and package prebuilt SPA as one versioned deployable artifact;
4. prefer ASP.NET Core static-file/fallback hosting inside the single IIS application to preserve one deployable application; API routes remain Controllers and SPA fallback excludes API/health paths;
5. validate artifact manifest/checksum and exact commit SHA;
6. create versioned release directory, stop/drain app pool, preserve previous artifact for rollback;
7. set ASP.NET Core environment, connection string and secrets using IIS/server protected configuration or environment variables; never commit production values;
8. run explicit reviewed EF migration bundle/command before application start; no automatic startup migration;
9. configure IIS app pool as `No Managed Code`, least privilege, read/execute artifact ACL, write only to approved log directory; configure HTTPS binding and redirect;
10. start app, check liveness/readiness, verify version, login and execute authenticated read-only smoke plus agreed safe workflow smoke;
11. record release SHA/version, migration result, smoke evidence and operator;
12. on failure, use documented app rollback; database rollback is never an unreviewed down migration—use compatible previous app, forward fix or verified restore according to the release recovery plan.

## 9. Version identification

Smallest safe proposal:

- embed application version + commit SHA at build/publish time (`InformationalVersion`/generated metadata);
- log version, SHA and environment once at startup;
- expose authenticated `GET /api/system/version` returning only application version, commit SHA and environment label;
- include the same values in deployment artifact manifest and release checklist;
- health endpoints do not need to expose full version publicly.

No connection string, hostname internals, secret, build path or dependency inventory is returned. This is a technical support mechanism derived from D-081/D-083 and does not require a Product decision.

## 10. Backup / restore baseline — approved D-090 contract

Do not implement backup inside SimpleStore. Use SQL Server Agent or Windows Task Scheduler + reviewed PowerShell/sqlcmd tooling under a restricted service identity.

Baseline design:

- SQL Server Full recovery model with nightly native full backup and transaction-log backup every 15 minutes;
- UTC timestamp + database + backup type + release/schema context in filename/manifest;
- destination outside the live database volume/server failure domain, encrypted at rest where available, ACL restricted to backup operators/service;
- job exit status and backup verification (`RESTORE VERIFYONLY` as early check) logged/alerted; verify-only does not replace restore drill;
- retention cleanup preserves a usable 14-day recovery chain and weekly full backups for 8 weeks; cleanup is scripted, scoped to the backup directory/database pattern and logged;
- restore runbook restores an actual full + transaction-log chain into a separate pilot-safe/test database, verifies the selected recovery point and integrity, applies required configuration, starts the exact application version and performs authenticated data reads/smoke;
- evidence records backup identifiers, recovery point, source schema/version, restore target, duration, verification queries/smoke, operator and result;
- restore drill occurs before M7 and repeats after material backup/schema/process changes.

Backup storage must be separate and access-restricted, not only the live database volume/failure domain; job, backup and log-chain failures must be detectable. If the pilot environment cannot support D-090, implementation must raise a new Product Owner decision with explicit limitation/tradeoff and must not silently downgrade the recovery model, schedule, retention or restore proof.

## 11. Persistent logging and trace correlation

Minimal pilot proposal: structured JSON rolling files on the Windows server (for example a small supported file logging provider), not ELK/Grafana/Application Insights/Sentry.

- write to a dedicated ACL-protected directory outside versioned deployment folders;
- daily + size rotation, default proposed retention 14 days, configurable without code;
- enrich each request log/exception with `traceId`, request method/path template, status, duration, authenticated UserId/StoreId when available, application version/SHA and environment;
- preserve existing ProblemDetails `traceId`; exception handler and application failures log the same correlation id;
- startup/shutdown, migration state/version, readiness transitions and account/inventory security-relevant actions are logged at appropriate levels;
- no request/response body logging by default.

Never log passwords, password reset material, cookies, antiforgery tokens, connection strings, secrets, raw credentials or sensitive customer/payment payloads. Email/phone and other PII must be omitted or minimized/redacted; operational actor IDs are preferred over raw identity text.

## 12. Health and readiness

Proposed endpoints:

- `/health/live` — process liveness only;
- `/health/ready` — includes bounded SQL Server connectivity check through EF/SQL (`SELECT 1` equivalent), with timeout;
- keep `/health` as compatibility alias to readiness or document/update existing automation explicitly so current tests/runners do not silently lose DB-aware coverage.

Endpoints may remain anonymous for IIS/load-balancer/deployment smoke but return only HTTP status + generic `Healthy/Unhealthy`, never exception, server/database name or connection detail. Detailed cause goes to correlated logs. IIS/network exposure should restrict health paths to intended pilot monitoring where practical.

Deployment smoke must fail if process is down or database unreachable. Tests cover healthy DB, unavailable DB, response information disclosure and liveness/readiness separation.

## 13. Support and recovery artifacts

PR-B creates and exercises:

- `docs/operations/deployment-runbook-v0.1.md`;
- `docs/operations/backup-restore-runbook-v0.1.md`;
- `docs/operations/support-runbook-v0.1.md`.

Support runbook minimum:

- identify deployed version/SHA and release record;
- check liveness/readiness without exposing internals;
- locate current/archived logs and search exact traceId;
- verify SQL connectivity and migration/schema version read-only;
- inspect last backup job/status and link restore procedure;
- safe IIS app-pool restart and post-restart smoke;
- severity/escalation/contact rules and evidence capture;
- links to deployment rollback and backup restore;
- explicit prohibition on direct production DB edits, deleting ledger/audit rows, changing completed transactions, resetting credentials in SQL or running unreviewed migrations.

## 14. Stage PR-C — Printer certification

Per D-091, PR-C certifies an `80 mm` thermal receipt using browser print as the default strategy. It records a repeatable evidence sheet from the actual pilot setup without inventing a printer model in advance:

- exact paper width;
- printer manufacturer/model/interface (`USB`, network or actual deployed connection)/configuration;
- Windows version, browser/version and installed driver/version where identifiable;
- print scale/margins/header-footer settings.

Certify one primary configuration and a secondary configuration only if it is actually used. `58 mm` and A4 are not baseline targets. A local print agent/service remains out of scope unless browser-print certification fails and a later decision approves that direction.

Test matrix:

- short and long Vietnamese Product names, wrapping and diacritics;
- SKU/unit, decimal quantity where supported, unit price, line amount and total alignment;
- payment and customer/debt information where relevant;
- Completed Sale initial print and stored-Sale reprint;
- Return/Void state/evidence where receipt/detail is involved;
- printer offline/unavailable, driver error and user cancels dialog;
- Completed transaction remains committed, reloadable and reprintable after every failure/cancel case.

Browser print remains default. A local print agent/service is outside this breakdown; certification failure must produce evidence and a separate technical decision.

## 15. Stage PR-C — Pilot release gate

Create `docs/operations/pilot-release-checklist.md` with a per-release evidence record:

1. exact commit SHA/version and artifact checksum;
2. GitHub CI run and backend restore/Release build/automated-test result;
3. frontend frozen install/build/test result;
4. migration clean/current-upgrade/existing-data preservation evidence as applicable;
5. critical real E2E command, environment and result;
6. backup readiness check when schema/data risk changes;
7. production-like Windows/IIS deployment result;
8. version endpoint/startup log match;
9. liveness/readiness and authenticated smoke;
10. printer sanity when print changed, plus current certification reference;
11. rollback/recovery instructions for the release;
12. release operator/reviewer/sign-off and timestamp.

Full continuous deployment is not required. Checklist evidence may link CI, scripts, log extracts and signed manual results; unchecked or unevidenced items fail the release gate.

## 16. Stage PR-C — Pilot onboarding and support plan

Create a per-Store onboarding/data preparation artifact covering:

- selected pilot Store identity/name and canonical IANA timezone;
- controlled first Owner bootstrap evidence and Owner Store initialization;
- Cashier list/setup/disable/reset contact procedure;
- Product CSV preparation/validation/preview/confirm and source-data owner;
- opening inventory quantity/cost preparation and reconciliation;
- explicit negative-stock policy confirmation;
- approved target printer/paper/browser/driver;
- deployed version/SHA and migration state;
- initial backup + restore-readiness evidence;
- support hours/contact/escalation and incident logging location.

No item is assumed complete from Slice approval; pilot Store-specific evidence is required.

## 17. Stage PR-C — Validation execution plan

D-084 remains the authority. Use lightweight pilot artifacts, not a generic analytics platform:

- observation notes per workflow/task;
- issue/incident log with severity, traceId/version and resolution;
- task success/failure and recovery notes for onboarding, import, Sale, print, Purchase, inventory, debt, Return/Void and EOD;
- short structured interview guide focused on trust, usability and operational fit;
- existing C14 events `TodayOpened`, `SignalShown`, `WhyOpened`, `PurchaseDraftStarted` exported/queried only as supporting quantitative evidence;
- separate C14 interview/observation notes for discovery, novelty, evidence trust, decision influence, continued use and willingness-to-pay.

No inference `click == value validated` or `PurchaseDraftStarted == recommendation succeeded`. Core MVP operational validation and C14 hypothesis validation have separate findings/conclusions.

## 18. Real E2E and regression plan

### 18.1 PR-A real E2E

Account lifecycle:

- production-like Owner bootstrap mechanism where automatable;
- Owner initializes Store, creates Cashier, Cashier logs in;
- Owner disables Cashier; new login and existing session fail;
- credential reset follows D-086; old credential/session fails and mandatory-change restriction blocks business APIs;
- Store A cannot observe/mutate Store B identity.

Stock Adjustment:

- increase/decrease updates balance and creates exact typed movement/source/reason;
- quantity/value/cost/reliability assertions follow D-087;
- same retry is idempotent, changed retry conflicts;
- Owner-only and Store isolation enforced;
- concurrent transaction cannot lose another movement.

Stocktake:

- expected/count/difference recorded; non-zero creates exactly one movement;
- zero difference creates result but no fake movement;
- stale expected revision follows D-089 without overwriting newer movement;
- retry/idempotency, authorization and Store isolation verified;
- costing follows D-088.

### 18.2 PR-B/PR-C evidence

Deployment/backup/log/readiness/printer exercises are production-like scripts/manual evidence where Playwright is not the correct boundary. Real E2E still proves critical application flows through Vue → ASP.NET Core → SQL Server. GitHub Actions need not host real Playwright yet, but release evidence must identify the command, environment, DB and result.

### 18.3 Existing regression floor

- Domain tests;
- SQL Server integration tests;
- frontend tests/build;
- Slice 0–6 critical real flows;
- clean + current-schema upgrade migrations and existing-data preservation;
- no test deletion/skip or weakened assertions to force green;
- C14 calculations, evidence, action and telemetry semantics unchanged.

## 19. Security review checklist

- Production never executes `DevelopmentOwnerSeeder`.
- Bootstrap has no public endpoint, no persistent plaintext secret and least-privilege operator access.
- Auth cookie remains `__Host-`, Secure, HttpOnly with reviewed SameSite/lifetime; HTTPS enforced.
- Antiforgery remains required for unsafe cookie-authenticated requests.
- Password policy/lockout/reset behavior and generic login errors are tested.
- Disabled account rejects login and existing session according to D-078.
- Owner/Cashier role boundary and backend authorization are preserved.
- Every account/inventory query and mutation proves Store isolation; user remains assigned to at most one Store.
- Secrets use protected server configuration/environment; none committed or logged.
- Logs redact credential/auth/antiforgery/secret/PII material.
- Anonymous health responses disclose no database/server detail.
- Backup/service identities and directories use least privilege; restore target is isolated.
- No enterprise security certification, IAM expansion or generic permissions platform is implied.

## 20. Resolved Product Owner Questions — D-085–D-091

PR-Q1–PR-Q7 are resolved and traceable to the following `APPROVED` decisions. These decisions fix the contracts used by this draft; they do not approve the Technical Breakdown or authorize implementation.

| Question | Decision | Approved contract |
|---|---|---|
| PR-Q1 | D-085 | Explicit one-shot admin CLI/command run by an authorized deployment operator; first Owner only; no public endpoint, Production seeder, normal-flow SQL edit or secret logging. |
| PR-Q2 | D-086 | Temporary Cashier password with mandatory change at next login; restricted pre-change session; reset/disable invalidates sessions; generic login failures. |
| PR-Q3 | D-087 | Positive adjustment uses reliable average or explicit Adjustment Unit Cost; negative adjustment snapshots `Reliable`, `Estimated` or `Unavailable` under the approved fallback chain; no retroactive revaluation or reliability promotion. |
| PR-Q4 | D-088 | Stocktake difference uses D-087 exactly while retaining a distinct immutable Stocktake source/movement; no deferred reconciliation. |
| PR-Q5 | D-089 | Expected rowversion/revision with typed `409 stocktake-stale`; refresh, recount and new submission; D-014 exact retry remains idempotent. |
| PR-Q6 | D-090 | Full recovery, nightly full, 15-minute transaction-log backup, 14-day recovery chain, weekly full for 8 weeks, separate restricted storage and actual isolated full+log restore proof. |
| PR-Q7 | D-091 | `80 mm` thermal/browser-print baseline; certify actual pilot model/interface/Windows/driver/browser/paper configuration; one primary and only an actually used secondary. |

### 20.1 Technical choices not escalated

The following are safely derived from approved decisions and existing architecture, so they are proposals for Technical Breakdown review rather than separate Product questions:

- closed `Adjustment`/`StocktakeAdjustment` movement types;
- OperationId idempotency, SQL lock/recheck and immutable source records;
- immediate disabled-session rejection;
- authenticated minimal version metadata + startup log;
- process liveness separated from DB readiness with non-detailed public response;
- structured rolling file logs as the minimal Windows-compatible sink;
- support/release/onboarding artifacts and evidence format.

## 21. Definition of Done by stage

### PR-A DoD

- D-085–D-089 contracts required by PR-A are approved and traceable.
- Production-safe bootstrap works; Development seeder remains disabled in Production.
- Cashier create/disable/reset flow works with immediate disabled-session behavior, backend authorization and Store isolation.
- Stock Adjustment and Stocktake contracts, ledger evidence, costing, idempotency and concurrency match approved semantics.
- Owner-only UI is usable and recovery-safe.
- Migration passes clean database + current baseline upgrade + data preservation.
- Domain, SQL Server integration, frontend and critical real E2E pass; existing Slice 0–6 regression remains green.
- PR-BLOCKER-01 and PR-BLOCKER-02 may close only after reviewed evidence, not code merge alone.

#### PR-A approved implementation evidence — D-093

- Approved implementation baseline: `40cdb6acb8e1ee8fba9d4a99bae219bfd56025e4`; implementation commit: `ceae40ee97da3468954da8e27b852bcbbfa94113`; integrity-hardening head: `d0abe321f198f05890f566adf137844826973e7a`.
- D-085: explicit `bootstrap-owner --email <email>` admin CLI with protected prompt or ephemeral `--password-stdin`, normalized identity, Identity password policy, global serialization, first-Owner-only behavior, exact safe retry and non-secret version/SHA evidence. No public bootstrap endpoint was added; Development seeding remains Development-only.
- D-086: Owner-only, server-side Store-scoped Cashier list/create/disable/reset; generated one-time temporary credentials; mandatory password change; backend business-API denial before change; security-stamp invalidation on reset/disable; generic failed login; immutable lifecycle audit without secret material; minimal `/settings/users` and password-change UI.
- D-087: immutable `StockAdjustment`, closed `Adjustment` movement, exact OperationId fingerprint retry, SQL balance lock, atomic source/movement/balance/BusinessOperation write, reliable/explicit/estimated/unavailable costing and no retroactive revaluation or unsafe reliability promotion.
- D-088/D-089: separate immutable `StocktakeResult`, opaque balance revision, typed `409 stocktake-stale`, refresh/recount flow, D-087 costing, closed `StocktakeAdjustment` movement, and persisted zero-difference count with no fake movement.
- Product inventory history now projects typed reason/note, actor id, occurred time, quantity/value/unit cost, `CostReliability`, and Stocktake expected/count evidence. UI keeps an ambiguous Adjustment/Stocktake attempt immutable for exact retry.
- Additive migration: `20260926023259_ImplementPilotReadinessPrA`; clean migration and upgrade from `20260924010903_ImplementSlice6Stage6BC14` preserve existing Identity/Store/Product/InventoryBalance data.
- Integrity hardening makes Cashier disable, credential reset and password change atomic across Identity password/security-stamp persistence, lifecycle state/timestamps and immutable audit. SQL-trigger failure-path integration tests prove all three flows roll back credential/state/stamp changes when the audit insert fails.
- Stocktake rejects negative counted quantity with typed `invalid-stocktake-counted-quantity` before mutation. A shared deterministic precision guard rejects quantity inputs that cannot fit `(18,3)` exactly, unit-cost inputs that cannot fit `(18,4)` exactly, and values outside those ranges; rejected requests create no source, movement or BusinessOperation and do not mutate InventoryBalance. Frontend retains quantity `step="0.001"`, cost `step="0.0001"`, and adds `min="0"` to counted quantity.
- No new migration is required for this hardening; additive migration `20260926023259_ImplementPilotReadinessPrA` remains current.
- Local verification on 2026-09-26: .NET Release build `0 warnings / 0 errors`; Domain `96/96`; SQL Server integration `97/97`; frontend `23` test files / `106` tests and production build pass; real PR-A Playwright `1/1` pass through Vue → ASP.NET Core → temporary LocalDB with production CLI bootstrap. GitHub Actions run #52 / `36231423946` at hardening head `d0abe321f198f05890f566adf137844826973e7a` is `SUCCESS`. GitHub CI does not run this real Playwright flow, so E2E remains local evidence only.
- Final reviewed state/evidence is `bf733a6923b2d0b7c2162b37fdcddd7f42643b74`; GitHub Actions run #53 / `36231672552` is `SUCCESS`, with backend and frontend both `SUCCESS`.
- Environment note: local Node `22.19.0` emits the repository engine warning (`>=24` expected), while frozen install, frontend tests and build pass.
- Product Owner approval baseline is `bf733a6923b2d0b7c2162b37fdcddd7f42643b74`. Review findings were fixed at `d0abe321f198f05890f566adf137844826973e7a`; PR-A is `APPROVED / COMPLETED — D-093`, and PR-BLOCKER-01/02 are `CLOSED — D-093`.
- D-093 approval boundary at that point: PR-B/PR-C were `APPROVED FOR IMPLEMENTATION / NOT STARTED`; PR-BLOCKER-03..07 remained open. PR-B has since reached the incomplete-evidence implementation state documented below, without changing the D-093 historical decision. M7 remains not achieved; Pilot has not started; Production readiness has not been declared; C14 value/willingness-to-pay has not been validated.

### PR-B DoD

- production-like Windows/IIS deployment from versioned artifact succeeds with no Node required on server;
- explicit migration and authenticated smoke pass; exact version/SHA is verified;
- `/health/live` and DB-aware readiness behave correctly without information leakage;
- persistent logs survive deployment/restart/rotation and a real ProblemDetails traceId locates the relevant log event;
- automated backup job succeeds, failure is detectable, retention works and a separate-database restore drill passes application start/read smoke;
- deployment, backup/restore and support runbooks are exercised by another operator/reviewer;
- rollback/recovery instruction is specific to the tested release;
- PR-BLOCKER-03, PR-BLOCKER-04 and PR-BLOCKER-05 close only after evidence review.

#### PR-B implementation evidence — incomplete / pending Product Owner review

- Implementation commit: `1284487939b26a7370b499d48d760e9335b011cc`. No new EF migration was required; current migration remains `20260926023259_ImplementPilotReadinessPrA`.
- ASP.NET Core now serves the prebuilt Vue SPA from publish `wwwroot` with history fallback, while unknown `/api/*`, `/health*`, and missing assets never return `index.html`; OpenAPI remains Development-only.
- Authenticated `GET /api/system/version` returns only embedded application version, commit SHA, and environment. The release builder passes identical metadata to assembly/startup logs and `artifact-manifest.json`.
- Serilog JSON rolling files use daily + size rolling and configurable path/14-day default time retention. Production default is outside versioned releases. Request completion/application scopes correlate the existing ProblemDetails `traceId` with safe method/path/route/status/duration, UserId/StoreId where available, version/SHA, and environment. No request/response-body logging or credential/header/connection-string logging was added.
- `/health/live` is process-only; `/health/ready` performs bounded `SELECT 1`; `/health` aliases readiness. Anonymous responses are generic `Healthy`/`Unhealthy`; SQL failure detail is restricted to safe server logs.
- `tools/release/New-ReleaseArtifact.ps1` produces one versioned ZIP with prebuilt SPA, backend publish, Windows migration bundle, 109-file manifest, SHA/version metadata and SHA-256 checksum. Artifact `SimpleStore-0.1.0-1284487939b2.zip` matched checksum `ec569ffae1b9db6c0ba87709a011ed6f57143ff1c8691c379746c310527e01e1`; production runtime does not require Node.
- Deployment/migration/smoke/rollback scripts and [deployment](../operations/deployment-runbook-v0.1.md), [backup/restore](../operations/backup-restore-runbook-v0.1.md), and [support](../operations/support-runbook-v0.1.md) runbooks were added with reusable evidence templates. Migration is explicit; normal startup does not auto-migrate; application rollback is separated from database recovery and never casually runs EF `Down()`.
- Backup tooling configures/verifies FULL recovery, creates native full/log backups with checksum and supported-edition compression, runs `RESTORE VERIFYONLY`, emits non-zero failures/JSONL evidence, checks 26-hour full/20-minute log freshness defaults, and conservatively retains the 14-day chain anchor/logs plus weekly fulls for 8 weeks. Cleanup is non-recursive and exact-pattern/root scoped.
- [Local actual restore drill evidence](../operations/evidence/pr-b-local-restore-drill-2026-09-26.md): FULL + transaction-log backup/verify, ordered isolated-database restore, `DBCC CHECKDB`, post-full data read from the log chain, exact published artifact startup, readiness, authenticated version/session/Store/product read, SPA/API boundary, and structured trace correlation all passed.
- Automated regression: .NET Release build `0 warnings / 0 errors`; Domain `96/96`; SQL Server integration `100/100`; frontend production build and `23` files / `106` tests pass. Real PR-A Playwright `1/1` and real Slice 6 Playwright `2/2` pass. The PR-A E2E now waits for the disable UI refresh before checking session invalidation, removing an observed test race without changing account behavior.
- Environment limitations: `PRODUCTION-LIKE IIS SMOKE PENDING — ENVIRONMENT LIMITATION`; IIS feature inspection required elevation and `WebAdministration` was unavailable. The successful restore drill used local SQL Express/LocalDB and same-machine backup storage, so actual pilot separate-failure-domain storage, scheduled nightly/15-minute jobs, elapsed retention/alerting, least-privilege IIS/SQL identities, real certificate/binding, app-pool deployment/rollback, and another-operator runbook exercise remain pending.
- Governance boundary: PR-B is only `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW`; PR-BLOCKER-03/04/05 remain open. No D-094 exists. PR-C remains not started. M7 remains not achieved; Pilot has not started; Production readiness has not been declared.

### PR-C DoD

- D-091 `80 mm` target is approved and actual paper/printer/browser/driver certification matrix passes with retained evidence;
- pilot release checklist passes for exact candidate SHA, including CI, migrations, real E2E, deployment/version/readiness smoke and rollback;
- Store onboarding/data preparation and support/contact/escalation plans are reviewed and executable;
- D-084 Core MVP and C14 validation execution artifacts are ready without claiming outcomes;
- PR-BLOCKER-06, PR-BLOCKER-07, Pilot operations gate and Validation readiness gate close only after evidence review.

## 22. M7 final gate and state boundaries

Completion of PR-A/PR-B/PR-C does not automatically set `M7 — Pilot-ready`. A separate final readiness review must reconcile every item in the approved Definition of Pilot Ready and receive explicit Product Owner approval.

- `MVP implementation completed` != `Pilot Ready`;
- `Pilot Ready` != `Pilot Started`;
- `Pilot Started` != `Pilot Validated`;
- `Pilot Validated` != `Production Ready`.

C14 remains an experiment. Technical delivery and event counts do not validate discovery, trust, decision influence, continued use or willingness-to-pay.

## 23. Approval and implementation review gate

1. PR-Q1–PR-Q7 remain resolved by D-085–D-091.
2. Product Owner approved this Technical Breakdown and exact stage contracts at D-092 using reviewed baseline `5e5720659cee1fd400001d2a2f7c5b750e0483b6`.
3. Product Owner approved PR-A at D-093 using final reviewed state/evidence `bf733a6923b2d0b7c2162b37fdcddd7f42643b74`; PR-A is `APPROVED / COMPLETED`, and PR-BLOCKER-01/02 are closed.
4. PR-B is `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW` at `1284487939b26a7370b499d48d760e9335b011cc`; PR-BLOCKER-03/04/05 remain open. PR-C remains approved and `NOT STARTED`.
5. Completion of all stages does not automatically achieve M7; final Pilot Readiness/M7 still requires separate explicit Product Owner approval.
