# Technical Breakdown Pilot Readiness v0.1

## Trạng thái

`DRAFT / PENDING PRODUCT OWNER REVIEW`

Baseline đã inspect: `94dfe79086df63a499d85e3a0c1c9e3e259f22a6`; GitHub Actions run #47 / `36001349239` là `SUCCESS`.

Tài liệu này chuyển D-077–D-084 và [Pilot Readiness v0.1](../product/pilot-readiness-v0.1.md) thành proposed implementation stages, contracts, operational artifacts, evidence và review gates. Đây chưa phải implementation approval. Pilot Readiness implementation vẫn `NOT STARTED`, M7 vẫn `NOT ACHIEVED`, pilot chưa bắt đầu và application chưa được tuyên bố production-ready.

## 1. Mục tiêu và nguyên tắc

Technical Breakdown phải:

- mô tả phần cần code, API/UI, schema/migration, scripts, runbooks, server configuration và manual evidence;
- đóng được PR-BLOCKER-01..07 bằng evidence kiểm chứng được;
- giữ nguyên behavior Slice 0–6 và toàn bộ approved C14 semantics;
- ưu tiên một deployable application + một SQL Server database trên Windows Server/IIS;
- không dùng document/code existence thay cho completion evidence;
- không tự resolve PR-Q1–PR-Q7 hoặc tự đánh dấu PR-A/PR-B/PR-C/M7 complete.

Trình tự review/implementation đề xuất:

`Product Owner resolve blocking questions → approve Technical Breakdown → PR-A → PR-B → PR-C → final M7 readiness review`

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

## 3. Proposed staging và blocker closure

| Stage | Trọng tâm | Blocker/gate đóng khi có evidence |
|---|---|---|
| PR-A — Functional pilot blockers | Production account provisioning; Stock Adjustment; Stocktake | PR-BLOCKER-01, PR-BLOCKER-02 |
| PR-B — Operational safety | Deployment, configuration/secrets, version, backup/restore, logs, health/readiness, support | PR-BLOCKER-03, PR-BLOCKER-04, PR-BLOCKER-05 |
| PR-C — Pilot certification and release gate | Printer certification, formal release evidence, onboarding/support plan, validation execution plan | PR-BLOCKER-06, PR-BLOCKER-07, Pilot operations gate, Validation readiness gate |

Stage order là proposed implementation order. PR-B documentation can begin while PR-A is under development, nhưng production-like smoke/restore/support exercises phải chạy trên reviewed candidate build. PR-C certification uses the resulting release candidate. M7 vẫn là separate final Product Owner gate sau cả ba stage.

## 4. Stage PR-A — Production account provisioning

### 4.1 First Owner bootstrap boundary

First Owner bootstrap phải tách khỏi day-to-day account management:

- không bật `DevelopmentOwnerSeeder` trong Production;
- không có anonymous public registration endpoint;
- không dùng manual SQL/Identity table edits như normal workflow;
- bootstrap tạo đúng một Owner account chưa có Store, tạo/verify closed Owner role, rồi existing Store initialization tiếp tục tạo Store/Main Warehouse;
- command phải refuse unsafe overwrite, normalize email, apply Identity password policy, avoid credential logging và return non-secret success/failure output;
- execution audit tối thiểu ghi timestamp, normalized account identity, result và deployed version vào secure operational log; không ghi password.

**Recommendation pending PR-Q1:** một explicit one-shot admin CLI/command chạy trên server bằng deployment operator, reuse Identity/EF infrastructure và đọc password từ protected prompt/ephemeral secret. Command là idempotent only for the exact existing Owner identity, từ chối khi đã có Owner khác, và không expose HTTP bootstrap surface. Deployment-time secret auto-seeding là option nhỏ hơn về thao tác nhưng có nguy cơ secret lưu lâu/replay và startup side effect.

### 4.2 Application and Identity boundary

Proposed structure:

- Application contracts/use cases định nghĩa account lifecycle intent, Store/role checks và typed results/errors.
- Infrastructure adapter encapsulates `UserManager`, `RoleManager`, password validation, security-stamp update và Identity persistence.
- Owner bootstrap host gọi riêng infrastructure/application bootstrap service; API không expose bootstrap endpoint.
- Cashier lifecycle always resolves acting Owner's Store server-side. Request không được chọn arbitrary StoreId hoặc role.
- User remains assigned to at most one Store; Cashier creation sets exactly current Owner Store and exactly Cashier role.

