# Project State

## Giai đoạn hiện tại

**MVP / Pilot Readiness — `PR-B IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW`**

Slice 6 — Understand & Act giữ trạng thái `APPROVED / COMPLETED — D-076`. Slice 5 — Debt + End-of-day giữ trạng thái `APPROVED / COMPLETED` tại D-060; baseline trước Slice 6 là commit `9f57a37ef97ab9f8f672b5309a42141ea6e267b8`.

## Primary Persona

Chủ cửa hàng tạp hóa nhỏ tại Việt Nam, trực tiếp tham gia vận hành và chịu trách nhiệm ít nhất cho bán hàng, nhập hàng/tồn kho và kết quả kinh doanh — `APPROVED`.

## Phạm vi hiện tại

- Step 1–11 đã APPROVED; kế hoạch triển khai theo vertical slice đã được ghi nhận.
- Slice 0 — Engineering Foundation: completed.
- Slice 1 — Setup + Product: completed.
- Slice 2 — Purchase → Inventory: `APPROVED / COMPLETED` ngày 2026-09-21 sau các vòng review và hardening.
- Slice 3 — Sale → Payment → Print: `APPROVED / COMPLETED` ngày 2026-09-22 sau implementation và technical review end-to-end.
- Slice 4 — Return / Void / Recovery: `APPROVED / COMPLETED`; Slice 5 baseline commit `5876082a`.
- Slice 1 có Store/Main Warehouse onboarding, Product, OpeningBalance ledger, InventoryBalance và fixed-template CSV import Validate → Preview → Confirm.
- Slice 2 có Supplier CRUD-lite, Purchase Draft → Completed, multiple actual PurchasePayment, supplier outstanding derived, inventory ledger/balance và Moving Weighted Average.
- CompletePurchase được Store-scope, Owner-only, atomic/idempotent và khóa SQL Server theo deterministic ProductId order để ngăn lost update.
- D-018–D-021 là các business decisions đã `APPROVED` và không thay đổi.
- CI backend/frontend đã pass tại commit hardening cuối `f68c8a111a5538be8e51cf8ff0e323e7ce2f41c0`.
- D-023–D-030 — Slice 3 lifecycle, price snapshot, payment/credit, Customer boundary, inventory/cost, negative stock, idempotency và printing/permissions — đã được Product Owner `APPROVED` ngày 2026-09-21.
- D-031 — Technical Breakdown Slice 3 Approval — đã được Product Owner `APPROVED` ngày 2026-09-21.
- D-032 — Slice 3 Implementation Approval — đã được Product Owner `APPROVED` ngày 2026-09-22.
- Technical Breakdown Slice 3 v0.1 là `APPROVED FOR IMPLEMENTATION`; negative-stock/inventory-costing hardening đã được review và chấp thuận.
- Slice 3 đã implement và được approval end-to-end: Customer tối thiểu; Sale, SaleLine và SalePayment; checkout và authoritative pricing; credit Sale; inventory deduction và InventoryMovement; cost snapshot với reliability `Reliable` / `Estimated` / `Unavailable`; negative-stock policy; `HasAverageCost` và negative residual InventoryValue guard; CompleteSale atomic/idempotent; shared deterministic Sale/Purchase locking; browser print/reprint; Owner/Cashier authorization.
- Implementation đã được technical review. CI backend/frontend tại commit `a0ce464384b6a2029d8ecf7be4cdc84f26367d9e` (GitHub Actions run `35627192222`) đã pass: backend build, 39 domain tests, 43 SQL Server integration tests, frontend build, 12 frontend test files và 32 frontend tests. Real Slice 3 Playwright flow đã chạy local; CI không chạy real E2E này.
- D-033–D-040 — Slice 4 permissions, Return lifecycle/quantity/refund/restock, Sale Void, safe Purchase Void và correction idempotency/concurrency/audit — đã được Product Owner `APPROVED` ngày 2026-09-22.
- D-041 — Technical Breakdown Slice 4 Approval — đã được Product Owner `APPROVED` ngày 2026-09-22.
- D-042 — Slice 4 Implementation Approval — đã được Product Owner `APPROVED` ngày 2026-09-22.
- Technical Breakdown Slice 4 v0.1 giữ nguyên `APPROVED FOR IMPLEMENTATION`; Purchase Void evidence hardening tại commit `305ffcc1186f7ddb24c5239b16d7b5323cb667bf` đã được review và chấp thuận.
- Slice 4 Stage 4A đã hoàn tất backend/domain/persistence/API cho immutable Completed Return, obligation-first authoritative refund, Restock/NoRestock, Sale Void, safe Purchase Void, correction recovery/idempotency, shared SQL locking, `LedgerSequence`, `ReferencePurchaseCostRevision`, trustworthy `PurchaseLineReversalBasis`, authoritative refund-payment history và migration-upgrade safety.
- Slice 4 Stage 4B đã hoàn tất frontend Return/detail, server-authoritative preview, immutable attempt/recovery, Sale Void, Purchase Void, Owner/Cashier action visibility, net/original transaction projections và void-aware receipt/history. Final preview-consistency hardening bind preview vào exact correction intention, invalidates stale preview và khóa input khi preview đang chạy.
- Product Owner final-approved Slice 4 ngày 2026-09-22. Final reviewed implementation commit `5ab9f5f1456b4dfc6b3baf2b916ba8ba12913874`; GitHub Actions run `35706827677` pass backend build, 60 Domain tests, 57 SQL Server integration tests, frontend build, 16 frontend test files và 70 frontend tests. Không còn known blocker cho Slice 4.
- Real local Playwright đã verify multiple Return (Restock/NoRestock), Sale Void và safe Purchase Void qua Vue → ASP.NET Core → SQL Server. GitHub CI hiện không chạy real E2E, vì vậy local execution không được suy diễn là CI evidence.
- Slice 5 Product Owner scope — Customer/Supplier debt payment và End-of-day — là `APPROVED`.
- D-043–D-050 — derived debt, actual customer/supplier debt payments, no overpayment/advance, no invoice allocation, EOD query-only, Revenue khác Collected và historical-cost Estimated Gross Profit — là `APPROVED` ngày 2026-09-22.
- D-051–D-056 — Customer debt Owner/Cashier access, Owner-only Supplier debt/EOD/profit, Net Collected headline, configurable IANA Store timezone, immutable optional Debt Payment Note và correction không tạo aggregate debt âm — là `APPROVED` ngày 2026-09-22.
- Toàn bộ sáu Product Owner Open Questions chặn ban đầu của Slice 5 đã được giải quyết; không còn known Product Owner blocker trong `OPEN_QUESTIONS.md` cho Slice 5.
- D-057 — Technical Breakdown Slice 5 Approval — là `APPROVED` ngày 2026-09-22; D-043–D-056 giữ nguyên `APPROVED`.
- D-058 — Slice 5 Stage 5A Implementation Approval — là `APPROVED` ngày 2026-09-23 tại reviewed baseline `49098e94c4f44acf3762f3e33ce6be3dfc00758f`.
- D-059 — Slice 5 Stage 5B Implementation Approval — là `APPROVED` ngày 2026-09-23 tại approved head `84f6f8d61bff19f6845204207ccb034bd9219bcd`.
- D-060 — Slice 5 Final Approval — là `APPROVED` ngày 2026-09-23; toàn bộ Slice 5 chuyển thành `APPROVED / COMPLETED`.
- Technical Breakdown Slice 5 v0.1 là `APPROVED FOR IMPLEMENTATION`.
- Slice 5 Stage 5A là `APPROVED / COMPLETED`: derived Customer/Supplier debt, shared immutable DebtPayment, debt read/payment APIs, BusinessOperation recovery, party-level SQL serialization, D-056 Return/Void integration, Store IANA timezone persistence foundation, additive migration và automated domain/SQL Server tests đã được Product Owner final-approve tại D-058.
- Stage 5A giữ invariant đã review: current/authoritative debt đọc toàn bộ committed history, không dùng clock cutoff; historical/as-of debt giữ strict `event timestamp < cutoff` cho future half-open business-date reporting.
- Approved head `49098e94c4f44acf3762f3e33ce6be3dfc00758f`; GitHub Actions run #28 / `35805222057` `SUCCESS` với backend restore, Release build, full `dotnet test`, frontend build và frontend tests. Không có manual Stage 5A E2E evidence được ghi nhận.
- Slice 5 Stage 5B là `APPROVED / COMPLETED` tại D-059 và approved head `84f6f8d61bff19f6845204207ccb034bd9219bcd`: End-of-day query/UI, configurable Store IANA timezone, historical COGS + `CostReliability`, Customer/Supplier debt screens, immutable retry/recovery, stale-balance UX, D-056 Return/Void typed guidance và SQL Server/frontend verification đã được review.
- GitHub Actions run #31 / `35848666879` là `SUCCESS`: Domain tests 78/78, SQL Server integration tests 78/78, frontend tests 82/82 và frontend build pass. Real Stage 5B Playwright E2E đã pass local; CI không được ghi nhận là đã chạy real E2E.
- Stage 5A giữ `APPROVED / COMPLETED` tại D-058, Stage 5B giữ `APPROVED / COMPLETED` tại D-059, và final Slice 5 approval đã hoàn tất tại D-060; toàn bộ approval gate của Slice 5 đã đóng.
- Slice 5 Definition of Done đã được final-review: happy path, important failure/recovery paths, authorization, Store isolation, domain/SQL Server integration/frontend tests, EOD/debt semantics và real local critical Stage 5B E2E evidence. GitHub CI không chạy real Playwright E2E.
- D-061–D-068 — Owner Today landing, summary semantics, C13 explainability, C14 7 completed-business-day velocity, threshold/data sufficiency, intentionally thin attention UI, action transition và experiment measurement — được Product Owner `APPROVED` ngày 2026-09-23.
- D-069–D-072 — exact new-debt-created, SaleCount, C14 full-history/factual sufficiency và active-only Product semantics — được Product Owner `APPROVED` ngày 2026-09-23.
- D-073 — Technical Breakdown Slice 6 Approval — là `APPROVED` ngày 2026-09-23 tại reviewed baseline `daffb39c8f4d75f8bae0d83ba12be0484ce99280`; Technical Breakdown và sequencing Stage 6A/6B chuyển thành `APPROVED FOR IMPLEMENTATION`.
- GitHub Actions run #36 / `35881101954` tại approved baseline `daffb39c8f4d75f8bae0d83ba12be0484ce99280` là `SUCCESS`: backend restore, Release build và `dotnet test` pass; frontend `pnpm install`, build và tests pass. Không ghi nhận real Slice 6 E2E vì implementation chưa bắt đầu.
- S6-Q1–S6-Q4 đã được resolve tại D-069–D-072 và chuyển sang resolved trong `OPEN_QUESTIONS.md`; không còn known Product Owner blocker cho Slice 6 technical semantics.
- D-074 — Slice 6 Stage 6A Implementation Approval — là `APPROVED` ngày 2026-09-23; Stage 6A chuyển thành `APPROVED / COMPLETED`. Không còn known Stage 6A blocker.
- D-075 — Slice 6 Stage 6B Implementation Approval — là `APPROVED` ngày 2026-09-24 tại final reviewed head `844af703173070872219c1cf729afa04d16fa4b4`; Stage 6B chuyển thành `APPROVED / COMPLETED`. Không còn known Stage 6A/6B implementation blocker.
- D-076 — Final Slice 6 Approval — là `APPROVED` ngày 2026-09-24; toàn bộ Slice 6 chuyển thành `APPROVED / COMPLETED`. Stage 6A giữ `APPROVED / COMPLETED — D-074`, Stage 6B giữ `APPROVED / COMPLETED — D-075`; không còn known Slice 6 implementation blocker.
- Stage 6A approved scope gồm shared EOD/Today financial projection, backend-authoritative Store-local Today, Owner-only summary/explanations, D-069 new-debt-created, D-070 SaleCount, backend-selected typed source drill-down, `/today` UI và role-aware default landing. Không có schema/migration mới.
- Reviewed implementation head `14703c54882f10eceaee69fa41f09e2315eab8ac` pass backend restore/Release build với 0 warnings, Domain 78/78, SQL Server integration 82/82, frontend frozen install/build và tests 93/93; GitHub Actions run #39 / `35891666016` là `SUCCESS`. Final docs/state head trước approval `8b11e1f7706bd25e51aca6aa21c1fd8af3baa60c` có run #40 / `35893244524` `SUCCESS`.
- Approved Stage 6A baseline `0b3a77992a3a01ff0881f598ed2c4e14f86855c8` có GitHub Actions run #41 / `35894463232` `SUCCESS` cho backend và frontend.
- Stage 6B là `APPROVED / COMPLETED — D-075` tại final reviewed head `844af703173070872219c1cf729afa04d16fa4b4`: deterministic C14 trên đúng 7 completed Store-local days; D-071 history sufficiency; D-072 active-only candidates; SQL-aggregated Sale − Return − SaleVoid velocity; current Main Warehouse balance; Owner-only preview/list/detail/evidence APIs và UI; Product → Purchase identity-only preselection; narrow immutable `C14ExperimentEvents`; best-effort/deduplicated telemetry; additive migration `20260924010903_ImplementSlice6Stage6BC14`; SQL Server/frontend/real E2E verification.
- Local full regression sau review hardening: backend Release build 0 warnings, Domain 83/83, SQL Server integration 88/88; frontend build pass và tests 100/100; real Slice 6 Playwright 2/2 pass qua Vue → ASP.NET Core → temporary LocalDB. Real coverage gồm D-069 required case `1,000,000 - 0 - 300,000 = 700,000` với standalone CustomerDebtPayment/refund không làm sai new-debt-created; D-070 partial/full Return vẫn count, same-day Void count `0`, cross-day Void không tạo count âm/Today contribution; Supplier `500,000 - 200,000 = 300,000` và standalone SupplierDebtPayment không giảm metric; cùng C14/action/measurement flow hiện có. Clean migration/app startup được thực thi bởi real runner; current-schema upgrade/data preservation được cover bởi SQL Server migration integration test. CI không được ghi nhận là chạy real Playwright E2E.
- GitHub Actions #42 / `35944048457`, #43 / `35944540478`, #44 / `35948003969` và #45 / `35996028241` đều `SUCCESS`; run #45 là CI của Stage 6B approval head với backend/frontend `SUCCESS`. GitHub CI không chạy real Playwright E2E; evidence đó vẫn là local temporary-database verification riêng.
- Giới hạn có chủ ý: C14 là descriptive deterministic experiment, không AI/forecast/replenishment/recommendation; measurement failure không chặn business flow và event counts không tự chứng minh discovery, trust, decision influence, continued use, product value hoặc willingness-to-pay. Local Node 22 phát engine warning vì workspace yêu cầu Node >=24, nhưng frozen install/build/tests đều pass.

