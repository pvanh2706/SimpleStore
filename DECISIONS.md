# Decision Log

File này ghi lại các quyết định sản phẩm và trạng thái phê duyệt của chúng.

## Quy ước trạng thái

- `PROPOSED`: đang được đề xuất hoặc xem xét.
- `APPROVED`: đã được Product Owner phê duyệt rõ ràng.
- `REJECTED`: đã bị từ chối.
- `SUPERSEDED`: đã được thay thế bởi quyết định khác.

## Quyết định

### D-001 — Product Vision v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-19
- **Người phê duyệt:** Product Owner
- **Quyết định:** SimpleStore giúp chủ cửa hàng nhỏ vận hành và hiểu tình hình kinh doanh mà không cần giỏi phần mềm hoặc kế toán. SimpleStore không được định vị chỉ là một hệ thống POS.
- **Tài liệu:** [`docs/product/product-vision-v0.1.md`](docs/product/product-vision-v0.1.md)

### D-002 — Problem Definition v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-19
- **Người phê duyệt:** Product Owner
- **Quyết định:** Problem Definition gồm hai lớp vấn đề chính: Adoption Problem và Decision Problem. Decision Problem là vấn đề sâu hơn cần tiếp tục nghiên cứu.
- **Tài liệu:** [`docs/product/problem-definition-v0.1.md`](docs/product/problem-definition-v0.1.md)

### D-003 — Primary Persona v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Primary Persona là chủ cửa hàng tạp hóa nhỏ tại Việt Nam, trực tiếp tham gia vận hành và chịu trách nhiệm ít nhất cho bán hàng, nhập hàng/tồn kho và kết quả kinh doanh. Không mặc định persona lớn tuổi, không biết công nghệ hoặc không biết kế toán.
- **Tài liệu:** [`docs/product/primary-persona-v0.1.md`](docs/product/primary-persona-v0.1.md)
- **Cơ sở nghiên cứu:** [`docs/research/step-2-market-user-research.md`](docs/research/step-2-market-user-research.md)

### D-004 — Jobs To Be Done v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** JTBD v0.1 gồm bảy job liên quan đến bán hàng, độ tin cậy của tồn kho, quyết định nhập hàng, tiền và công nợ, xử lý ngoại lệ, lợi nhuận và HĐĐT/thuế.
- **Ngoài phạm vi phê duyệt:** “Cho tôi biết điều gì cần chú ý và nên làm gì tiếp theo” vẫn là Differentiator Hypothesis ưu tiên cao, chưa phải JTBD đã được phê duyệt.
- **Tài liệu:** [`docs/product/jobs-to-be-done-v0.1.md`](docs/product/jobs-to-be-done-v0.1.md)
- **Cơ sở nghiên cứu:** [`docs/research/step-2-market-user-research.md`](docs/research/step-2-market-user-research.md)

### D-005 — Current User Journey v0.1, Pain Point Map v0.1 và Product Model

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Current User Journey v0.1 gồm 8 giai đoạn; Pain Point Map v0.1 gồm P1–P7; Product Model là DO → TRUST → UNDERSTAND & ACT.
- **Nguyên tắc:** Nếu DO và TRUST chưa tốt thì UNDERSTAND & ACT không đáng tin.
- **Tài liệu:** [`docs/product/current-user-journey-v0.1.md`](docs/product/current-user-journey-v0.1.md), [`docs/product/pain-point-map-v0.1.md`](docs/product/pain-point-map-v0.1.md), [`docs/product/product-model-do-trust-understand-act.md`](docs/product/product-model-do-trust-understand-act.md)

### D-006 — Product Principles v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Principles v0.1 gồm 6 principles: Simple on the surface, capable underneath; Reliability before intelligence; Design around user outcomes, not software modules; Explain instead of exposing complexity; Protect the critical path; Information should lead toward action.
- **Lưu ý:** “Dễ dùng” không phải Product Principle độc lập vì quá chung chung.
- **Tài liệu:** [`docs/product/product-principles-v0.1.md`](docs/product/product-principles-v0.1.md)

### D-007 — Capability Map v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Capability Map v0.1 gồm 19 capability thuộc bốn nhóm DO, TRUST, UNDERSTAND & ACT và FOUNDATION.
- **Ranh giới:** Capability Map chưa quyết định capability nào thuộc MVP. AI không phải capability cấp cao mà chỉ là một possible implementation mechanism.
- **Chưa được xác nhận:** C14 — Attention & Decision Support thuộc Capability Map đã được phê duyệt, nhưng nhu cầu và willingness-to-pay cho outcome liên quan vẫn chưa được validated.
- **Tài liệu:** [`docs/capabilities/capability-map-v0.1.md`](docs/capabilities/capability-map-v0.1.md)

