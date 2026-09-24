# Open Questions

File này theo dõi các câu hỏi chưa được trả lời trong giai đoạn Product Discovery.

## Câu hỏi đang mở

Các nội dung dưới đây là câu hỏi hoặc assumption cần kiểm chứng, không phải product decision đã được phê duyệt.

### Primary Persona / Jobs To Be Done — kiểm chứng tiếp

- Những phân khúc nào tồn tại bên trong Primary Persona đã được phê duyệt theo quy mô cửa hàng, địa bàn, số người vận hành và mức độ trưởng thành số?
- Tần suất, mức độ quan trọng và mức độ khó hiện tại của từng JTBD đã được phê duyệt là gì?
- Họ đang dùng quy trình và công cụ nào để hoàn thành các công việc đó?
- Vai trò bán hàng, nhập hàng/tồn kho và theo dõi kết quả được một người hay nhiều người cùng đảm nhiệm?

### Adoption Problem

- Những rào cản nào khiến họ khó bắt đầu hoặc duy trì sử dụng một giải pháp hỗ trợ vận hành?
- Mức độ thành thạo phần mềm và kế toán khác nhau thế nào giữa các phân khúc, và ảnh hưởng ra sao đến khả năng sử dụng?

### Decision Problem

- Những quyết định kinh doanh nào thường xuyên, quan trọng và khó khăn nhất?
- Họ hiện dựa vào dữ liệu, kinh nghiệm hoặc tín hiệu nào để ra quyết định?
- Bằng chứng nào cho thấy chiều sâu và mức độ ảnh hưởng của Decision Problem trong thực tế vận hành?

### C14 / Differentiator Hypothesis — cần tiếp tục kiểm chứng

C14 — Attention & Decision Support đã được phê duyệt là một phần của Capability Map. Step 6 đã phê duyệt đưa C14 vào MVP chỉ ở mức experiment mỏng, ưu tiên rule-based/deterministic, chưa cần AI. Việc phê duyệt capability và phạm vi experiment không xác nhận nhu cầu, value hoặc willingness-to-pay cho outcome liên quan; các giả thuyết này vẫn chưa validated.

- “Cho tôi biết điều gì cần chú ý và nên làm gì tiếp theo” có phải là một outcome đủ quan trọng và thường xuyên không?
- Người dùng cần thấy bằng chứng hoặc cách giải thích nào để tin và hành động theo gợi ý?
- Hypothesis này có tạo khác biệt đủ rõ so với các sản phẩm hiện có không?
- Người dùng có willingness-to-pay cho outcome này hay chỉ sẵn sàng trả tiền cho các workflow nền tảng?

### Current User Journey / Pain Point Map — kiểm chứng tiếp

- Tần suất và mức độ nghiêm trọng của P1–P7 khác nhau thế nào giữa các phân khúc trong Primary Persona?
- Giai đoạn nào trong vòng vận hành tạo ra nhiều gián đoạn, sai sót hoặc mất niềm tin nhất?
- HĐĐT/thuế phát sinh tại những điểm nào trong bán hàng, đổi trả và điều chỉnh đối với từng nhóm cửa hàng?
- Có thể quan sát và đo lường DO, TRUST và UNDERSTAND & ACT bằng những hành vi hoặc kết quả nào?

### Làm rõ trong phạm vi MVP đã duyệt

Các câu hỏi này không mở lại phạm vi Step 6 và không tự tạo thêm capability hoặc feature:

- Step 7 đã chọn duy nhất hypothesis C14: nguy cơ sắp hết hàng từ tồn hiện tại và tốc độ bán gần đây. D-064/D-065 đã chốt cửa sổ/threshold và D-071 đã chốt exact data-sufficiency rule. Bằng chứng nào đủ để đánh giá value/willingness-to-pay vẫn phải được kiểm chứng bằng pilot/research theo D-068; đây là experiment-validation question, không phải blocker cho Technical Breakdown semantics.
- C17 đã chốt keyboard scanner phổ biến và in bill. D-082 đã approve strategy: trước pilot certify một khổ giấy và 1–2 target printer/model/configuration, ưu tiên browser print; exact model và paper size vẫn cần được chọn trong Pilot Readiness work. Integration C19 cụ thể nào thực sự cần cho vòng MVP?
- Step 7 đã chốt C7 chỉ yêu cầu domain sale đủ sạch để sau này ánh xạ sang request của service API HĐĐT hiện có mà không phá cấu trúc giao dịch. Chi tiết ánh xạ chỉ làm rõ ở bước thiết kế phù hợp sau này; không xây capability HĐĐT nội bộ hoặc kết nối service ngay trong MVP.