## MVP / Pilot Readiness

- D-077–D-084 được Product Owner `APPROVED` ngày 2026-09-24, định nghĩa deployment topology, production account provisioning, inventory pilot completeness, backup/restore, observability/support, printer certification, pilot release gate và pilot validation plan.
- D-085–D-091 được Product Owner `APPROVED` ngày 2026-09-25, resolve chính xác PR-Q1–PR-Q7 cho first Owner bootstrap, Cashier reset, Adjustment/Stocktake costing, stale Stocktake, backup schedule/retention và printer target.
- D-092 — Pilot Readiness Technical Breakdown Approval — được Product Owner `APPROVED` ngày 2026-09-25 tại reviewed baseline `5e5720659cee1fd400001d2a2f7c5b750e0483b6`; GitHub Actions CI #49 / `36037545751` là `SUCCESS`, backend/frontend đều `SUCCESS`. Đây là approval-baseline evidence, không phải PR-A implementation evidence.
- D-093 — Pilot Readiness PR-A Approval — được Product Owner `APPROVED / COMPLETED` ngày 2026-09-26 sau review implementation `ceae40ee97da3468954da8e27b852bcbbfa94113`, integrity hardening `d0abe321f198f05890f566adf137844826973e7a`, và final reviewed state/evidence `bf733a6923b2d0b7c2162b37fdcddd7f42643b74`.
- Tài liệu [Pilot Readiness v0.1](docs/product/pilot-readiness-v0.1.md) là `APPROVED`; mục tiêu hiện tại là đạt `M7 — Pilot-ready` bằng implementation và reviewed evidence. Decision approval không có nghĩa M7 đã đạt.
- [Technical Breakdown Pilot Readiness v0.1](docs/architecture/technical-breakdown-pilot-readiness-v0.1.md) là `APPROVED FOR IMPLEMENTATION — D-092`; approved sequence gồm PR-A functional blockers, PR-B operational safety và PR-C certification/release gate.
- PR-Q1–PR-Q7 đã `RESOLVED` tại D-085–D-091 và không được reopen; D-092 approve architecture/sequence/contracts nhưng không phải implementation completion.
- PR-A — Functional pilot blockers: `APPROVED / COMPLETED — D-093`; implementation commit `ceae40ee97da3468954da8e27b852bcbbfa94113`, integrity-hardening head `d0abe321f198f05890f566adf137844826973e7a`, Product Owner approval baseline `bf733a6923b2d0b7c2162b37fdcddd7f42643b74`.
- PR-B — Operational safety: `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW` tại implementation commit `1284487939b26a7370b499d48d760e9335b011cc`.
- PR-C — Pilot certification and release gate: `APPROVED FOR IMPLEMENTATION / NOT STARTED`.
- `PR-BLOCKER-01` — `CLOSED — D-093`: production-safe Owner bootstrap và Store-scoped Cashier lifecycle đã được Product Owner review/approve.
- `PR-BLOCKER-02` — `CLOSED — D-093`: Stock Adjustment, Stocktake và inventory completeness evidence đã được Product Owner review/approve.
- `PR-BLOCKER-03` — `OPEN`: full/log backup, verify, conservative retention/freshness tooling và actual isolated local restore drill đã implement/pass; pilot separate-failure-domain storage, scheduled nightly/15-minute jobs, elapsed retention/alerting và actual operator evidence còn thiếu.
- `PR-BLOCKER-04` — `OPEN`: versioned artifact, prebuilt SPA hosting, checksum/manifest, explicit migration, deploy/smoke/rollback scripts và runbook đã implement; `PRODUCTION-LIKE IIS SMOKE PENDING — ENVIRONMENT LIMITATION` vì máy hiện tại không có elevated IIS/WebAdministration/pilot certificate-app-pool environment.
- `PR-BLOCKER-05` — `OPEN`: JSON rolling files, trace/version/UserId/StoreId correlation, DB-aware readiness, generic health responses và support runbook đã implement/test; real server retention/restart/support-operator exercise chưa có reviewed evidence.
- `PR-BLOCKER-06` — browser print chưa certify trên target paper size và 1–2 real printer configurations.
- `PR-BLOCKER-07` — chưa có formal pilot release checklist, production-like smoke và rollback/recovery gate.
- Approved PR-A verification: Release build `0 warnings / 0 errors`; Domain `96/96`; SQL Server integration `97/97`; frontend `23 test files / 106 tests`; production frontend build `PASS`; real PR-A Playwright `1/1 PASS` qua Vue → ASP.NET Core → temporary LocalDB/SQL Server test environment. CI không chạy real Playwright.
- GitHub Actions CI #52 / `36231423946` tại hardening head và CI #53 / `36231672552` tại final reviewed HEAD đều `SUCCESS`; run #53 pass cả backend và frontend.
- Approved migration `20260926023259_ImplementPilotReadinessPrA` pass clean database migration và upgrade từ Slice 6 baseline với existing Identity/Store/Product/InventoryBalance preservation; integrity hardening không cần migration mới.
- PR-B artifact `SimpleStore-0.1.0-1284487939b2.zip` được build từ exact commit, chứa prebuilt SPA + backend publish + IIS `web.config` + Windows migration bundle + manifest 109 files, không cần Node trên production; SHA-256 `ec569ffae1b9db6c0ba87709a011ed6f57143ff1c8691c379746c310527e01e1` khớp checksum.
- PR-B local actual restore drill `PASS`: SQL Express/LocalDB `FULL`, native full + transaction-log backup, checksum/`RESTORE VERIFYONLY`, ordered restore vào separate temporary database, `DBCC CHECKDB`, đọc Store tạo sau full từ log chain, start exact published artifact, readiness, authenticated version/session/Store/product read, direct SPA/API fallback và structured trace logging đều pass. Backup drill storage vẫn ở cùng local failure domain nên không thay thế pilot infrastructure evidence.
- PR-B regression: Release build `0 warnings / 0 errors`; Domain `96/96`; SQL Server integration `100/100`; frontend production build pass và `23` files / `106` tests; real PR-A Playwright `1/1`; real Slice 6 Playwright `2/2`.
- `PR-BLOCKER-03..07` vẫn `OPEN`. Các blocker readiness còn lại không revoke trạng thái completed của Slice 0–6 hoặc PR-A. `M7 — NOT ACHIEVED`; `Pilot — NOT STARTED`; `Production readiness — NOT DECLARED`; `C14 value / willingness-to-pay — NOT VALIDATED` và phải được đánh giá trong pilot theo D-084.