### D-008 — MVP Scope v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** MVP phải đủ để một cửa hàng tạp hóa nhỏ vận hành thật theo vòng end-to-end từ khởi tạo, tạo/import hàng, nhập hàng, bán + thanh toán + in, tự cập nhật tồn/tiền/giá vốn, xử lý trả hoặc sửa giao dịch, đối soát cuối ngày đến hiểu “Hôm nay cửa hàng thế nào?” và biết ít nhất một việc đáng chú ý.
- **Ranh giới:** 1 cửa hàng, 1 kho chính, 1 chủ cửa hàng và một số nhân viên bán hàng; chưa hỗ trợ multi-branch đầy đủ hoặc kế toán đầy đủ.
- **CORE:** C1, C2, C3, C4, C8, C9, C10, C15, C17.
- **CORE-LITE:** C5, C6, C11, C16, C18.
- **DIFFERENTIATOR-LITE / EXPERIMENT:** C12, C13, C14. C14 chỉ là experiment mỏng, ưu tiên rule-based/deterministic, chưa cần AI; nhu cầu, value và willingness-to-pay vẫn chưa validated.
- **LIMITED / OUT OF BUILD SCOPE:** C19 chỉ gồm integration thực sự cần cho MVP. C7 chỉ chừa integration point để sau này gọi service API HĐĐT riêng của Product Owner; không xây capability HĐĐT nội bộ trong MVP.
- **Nguyên tắc:** Vòng vận hành hoàn chỉnh; DO và TRUST trước UNDERSTAND & ACT; C8–C11 là nền tảng TRUST. Không cần full offline nhưng phải xử lý timeout, double-submit, retry, lỗi thiết bị/external API mà không tạo double sale hoặc dữ liệu nửa vời.
- **Ngoài MVP:** CRM đầy đủ, loyalty phức tạp, marketing automation, website/e-commerce, multi-branch đầy đủ, kế toán đầy đủ, AI chatbot, forecasting phức tạp, dashboard BI lớn, phân quyền enterprise, full offline synchronization.
- **Bảo toàn quyết định:** Không thay đổi Step 1–5 đã `APPROVED`; không tự mở rộng MVP.
- **Tài liệu:** [`docs/product/mvp-scope-v0.1.md`](docs/product/mvp-scope-v0.1.md)

### D-009 — Capability Decomposition / Functional Scope v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Phê duyệt Step 7: functional scope cụ thể theo capability, với các mức MUST/SHOULD, phạm vi tối thiểu, experiment và integration boundary được ghi trong tài liệu.
- **Traceability:** Feature → Capability → JTBD / Pain Point → MVP Outcome. Feature không trace được mặc định không đưa vào MVP tới khi chứng minh được lý do.
- **Vertical slices:** (1) Setup + Product; (2) Purchase → Inventory; (3) Sale → Payment → Print; (4) Return / Void → dữ liệu vẫn đúng; (5) End-of-day → tiền + tồn + lãi gộp; (6) “Hôm nay cửa hàng thế nào?” + 1 attention experiment. Ưu tiên end-to-end outcome thay vì module completeness.
- **Giới hạn pilot:** C5 trả toàn bộ/một phần và void có audit, không sửa trực tiếp transaction hoàn tất hoặc xây correction framework tổng quát. C6 chỉ theo dõi tiền/nợ sale và purchase cùng số liệu cuối ngày. Import template cố định; 2 role Owner/Cashier.
- **TRUST và resilience:** Giao dịch nhất quán, idempotency cho critical operation, retry an toàn, trạng thái rõ khi timeout; printer lỗi không làm mất sale và phải có thể reprint. Không hard-delete transaction quan trọng. Phân biệt doanh thu, tiền thu, công nợ, giá vốn và lãi gộp; trả/hủy phản ánh đúng.
- **C14:** Chỉ experiment nguy cơ sắp hết hàng từ tồn hiện tại và tốc độ bán gần đây, có dữ liệu giải thích và xem chi tiết; không khuyến nghị mạnh khi dữ liệu chưa đáng tin. Nhu cầu, value/willingness-to-pay chưa validated; không xây subsystem alert lớn hoặc AI recommendation engine.
- **C7/C19:** C7 không build trong MVP; chỉ giữ domain sale đủ sạch để sau này ánh xạ sang request của service API HĐĐT hiện có mà không phá cấu trúc giao dịch. C19 chỉ integration thực sự cần cho vòng MVP.
- **Ranh giới bước này:** Không thiết kế database schema, API contract, UI chi tiết hoặc architecture implementation. Không thêm feature; giữ nguyên Step 1–6 đã APPROVED.
- **Bước tiếp theo:** Step 8 — MVP User Flows.
- **Tài liệu:** [`docs/capabilities/mvp-functional-scope-v0.1.md`](docs/capabilities/mvp-functional-scope-v0.1.md)

