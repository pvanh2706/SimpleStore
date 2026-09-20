# Project State

## Giai đoạn hiện tại

**Slice 0 — Engineering Foundation đã được triển khai và kiểm chứng**

## Primary Persona

Chủ cửa hàng tạp hóa nhỏ tại Việt Nam, trực tiếp tham gia vận hành và chịu trách nhiệm ít nhất cho bán hàng, nhập hàng/tồn kho và kết quả kinh doanh — `APPROVED`.

## Phạm vi hiện tại

- Step 1–11 đã APPROVED; kế hoạch triển khai theo vertical slice đã được ghi nhận.
- Slice 0 Engineering Foundation đã có backend, frontend, testing và CI foundation theo D-015.
- Migration hiện tại chỉ tạo ASP.NET Core Identity schema; chưa có domain table của Slice 1.
- Bước triển khai tiếp theo là Slice 1 — Setup + Product.

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

**Ready to begin implementation — Slice 0 Engineering Foundation**

Sau đó: **Slice 1 — Setup + Product**.

## Chưa triển khai trong lần cập nhật tài liệu này

- Phát triển frontend.
- Phát triển backend.
- Thiết kế hoặc triển khai database.
- Viết source code ứng dụng.

## Cập nhật gần nhất

2026-09-21 — Hoàn thành implementation Slice 0 Engineering Foundation: .NET/Vue solution skeleton, SQL Server + Identity/EF migration foundation, secure cookie direction, ProblemDetails, health/OpenAPI, test foundations và CI. Chưa triển khai business functionality của Slice 1. Các quyết định Step 1–11 giữ nguyên.