Phạm vi đã chốt được ghi tại [MVP Scope v0.1](docs/product/mvp-scope-v0.1.md).

### Sau Step 7 — chi tiết còn cần làm rõ

Các câu hỏi dưới đây không mở rộng functional scope đã APPROVED và chưa phải quyết định thiết kế:

- Pilot thực tế có cần nhiều barcode cho một sản phẩm không (SHOULD có điều kiện tại C2)?
- Chi tiết định dạng template/validation per field sẽ hoàn thiện cho Slice 1 theo required fields và Opening Cost rule đã chốt tại Step 11; không mở lại all-or-nothing policy.

Phạm vi functional scope tham chiếu: [MVP Functional Scope v0.1](docs/capabilities/mvp-functional-scope-v0.1.md).

### Sau Step 8 — các quyết định còn cần làm rõ

Step 8 đã APPROVED 6 user flows và các nguyên tắc identity/idempotency, timeout recovery, Completed bất biến, Purchase consistency, Return validation và Reprint. Các câu hỏi sau không mở lại các nguyên tắc đó:

- Khổ giấy/thiết bị pilot sẽ theo D-082: chọn một paper size và 1–2 target printer configurations để certify trước pilot; exact target vẫn cần chọn trong readiness work. Ngưỡng/cửa sổ C14 đã được resolve sau đó tại D-064/D-065/D-071.

Tài liệu: [MVP User Flows v0.1](docs/ux/mvp-user-flows-v0.1.md). Chưa chốt database schema, API contract, UI/wireframe chi tiết hoặc architecture implementation ở Step 8.

## Resolved Pilot Readiness Questions — D-085–D-091

[Technical Breakdown Pilot Readiness v0.1](docs/architecture/technical-breakdown-pilot-readiness-v0.1.md) vẫn ở trạng thái `DRAFT / PENDING PRODUCT OWNER REVIEW`; PR-Q1–PR-Q7 đã được Product Owner resolve nhưng chưa có nghĩa Technical Breakdown được approve hoặc implementation được authorize.

- PR-Q1 → D-085: explicit one-shot admin CLI/command cho first Owner, do authorized deployment operator chạy; không public endpoint, Production seeder hoặc normal-flow manual SQL.
- PR-Q2 → D-086: temporary Cashier password + mandatory change; pre-change session chỉ cho password change/logout/minimal auth state; reset/disable invalidates sessions.
- PR-Q3 → D-087: Stock Adjustment costing/reliability dùng reliable average hoặc explicit cost cho positive; negative dùng closed `Reliable` / `Estimated` / `Unavailable` chain; không retroactive revaluation.
- PR-Q4 → D-088: Stocktake difference reuse chính xác D-087, giữ immutable typed Stocktake source/movement và không deferred reconciliation.
- PR-Q5 → D-089: typed `409 stocktake-stale`, refresh + recount + new submission; exact completed OperationId retry giữ idempotent theo D-014.
- PR-Q6 → D-090: Full recovery, nightly full, transaction-log mỗi 15 phút, recovery chain 14 ngày, weekly full 8 tuần, separate restricted storage và actual isolated full+log restore drill.
- PR-Q7 → D-091: `80 mm` thermal/browser print baseline; actual model/interface/Windows/driver/browser/paper config được ghi trong PR-C, không invent model trước evidence.

## Đã giải quyết cho Slice 5 — APPROVED

Toàn bộ sáu Product Owner questions chặn ban đầu của Slice 5 đã được giải quyết tại D-051–D-056:

- D-051: Owner và Cashier được xem/thu Customer debt.
- D-052: Supplier debt, Supplier Debt Payment, End-of-day và Estimated Gross Profit là Owner-only; không có Cashier-specific reduced EOD.
- D-053: headline Collected là net actual collected sau actual customer refunds, đồng thời giữ component breakdown.
- D-054: Store dùng configurable canonical IANA timezone; Store hiện hữu backfill/default `Asia/Ho_Chi_Minh`.
- D-055: Debt Payment có optional immutable Note tối đa 250 ký tự; normalized Note tham gia fingerprint; không có Reference riêng.
- D-056: correction không được tạo aggregate debt âm; Return refund phần obligation reduction vượt current Customer debt, còn Sale/Purchase Void gây debt âm phải bị reject.