## Tiến độ

### Step 1 — Hoàn thành

- Product Vision v0.1 — `APPROVED`.
- Problem Definition v0.1 — `APPROVED`.

### Step 2 — Hoàn thành

- Primary Persona v0.1 — `APPROVED`.
- Jobs To Be Done v0.1 — `APPROVED`.
- Differentiator Hypothesis “Cho tôi biết điều gì cần chú ý và nên làm gì tiếp theo” — ưu tiên cao, chưa được phê duyệt là JTBD.
- Cơ sở: [`docs/research/step-2-market-user-research.md`](docs/research/step-2-market-user-research.md).

### Step 3 — Hoàn thành

- Current User Journey v0.1 — `APPROVED`.
- Pain Point Map v0.1 — `APPROVED`.
- Product Model DO → TRUST → UNDERSTAND & ACT — `APPROVED`.
- Nguyên tắc: Nếu DO và TRUST chưa tốt thì UNDERSTAND & ACT không đáng tin.

### Step 4 — Hoàn thành

- Product Principles v0.1 gồm 6 principles — `APPROVED`.
- Product Principle Evaluation Checklist được lưu cùng tài liệu để hỗ trợ review feature.

### Step 5 — Hoàn thành

- Capability Map v0.1 gồm 19 capability thuộc DO, TRUST, UNDERSTAND & ACT và FOUNDATION — `APPROVED`.
- Capability Map chưa quyết định capability nào thuộc MVP.
- C14 — Attention & Decision Support đã được phê duyệt là capability; value và willingness-to-pay liên quan vẫn cần được kiểm chứng.