### 4.3 Cashier API/UI proposal

Owner-only endpoints:

- `GET /api/users/cashiers` — list Store-scoped Cashiers with id, email, enabled state, credential-change state if approved, created/updated timestamps.
- `POST /api/users/cashiers` — create Cashier in current Store with normalized email and approved initial credential semantics.
- `POST /api/users/cashiers/{cashierId}/disable` — idempotently disable only a Cashier in current Store; update security stamp and invalidate active sessions.
- `POST /api/users/cashiers/{cashierId}/credentials/reset` — apply the PR-Q2-approved reset model; response never echoes stored hashes or reusable secrets beyond the one-time UX explicitly approved.
- `POST /api/auth/change-password` — only required if PR-Q2 selects temporary-password/forced-change; verifies current/temporary credential, changes password, clears forced-change state and refreshes sign-in.

Frontend proposal:

- Owner-only `/settings/users` route with Cashier list, create, disable and reset actions.
- confirmation for disable/reset; no role editor; no invitation/email-delivery subsystem.
- if forced change is approved, router/backend restrict the Cashier to password-change/logout until completed.

### 4.4 Disabled-user behavior

Technical recommendation derived from D-078:

- add explicit enabled/disabled state rather than overload temporary Identity lockout;
- login returns the same generic invalid-credentials response for missing, disabled or bad-password users to avoid account enumeration;
- disabling updates security stamp and cookie validation checks enabled state on every authenticated request (or an equivalently immediate server-side validation), rejecting/signing out existing sessions;
- authorization remains backend authoritative; frontend state is convenience only;
- disabled users remain in historical actor foreign keys and audit evidence; no hard delete.

### 4.5 Account audit

Add immutable `AccountLifecycleAudit` (name subject to implementation naming) for `CashierCreated`, `CashierDisabled`, `CredentialReset`, and optionally `CredentialChanged` when forced-change is approved. Minimum fields: Id, StoreId, TargetUserId, Action, PerformedByUserId, OccurredAt; no password/token/credential material. Bootstrap has separate secure operational audit because no Store/actor may exist yet.

### 4.6 Account tests

- bootstrap command absent from normal HTTP surface and production seeder remains disabled;
- bootstrap success, duplicate retry/existing different Owner refusal, password-policy failure and no secret leakage;
- Owner creates only Cashier for own Store; Cashier/anonymous forbidden;
- duplicate normalized email conflict is deterministic;
- disabled Cashier cannot login and an existing session is rejected;
- reset obeys approved PR-Q2 semantics and invalidates prior credentials/session;
- Store A Owner cannot list/mutate Store B Cashier;
- audit rows are exact, immutable and contain no secret.

## 5. Stage PR-A — Stock Adjustment

### 5.1 Proposed aggregate and contract

Add immutable `StockAdjustment` aggregate as the business source, not a direct balance edit. Proposed fields:

- Id/OperationId;
- StoreId/MainWarehouseId/ProductId;
- QuantityDelta (non-zero, precision consistent with inventory);
- costing input/snapshot fields required by approved PR-Q3 policy;
- InventoryValueDelta and effective UnitCost snapshot;
- normalized required Reason;
- PerformedByUserId/OccurredAt;
- resulting InventoryMovementId;
- request fingerprint or equivalent immutable identity for retry conflict detection.

Proposed Owner-only endpoint:

`POST /api/inventory/adjustments`

Request includes `operationId`, `productId`, `quantityDelta`, `reason`, and only the cost field(s) authorized by PR-Q3. Store/Warehouse/actor/time are server-derived. Response returns adjustment identity, before/after quantity/value/cost state and movement identity.

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

### 5.3 Costing boundary — unresolved PR-Q3

Example baseline: quantity `10`, average cost `20,000`, value `200,000`; found `+2` units.

Recommendation pending approval:

- when `HasAverageCost = true`, positive adjustment uses current authoritative average cost, adds value `40,000`, yielding quantity `12`, value `240,000`, average `20,000`;
- when cost basis is unavailable, require an explicit approved cost input rather than silently use Product reference cost;
- negative adjustment removes value at authoritative average cost when reliable;
- abnormal negative quantity/value or unavailable cost must follow an explicit PR-Q3 rule and preserve unreliable state rather than claim reliable cost;
- historical SaleLine cost snapshots and prior movements never change.