Không còn Product Owner Open Question nào đang chặn Technical Breakdown Slice 5. Các lựa chọn schema, EF mapping, timezone conversion compatibility, SQL lock resource và index là implementation details phải tuân theo các decision đã approve, không phải Product Owner questions mới.

[Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) đã được Product Owner final-approve tại D-057 và hiện là `APPROVED FOR IMPLEMENTATION`; không còn Product Owner blocker trước Stage 5A.

## Đã giải quyết cho Slice 6 — APPROVED

D-061–D-068 đã approve scope A–H; D-069–D-072 đã resolve toàn bộ bốn semantic questions phát hiện trong [Technical Breakdown Slice 6 v0.1](docs/architecture/technical-breakdown-slice-6-v0.1.md). Không còn known Product Owner semantic blocker; Technical Breakdown đã được Product Owner approve tại D-073 với trạng thái `APPROVED FOR IMPLEMENTATION`. Các câu hỏi nghiên cứu dài hạn về C14 value/willingness-to-pay vẫn intentionally unresolved và không chặn implementation.

### S6-Q1 — New debt created và correction/unallocated DebtPayment — RESOLVED by D-069

New debt created là direct unpaid obligation của Sale/Purchase trong business date. Standalone DebtPayment không giảm metric. Same-day correction chỉ giảm contribution của actual original transaction trong cùng date và floor tại zero; cross-day correction không rewrite ngày gốc hoặc tạo negative metric ngày correction.

### S6-Q2 — Exact Sale count với Void và Return — RESOLVED by D-070

Đếm Sale Completed trong current business-date, loại Sale bị Void cùng date. Partial/full Return không giảm count. Cross-day Void không tạo negative count hôm nay và không rewrite historical count.

### S6-Q3 — Exact data sufficiency cho 7 completed business days — RESOLVED by D-071

LowStockRisk yêu cầu Store và Product tồn tại từ trước hoặc đúng `velocityStart`; zero-sale days vẫn hợp lệ và denominator là 7. Factual stock-out/negative-stock không cần full Product history nhưng yêu cầu recent `NetSoldQuantity > 0` trong observable part của window. Typed state phân biệt insufficient history, no positive evidence và sufficient.

### S6-Q4 — Active/inactive Product trong C14 — RESOLVED by D-072

C14 chỉ đánh giá active Product cho mọi factual/risk state và action. Inactive Product hoàn toàn nằm ngoài C14 candidate/list/action; data-integrity detection cho inactive Product là capability riêng ngoài scope.

## Đã giải quyết tại Step 9 — APPROVED

### Phương pháp giá vốn — D-012 / Decision A

**Đã quyết định:** Moving Weighted Average — Bình quân gia quyền di động.

SaleLine snapshot giá vốn tại Sale Completed; không tính lại lịch sử từ CurrentCost. Return dùng cost basis SaleLine gốc; restock phục hồi quantity/value phù hợp. Purchase reversal đảo đúng quantity/value contribution của transaction gốc theo business semantics. Không đưa lot/FIFO/serial/expiry costing vào MVP.

### Payment / Debt behavior — D-013 / Decision B

**Đã quyết định:** Payment là tiền thực thu/thực trả, domain cho phép nhiều Payment liên quan một transaction. Khách trả nợ sau và cửa hàng trả nợ NCC sau đều phải ghi nhận Payment và giảm Outstanding Debt.

Customer/Supplier debt được suy ra và giải thích từ transaction, payment, returns/reversals/adjustments phù hợp; không sửa số nợ tùy ý. Customer chỉ cần đủ để nhận diện người đang nợ; không mở CRM hoặc generic accounting ledger.

## Đã giải quyết tại Step 10 — APPROVED

### Transaction / Idempotency strategy — D-014 / Decision A

**Đã quyết định:** Critical operation dùng client-generated `OperationId`/`IdempotencyKey`. Retry cùng operation đã Completed trả lại kết quả cũ; không tạo business transaction mới. Cùng OperationId nhưng request identity khác bị từ chối.

Business changes và operation result phải commit atomically trong cùng local SQL transaction. Frontend disable button chỉ hỗ trợ UX; backend là idempotency boundary. Các chi tiết schema, request fingerprint representation, status transition và recovery implementation sẽ được làm just enough cho slice tương ứng; Step 11 chưa chốt toàn bộ.

### Inventory ledger / materialized balance / concurrency — D-014 / Decision B