### D-010 — MVP User Flows v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Phê duyệt Step 8 gồm 6 user flows end-to-end: Setup + Product; Purchase → Inventory; Sale → Payment → Print; Return / Void; End-of-day Reconciliation; “Hôm nay cửa hàng thế nào?”.
- **Cấu trúc flow:** User Action → System Behavior → Business Outcome → Failure Path → Recovery Path. Mỗi critical flow có Happy Path, Failure Path và Recovery Path.
- **Immutability:** Transaction Completed không sửa trực tiếp hoặc hard-delete; correction qua Return / Void / Reverse phù hợp và có audit.
- **Operation identity và idempotency:** Ít nhất Complete Sale, Complete Purchase, Create Return và Void Transaction phải có identity riêng và retry an toàn; cùng operation không tạo business transaction mới. Đây là yêu cầu hành vi, chưa chốt implementation.
- **Timeout recovery:** Timeout không đồng nghĩa thất bại. Kiểm tra operation: Completed trả kết quả cũ; Failed cho retry an toàn; Processing/Unknown từ góc nhìn client không được tùy tiện tạo operation mới.
- **Purchase consistency:** Chỉ Completed khi tồn kho, giá vốn và công nợ NCC nhất quán; không báo thành công khi cập nhật một phần.
- **Return / Void:** Return không vượt số đã bán trừ số đã trả trước đó; restock tăng tồn, no restock không tăng tồn, hệ thống không tự đoán. Void khác Return và giữ original transaction, Void/reversal transaction, audit reason. MVP có thể giới hạn chỉ Owner Void transaction Completed.
- **Printing:** Là hậu xử lý; Sale Completed + Print Failed phải hiển thị bán thành công và cho Reprint. Print failure không rollback sale.
- **Đối soát / hiểu tình hình:** Phân biệt doanh thu và tiền đã thu, xem giao dịch nguồn khi lệch; chỉ gọi lãi gộp/lãi gộp ước tính khi chưa quản lý đầy đủ chi phí. C14 chỉ experiment nguy cơ sắp hết hàng, hiển thị evidence, không recommendation mạnh khi dữ liệu chưa tin cậy; willingness-to-pay chưa validated.
- **Ranh giới:** Giữ nguyên Step 1–7; không thêm feature, không thiết kế database schema, API contract, UI/wireframe chi tiết hoặc architecture implementation. C7 vẫn chỉ là integration point.
- **Bước tiếp theo:** Step 9 — Domain Model.
- **Tài liệu:** [`docs/ux/mvp-user-flows-v0.1.md`](docs/ux/mvp-user-flows-v0.1.md)

### D-011 — Domain Model v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Phê duyệt Step 9 — conceptual domain model gồm Store, Warehouse, User, Product, Customer tối thiểu, Supplier, Purchase/PurchaseLine, Sale/SaleLine, Return/ReturnLine, Payment, InventoryMovement, StockAdjustment và Reversal/Audit relationships.
- **Inventory:** InventoryMovement giải thích nguồn tồn, quantity/value và cost basis; mọi thay đổi tồn có movement/source. Current stock là tổng movement hợp lệ; cache/materialization không thay thế nguồn nghiệp vụ có thể audit.
- **Cost history / Return:** SaleLine giữ UnitCostAtSale/CostBasis tại Sale Completed; không lấy CurrentCost để tính lại lịch sử. ReturnLine tham chiếu OriginalSaleLine, không vượt số còn được trả; restock phục hồi quantity/value theo cost basis gốc.
- **Invariants:** Phê duyệt 13 domain invariants trong tài liệu. Completed Sale/Purchase/Return bất biến; không hard-delete; correction có reversal/audit; consistency/idempotency Step 8 tiếp tục áp dụng. Số liệu tổng hợp và “Hôm nay cửa hàng thế nào?” là projection từ domain data.
- **Ranh giới:** Giữ nguyên Step 1–8; không thêm feature ngoài nội dung được duyệt, không thiết kế SQL/database schema, API contracts, EF Core entities, frontend model hoặc architecture implementation chi tiết.
- **Bước tiếp theo:** Step 10 — Architecture.
- **Tài liệu:** [`docs/architecture/domain-model-v0.1.md`](docs/architecture/domain-model-v0.1.md)