No implementation may encode this recommendation until PR-Q3 is approved.

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
- costing snapshot/input according to PR-Q4;
- optional bounded Note; reason/source label identifying stocktake;
- PerformedByUserId/OccurredAt;
- optional InventoryMovementId for non-zero difference.

Proposed flow/endpoints:

- `GET /api/inventory/stocktakes/context/{productId}` — current Product, balance quantity and opaque rowversion/revision.
- `POST /api/inventory/stocktakes` — `operationId`, `productId`, expected quantity/revision, counted quantity, optional note and only approved cost input.
- optional `GET /api/inventory/stocktakes/{id}` for recovery/audit; operation recovery remains available by OperationId.

### 6.2 Concurrency-safe submit

Within the same transaction/lock pattern as Adjustment, reload and lock the balance, then compare request expected revision/quantity with current state. Never apply a difference calculated from a stale expected quantity to a newer balance.

**Recommendation pending PR-Q5:** return typed `409 stocktake-stale` with current quantity/revision and require refresh/recount/explicit resubmit. Holding a database lock while a human counts is prohibited. Silent recalculation against the latest balance is prohibited unless Product Owner explicitly chooses that UX.

### 6.3 Movement and zero-difference behavior

- Non-zero difference creates exactly one `InventoryMovementType.StocktakeAdjustment` referencing StocktakeResult and atomically updates balance.
- Zero difference persists the StocktakeResult/audit but creates no zero-value fake movement and leaves balance unchanged.
- Same OperationId + same normalized payload returns the existing result; reuse for another payload conflicts.
- Product detail history explains count, expected, difference, note, actor/time and typed source.

### 6.4 Costing boundary — unresolved PR-Q4

Recommendation: Stocktake difference reuses the exact approved PR-Q3 adjustment costing policy so two workflows cannot value the same physical delta differently. Stocktake remains a distinct source/movement type for explainability. Alternative policies and their impacts remain open at PR-Q4.

## 7. Movement types and inventory migration contract

Add closed enum values only if PR-A is approved:

- `Adjustment`;
- `StocktakeAdjustment`.

Continue typed constructors with exact source identities `StockAdjustment` and `StocktakeResult`; do not introduce arbitrary user-selected movement/source types. Reason/note lives on immutable source aggregate and is projected into history. This extends the inventory ledger; it does not create a generic accounting ledger.

Expected PR-A schema candidates:

- `AspNetUsers`: enabled state and, only if PR-Q2 requires it, forced-change state/timestamps;
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

## 10. Backup / restore baseline — PR-Q6 pending

Do not implement backup inside SimpleStore. Use SQL Server Agent or Windows Task Scheduler + reviewed PowerShell/sqlcmd tooling under a restricted service identity.

Baseline design:

- SQL Server native full/differential/log backup type chosen according to approved RPO option;
- UTC timestamp + database + backup type + release/schema context in filename/manifest;
- destination outside the live database volume/server failure domain, encrypted at rest where available, ACL restricted to backup operators/service;
- job exit status and backup verification (`RESTORE VERIFYONLY` as early check) logged/alerted; verify-only does not replace restore drill;
- retention cleanup is scripted, scoped to the backup directory/database pattern and logged;
- restore runbook restores into a separate pilot-safe/test database, verifies integrity, applies required configuration, starts the exact application version and performs authenticated data reads/smoke;
- evidence records backup identifier, source schema/version, restore target, duration, verification queries/smoke, operator and result;
- restore drill occurs before M7 and repeats after material backup/schema/process changes.

Exact schedule/retention is unresolved at PR-Q6. Technical recommendation is an RPO-oriented option with nightly full plus intra-day backup and off-host retention, but Product Owner must accept the data-loss/operational tradeoff.

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

PR-C does not choose a physical target without Product Owner/pilot environment input (PR-Q7). It creates a repeatable evidence sheet for the approved setup:

- exact paper width;
- printer manufacturer/model/interface/configuration;
- Windows version, browser/version and installed driver;
- print scale/margins/header-footer settings.

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
- credential reset follows approved PR-Q2; old credential/session fails;
- Store A cannot observe/mutate Store B identity.

Stock Adjustment:

- increase/decrease updates balance and creates exact typed movement/source/reason;
- quantity/value/cost assertions follow approved PR-Q3;
- same retry is idempotent, changed retry conflicts;
- Owner-only and Store isolation enforced;
- concurrent transaction cannot lose another movement.

Stocktake:

- expected/count/difference recorded; non-zero creates exactly one movement;
- zero difference creates result but no fake movement;
- stale expected revision follows approved PR-Q5 without overwriting newer movement;
- retry/idempotency, authorization and Store isolation verified;
- costing follows approved PR-Q4.

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

## 20. Open Product Owner Questions discovered during Pilot Readiness Technical Breakdown

All PR-Q items below are `OPEN / BLOCKING WHERE NOTED`. Recommendations are technical proposals, not approvals.

### PR-Q1 — First Owner bootstrap mechanism

**Why it matters:** determines the only production trust root, secret lifetime, operational audit and exposed attack surface before a Store exists.

| Option | Tradeoff/impact |
|---|---|
| A. Explicit one-shot admin CLI/command | Small HTTP attack surface, operator-controlled and auditable; requires server access and a documented command. |
| B. One-time deployment config/secret consumed at startup | Simple deployment automation, but introduces startup side effect, replay/idempotency handling and risk of a long-lived secret in configuration. |
| C. Time-limited bootstrap web token/page | Easier remote UX, but materially larger unauthenticated surface, token lifecycle and support burden. |

**Recommendation:** A. It is the smallest production-safe mechanism for the pilot and keeps bootstrap outside normal web traffic. Product Owner approval is required before PR-A implementation.

### PR-Q2 — Cashier credential reset model

**Why it matters:** changes UI, schema/state, session invalidation, password exposure and Cashier recovery workflow.

| Option | Tradeoff/impact |
|---|---|
| A. Owner directly sets a new permanent password | Least code and fastest pilot recovery; Owner knows Cashier password and password sharing risk is higher. |
| B. Owner issues temporary password; Cashier must change at next login | Better separation after handoff and clearer audit; requires forced-change state, restricted session flow and change-password UI/API. |
| C. One-time reset link/code delivered out-of-band | Better secret handling when delivery is trustworthy; requires token delivery/support infrastructure not otherwise in pilot scope. |

**Recommendation:** B, with one-time display, short operational handoff, security-stamp invalidation and forced change. A is acceptable only if Product Owner explicitly accepts shared-credential risk for the pilot. Blocking for account contract/schema.

### PR-Q3 — Positive Stock Adjustment costing

**Why it matters:** quantity increase must also define inventory value/average cost and reliability; wrong semantics corrupt future Moving Weighted Average and profit while historical SaleLine snapshots must remain immutable.

| Option | Tradeoff/impact |
|---|---|
| A. Always use current average cost; reject when `HasAverageCost = false` | Deterministic and simple; cannot record found stock without reliable current cost. |
| B. Always require explicit adjustment unit cost | Most explicit; extra burden and Owner may enter an arbitrary value even when reliable average already exists. |
| C. Hybrid: current average when reliable, otherwise require explicit unit cost; never silently use reference cost | Preserves reliable average and handles missing basis explicitly; needs clear UI and approved meaning of owner-entered cost/reliability. |

For negative adjustment, the same decision must specify reliable-cost removal and behavior when stock/value is already negative or cost is unavailable.

**Recommendation:** C; in the example `10 × 20,000`, `+2` adds `40,000` and keeps average `20,000`. When cost is unavailable, require explicit cost and keep reliability semantics explicit. Blocking for adjustment domain/API/migration tests.

### PR-Q4 — Stocktake difference costing

**Why it matters:** a physical count difference changes the same quantity/value ledger as Adjustment; separate rules can produce inconsistent valuation.

| Option | Tradeoff/impact |
|---|---|
| A. Reuse PR-Q3 policy exactly | One valuation rule and consistent ledger; Stocktake UI may request cost when an upward difference lacks reliable basis. |
| B. Require explicit unit cost for every positive Stocktake difference | Strongly explicit but adds repeated input and may unnecessarily override a reliable average. |
| C. Record count with unavailable/zero cost and defer value correction | Easier counting but leaves quantity/value reliability unresolved and creates a later reconciliation workflow. |

**Recommendation:** A, while keeping `StocktakeAdjustment` as a distinct source type. Blocking until PR-Q3 and PR-Q4 are approved.

### PR-Q5 — Stale Stocktake UX