**Đã quyết định:** `InventoryMovement` là immutable/explainable ledger; `InventoryBalance` là materialized operational state. Movement và balance cập nhật atomically trong cùng transaction.

Concurrent mutation trên cùng `Product + Warehouse` phải được serialized/controlled và operation nhiều sản phẩm dùng deterministic order. Không hỗ trợ direct retroactive Purchase Void khi downstream movement làm costing không còn an toàn; không xây retroactive costing/revaluation engine trong MVP. Locking/isolation/optimistic concurrency strategy cụ thể còn mở cho technical design của slice tương ứng.

### Negative Stock policy — D-014 / Decision C

**Đã quyết định:** Store có `AllowNegativeStock`, mặc định `false`; chỉ Owner thay đổi và thay đổi phải audit được. Khi tắt, backend từ chối Sale làm âm tồn và báo lượng thiếu. Khi bật, Sale được hoàn tất, movement vẫn ghi và balance có thể âm.

Cost tạm dùng last known average cost, fallback reference purchase cost; nếu cost vẫn chưa đáng tin thì cost/profit liên quan phải được đánh dấu và hiển thị là ước tính. Không retroactively revalue lịch sử trong MVP.

## Chi tiết kỹ thuật còn mở sau Step 11

Các câu hỏi này là technical/design detail hoặc validation tiếp theo, không mở lại Architecture v0.1:

- Định dạng template cụ thể trong phạm vi các rule Step 11 và nhu cầu nhiều barcode cho một sản phẩm trong pilot.
- D-082 đã chọn browser print làm default strategy và yêu cầu certify một paper size + 1–2 target printer configurations; exact model/paper vẫn cần chọn. Chỉ mở technical decision về local print agent/printer service nếu browser print không đạt trên target setup.
- D-064/D-065/D-071/D-072 đã chốt C14 window, formula, threshold, exact sufficiency/factual evidence và active-only candidate set. Value/willingness-to-pay vẫn cần pilot/research theo D-068 nhưng không chặn technical semantics.
- D-077/D-080/D-081/D-083 đã chốt direction cho pilot deployment topology, backup/restore, observability/support và release gate. Technical Breakdown vẫn cần cụ thể hóa Windows Server/IIS procedure, config/secrets, backup schedule/retention/storage, DB-aware health/log retention và release/rollback evidence; Authentication direction giữ theo Step 11 và production provisioning theo D-078.
- Request mapping, lifecycle/status và retry/dispatch mechanism nếu HĐĐT integration được bổ sung sau MVP core.

Tham chiếu: [Architecture v0.1](docs/architecture/architecture-v0.1.md). Các chi tiết Slice 2+ được giải quyết per slice; không mở lại Step 1–10.

## Đã giải quyết tại Step 11 — APPROVED

### Tenancy Foundation — D-016 / Decision D

1 tenant/account → 1 Store → 1 Main Warehouse; shared deployment/database có thể nhiều Store tenant. Business data phải scope theo Store/Tenant; test User Store A không đọc/sửa dữ liệu Store B. Không multi-branch/cross-store hoặc tenant management platform lớn.

### Initial Import Policy — D-017 / Decision E

Template cố định; Validate → Preview → Confirm. Có lỗi thì không import và báo rõ dòng/trường/lý do. Confirm all-or-nothing; không partial import trong pilot đầu. Product, OpeningBalance movement và InventoryBalance ghi atomically; retry confirm không duplicate.

SKU bắt buộc nhưng có thể auto-generate; Barcode optional; Name/Unit/SalePrice required; ReferencePurchaseCost optional. Opening Qty > 0 cần Opening Cost hợp lệ. Các câu hỏi policy import ở Step 8/10 đã được giải quyết.

### Foundation direction — D-015

Identity/Auth + secure HttpOnly cookie cho SPA cùng site; không JWT/localStorage mặc định. EF Core migrations trong Infrastructure, production migration explicit. Testing dùng xUnit, WebApplicationFactory + SQL Server test DB, Vitest/Vue Test Utils và Playwright. Không dùng EF InMemory để kiểm chứng inventory transaction/concurrency.

**Ready to begin implementation — Slice 0 Engineering Foundation**

Sau đó: **Slice 1 — Setup + Product**. Tham chiếu: [Development Plan](docs/architecture/development-plan-v0.1.md), [Technical Breakdown Slice 0–1](docs/architecture/technical-breakdown-slice-0-1-v0.1.md).