### Step 6 — Hoàn thành

- MVP Scope v0.1 — `APPROVED`.
- MVP phục vụ vận hành thật cho 1 cửa hàng, 1 kho chính, 1 chủ cửa hàng và một số nhân viên bán hàng.
- Vòng end-to-end: khởi tạo → tạo/import hàng → nhập hàng → bán + thanh toán + in → tồn/tiền/giá vốn tự cập nhật → trả hoặc sửa giao dịch → đối soát cuối ngày → hiểu tình hình hôm nay → biết ít nhất một việc đáng chú ý.
- CORE: C1, C2, C3, C4, C8, C9, C10, C15, C17.
- CORE-LITE: C5, C6, C11, C16, C18.
- DIFFERENTIATOR-LITE / EXPERIMENT: C12, C13, C14; C14 chỉ là experiment mỏng, ưu tiên deterministic/rule-based, nhu cầu và willingness-to-pay chưa validated.
- C19 chỉ gồm integration thực sự cần cho MVP. C7 chỉ chừa integration point cho service API HĐĐT riêng của Product Owner; không xây capability HĐĐT nội bộ.
- DO và TRUST phải đủ chắc; resilience phải ngăn double sale và dữ liệu nửa vời khi timeout, double-submit, retry hoặc lỗi thiết bị/external API. Không yêu cầu full offline.
- Tài liệu: [`docs/product/mvp-scope-v0.1.md`](docs/product/mvp-scope-v0.1.md).
- Các nội dung Step 1–5 ở trên ghi nhận phạm vi phê duyệt tại từng bước và được giữ nguyên; phạm vi MVP hiện tại được chốt tại Step 6.