### D-012 — Step 9 / Decision A — Costing Method

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** MVP dùng Moving Weighted Average — Bình quân gia quyền di động.
- **Hành vi:** SaleLine snapshot giá vốn khi Sale Completed. Return dùng cost basis của SaleLine gốc; restock phục hồi quantity và inventory value tương ứng. Purchase reversal phải đảo đúng quantity/value contribution của transaction gốc theo business semantics, không chỉ giảm quantity.
- **Ranh giới:** Không đưa lot/FIFO/serial/expiry costing complexity vào MVP. Các câu hỏi chọn phương pháp giá vốn ở bước trước được giải quyết bởi quyết định này; tài liệu Step 1–8 được giữ nguyên.
- **Tài liệu:** [Domain Model v0.1 — Decision A](docs/architecture/domain-model-v0.1.md#decision-a--costing-method--approved)

### D-013 — Step 9 / Decision B — Payment & Debt Model

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Quyết định:** Payment chỉ biểu diễn tiền thực nhận/thực trả, không đồng nghĩa Debt. Domain cho phép nhiều Payment liên quan một transaction.
- **Hành vi:** Customer trả nợ sau hoặc cửa hàng trả nợ NCC sau đều ghi nhận Payment và giảm Outstanding Debt. Debt phải suy ra, giải thích được từ transaction, payment và returns/reversals/adjustments phù hợp; không sửa Customer.Debt hoặc Supplier outstanding debt tùy ý.
- **Ranh giới:** Customer tối thiểu để xác định người đang nợ, không CRM; Payment/Debt không trở thành generic accounting ledger đầy đủ. Sale Payment, Customer Debt Payment, Purchase Payment và Supplier Debt Payment là các vai trò conceptual; việc dùng chung abstraction chưa chốt.
- **Tài liệu:** [Domain Model v0.1 — Decision B](docs/architecture/domain-model-v0.1.md#decision-b--payment--debt-model--approved)

### D-014 — Architecture v0.1

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Architecture style:** MVP dùng Modular Monolith: Vue 3 SPA → HTTPS/JSON → ASP.NET Core Backend → SQL Server. Một backend deployable và một database; code chia domain/module boundary rõ, không coi module là microservice.
- **Backend structure:** Định hướng `SimpleStore.Api`, `SimpleStore.Application`, `SimpleStore.Domain`, `SimpleStore.Infrastructure`; Domain không phụ thuộc EF Core/API/Vue. SQL Server + EF Core là persistence direction.
- **Decision A — Transaction/Idempotency:** Critical operation dùng client-generated `OperationId`/`IdempotencyKey`; retry cùng operation đã Completed trả kết quả cũ; cùng ID nhưng request khác bị từ chối. Business changes và operation result commit atomically trong cùng local SQL transaction.
- **Decision B — Inventory:** `InventoryMovement` là immutable/explainable ledger; `InventoryBalance` là materialized operational state. Hai phần cập nhật atomically. Concurrent mutation cùng `Product + Warehouse` phải được serialized/controlled; operation nhiều sản phẩm dùng deterministic order. MVP không cho direct retroactive Purchase Void khi downstream movement làm costing không còn an toàn và không xây revaluation engine.
- **Decision C — Negative stock:** Store có `AllowNegativeStock`, mặc định `false`, chỉ Owner thay đổi và có audit. Khi tắt, backend từ chối Sale làm âm tồn và báo lượng thiếu. Khi bật, Sale được hoàn tất, movement vẫn ghi và balance có thể âm. Cost tạm dùng last known average cost, fallback reference purchase cost; profit phải thể hiện là ước tính khi cost chưa đáng tin.
- **Read model/C14:** Reporting dùng query/projection từ transactional data; chưa cần analytics service, data warehouse hoặc cache riêng. C14 dùng deterministic rule/query, không AI/vector DB/agent/generic rule engine lớn.
- **External boundaries:** Printing và HĐĐT nằm ngoài core Sale transaction. Print failure không rollback Sale và phải cho Reprint. HĐĐT external failure không mặc định rollback Sale; integration có lifecycle riêng nếu bổ sung sau.
- **Authorization/deployment:** Backend là authorization boundary với role Owner/Cashier. Deployment ưu tiên Windows Server/IIS, Vue static files, ASP.NET Core API và SQL Server; không cần Kubernetes, orchestration hoặc microservices infrastructure.
- **Ranh giới:** Không thiết kế SQL schema đầy đủ, API contracts, EF Core mapping, frontend component tree, UI/wireframe hoặc production infrastructure phức tạp. Giữ nguyên Step 1–9; không thêm feature.
- **Bước tiếp theo:** Step 11 — Development Plan / Technical Design Breakdown.
- **Tài liệu:** [`docs/architecture/architecture-v0.1.md`](docs/architecture/architecture-v0.1.md)

### D-015 — Development Plan / Technical Design Breakdown v0.1

- **Trạng thái:** `APPROVED`
- **Người phê duyệt:** Product Owner
- **Quyết định:** Step 11 triển khai theo Foundation → Setup + Product → Purchase → Inventory → Sale → Payment → Print → Return / Void → Debt + End-of-day → Understand & Act → Pilot; tổ chức theo vertical slice end-to-end, không theo module completeness.
- **Definition of Done:** Happy path và failure/recovery quan trọng, authorization, consistency, automated business-rule tests, Vue → API → DB chạy thật, log/error đủ debug và không phá domain/architecture invariants. Backend riêng lẻ chưa đủ để gọi slice Done.
- **Milestones/testing:** M0–M7 từ skeleton deploy được đến Pilot-ready; không estimate ngày cứng. Domain Unit, Application/Integration và một số critical E2E; không đặt mục tiêu 100% coverage.
- **Foundation:** .NET 10 LTS / ASP.NET Core / EF Core 10 / SQL Server; Vue 3.5 stable / TypeScript / Vite / Vue Router / Pinia / Tailwind / pnpm / Node 24 LTS; shadcn-vue khi cần. Không Vue RC.
- **Implementation direction:** Thin Controllers; use cases trong Application/Domain, không bắt buộc MediatR. ProblemDetails/typed errors; frontend không parse message để điều khiển logic. SPA cùng site dùng Identity/Auth và secure HttpOnly cookie; backend enforce authorization.
- **Migration/testing:** Migration trong Infrastructure, production migration là explicit deployment step, không mặc định startup migration. xUnit, WebApplicationFactory + SQL Server test DB, Vitest/Vue Test Utils, Playwright; không dùng EF InMemory để kiểm chứng inventory transaction/concurrency.
- **Slice 1:** Store/Main Warehouse/Owner, Product, import tồn đầu, InventoryMovement/InventoryBalance. SKU bắt buộc nhưng có thể auto-generate; Barcode và ReferencePurchaseCost optional; Name/Unit/SalePrice required. Opening Qty > 0 cần Opening Cost hợp lệ.
- **Ranh giới:** Just enough technical design per slice; không thêm functional scope, không triển khai code trong cập nhật này; giữ nguyên Step 1–10.
- **Tiếp theo:** Ready to begin implementation — Slice 0 Engineering Foundation; sau đó Slice 1 — Setup + Product.
- **Tài liệu:** [Development Plan](docs/architecture/development-plan-v0.1.md), [Technical Breakdown Slice 0–1](docs/architecture/technical-breakdown-slice-0-1-v0.1.md).

### D-016 — Step 11 / Decision D — Tenancy Foundation

- **Trạng thái:** `APPROVED`
- **Người phê duyệt:** Product Owner
- **Quyết định:** 1 tenant/account → 1 Store → 1 Main Warehouse; shared deployment/database có thể phục vụ nhiều Store tenant.
- **Scope:** Product, Sale, Purchase, InventoryBalance, InventoryMovement, Customer, Supplier và business data phải scope theo Store/Tenant.
- **Isolation:** Phải test User Store A không đọc/sửa dữ liệu Store B.
- **Ranh giới:** Không multi-branch, cross-store business feature hoặc tenant management platform lớn.
- **Tài liệu:** [Technical Breakdown Slice 0–1](docs/architecture/technical-breakdown-slice-0-1-v0.1.md).

### D-017 — Step 11 / Decision E — Initial Import Policy

- **Trạng thái:** `APPROVED`
- **Người phê duyệt:** Product Owner
- **Quyết định:** Template cố định; Validate → Preview → Confirm. Có lỗi thì không import; Confirm là all-or-nothing DB transaction. Không partial import trong pilot đầu.
- **Validation:** Báo dòng, trường và lý do lỗi; barcode/SKU duplicate phù hợp, kiểu dữ liệu, required fields.
- **Opening inventory:** Opening Qty > 0 cần Opening Cost hợp lệ. Product + InventoryMovement(Type = OpeningBalance) + InventoryBalance ghi atomically; không sửa Product.Stock trực tiếp.
- **Kiểm chứng:** Retry confirm không duplicate; balance nhất quán movement; isolation hoạt động.
- **Tài liệu:** [Technical Breakdown Slice 0–1](docs/architecture/technical-breakdown-slice-0-1-v0.1.md).