**Why it matters:** Sales/Purchases can change balance while a person counts; silently applying the old difference can overwrite newer operational facts.

| Option | Tradeoff/impact |
|---|---|
| A. Reject typed 409 and force refresh/recount | Safest and simplest concurrency semantics; user may need to recount. |
| B. Show current balance and require explicit confirmation/recalculation | Fewer abandoned counts but adds a second confirmation contract and risk that the physical count timestamp no longer matches. |
| C. Reserve/lock inventory during count | Strong isolation but blocks live operations and is disproportionate for pilot MVP. |

**Recommendation:** A using balance rowversion/revision and clear UX with expected/current quantities. Blocking for Stocktake submit behavior.

### PR-Q6 — Backup schedule / retention

**Why it matters:** defines accepted RPO/data-loss exposure, storage cost and operational complexity; “daily backup” is not enough without an accepted recovery target.

| Option | Tradeoff/impact |
|---|---|
| A. Nightly full, retain 14 days | Simplest; up to roughly 24 hours of data loss. |
| B. Nightly full + differential every 4 hours; daily 14 days + weekly 8 weeks | Moderate complexity/storage; roughly 4-hour RPO. |
| C. Nightly full + transaction-log backup every 15–30 minutes; daily/weekly retention | Lowest RPO; requires Full recovery model, log-chain monitoring and more restore steps. |

**Recommendation:** B for the first pilot unless transaction volume/data-loss tolerance demands C. Backup destination must be off the live DB volume with restricted access; conduct restore drill before M7. Blocking for final backup runbook/evidence.

### PR-Q7 — Printer target

**Why it matters:** receipt CSS, wrapping, margins and browser/driver behavior cannot be certified without a physical target.

| Option | Tradeoff/impact |
|---|---|
| A. 80 mm thermal printer via installed Windows vendor driver | More usable width and common receipt format; recommended default candidate. |
| B. 58 mm thermal printer | Smaller/cheaper but materially tighter wrapping and monetary columns. |
| C. A4/office printer | Easy availability but poor counter ergonomics and different layout. |

**Recommendation:** select an actual pilot-available 80 mm USB/network thermal model, Windows driver and Chrome/Edge version, then certify; keep one secondary configuration only if actually used. Product Owner/pilot environment must supply exact paper/model/configuration. Blocking for printer certification, not PR-A/PR-B coding.

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

- PR-Q1–PR-Q5 decisions required by implementation are approved and traceable.
- Production-safe bootstrap works; Development seeder remains disabled in Production.
- Cashier create/disable/reset flow works with immediate disabled-session behavior, backend authorization and Store isolation.
- Stock Adjustment and Stocktake contracts, ledger evidence, costing, idempotency and concurrency match approved semantics.
- Owner-only UI is usable and recovery-safe.
- Migration passes clean database + current baseline upgrade + data preservation.
- Domain, SQL Server integration, frontend and critical real E2E pass; existing Slice 0–6 regression remains green.
- PR-BLOCKER-01 and PR-BLOCKER-02 may close only after reviewed evidence, not code merge alone.

### PR-B DoD

- production-like Windows/IIS deployment from versioned artifact succeeds with no Node required on server;
- explicit migration and authenticated smoke pass; exact version/SHA is verified;
- `/health/live` and DB-aware readiness behave correctly without information leakage;
- persistent logs survive deployment/restart/rotation and a real ProblemDetails traceId locates the relevant log event;
- automated backup job succeeds, failure is detectable, retention works and a separate-database restore drill passes application start/read smoke;
- deployment, backup/restore and support runbooks are exercised by another operator/reviewer;
- rollback/recovery instruction is specific to the tested release;
- PR-BLOCKER-03, PR-BLOCKER-04 and PR-BLOCKER-05 close only after evidence review.

### PR-C DoD

- PR-Q7 target is approved and actual paper/printer/browser/driver certification matrix passes with retained evidence;
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

## 23. Review gate

Before implementation:

1. Product Owner reviews this draft and resolves PR-Q1–PR-Q7 where blocking.
2. Draft is updated with approved answers and exact stage contracts.
3. Product Owner explicitly approves the Technical Breakdown in a future decision.
4. Only then may PR-A implementation begin.

No D-085 approval decision is created by this draft. Current status remains `DRAFT / PENDING PRODUCT OWNER REVIEW`.