### Step 7 — Hoàn thành

- Capability Decomposition / Functional Scope v0.1 — `APPROVED`.
- Functional scope theo capability, giữ rõ MUST/SHOULD, phạm vi tối thiểu, experiment và integration boundary.
- Mỗi feature trace tới Capability → JTBD / Pain Point → MVP Outcome; feature không trace được mặc định không đưa vào MVP tới khi chứng minh được lý do.
- 6 vertical slices: Setup + Product; Purchase → Inventory; Sale → Payment → Print; Return / Void → dữ liệu vẫn đúng; End-of-day → tiền + tồn + lãi gộp; “Hôm nay cửa hàng thế nào?” + 1 attention experiment.
- C14 chỉ thử nghiệm nguy cơ sắp hết hàng từ tồn hiện tại và tốc độ bán gần đây; nhu cầu/value/willingness-to-pay chưa validated.
- C7 không build: chỉ giữ domain sale đủ sạch cho việc ánh xạ sang service API HĐĐT hiện có sau này. C19 chỉ integration cần cho vòng MVP.
- Chưa thiết kế database schema, API contract, UI chi tiết hoặc architecture implementation; không thêm feature.
- Nội dung Step 1–6 đã APPROVED được giữ nguyên.
- Tài liệu: [`docs/capabilities/mvp-functional-scope-v0.1.md`](docs/capabilities/mvp-functional-scope-v0.1.md).

