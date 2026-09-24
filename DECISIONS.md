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

### D-018 — Slice 2 / Purchase Lifecycle

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Purchase chỉ có lifecycle `Draft → Completed`. Draft được sửa Supplier và PurchaseLines. Completed Purchase bất biến, không direct edit và không hard-delete.
- **Ranh giới:** Purchase Void/Reverse không triển khai trong Slice 2; thuộc Slice 4.
- **Tài liệu:** [Technical Breakdown Slice 2](docs/architecture/technical-breakdown-slice-2-v0.1.md).

### D-019 — Slice 2 / Precision & Rounding

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Quantity dùng `decimal(18,3)`, Unit Purchase Price/LineAmount/Money/InventoryValue dùng `decimal(18,2)`, AverageCost dùng `decimal(18,4)`. LineAmount và AverageCost dùng `MidpointRounding.AwayFromZero`; Purchase Total là tổng LineAmount.
- **Authority:** Backend/domain là nguồn tính authoritative; frontend chỉ preview UX.
- **Tài liệu:** [Technical Breakdown Slice 2](docs/architecture/technical-breakdown-slice-2-v0.1.md).

### D-020 — Slice 2 / One Product Per Purchase

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Trong một Purchase, một Product chỉ được xuất hiện một lần; duplicate ProductId bị từ chối ở domain/backend và được bảo vệ bằng database unique constraint.
- **Tài liệu:** [Technical Breakdown Slice 2](docs/architecture/technical-breakdown-slice-2-v0.1.md).

### D-021 — Slice 2 / Supplier & Purchase Permission

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Supplier và Purchase trong Slice 2 là Owner-only. Cashier không được create/update/deactivate Supplier hoặc create/edit/complete Purchase; backend authorization là security boundary.
- **Tài liệu:** [Technical Breakdown Slice 2](docs/architecture/technical-breakdown-slice-2-v0.1.md).

### D-022 — Slice 2 / Implementation Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Phê duyệt implementation Slice 2 — Purchase → Inventory sau các vòng review và hardening. Slice 2 được ghi nhận `APPROVED / IMPLEMENTED`.
- **Verification:** CI backend/frontend pass tại commit hardening cuối `f68c8a111a5538be8e51cf8ff0e323e7ce2f41c0`.
- **Bảo toàn quyết định:** D-018–D-021 giữ nguyên `APPROVED`; không thay đổi business scope, architecture hoặc các business decisions đã phê duyệt.
- **Tiếp theo:** Slice 3 — Sale → Payment → Print là bước implementation tiếp theo và chưa bắt đầu.
- **Tài liệu:** [Technical Breakdown Slice 2](docs/architecture/technical-breakdown-slice-2-v0.1.md).

