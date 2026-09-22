# Project State

## Giai đoạn hiện tại

**Slice 5 — Stage 5A Debt backend/domain/persistence/tests: `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`; Stage 5B: `NOT STARTED / PLANNED`**

Slice 4 — Return / Void / Recovery giữ trạng thái `APPROVED / COMPLETED`; baseline trước Slice 5 là commit `5876082a`.

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
- Technical Breakdown Slice 5 v0.1 là `APPROVED FOR IMPLEMENTATION`.
- Slice 5 Stage 5A là `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`: derived Customer/Supplier debt, shared immutable DebtPayment, debt read/payment APIs, BusinessOperation recovery, party-level SQL serialization, D-056 Return/Void integration và Store IANA timezone persistence foundation đã có production code, additive migration và automated domain/SQL Server tests.
- Stage 5B vẫn `NOT STARTED / PLANNED`; chưa implement End-of-day aggregation/UI, debt management screens, dashboard/charts hoặc full Stage 5B E2E. Toàn bộ Slice 5 chưa được đánh dấu completed và chưa có Product Owner implementation approval.

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

**Product Owner review Stage 5A.** Stage 5A hiện `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`. Stage 5B giữ `NOT STARTED / PLANNED` và chỉ bắt đầu theo sequencing/review process đã approve; không suy diễn việc Stage 5A implemented thành approval hoặc hoàn thành toàn bộ Slice 5.

## Chưa triển khai

- Stage 5B: End-of-day aggregation/reporting, Customer/Supplier debt frontend hoàn chỉnh, EOD UI/dashboard/charts, Estimated Gross Profit presentation và full Stage 5B E2E/recovery UX.
- C14, HĐĐT và các capability ngoài Slice 1.
- Real Slice 1, Slice 2, Slice 3 và Slice 4 Playwright flows chạy local trên Windows/LocalDB; CI tiếp tục dùng SQL Server integration tests và chưa chạy real E2E.

## Cập nhật gần nhất

2026-09-23 — Từ implementation baseline `5115d9100b1541be21fb2d92a80a58b7acdd1142`, Stage 5A đã implement production backend/domain/persistence/tests và đang `PENDING PRODUCT OWNER REVIEW`. Stage 5B vẫn `NOT STARTED / PLANNED`; full Slice 5 chưa completed. Slice 4 giữ `APPROVED / COMPLETED`.