### Step 8 — Hoàn thành

- MVP User Flows v0.1 — `APPROVED`.
- 6 flows: Setup + Product; Purchase → Inventory; Sale → Payment → Print; Return / Void; End-of-day Reconciliation; “Hôm nay cửa hàng thế nào?”.
- Mỗi critical flow có Happy Path, Failure Path và Recovery Path; mô tả User Action → System Behavior → Business Outcome.
- Transaction Completed bất biến; correction có audit. Complete Sale, Complete Purchase, Create Return, Void Transaction cần identity riêng và idempotency.
- Timeout kiểm tra operation cũ: Completed trả kết quả cũ; Failed retry an toàn; Processing/Unknown không tùy tiện tạo operation mới.
- Purchase chỉ Completed khi tồn/giá vốn/công nợ NCC nhất quán. Return không vượt số còn được trả, restock phải do người dùng chọn.
- Printing là hậu xử lý: print lỗi không rollback sale Completed, phải cho Reprint.
- Doanh thu khác tiền thu; C14 chỉ signal nguy cơ sắp hết hàng có evidence, chưa validated value/willingness-to-pay.
- Giữ nguyên Step 1–7; không thêm feature hoặc thiết kế database/API/UI/architecture.
- Tài liệu: [`docs/ux/mvp-user-flows-v0.1.md`](docs/ux/mvp-user-flows-v0.1.md).