### D-023 — Slice 3 / Sale Lifecycle

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Không persist Sale Draft. Cart là transient interaction state phía frontend; chỉ `CompleteSale` thành công mới tạo Sale trực tiếp ở trạng thái `Completed`. Completed Sale immutable và không hard-delete.
- **Lý do:** Không tạo Sale Draft rác khi Cashier bỏ dở giỏ hàng và giữ rõ `Cart ≠ Sale`; Sale chỉ tồn tại như một business fact sau completion thành công.
- **Ranh giới:** Correction, Return và Void thuộc Slice 4.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-024 — Slice 3 / Sale Price Snapshot

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Backend/domain là authority cho giá bán. `CompleteSale` dùng `Product.SalePrice` hiện hành tại thời điểm complete; `SaleLine` snapshot giá bán và lịch sử không đổi khi Product đổi giá sau này. Frontend chỉ preview và không gửi authoritative SalePrice.
- **Ranh giới:** Slice 3 chưa implement discount vì discount là SHOULD, không phải MUST.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-025 — Slice 3 / Sale Payment and Credit Sale

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Sale cho phép nhiều Payment, Slice 3 chỉ hỗ trợ `Cash` và `Transfer`. Payment là tiền thực thu; tổng Payment phải từ 0 đến Sale Total. Tendered cash/change không được model thành Payment vượt Total.
- **Debt:** `Outstanding = Sale.Total − sum(SalePayments)`; outstanding không phải field được chỉnh tùy ý.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-026 — Slice 3 / Minimal Customer Boundary

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Customer optional nếu Sale thanh toán đủ và bắt buộc nếu còn outstanding. Customer trong Slice 3 chỉ là identity tối thiểu gồm Id, StoreId, Name và Phone optional để xác định khoản nợ thuộc về ai.
- **Ranh giới:** Không CRM, loyalty, segmentation, credit limit, statement phức tạp hoặc workflow thu nợ sau. Customer debt repayment thuộc Slice 5; Slice 3 chỉ tạo dữ liệu Sale/Customer đủ sạch để tiếp tục mà không sửa lại Sale model.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-027 — Slice 3 / Inventory and Cost on Sale

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** `CompleteSale` atomically tạo Sale/SaleLines/actual Payments, giảm InventoryBalance, tạo InventoryMovement âm, snapshot `UnitCostAtSale` và ghi BusinessOperation result. SaleLine giữ cost snapshot lịch sử.
- **Concurrency:** Mutation cùng Product + Warehouse phải được serialize/control theo architecture đã duyệt; Sale nhiều Product khóa theo deterministic ProductId order. Concurrent Sale/Purchase cùng Product không được lost update.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-028 — Slice 3 / Negative Stock

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Giữ nguyên Architecture Decision C. Store có `AllowNegativeStock`, mặc định `false`; chỉ Owner thay đổi và thay đổi phải audit được.
- **Khi tắt:** `CompleteSale` làm âm tồn bị reject toàn bộ, không partial state; response cho biết Product thiếu và số lượng thiếu; backend enforce.
- **Khi bật:** Sale được Completed, InventoryMovement vẫn đầy đủ và balance có thể âm. Cost basis ưu tiên last known AverageCost, fallback ReferencePurchaseCost; nếu vẫn không có cost đáng tin thì Sale vẫn theo policy nhưng cost/profit phải thể hiện không đủ tin cậy hoặc estimated. Không retroactive revaluation khi Purchase xảy ra sau.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-029 — Slice 3 / CompleteSale Idempotency

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** `CompleteSale` dùng client-generated OperationId. ID mới tạo operation mới; cùng ID và fingerprint đã Completed trả Sale cũ; cùng ID nhưng payload khác bị reject `idempotency-key-reused`. Sale, lines, payments, inventory effects và BusinessOperation commit atomically.
- **Frontend recovery:** Immutable attempt snapshot; double-click không tạo operation thứ hai. Network timeout, HTTP 408 và ambiguous `operation-lock-timeout` giữ nguyên OperationId/payload, kiểm tra status và retry exact attempt. Completed recovery phải load Sale authoritative; không tạo ID mới khi operation cũ còn ambiguous.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-030 — Slice 3 / Printing Boundary and Permissions

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Printing xảy ra sau khi Sale đã Completed. Print failure không rollback Sale, không gọi lại `CompleteSale`, phải hiển thị Sale thành công/in thất bại và cho phép Reprint từ Sale Completed. Slice 3 dùng browser-print/printable receipt boundary, không xây printer orchestration service phức tạp; browser không chứng minh chắc chắn giấy đã in.
- **Permissions:** Owner và Cashier được checkout/Sale; Cashier được xem Sale cần cho bán hàng và Reprint. Chỉ Owner thay đổi `AllowNegativeStock`. Return/Void authorization thuộc Slice 4.
- **Tài liệu:** [Technical Breakdown Slice 3](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-031 — Slice 3 / Technical Breakdown Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner final-approve `Technical Breakdown Slice 3 v0.1` sau vòng review/hardening inventory costing.
- **Phạm vi:** Technical Breakdown Slice 3 được phép chuyển sang implementation.
- **Bảo toàn:** D-023–D-030 giữ nguyên `APPROVED`; không thay đổi Slice 2 hoặc các quyết định Step 1–11.
- **Tiếp theo:** Bắt đầu implementation Slice 3 — Sale → Payment → Print.
- **Tài liệu:** [Technical Breakdown Slice 3 v0.1](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-032 — Slice 3 Implementation Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner final-approve implementation `Slice 3 — Sale → Payment → Print` sau review implementation, migration, concurrency, idempotency, costing, frontend recovery, print/reprint và CI.
- **Implementation boundary:** Sale Completed immutable; backend authoritative pricing; credit Sale yêu cầu Customer; Payment là actual money received; CompleteSale atomic/idempotent; inventory mutation dùng shared deterministic locking với Purchase; negative stock theo Store policy; cost reliability dùng explicit known-state; printing là hậu xử lý; Reprint không tạo Sale mới hoặc inventory effect mới.
- **Bảo toàn:** D-023–D-031 giữ nguyên `APPROVED`; không thay đổi business decisions đã được phê duyệt và không bắt đầu Slice 4.
- **Verification:** Latest verified implementation commit `a0ce464384b6a2029d8ecf7be4cdc84f26367d9e`; GitHub Actions run `35627192222` pass backend/frontend. Real Slice 3 Playwright flow đã chạy local và không được suy diễn là đã chạy trong CI.
- **Tài liệu:** [Technical Breakdown Slice 3 v0.1](docs/architecture/technical-breakdown-slice-3-v0.1.md).

### D-033 — Slice 4 / Return and Void Permission

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Trong MVP Slice 4, chỉ Owner được Create Return, Void Completed Sale và Void Completed Purchase. Cashier tiếp tục được xem Sale và thông tin operational đã được phép ở Slice 3 nhưng không được thực hiện correction mutation.
- **Boundary:** Backend là authorization boundary; không xây permission matrix tổng quát.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-034 — Slice 4 / Return Lifecycle

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Không persist Return Draft. `CreateReturn` tạo Return trực tiếp ở trạng thái `Completed`; Completed Return immutable. Return tham chiếu OriginalSale và mỗi ReturnLine tham chiếu OriginalSaleLine. Form Return phía frontend chỉ là interaction state.
- **Bảo toàn lịch sử:** Không hard-delete Sale, Return hoặc ReturnLine.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-035 — Slice 4 / Return Quantity and Concurrent Correction

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** `ReturnableQuantity = OriginalSoldQuantity - cumulative PreviouslyReturnedQuantity`; mỗi requested quantity phải lớn hơn 0 và không vượt quantity còn return được. Cho phép partial Return và nhiều Return cho cùng SaleLine nhưng không được vượt tổng quantity đã bán.
- **Concurrency:** Concurrent Returns trên cùng Sale phải được serialize/control. Return và Sale Void trên cùng OriginalSale cũng mutually exclusive/serialized.
- **Exclusion:** Sale đã Void không được Return; Sale đã có bất kỳ Completed Return không được Void.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-036 — Slice 4 / Return Financial Effect and Refund

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Return value dùng historical `OriginalSaleLine.UnitSalePrice` và quantity, không dùng Product.SalePrice hiện tại. Return giảm customer obligation trước; refund chỉ là tiền thực tế phải trả lại sau khi tính nghĩa vụ còn lại.
- **Công thức:** `NetCashHeldBeforeRefund = OriginalCollected - PreviousRefunds`; `NetSaleObligation = OriginalSaleTotal - CumulativeReturnedValue`; `RefundDueNow = max(0, NetCashHeldBeforeRefund - NetSaleObligation)`; sau refund, `Outstanding = max(0, NetSaleObligation - NetCashHeld)`.
- **Refund:** Khi `RefundDueNow > 0`, Owner chọn Cash hoặc Transfer; backend tính authoritative amount. Khi bằng 0, không tạo RefundPayment. Không tạo refund debt payable trong MVP.
- **Rounding:** Mỗi ReturnLine được round 2 chữ số `AwayFromZero`, bị cap bởi remaining financial value; lần return exact remaining quantity dùng exact remaining financial value để đóng residual. Cumulative returned value không vượt OriginalSaleLine.LineAmount.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-037 — Slice 4 / Restock and Historical Cost Basis

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Mỗi ReturnLine bắt buộc Owner chọn `Restock` hoặc `NoRestock`; hệ thống không tự đoán. Restock tăng quantity/value và tạo positive InventoryMovement theo `OriginalSaleLine.UnitCostAtSale`; NoRestock không mutate inventory hoặc tạo receive movement nhưng vẫn lưu original cost basis.
- **Valuation:** Return Restock dùng cùng inbound Q/V/`HasAverageCost` safety rule đã duyệt: chỉ establish AverageCost khi `Q1 > 0 && V1 >= 0`; khi `Q1 > 0 && V1 < 0` giữ numeric AverageCost và đặt `HasAverageCost = false`; khi `Q1 <= 0` giữ AverageCost và known-state. Không persist negative authoritative AverageCost và không retroactively revalue Sale.
- **Rounding:** Cumulative restocked inventory value không vượt historical inventory value đã issue cho OriginalSaleLine; partial restock xử lý residual deterministic tương tự revenue return.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-038 — Slice 4 / Sale Void

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Sale Void là correction khi original transaction không nên tồn tại theo business intent, khác Return. Chỉ Owner được Void; Reason bắt buộc; tạo explicit SaleVoid/reversal record với original, reason, actor và timestamp; không xóa hoặc biến Sale gốc thành editable transaction.
- **Guards:** Không double Void; không Void Sale đã có Completed Return; không Return trên Sale đã Void.
- **Inventory:** Đảo toàn bộ Sale inventory effect bằng positive reversal InventoryMovement và original `UnitCostAtSale`; cập nhật balance bằng inbound valuation safety rule.
- **Financial:** Không tạo fake RefundPayment. Original SalePayments giữ nguyên historical facts; read projections loại active obligation/revenue/collection contribution của Sale đã Void.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-039 — Slice 4 / Purchase Void Limitation and Reversal Basis

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Completed Purchase immutable. Owner chỉ được direct Void khi hệ thống chứng minh costing reversal an toàn; không hỗ trợ arbitrary retroactive Purchase Void, historical revaluation hoặc costing replay.
- **Reversal basis:** Từ Slice 4, CompletePurchase lưu technical/audit pre-mutation basis tối thiểu cho mỗi line/Product gồm QuantityBefore, InventoryValueBefore, AverageCostBefore và HasAverageCostBefore trong cùng transaction. Khi Void an toàn, tạo typed PurchaseVoid/reversal, reversal InventoryMovement và restore exact pre-Purchase balance; original Purchase được giữ.
- **Legacy/dependency:** Legacy Purchase thiếu trustworthy basis chỉ được Void khi chứng minh được pre-state và dependency không mơ hồ; nếu không thì reject. Dependency check dựa trên persisted movement/audit ordering, không chỉ so current quantity/value. Một line không an toàn làm reject toàn bộ multi-line Purchase Void.
- **Financial:** Supplier obligation/payment effects được reversed trong derived projections; không tạo fake actual Payment. Supplier-return workflow ngoài Slice 4.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-040 — Slice 4 / Idempotency, Concurrency and Audit

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** `CreateReturn` và typed Sale/Purchase Void operations dùng client-generated OperationId với cùng idempotency/recovery semantics Slice 2–3: exact fingerprint retry trả result cũ; reused ID với payload khác bị reject; ambiguous result kiểm tra và retry exact immutable attempt; không tạo ID mới khi operation cũ còn ambiguous.
- **Atomicity:** Return/lines/refund/restock movements/balances/operation; SaleVoid/reversal effects/operation; hoặc PurchaseVoid/exact balance restore/reversal effects/operation phải commit/rollback atomically, không partial correction.
- **Serialization:** Sale correction applock `SimpleStore:SaleCorrection:{SaleId}` serialize Return/Return, Return/SaleVoid và SaleVoid/SaleVoid. Purchase Void dùng `SimpleStore:PurchaseCorrection:{PurchaseId}`. Inventory mutation tiếp tục dùng shared `UPDLOCK, HOLDLOCK` theo deterministic ProductId order.
- **Audit:** Lưu actor, timestamp, operation type, original transaction, return/reversal transaction và mandatory Void reason. Không xây generic audit/correction framework.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-041 — Slice 4 / Technical Breakdown Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner final-approve `Technical Breakdown Slice 4 v0.1` sau review/hardening Return refund/debt math, cumulative return quantity/value rounding, Return/Void concurrency, Sale Void inventory reversal, safe Purchase Void, InventoryMovement ordering evidence và ReferencePurchaseCost ABA protection bằng semantic revision.
- **Phạm vi:** Technical Breakdown Slice 4 được phép chuyển sang implementation.
- **Bảo toàn:** D-033–D-040 giữ nguyên `APPROVED`; không thay đổi Slice 0–3 hoặc Step 1–11; không bắt đầu Slice 5.
- **Review evidence:** Purchase Void evidence hardening tại commit `305ffcc1186f7ddb24c5239b16d7b5323cb667bf` đã được review và chấp thuận.
- **Tiếp theo:** Bắt đầu implementation Slice 4 — Return / Void / Recovery.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-042 — Slice 4 Implementation Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner final-approve implementation end-to-end `Slice 4 — Return / Void / Recovery` sau review backend/domain/persistence/API, migration upgrade safety, frontend correction/recovery, concurrency/idempotency và verification.
- **Return:** Completed Return immutable; ReturnLine tham chiếu OriginalSaleLine; hỗ trợ partial/multiple Return với cumulative quantity/financial-value caps và deterministic final residual rounding; Restock/NoRestock explicit; historical SaleLine cost basis; obligation-first refund và actual ReturnRefundPayment là financial source of truth; refund aggregate/payment consistency validation; server preview, history/read projection, immutable OperationId recovery và correction concurrency safety.
- **Sale Void:** Owner-only với mandatory reason; giữ nguyên original Sale, SaleLines và SalePayments; explicit SaleVoid record; full inventory reversal từ historical Sale evidence; không fake RefundPayment; Return/Void mutually exclusive; void-aware projection/receipt/history và safe recovery.
- **Purchase Void:** Owner-only với mandatory reason; giữ original Purchase/payments; trustworthy PurchaseLineReversalBasis và exact InventoryMovement evidence; LedgerSequence downstream dependency detection; exact pre-state restore; không retroactive costing replay; legacy data thiếu trustworthy basis bị reject; ReferencePurchaseCostRevision chống ABA và giữ monotonic; multi-line reversal all-or-nothing với typed unsafe rejection.
- **Idempotency/concurrency:** Shared global BusinessOperation OperationId lock; exact fingerprint retry; cross-operation reuse protection; SaleCorrection/PurchaseCorrection locks; deterministic InventoryBalance locking; không partial correction.
- **Frontend:** Return screen/detail; server-authoritative preview gắn với exact correction intention; stale preview invalidation và preview-in-flight input locking; Return/Sale Void/Purchase Void retry/recovery; Owner/Cashier visibility; typed errors; void state và historical transaction visibility.
- **Review evidence:** Technical review bao phủ refund/debt math, partial Return và inventory-value residual rounding, actual refund authority, corrupted refund snapshot detection, migration upgrade từ Slice 3 data, Return/Return và Return/Sale Void races, Sale Void reversal, safe Purchase Void eligibility, downstream movement, ReferencePurchaseCost ABA, cross-operation OperationId, frontend immutable retry và stale Return preview race.
- **Implementation commits:** `b34c1f6e56fe0fb66a594fdbad32a542a6405aec`, `e0c3ae58bac7b4d73596369b4c2315674d9e1c3a`, `ecc9c709b19e5dd9f265cade77c3cd564f1144ab`, `5ab9f5f1456b4dfc6b3baf2b916ba8ba12913874`.
- **Verification:** Final reviewed commit `5ab9f5f1456b4dfc6b3baf2b916ba8ba12913874`; GitHub Actions run `35706827677` pass backend build, 60 Domain tests, 57 SQL Server integration tests và 16 frontend test files. Real Slice 4 Playwright flows cho multiple Return Restock/NoRestock, Sale Void và safe Purchase Void đã chạy local; GitHub CI hiện không chạy real E2E này.
- **Bảo toàn:** D-033–D-041 giữ nguyên `APPROVED`; không thay đổi Step 1–11 hoặc Slice 0–3; không bắt đầu Slice 5 trong approval commit.
- **Tiếp theo:** Slice 5 — Debt + End-of-day là planned next slice nhưng chưa bắt đầu.
- **Tài liệu:** [Technical Breakdown Slice 4 v0.1](docs/architecture/technical-breakdown-slice-4-v0.1.md).

### D-043 — Slice 5 / Debt is derived, not manually editable

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Customer/Supplier outstanding debt phải được suy ra từ business transactions, actual payments và Returns/Voids/Reversals liên quan. Không cho phép sửa trực tiếp outstanding debt thành một giá trị tùy ý.
- **Source of truth:** Transaction/payment/correction history là business source of truth. Nếu có materialized/cache balance thì chỉ là operational projection, phải cập nhật atomically và có cơ chế kiểm tra/rebuild consistency.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-044 — Slice 5 / Customer Debt Payment represents actual collected money

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Customer debt payment là nghiệp vụ thu tiền thực tế; hỗ trợ partial payment, full payment và multiple payments. Payment làm tăng actual collected amount và giảm customer outstanding debt nhưng không tạo Revenue mới.
- **Invariant:** `Revenue != Collected`; không dùng debt payment amount để suy ra Revenue.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-045 — Slice 5 / Supplier Debt Payment represents actual paid money

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Supplier debt payment là tiền thực tế Store trả cho Supplier; hỗ trợ partial payment, full payment và multiple payments. Payment làm tăng actual supplier payment và giảm supplier outstanding debt nhưng không thay đổi Purchase value đã ghi nhận.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-046 — Slice 5 / No overpayment or advance balance in MVP

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** MVP không hỗ trợ customer credit balance, supplier advance, advance payment hoặc prepaid balance.
- **Rule:** `DebtPaymentAmount <= CurrentOutstandingDebt` cho cả Customer và Supplier; backend phải kiểm tra trên authoritative balance trong transaction, không tin balance stale từ client.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-047 — Slice 5 / Debt payment is not allocated to individual invoices in MVP

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Debt payment được quản lý ở cấp Customer hoặc Supplier; không bắt buộc phân bổ tới từng Sale, Purchase hoặc Invoice.
- **Ngoài Slice 5:** Không FIFO settlement, invoice allocation, debt aging hoặc statement reconciliation theo invoice. Debt tiếp tục được tính từ transaction + payment history.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-048 — Slice 5 / End-of-day is a query, not accounting close

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** End-of-day trong MVP là reporting/query theo business/local date của Store, không phải business-state transition hoặc accounting close.
- **Ngoài phạm vi:** Không Close Day, Reopen Day, accounting period lock hoặc carry-forward cash closing balance.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-049 — Slice 5 / Revenue and Collected remain separate concepts

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** End-of-day phải phân biệt Revenue generated và Money actually collected. Payment amount, gồm cả thu nợ cũ, không được dùng để suy ra Revenue; debt creation không phải collected money.
- **Ví dụ:** Sales revenue trong ngày `10,000,000`, thanh toán ngay `7,000,000`, thu nợ cũ `1,000,000` thì `Revenue = 10,000,000` và `Collected = 8,000,000`.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-050 — Slice 5 / Estimated Gross Profit uses historical SaleLine cost snapshot

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** `EstimatedGrossProfit = NetSalesRevenue - COGS`; COGS dùng historical `SaleLine.UnitCostAtSale`/cost basis đã snapshot, có Return/Void adjustment phù hợp. Thay đổi Product average cost hiện tại không được làm thay đổi historical gross profit của Sale cũ.
- **Boundary:** Đây là Estimated Gross Profit, không phải accounting/net profit; không gồm salary, rent, electricity, depreciation, tax hoặc operating expenses khác.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-051 — Slice 5 / Customer debt access and collection

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Owner và Cashier đều được xem Customer outstanding debt và ghi nhận Customer Debt Payment. Customer debt collection là continuation của luồng bán hàng/thu tiền thông thường.
- **Authorization/isolation:** Backend authorization là authority; UI visibility không thay thế authorization. Cross-store access tiếp tục bị chặn bởi Store isolation hiện tại.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-052 — Slice 5 / Supplier debt and financial summary are Owner-only

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Xem Supplier debt, ghi nhận Supplier Debt Payment, xem End-of-day summary và xem Estimated Gross Profit đều là Owner-only trong MVP.
- **Boundary:** Không tạo Cashier-specific reduced EOD dashboard trong Slice 5 và không mở rộng quyền Supplier/Purchase hiện tại cho Cashier.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-053 — Slice 5 / Collected headline uses net actual cash movement

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Headline `Collected = SalePayments + CustomerDebtPayments - ActualCustomerRefunds` trong business date. Đây là net actual collected; API/UI vẫn phải hiển thị riêng Sale payments, Customer debt collected, Customer refunds và Net collected.
- **Invariant:** Debt creation không phải Collected; Collected không được dùng để suy ra Revenue.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-054 — Slice 5 / Store timezone uses configurable IANA timezone

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Mỗi Store có timezone configuration riêng, canonical persisted format là IANA timezone ID và có thể cấu hình trong onboarding/settings. Backend EOD dùng Store timezone; browser timezone, application-server local timezone và UTC calendar date không phải authority cho business date.
- **Existing Store:** MVP backfill/default `Asia/Ho_Chi_Minh`; technical design vẫn phải hỗ trợ Store timezone khác. Runtime conversion compatibility là implementation detail, canonical persisted ID vẫn là IANA.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-055 — Slice 5 / Debt Payment supports optional immutable Note

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Customer/Supplier Debt Payment hỗ trợ `Note` optional, tối đa 250 ký tự. Trim đầu/cuối; null/empty đều canonical thành “không có note”; Note immutable sau Completed và normalized Note tham gia idempotency fingerprint.
- **Boundary:** Không thêm field `Reference` riêng trong Slice 5.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-056 — Slice 5 / Correction must not create negative aggregate debt

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Invariant:** Sau mọi Completed mutation liên quan, `CustomerOutstandingDebt >= 0` và `SupplierOutstandingDebt >= 0`. Không clamp âm về zero, sửa/xóa historical payment, tự allocate debt payment vào invoice hoặc tạo implicit customer credit/supplier advance.
- **Customer Return:** Return khóa và recompute authoritative aggregate Customer debt. `DebtReduction = min(ReturnObligationReduction, CurrentAggregateCustomerDebt)` và `RequiredActualRefund = ReturnObligationReduction - DebtReduction`. Phần vượt debt trở thành actual Return refund; refund/Return/operation commit atomically và giữ idempotency/recovery/refund-history semantics Slice 4.
- **Compatibility:** D-056 mở rộng obligation-first rule D-036 sang Customer aggregate khi có unallocated debt payments; RequiredActualRefund là một authoritative result duy nhất, không cộng thêm vào refund đã tính riêng ở cấp Sale và không double refund.
- **Sale Void:** Nếu hypothetical Void làm aggregate Customer debt âm thì reject bằng typed business error; không auto-create refund. User dùng Return với actual refund khi phù hợp.
- **Purchase Void:** Nếu hypothetical Void làm aggregate Supplier debt âm thì reject bằng typed business error; không tạo Supplier Advance hoặc implicit Supplier Refund. Supplier refund/recovery flow riêng nằm ngoài Slice 5.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION` tại D-057.

### D-057 — Slice 5 / Technical Breakdown Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-22
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner final-approve [`docs/architecture/technical-breakdown-slice-5-v0.1.md`](docs/architecture/technical-breakdown-slice-5-v0.1.md). Technical Breakdown Slice 5 chuyển từ `PROPOSED / PENDING PRODUCT OWNER APPROVAL` sang `APPROVED FOR IMPLEMENTATION`.
- **Approved direction:** Derived Customer/Supplier debt; actual Customer/Supplier Debt Payments; no overpayment/advance hoặc invoice allocation; shared idempotency/recovery; party-level serialization; D-056 Return aggregate-debt/refund và Sale/Purchase Void protection; Store-local date với canonical IANA timezone; Revenue/Collected separation và net Collected headline; ending debts, Supplier payments, historical-cost Estimated Gross Profit; D-051/D-052 authorization; D-055 immutable Note; Stage 5A/5B sequencing và automated SQL Server/frontend/E2E verification plan.
- **Bảo toàn:** D-043–D-056 giữ nguyên `APPROVED`; approval này không ghi nhận production implementation hoặc migration đã hoàn thành.
- **Implementation sequencing:** Stage 5A — Debt backend/domain/persistence/tests được phép bắt đầu sau approval commit này. Stage 5B giữ planned và chỉ bắt đầu theo sequencing/review process đã approve.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `APPROVED FOR IMPLEMENTATION`.

### D-058 — Slice 5 Stage 5A Implementation Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner final-approve implementation `Slice 5 — Stage 5A: Debt backend/domain/persistence/tests` tại baseline `49098e94c4f44acf3762f3e33ce6be3dfc00758f`.
- **Approved implementation:** Derived Customer/Supplier outstanding debt; shared immutable `DebtPayment`; Customer/Supplier Debt Payment; no overpayment, customer credit balance hoặc supplier advance; Store-scoped debt APIs; Owner/Cashier Customer debt authorization; Owner-only Supplier debt authorization; BusinessOperation idempotency/recovery và same-OperationId exact retry; party-level SQL Server application locks; concurrent-overpayment prevention; stale expected-balance protection; CompleteSale/CompletePurchase participation trong party debt serialization; D-056 Customer Return aggregate-debt/refund integration; D-056 Sale/Purchase Void negative-debt protection.
- **Persistence/timezone:** Additive Stage 5A migration; `Store.TimeZoneId` persistence foundation; canonical IANA timezone; existing Store backfill/default `Asia/Ho_Chi_Minh`; migration-upgrade verification.
- **Current/historical debt invariant:** Current/authoritative debt dùng toàn bộ committed history và không phụ thuộc clock cutoff; mutation decisions gồm DebtPayment, Return, Sale Void và Purchase Void phải dùng current semantic dưới party lock. Historical/as-of debt giữ strict `event timestamp < cutoff` để bảo toàn half-open business-date reporting cho Stage 5B. Hai semantic không được collapse.
- **Review hardening:** Product Owner review chấp thuận commit `49098e94c4f44acf3762f3e33ce6be3dfc00758f`, gồm deterministic equal-timestamp regression cho current Customer/Supplier debt, Customer Return, Sale Void và Purchase Void; blocker current-vs-historical debt cutoff đã được fix.
- **Verification:** GitHub Actions run #28 / `35805222057` — `SUCCESS` tại approved head `49098e94c4f44acf3762f3e33ce6be3dfc00758f`; backend restore, Release build và full `dotnet test` pass; frontend build và tests pass. Không ghi nhận manual E2E evidence cho Stage 5A approval này.
- **Bảo toàn/phạm vi:** D-043–D-057 giữ nguyên `APPROVED`. Approval chỉ áp dụng cho Stage 5A; toàn bộ Slice 5 chưa completed. Stage 5B giữ `NOT STARTED / PLANNED`. Không mở rộng sang General Ledger, accounting close/reopen, full cashbook, debt aging, invoice-level settlement allocation, customer credit balance, supplier advance, financial statements, operating-expense accounting, net profit, BI dashboard hoặc AI.
- **Tiếp theo:** Stage 5B — End-of-day + frontend + E2E/recovery theo sequencing và phạm vi đã approve; D-058 không bắt đầu Stage 5B.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — Stage 5A `APPROVED / COMPLETED`, Stage 5B `NOT STARTED / PLANNED`.

### D-059 — Slice 5 Stage 5B Implementation Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner approve implementation `Slice 5 — Stage 5B: End-of-day + frontend + E2E/recovery` tại approved head `84f6f8d61bff19f6845204207ccb034bd9219bcd`. Stage 5B chuyển thành `APPROVED / COMPLETED`.
- **Cơ sở approval:** Technical Breakdown đã approve tại D-057; Stage 5A đã approve tại D-058; implementation commit `42842e17b54a396f40ad6ef7ad55da59c160161a`; hardening commit `84f6f8d61bff19f6845204207ccb034bd9219bcd`; toàn bộ Product Owner review findings cho Stage 5B đã được fix.
- **Approved implementation:** Store configurable canonical IANA timezone; End-of-day aggregation/query; historical SaleLine COGS và `CostReliability`; Customer debt frontend; Supplier debt frontend; EOD frontend; immutable retry/recovery; stale-balance UX; D-056 typed guidance cho Return/Void; SQL Server integration và frontend tests.
- **Verification:** GitHub Actions run #31 / `35848666879` — `SUCCESS` tại approved head; Domain tests 78/78, SQL Server integration tests 78/78, frontend tests 82/82 và frontend build pass. Real Stage 5B Playwright E2E đã pass local; GitHub CI không được ghi nhận là đã chạy real Playwright E2E.
- **Bảo toàn/phạm vi:** D-043–D-058 giữ nguyên `APPROVED`. Approval này hoàn tất Stage 5B nhưng không tự approve hoặc complete toàn bộ Slice 5; final Slice-level Product Owner approval vẫn là gate riêng đang pending. Không mở rộng scope và không thêm schema migration ở Stage 5B.
- **Tiếp theo:** Product Owner final review / approval của toàn bộ Slice 5.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — Stage 5A và Stage 5B đều `APPROVED / COMPLETED`; final Slice 5 approval chưa được tạo.

### D-060 — Slice 5 Final Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Quyết định:** Product Owner final-approve toàn bộ `Slice 5 — Debt + End-of-day` sau Technical Breakdown Approval D-057, Stage 5A Implementation Approval D-058 và Stage 5B Implementation Approval D-059. Slice 5 chuyển thành `APPROVED / COMPLETED`.
- **Reviewed heads:** Stage 5A approved head `49098e94c4f44acf3762f3e33ce6be3dfc00758f`; Stage 5B final reviewed implementation head `84f6f8d61bff19f6845204207ccb034bd9219bcd`; Stage 5B approval documentation commit `ec93b9a673ccd638c280cd40745009fd9d06935e`.
- **Final-approved debt scope:** Derived Customer/Supplier outstanding debt; partial/full Customer Debt Payment và Supplier Debt Payment; immutable `DebtPayment`; no overpayment, customer credit hoặc supplier advance; Store-scoped authorization/isolation; current debt khác historical/as-of debt semantics; D-056 correction integration.
- **Final-approved EOD scope:** Configurable canonical IANA Store timezone; End-of-day query theo Store-local business date; Revenue, Collected, Customer refunds, ending Customer debt, Supplier payments, ending Supplier debt; historical-cost Estimated Gross Profit và `CostReliability`.
- **Final-approved UI/recovery scope:** Customer/Supplier debt frontend; End-of-day frontend; immutable retry/recovery; stale-balance UX; Return/Void typed recovery guidance; SQL Server integration tests, frontend tests và real local Stage 5B E2E evidence.
- **Stage 5A evidence:** D-058; approved head `49098e94c4f44acf3762f3e33ce6be3dfc00758f`; GitHub Actions run #28 / `35805222057` — `SUCCESS`.
- **Stage 5B evidence:** D-059; approved head `84f6f8d61bff19f6845204207ccb034bd9219bcd`; GitHub Actions run #31 / `35848666879` — `SUCCESS`; Domain tests 78/78; SQL Server integration tests 78/78; frontend tests 82/82; frontend build pass. Real Stage 5B Playwright E2E là local evidence; GitHub CI không chạy real Playwright E2E.
- **Definition of Done:** Happy path, important failure/recovery paths, authorization, Store isolation, domain/integration/frontend tests, EOD/debt semantics, real local critical Stage 5B E2E evidence và toàn bộ Product Owner approval gates D-057–D-060 đã được review hoàn tất.
- **Bảo toàn/phạm vi:** D-043–D-059 giữ nguyên historical meaning và trạng thái `APPROVED`. D-060 không mở rộng ra ngoài Slice 5 và không bắt đầu hoặc approve Slice 6.
- **Tiếp theo:** Product Owner bắt đầu Slice 6 — Understand & Act theo quy trình discovery/decision/technical breakdown hiện tại; planned scope là “Hôm nay cửa hàng thế nào?” và C14 experiment nguy cơ sắp hết hàng, không AI, không dashboard lớn, không generic rule engine framework.
- **Tài liệu:** [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — toàn bộ Slice 5 `APPROVED / COMPLETED`.

### D-061 — Slice 6 / Owner “Today” landing experience

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Quyết định:** Slice 6 có entry point riêng cho câu hỏi “Hôm nay cửa hàng thế nào?”, dự kiến route `/today`, và đây là default landing page của Owner sau login. Cashier tiếp tục operational/sales flow phù hợp và không mặc định vào financial Owner summary.
- **Business date:** “Hôm nay” là current Store-local business date theo `Store.TimeZoneId`; browser timezone và application-server timezone không phải authority. Today không có date picker và không hiển thị historical date; historical reporting tiếp tục dùng End-of-day capability Slice 5.
- **Boundary:** Today không trở thành historical reporting dashboard. Backend authorization vẫn là authority.

### D-062 — Slice 6 / Today summary semantics

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Minimum summary:** Doanh thu hôm nay; Tiền thu thuần hôm nay; Lãi gộp ước tính; Số đơn bán; công nợ Customer/Supplier mới phát sinh hôm nay; attention signal C14.
- **Reuse:** Metric đã có semantic tại Slice 5 phải reuse semantic đã `APPROVED`, không tạo định nghĩa cạnh tranh.
- **New debt created:** Đây không phải ending outstanding debt. Customer/Supplier new debt là nghĩa vụ mới từ Sale/Purchase trong business date chưa được actual payment cover. Thu Customer debt cũ hoặc trả Supplier debt cũ không được tính là new debt created; ending debt vẫn thuộc EOD/debt views.
- **Correction boundary:** Technical Breakdown phải làm rõ correction/Return/Void theo approved transaction history và D-043–D-060. Ambiguity chưa được Product Owner quyết định phải vào `OPEN_QUESTIONS.md`, không tự invent rule.

### D-063 — Slice 6 / Explainability: Summary → Why → Source data

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Pattern:** C13 trong Slice 6 dùng `Summary → Vì sao? → Dữ liệu nguồn`, không xây report builder hoặc BI dashboard.
- **Financial evidence:** Revenue giải thích từ Sale/Return/Sale Void; Collected từ Sale Payment/Customer Debt Payment/Actual Customer Refund; Estimated Gross Profit từ Net Revenue/historical COGS/`CostReliability`; debt created từ transaction tạo obligation; Sale count từ transaction được tính.
- **C14 evidence:** Giải thích từ current inventory và recent sales evidence. Khi phù hợp, user điều hướng tới existing source transaction/detail.
- **Boundary:** Không parse human-readable message để quyết định logic; source kind/reference phải typed.

### D-064 — Slice 6 / C14 sales velocity uses 7 completed business days

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Hypothesis:** C14 chỉ thử nghiệm `Nguy cơ sắp hết hàng`.
- **Window:** Sales velocity dùng đúng 7 completed business days theo `Store.TimeZoneId`; current/in-progress business date không vào denominator. `AverageDailySales = NetSoldQuantityLast7CompletedBusinessDays / 7`.
- **Net sold quantity:** Completed Sale tăng; Sale Void reverse quantity Sale tương ứng; Return giảm theo approved correction history; Purchase/InventoryAdjustment không tham gia sales velocity.
- **Days of cover:** Chỉ tính khi `AverageDailySales > 0`; `DaysOfCover = CurrentStock / AverageDailySales`.
- **Boundary:** Đây là deterministic operational signal, không forecasting engine, machine learning, AI, seasonal model hoặc demand-planning subsystem. Window boundary dùng Store timezone, không browser/server timezone.

### D-065 — Slice 6 / C14 threshold and data sufficiency

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Threshold:** Risk threshold cố định `DaysOfCover <= 3`; không có per-Store/per-Product configurable threshold trong MVP Slice 6.
- **Factual inventory attention:** Khi `CurrentStock <= 0` và có recent sales evidence phù hợp, hiển thị factual state `Đã hết hàng` hoặc `Tồn kho đang âm`; đây không phải forecast.
- **Risk attention:** Khi `CurrentStock > 0`, `AverageDailySales > 0`, `DaysOfCover <= 3` và data sufficiency rule đạt, hiển thị `Nguy cơ sắp hết hàng`.
- **Insufficient data:** Chưa đủ 7 completed business days thì không đưa strong low-stock conclusion; UI thể hiện dữ liệu chưa đủ khi phù hợp và không giả vờ forecast chính xác. Technical Breakdown phải định nghĩa data sufficiency từ domain hiện có hoặc nêu Product Owner Open Question nếu cần.

### D-066 — Slice 6 / Attention UI stays intentionally thin

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Today UI:** Có section `Cần chú ý`, hiển thị tối đa 3 Product attention items chính. Factual out-of-stock/negative-stock đứng trước, sau đó risk theo `DaysOfCover` thấp nhất; nếu còn item thì hiển thị `Xem tất cả X mặt hàng`.
- **Evidence per risk:** Product, current stock, average sales/day từ 7 completed days, estimated days of cover và action `Xem vì sao`.
- **Neutral empty state:** Khi data đủ nhưng không có signal, dùng wording hẹp như `Chưa thấy mặt hàng có nguy cơ sắp hết theo quy tắc hiện tại`, không tuyên bố toàn bộ kho “ổn”.
- **Boundary:** Không notification center, push/email alert, background alert subsystem hoặc generic alert framework.

### D-067 — Slice 6 / Information can lead to action without deciding for Owner

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Actions:** Từ C14 detail, Owner có thể `Xem sản phẩm` hoặc `Tạo phiếu nhập`; Create Purchase có thể preselect Product để giảm thao tác.
- **Owner authority:** Không tự chọn Supplier, tính/recommend quantity, tạo Purchase, auto-order, commit transaction hoặc khẳng định “nên nhập X đơn vị”. Existing Purchase business rules vẫn áp dụng; Owner quyết định Supplier, quantity và có nhập hay không.
- **Nguyên tắc:** `Information should lead toward action` không đồng nghĩa software quyết định thay user.

### D-068 — Slice 6 / C14 experiment must be measurable

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Experiment status:** C14 vẫn là `EXPERIMENT`; nhu cầu, value và willingness-to-pay chưa được validated.
- **Minimum measurement:** Có khả năng đo Owner mở Today; C14 signal được shown; Owner bấm `Xem vì sao`; Owner bấm `Tạo phiếu nhập` từ C14 flow. Technical Breakdown có thể đề xuất immutable C14-specific experiment-event model tối thiểu nếu chứng minh cần thiết.
- **Boundary:** Không analytics platform, generic event-tracking framework, data warehouse hoặc telemetry product lớn.
- **Interpretation:** `click != validated product value`. Product Owner vẫn phải pilot/research xem signal có phát hiện việc chưa chú ý, có evidence đáng tin, có ảnh hưởng quyết định nhập hàng, có được tiếp tục dùng và có willingness-to-pay hay không; CTR/event count không tự động là validation.

### D-069 — Slice 6 / New debt created today semantics

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Metric:** Customer/Supplier debt created today đo new obligation do Sale/Purchase trong current Store-local business-date tạo ra, không phải ending debt hoặc net movement của aggregate debt.
- **Customer:** Tại Sale completion, `CustomerDebtCreated = max(SaleTotal - DirectSalePayments, 0)` cho Sale trong current business-date window.
- **Supplier:** Tại Purchase completion, `SupplierDebtCreated = max(PurchaseTotal - DirectPurchasePayments, 0)` cho Purchase trong current business-date window.
- **Standalone debt payment:** Customer/Supplier `DebtPayment` không giảm new-debt-created kể cả cùng ngày, vì D-047 không allocate payment vào transaction cụ thể. Không tạo aggregate FIFO/allocation rule.
- **Same-business-date correction:** Return/Sale Void chỉ có thể giảm Customer debt created của actual original Sale khi Sale và correction cùng business date; Purchase Void tương tự cho actual original Purchase. Reuse authoritative obligation reduction, không double-count actual refund/payment, không hidden credit và floor contribution từng transaction tại `0`.
- **Cross-business-date correction:** Không rewrite historical new-debt-created của ngày transaction và không tạo negative new debt trong ngày correction. Correction vẫn xuất hiện ở financial/correction explainability phù hợp.
- **Meaning:** Metric trả lời “Hôm nay các giao dịch mới đã tạo ra bao nhiêu nghĩa vụ nợ mới?”, không trả lời “Hôm nay tổng công nợ thay đổi bao nhiêu?”.

### D-070 — Slice 6 / Sale count semantics

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Count:** `SaleCount` đếm Sale Completed trong current Store-local business-date window và loại Sale bị SaleVoid trong cùng business date.
- **Return:** Partial Return và full Return đều không giảm SaleCount.
- **Cross-day correction:** Sale ngày trước bị Void hôm nay không tạo `-1` hôm nay và không rewrite SaleCount lịch sử. Sale Completed và Void cùng ngày không được tính hôm nay.
- **Other metrics:** Return/Void vẫn phản ánh trong Revenue, Collected, COGS và explainability theo approved semantics.
- **Meaning:** SaleCount trả lời “Có bao nhiêu đơn bán được hoàn tất hôm nay và không bị hủy ngay trong cùng business date?”, không phải gross transaction-event count.

### D-071 — Slice 6 / Exact C14 data sufficiency

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **LowStockRisk full-history gate:** Store và Product đều phải có `CreatedAt <= velocityStartUtc`, để hệ thống có khả năng quan sát đủ toàn bộ 7 completed Store-local business days. Không yêu cầu Sale ở đủ 7 ngày, N ngày hoặc mỗi ngày có transaction; zero-sale day hợp lệ và denominator vẫn là `7`.
- **Risk calculation:** Sau sufficiency gate, `AverageDailySales = NetSoldQuantity / 7`; chỉ khi average `> 0` mới tính DaysOfCover/risk.
- **Factual state:** `OutOfStock`/`NegativeStock` không yêu cầu Product tồn tại đủ 7 ngày nhưng yêu cầu `NetSoldQuantity > 0` trong phần observable của cùng seven-completed-day window. Product mới có recent positive sales evidence có thể được factual attention sớm.
- **Noise prevention:** Current stock `<= 0` mà không có recent positive net-sales evidence không tạo C14 factual attention.
- **Store onboarding:** Không giả định lịch sử trước `Store.CreatedAt`; LowStockRisk chỉ sau full 7-day Store coverage, factual state có thể sớm hơn khi có positive evidence.
- **Typed evaluation:** Phân biệt bằng typed state giữa insufficient full history, no recent positive sales evidence và sufficient; localized text không làm business logic.

### D-072 — Slice 6 / C14 evaluates active Products only

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Candidate set:** Chỉ `Product.IsActive = true` được đánh giá cho `LowStockRisk`, `OutOfStock` và `NegativeStock`.
- **Inactive Product:** Không xuất hiện trong Today `Cần chú ý`, full C14 list hoặc action `Tạo phiếu nhập` từ C14, vì inactive thể hiện ý định không tiếp tục operationally sell/replenish Product.
- **Boundary:** Detect inactive Product có tồn âm/data bất thường là data-integrity capability riêng ngoài C14/Slice 6 hiện tại; không mở rộng scope để xử lý.

### D-073 — Technical Breakdown Slice 6 Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Approval:** Product Owner approve [Technical Breakdown Slice 6 v0.1](docs/architecture/technical-breakdown-slice-6-v0.1.md) tại reviewed baseline `daffb39c8f4d75f8bae0d83ba12be0484ce99280`; trạng thái tài liệu chuyển từ `DRAFT / PENDING PRODUCT OWNER REVIEW` sang `APPROVED FOR IMPLEMENTATION`.
- **Decision basis:** Approval bao gồm D-061–D-068 cho Slice 6 Product Owner scope, D-069 exact new-debt-created semantics, D-070 SaleCount semantics, D-071 C14 data sufficiency và D-072 active-only C14 candidate policy. S6-Q1–S6-Q4 đã được resolve trước approval; D-061–D-072 giữ nguyên `APPROVED`.
- **Today / C12:** Owner-only `/today` là default Owner landing cho current Store-local business date theo canonical `Store.TimeZoneId`, không có date picker; historical reporting tiếp tục dùng Slice 5 End-of-day.
- **Shared financial semantics:** Today reuse/shared projection với Slice 5 cho Revenue, Net Collected, Estimated Gross Profit và `CostReliability`; không tạo financial source of truth thứ hai.
- **D-069:** New debt created là direct unpaid obligation của Sale/Purchase hôm nay; direct transaction payment tham gia base calculation, standalone DebtPayment không được allocate. Same-day Return giảm transaction-local contribution của đúng original Sale bằng full `Return.TotalReturnAmount`, `RefundAmount` không phải input; same-day Void zero contribution; cross-day correction không rewrite historical metric; contribution floor tại `0`.
- **D-070:** SaleCount đếm Completed Sale trong Today, loại Sale có same-day SaleVoid; Return không giảm count; cross-day Void không tạo negative count hoặc rewrite ngày cũ.
- **C13 explainability:** Giữ pattern `Summary → Vì sao? → Dữ liệu nguồn` với typed evidence/source references; localized message không được parse để điều khiển logic.
- **C14:** Dùng đúng 7 completed Store-local business days, current day excluded và denominator luôn `7` khi full-history gate đạt; quantity theo Sale/Return/SaleVoid, CurrentStock từ InventoryBalance, risk threshold `DaysOfCover <= 3`, factual OutOfStock/NegativeStock, D-071 sufficiency, D-072 active-only candidates, deterministic ordering, Today preview tối đa 3 và full attention list/detail.
- **Action transition:** Cho phép Product detail và preselect Product khi đi tới Create Purchase; không tự chọn Supplier, recommend quantity, auto-create hoặc auto-commit Purchase.
- **Experiment measurement:** Chỉ authorize bốn immutable C14-specific events `TodayOpened`, `SignalShown`, `WhyOpened`, `PurchaseDraftStarted`. Reactive render không duplicate `TodayOpened`; mỗi `ProductId + AttentionKind` tối đa một `SignalShown` trong một Today view instance; genuine reload/navigation/new view có thể là exposure mới; EventId retry không duplicate persisted row; measurement failure không ảnh hưởng business transaction; `click != validated value`. Không tạo generic analytics platform.
- **Approved staging:** Stage 6A — Today/C12/C13 reporting foundation — và Stage 6B — C14/action/measurement/E2E — được authorize làm implementation sequence với trạng thái `APPROVED FOR IMPLEMENTATION`. D-073 không có nghĩa Stage 6A/6B đã bắt đầu hoặc hoàn tất; Slice 6 implementation vẫn `NOT STARTED`.
- **Stage 6A scope:** Shared daily financial projection; current Store-local Today orchestration; Today summary; D-069 new-debt-created; D-070 SaleCount; typed financial explanations; Owner `/today` và default landing; domain, SQL Server integration và frontend tests.
- **Stage 6B scope:** D-071 data sufficiency; D-072 active-only candidate set; C14 preview/list/detail và evidence; Product → Purchase transition; narrow immutable experiment events; frontend/integration tests và critical real local E2E/regression.
- **Verification:** GitHub Actions run #36 / `35881101954` tại head `daffb39c8f4d75f8bae0d83ba12be0484ce99280` — `SUCCESS`; backend restore, Release build và `dotnet test` pass; frontend `pnpm install`, build và tests pass. Slice 6 implementation chưa bắt đầu và chưa có real Slice 6 E2E evidence.
- **Boundaries:** Không AI/ML, forecasting/seasonality/replenishment engine, recommendation quantity/Supplier, auto-order, generic rule/alert/analytics/event platform, BI dashboard, notification center hoặc push/email alerts.
- **Tiếp theo:** Bắt đầu Stage 6A implementation theo D-073; không suy diễn rằng approval tài liệu là implementation approval.

### D-074 — Slice 6 Stage 6A Implementation Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-23
- **Người phê duyệt:** Product Owner
- **Approval:** Product Owner approve Stage 6A — Today/C12/C13 reporting foundation sau implementation review và source-drill-down/documentation hardening. Reviewed implementation head là `14703c54882f10eceaee69fa41f09e2315eab8ac`; final documentation/state head trước approval là `8b11e1f7706bd25e51aca6aa21c1fd8af3baa60c`. Stage 6A chuyển từ `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW` sang `APPROVED / COMPLETED`.
- **Shared financial projection:** EOD và Today dùng chung `DailyFinancialProjection`; Revenue, Net Collected, historical COGS, Estimated Gross Profit và `CostReliability` không có semantic/source-of-truth thứ hai. Existing Slice 5 EOD behavior được giữ nguyên.
- **Store-local Today:** Backend quyết định current business date bằng injected `TimeProvider`, canonical IANA `Store.TimeZoneId` và `BusinessDateWindow`; mọi window dùng half-open `[StartUtc, EndUtc)`, hỗ trợ DST boundary 23/25 giờ. Browser/server OS timezone không phải authority.
- **Owner API/UI:** Owner-only `GET /api/today`, `/today` không có date picker và hiển thị Store-local date/timezone. Owner mặc định vào `/today`; Cashier giữ operational landing; backend tiếp tục là authorization authority.
- **Today summary:** Approved implementation gồm Sales Revenue, Net Collected, Estimated Gross Profit, historical COGS, `CostReliability`, SaleCount, CustomerDebtCreated và SupplierDebtCreated.
- **D-069:** Direct transaction payments giảm base obligation; standalone DebtPayment không được allocate và không trực tiếp/gián tiếp giảm new-debt-created. Same-day Return dùng full `Return.TotalReturnAmount`, không dùng `RefundAmount`; same-day SaleVoid/PurchaseVoid zero contribution; cross-day correction không rewrite original date; contribution không âm và excess correction không chuyển sang transaction khác. Regression Sale `1,000,000`, direct payment `0`, standalone CustomerDebtPayment `1,000,000`, same-day Return `300,000` cho `CustomerDebtCreated = 700,000` được approve.
- **D-070:** SaleCount đếm Completed Sale trong Today, loại same-day SaleVoid; Return không giảm count; prior-day Sale + Today Void không tạo `-1`; exact start được include và exact end bị exclude.
- **C13 explainability:** Approve typed flow `Summary → Vì sao? → Dữ liệu nguồn`, closed metric identifiers, paged evidence, evidence/headline reconciliation, typed contribution amount/count, debt contribution breakdown và không parse localized text để điều khiển logic.
- **Typed source drill-down:** Backend chọn optional navigation thuộc closed set `Sale | Return | Purchase`; Sale → Sale detail, Return → Return detail, SaleVoid → original Sale, SalePayment → Sale, ActualCustomerRefund → Return, Purchase → Purchase detail. Historical COGS DirectSale → Sale, RestockedReturn → Return, VoidedSale → original Sale. CustomerDebtPayment không có phù hợp detail route nên `Navigation = null`; frontend không suy route từ localized title.
- **Authorization/isolation:** Owner được truy cập; Cashier bị forbidden; anonymous là unauthenticated. Summary/evidence luôn Store-scoped và Store A không đọc source identity/evidence của Store B.
- **Verification:** Tại implementation/hardening head `14703c54882f10eceaee69fa41f09e2315eab8ac`, local backend restore và Release build pass với 0 warnings; Domain tests 78/78; SQL Server integration tests 82/82; frontend frozen install/build pass; frontend tests 93/93. GitHub Actions run #39 / `35891666016` tại cùng head là `SUCCESS`, gồm backend và frontend jobs `SUCCESS`.
- **Documentation evidence:** Docs alignment head `8b11e1f7706bd25e51aca6aa21c1fd8af3baa60c`; GitHub Actions run #40 / `35893244524` là `SUCCESS`.
- **Boundaries:** D-074 không approve hoặc implement Stage 6B. C14 7-day velocity/DaysOfCover/factual attention, preview/list/detail, `C14ExperimentEvents`, `TodayOpened`, `SignalShown`, `WhyOpened`, `PurchaseDraftStarted`, Product → Purchase preselection, Stage 6B migration và Stage 6B real E2E vẫn chưa implement. Không AI, forecasting/replenishment engine, generic rule/analytics platform hoặc notification system; không tuyên bố real Stage 6B E2E.
- **Tiếp theo:** Bắt đầu Stage 6B — C14/action/measurement/E2E theo D-073. D-074 không có nghĩa Stage 6B đã bắt đầu.

### D-075 — Slice 6 Stage 6B Implementation Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-24
- **Người phê duyệt:** Product Owner
- **Approval:** Product Owner approve Stage 6B — C14/action/measurement/E2E. Stage 6B chuyển từ `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW` sang `APPROVED / COMPLETED`.
- **Reviewed implementation chain:** implementation commit `17e03c23c83630b84535fdb7be0a41ce16a99b6f`; implementation/docs evidence `610b53d8c31a7d5d70b07a6883c47ad146ee1944`; CI evidence docs `d01e43517c576f832fcc3ddc432de7edc9079834`; final E2E hardening/review head `844af703173070872219c1cf729afa04d16fa4b4`.
- **Deterministic C14 window/velocity:** Approve exactly 7 completed Store-local business days, current incomplete day excluded, canonical `Store.TimeZoneId` + `BusinessDateWindow`, DST-safe boundaries; Sale quantity positive, Return quantity negative và SaleVoid original quantity negative theo Void event date. Purchase, PurchaseVoid, OpeningBalance, InventoryAdjustment và inventory movements nói chung không tham gia sales velocity. Current stock đọc từ Main Warehouse `InventoryBalance`; missing balance được xem là stock `0`.
- **D-071 sufficiency:** LowStockRisk yêu cầu Store và Product `CreatedAt <= velocityStartUtc`; denominator luôn đúng `7`, zero-sale days hợp lệ và không có minimum sales-day/transaction requirement. Factual OutOfStock/NegativeStock có thể xuất hiện dưới partial observation khi `NetSoldQuantity > 0`; stock `<= 0` mà không có positive recent net-sales evidence không tạo factual attention. Approve closed typed history coverage, recent-sales evidence và risk-evaluation states.
- **D-072 active-only:** Chỉ `Product.IsActive == true` là C14 candidate. Inactive Product không xuất hiện trong Today preview, full list, detail, telemetry target hoặc Product → Purchase action.
- **Classification/order:** Approve closed `NegativeStock`, `OutOfStock`, `LowStockRisk`. LowStockRisk yêu cầu `CurrentStock > 0`, full 7-day history, `NetSoldQuantity > 0` và exact `DaysOfCover <= 3`, với `AverageDailySales = NetSoldQuantity / 7` và `DaysOfCover = CurrentStock / AverageDailySales`; threshold so sánh trước presentation rounding và đúng `3` vẫn qualify. Ordering là factual trước risk; risk theo exact DaysOfCover tăng dần; normalized Product name + ProductId là stable tie-breaker; Today preview tối đa 3 và giữ full total count.
- **Explainability:** Detail bao gồm current stock, 7 completed dates và exact UTC boundaries, NetSoldQuantity, AverageDailySales, DaysOfCover, typed sufficiency/evaluation, formula inputs và typed Sale/Return/SaleVoid evidence. Quantity evidence reconcile chính xác với NetSoldQuantity; navigation tới source transaction là typed.
- **Action transition:** Approve `Xem sản phẩm` và `Tạo phiếu nhập`. Product → Purchase chỉ preselect Product identity; Supplier, quantity và unit price để trống. Không Supplier/quantity recommendation và không tự tạo/complete Purchase.
- **Experiment domain:** Approve append-only `C14ExperimentEvents` với closed EventTypes `TodayOpened | SignalShown | WhyOpened | PurchaseDraftStarted` và closed AttentionKinds `NegativeStock | OutOfStock | LowStockRisk`. Store, Actor, BusinessDate và OccurredAt do server derive; Product phải active và Store-scoped. Same EventId + same normalized identity là idempotent retry; reuse EventId cho identity khác là conflict; concurrency-safe; không có update/delete API. Telemetry là best-effort và không chặn business flow.
- **Exposure semantics:** `TodayOpened` chỉ sau real Today presentation và một lần mỗi mounted view; rerender/state change không duplicate, còn navigation/reload mới có thể tạo event mới. `SignalShown` yêu cầu actual visible exposure, tối đa một lần cho mỗi `ProductId + AttentionKind` trong Today view, dùng per-view guard và `IntersectionObserver`/equivalent. `WhyOpened` chỉ từ explicit `Xem vì sao`; `PurchaseDraftStarted` chỉ từ explicit `Tạo phiếu nhập` trong C14 flow. Event counts chỉ là descriptive telemetry, không chứng minh C14 value hoặc willingness-to-pay.
- **Migration:** Approve additive migration `20260924010903_ImplementSlice6Stage6BC14`, chỉ thêm `C14ExperimentEvents` cùng approved keys/FKs/check constraint/indexes; không có unrelated schema change. Clean DB migration/startup, upgrade từ pre-Stage-6B schema và existing-data preservation đều đã verify.
- **Local verification:** Backend Release build pass, 0 warnings; Domain 83/83; SQL Server integration 88/88; frontend build pass; frontend 100/100; real Slice 6 Playwright 2/2 qua Vue → ASP.NET Core → temporary SQL Server/LocalDB.
- **Real Today evidence:** D-069 real UI xác nhận Sale `1,000,000`, direct payment `0`, standalone CustomerDebtPayment `1,000,000`, same-day Return/refund `300,000` cho CustomerDebtCreated `700,000`; explanation hiển thị original `1,000,000`, direct `0`, base `1,000,000`, Return correction `300,000`, final `700,000`, không allocate standalone payment. D-070 real UI xác nhận partial/full Return vẫn count, same-day Void contribution `0`, cross-day Sale/Void không tạo Today contribution hoặc negative count. Supplier smoke xác nhận `500,000 - 200,000 = 300,000` và standalone SupplierDebtPayment không giảm metric.
- **Real C14 evidence:** Verify max-3 preview, full list, inactive exclusion, Sale/Return/Void evidence, Why detail, Product navigation, Purchase transition, `TodayOpened`, `SignalShown`, `WhyOpened`, `PurchaseDraftStarted` và không tạo Purchase chỉ do mở preselected form.
- **CI evidence:** Run #42 / `35944048457`, run #43 / `35944540478` và run #44 / `35948003969` đều `SUCCESS`; backend/frontend jobs `SUCCESS`. Run #44 tại final reviewed head `844af703173070872219c1cf729afa04d16fa4b4` pass restore/build/test. GitHub CI không chạy real Playwright E2E; real E2E là local temporary-database evidence riêng.
- **Boundaries:** D-075 approve implementation, không validate C14 hypothesis/product value, discovery, trust, decision influence, continued use hoặc willingness-to-pay. Không mở rộng sang AI/ML, forecasting, seasonality, replenishment, Supplier/quantity recommendation, auto-order, generic rule/alert/analytics platform, notification center hoặc BI dashboard.
- **Slice gate:** D-075 chỉ approve Stage 6B. Stage 6A giữ `APPROVED / COMPLETED — D-074`; Stage 6B là `APPROVED / COMPLETED — D-075`. Toàn bộ Slice 6 chưa được final-approved trong decision này; bước tiếp theo là final Slice 6 review và explicit Product Owner approval riêng.

### D-076 — Final Slice 6 Approval

- **Trạng thái:** `APPROVED`
- **Ngày:** 2026-09-24
- **Người phê duyệt:** Product Owner
- **Approval:** Product Owner final-approve Slice 6 — Understand & Act sau product decisions D-061–D-072, Technical Breakdown approval D-073, Stage 6A approval D-074, Stage 6B approval D-075, implementation review, hardening, migration verification, backend/frontend regression, SQL Server integration tests và real local Slice 6 E2E. Toàn bộ Slice 6 chuyển thành `APPROVED / COMPLETED`.
- **C12 — Business Situation Understanding:** Owner-only `/today` trả lời “Hôm nay cửa hàng thế nào?” bằng Sales Revenue, Net Collected, Estimated Gross Profit, `CostReliability`, SaleCount, CustomerDebtCreated, SupplierDebtCreated và C14 attention. Current business date theo Store-local timezone do backend làm authority.
- **C13 — Explainable Insights:** Approve flow `Summary → Vì sao? → Dữ liệu nguồn` với typed evidence, bounded/paged evidence, source reconciliation và backend-selected typed navigation tới Sale, Return hoặc Purchase detail khi có route an toàn.
- **D-069:** Final-approve transaction-local new-debt-created semantics. Case bắt buộc Sale `1,000,000`, direct SalePayment `0`, standalone CustomerDebtPayment `1,000,000`, same-day `Return.TotalReturnAmount = 300,000` cho `CustomerDebtCreated = 700,000`. Standalone DebtPayment và `RefundAmount` không định nghĩa lại transaction-local obligation contribution.
- **D-070:** Final-approve SaleCount là Completed Sale hôm nay; Return không giảm count; same-day Void loại Sale; prior-day Sale + today Void không tạo negative Today count hoặc rewrite historical SaleCount.
- **C14 deterministic experiment:** Final-approved implementation dùng đúng 7 completed Store-local business days; velocity là Sale − Return − SaleVoid net sold quantity; current Main Warehouse `InventoryBalance`; D-071 history sufficiency; D-072 active-only candidates; closed `NegativeStock`, `OutOfStock`, `LowStockRisk`; exact `DaysOfCover <= 3`; deterministic ordering; Today preview tối đa 3; full list/detail/evidence. C14 vẫn là experiment, không phải product value đã validated.
- **Action transition:** Approve `C14 → Xem vì sao → Tạo phiếu nhập`; Product identity có thể được preselect. Hệ thống không chọn Supplier, quantity hoặc purchase price; không tự động tạo/complete Purchase.
- **Measurement:** Approve bốn immutable events `TodayOpened`, `SignalShown`, `WhyOpened`, `PurchaseDraftStarted`. Measurement chỉ có tính descriptive; click/event không chứng minh product value.
- **Stage approvals:** Stage 6A là `APPROVED / COMPLETED — D-074`, reviewed implementation head `14703c54882f10eceaee69fa41f09e2315eab8ac`. Stage 6B là `APPROVED / COMPLETED — D-075`, implementation `17e03c23c83630b84535fdb7be0a41ce16a99b6f`, final reviewed/hardened head `844af703173070872219c1cf729afa04d16fa4b4`, approval head `a24437cd37762d027fe6970f9809baf904a8e3e8`.
- **Verification:** Backend Release build pass với 0 warnings; Domain 83/83; SQL Server integration 88/88. Frontend build pass và tests 100/100. Real local Slice 6 Playwright 2/2 pass qua Vue → ASP.NET Core → temporary SQL Server/LocalDB, bao phủ D-069 required 700,000 scenario, D-070 Return/Void, Supplier new-debt-created smoke, C14 preview/full list/detail, Sale/Return/Void evidence, Product navigation, Product → Purchase transition và bốn approved measurement events.
- **Migration:** `20260924010903_ImplementSlice6Stage6BC14` đã được verify bằng clean migration, application startup, upgrade từ pre-Stage-6B schema và data preservation.
- **CI evidence:** GitHub Actions #42 / `35944048457`, #43 / `35944540478`, #44 / `35948003969` và #45 / `35996028241` đều `SUCCESS`; run #45 là CI của Stage 6B approval head, backend/frontend đều `SUCCESS`. GitHub CI không chạy real Playwright E2E; real E2E là local temporary-database evidence riêng.
- **Validation boundary:** D-076 final-approve implemented Slice 6 scope nhưng không xác nhận C14 hypothesis/product value, discovery, trust, decision influence, continued use hoặc willingness-to-pay. Measurement không thay thế pilot/research.
- **Scope boundary:** D-076 không approve AI/ML, forecasting/seasonality, replenishment engine, Supplier/quantity recommendation, auto-order, generic rules/alerts/analytics platform, BI dashboard hoặc notification subsystem; cũng không có nghĩa MVP đã release, production-ready hoặc pilot đã hoàn tất.
- **Next phase:** Bắt đầu MVP / pilot-readiness planning và validation; không mở implementation slice mới trong decision này.