### Step 9 — Hoàn thành

- Domain Model v0.1 — `APPROVED`: domain concepts, relationships, 13 invariants, costing và payment/debt behavior.
- Decision A — `APPROVED`: Moving Weighted Average; SaleLine giữ cost snapshot, Return dùng cost basis gốc; Purchase reversal đảo đúng quantity/value contribution.
- Decision B — `APPROVED`: Payment là tiền thực thu/thực trả; một transaction có thể có nhiều Payment; trả nợ khách/NCC sau ghi nhận Payment và giảm outstanding debt.
- InventoryMovement là nguồn giải thích tồn; debt giải thích được từ transaction/payment; các số liệu tổng hợp là derived information.
- Completed Sale/Purchase/Return bất biến, không hard-delete; ReturnLine tham chiếu OriginalSaleLine; consistency/idempotency Step 8 giữ nguyên.
- Customer chỉ đủ nhận diện người nợ, không CRM; không generic accounting ledger.
- Giữ nguyên Step 1–8; chưa thiết kế database/API/EF Core/frontend model hoặc architecture implementation chi tiết.
- Tài liệu: [`docs/architecture/domain-model-v0.1.md`](docs/architecture/domain-model-v0.1.md).

### Step 10 — Hoàn thành

- Architecture v0.1 — `APPROVED`.
- MVP dùng Modular Monolith: Vue 3 SPA → ASP.NET Core Backend → SQL Server; một deployable backend và một database, chia domain/module boundary rõ.
- Backend định hướng `SimpleStore.Api`, `SimpleStore.Application`, `SimpleStore.Domain`, `SimpleStore.Infrastructure`; SQL Server + EF Core.
- Critical operation dùng client-generated `OperationId`/`IdempotencyKey`; business changes và operation result commit atomically trong cùng local transaction; retry sau timeout không tạo duplicate.
- `InventoryMovement` là ledger có thể giải thích/audit; `InventoryBalance` là materialized current state. Cả hai cập nhật atomically; mutation cùng Product + Warehouse phải được kiểm soát concurrency và dùng deterministic order khi xử lý nhiều balance.
- Không cho direct retroactive Purchase Void khi downstream movement làm Moving Weighted Average không còn an toàn; không xây retroactive costing/revaluation engine trong MVP.
- `AllowNegativeStock` ở cấp Store, mặc định `false`, chỉ Owner thay đổi và có audit. Khi bật, balance có thể âm; cost dùng last known average cost, fallback reference purchase cost; profit phải thể hiện là ước tính nếu cost chưa đáng tin.
- Reporting dùng read query/projection trên transactional data; C14 là deterministic rule/query, không AI subsystem.
- Printing và HĐĐT nằm ngoài core Sale transaction; failure bên ngoài không mặc định rollback Sale. Backend là authorization boundary.
- Deployment ưu tiên Windows Server/IIS, Vue static files, ASP.NET Core API và SQL Server; không cần distributed infrastructure phức tạp.
- Giữ nguyên Step 1–9; chưa thiết kế SQL schema/API contract/EF mapping/frontend component tree/UI hoặc production infrastructure chi tiết.
- Tài liệu: [`docs/architecture/architecture-v0.1.md`](docs/architecture/architecture-v0.1.md).

### Step 11 — Hoàn thành

- Development Plan / Technical Design Breakdown v0.1 — `APPROVED`.
- Foundation → Setup + Product → Purchase → Inventory → Sale → Payment → Print → Return / Void → Debt + End-of-day → Understand & Act → Pilot, theo vertical slice end-to-end.
- Chốt engineering foundation, repo structure, stack direction, thin Controllers/use cases, typed errors, cookie auth, explicit migrations và testing direction.
- Decision D: 1 tenant/account → 1 Store → 1 Main Warehouse; shared deployment/database có thể nhiều tenant, business data scope theo Store/Tenant và phải test isolation.
- Decision E: import template cố định, Validate → Preview → Confirm all-or-nothing; không partial import.
- Slice 1: Product + OpeningBalance movement + InventoryBalance ghi atomically; tồn đầu dương cần cost hợp lệ, retry confirm không duplicate.
- Milestones M0–M7; mỗi slice phải chạy thật Vue → API → DB và thỏa Definition of Done; không estimate ngày cứng.
- Giữ nguyên Step 1–10; không mở rộng functional scope hoặc triển khai code trong lần cập nhật này.
- Tài liệu: [Development Plan](docs/architecture/development-plan-v0.1.md), [Technical Breakdown Slice 0–1](docs/architecture/technical-breakdown-slice-0-1-v0.1.md).

### Bước tiếp theo

**Complete/review PR-B environment evidence.** PR-B là `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW`; cần production-like Windows Server/IIS deploy/rollback, pilot separate-storage scheduled backup/retention/alert evidence và actual operator runbook exercise. PR-BLOCKER-03/04/05 vẫn mở. Không bắt đầu PR-C trong task này.

## Chưa triển khai

- Ngoài phạm vi: General Ledger, accounting close/reopen, full cashbook, debt aging, invoice-level settlement allocation, customer credit balance, supplier advance, financial statements, operating-expense accounting, net profit, BI dashboard và AI.
- HĐĐT và các capability chưa được triển khai khác. Slice 6 đã đóng governance gate tại D-076; các capability ngoài scope không được tự động approve bởi quyết định này.
- Real Slice 1–5B Playwright flows chạy local trên Windows/LocalDB; CI tiếp tục dùng SQL Server integration tests và chưa chạy real E2E.

## Cập nhật gần nhất

2026-09-26 — PR-B — Operational safety implemented tại `1284487939b26a7370b499d48d760e9335b011cc`: single IIS-ready publish phục vụ prebuilt Vue SPA; safe authenticated version metadata; JSON rolling logs/correlation; live/DB-ready/generic health; versioned manifest/checksum artifact; explicit EF migration bundle; deployment/smoke/rollback tooling; D-090 FULL/full/log/verify/freshness/conservative-retention scripts; deployment, backup/restore và support runbooks/evidence templates. Artifact checksum pass; no Node production dependency; không có migration mới. Local actual full+log restore drill vào database cô lập, DBCC/data/readiness/authenticated restored-app smoke và logging correlation pass. Regression: Release build 0 warnings/0 errors, Domain 96/96, SQL Server integration 100/100, frontend 23 files/106 tests + build, PR-A E2E 1/1, Slice 6 E2E 2/2. IIS production-like exercise không thể chạy vì thiếu elevated IIS/WebAdministration; pilot separate-storage schedules/retention/alert/operator evidence chưa có. Vì vậy PR-B giữ `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW`; PR-BLOCKER-03/04/05 vẫn `OPEN`, không tạo D-094, PR-C chưa bắt đầu và M7 chưa đạt.
