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

- Step 7 đã chọn duy nhất hypothesis C14: nguy cơ sắp hết hàng từ tồn hiện tại và tốc độ bán gần đây. Cửa sổ dữ liệu, ngưỡng và điều kiện dữ liệu đủ tin cậy nào phù hợp cho pilot? Bằng chứng nào đủ để đánh giá value/willingness-to-pay?
- C17 đã chốt keyboard scanner phổ biến và in bill. Model thiết bị/khổ giấy mục tiêu nào cần hỗ trợ trong pilot? Integration C19 cụ thể nào thực sự cần cho vòng MVP?
- Step 7 đã chốt C7 chỉ yêu cầu domain sale đủ sạch để sau này ánh xạ sang request của service API HĐĐT hiện có mà không phá cấu trúc giao dịch. Chi tiết ánh xạ chỉ làm rõ ở bước thiết kế phù hợp sau này; không xây capability HĐĐT nội bộ hoặc kết nối service ngay trong MVP.

Phạm vi đã chốt được ghi tại [MVP Scope v0.1](docs/product/mvp-scope-v0.1.md).

### Sau Step 7 — chi tiết còn cần làm rõ

Các câu hỏi dưới đây không mở rộng functional scope đã APPROVED và chưa phải quyết định thiết kế:

- Pilot thực tế có cần nhiều barcode cho một sản phẩm không (SHOULD có điều kiện tại C2)?
- Chi tiết định dạng template/validation per field sẽ hoàn thiện cho Slice 1 theo required fields và Opening Cost rule đã chốt tại Step 11; không mở lại all-or-nothing policy.

Phạm vi functional scope tham chiếu: [MVP Functional Scope v0.1](docs/capabilities/mvp-functional-scope-v0.1.md).

### Sau Step 8 — các quyết định còn cần làm rõ

Step 8 đã APPROVED 6 user flows và các nguyên tắc identity/idempotency, timeout recovery, Completed bất biến, Purchase consistency, Return validation và Reprint. Các câu hỏi sau không mở lại các nguyên tắc đó:

- Khổ giấy/thiết bị pilot và ngưỡng/cửa sổ dữ liệu C14 vẫn cần làm rõ như các câu hỏi ở trên; ví dụ trong user flows không chốt các lựa chọn này.

Tài liệu: [MVP User Flows v0.1](docs/ux/mvp-user-flows-v0.1.md). Chưa chốt database schema, API contract, UI/wireframe chi tiết hoặc architecture implementation ở Step 8.

### Slice 5 — Debt + End-of-day

D-043–D-050 đã `APPROVED`. Các câu hỏi dưới đây không mở lại scope Debt + End-of-day; đây là những lựa chọn Product Owner chưa quyết định rõ và có ảnh hưởng trực tiếp đến behavior, security hoặc dữ liệu:

1. **Customer debt authorization:** Cashier có được xem customer outstanding debt và ghi nhận Customer Debt Payment, hay cả hai action phải Owner-only?
2. **Sensitive financial authorization:** Có chốt Supplier Debt Payment, End-of-day và Estimated Gross Profit là Owner-only không? Nếu End-of-day Owner-only, có cần cho Cashier xem một subset không chứa profit/debt tổng hợp không?
3. **Refund presentation trong Collected:** Headline `Collected` là net actual inflow sau khi trừ actual Return refunds, hay UI phải hiển thị gross collected và refunds như hai headline tách biệt? Technical proposal sẽ luôn trả component breakdown để không mất thông tin.
4. **Store timezone source:** Store timezone được chọn ở onboarding/settings, lấy từ deployment configuration hay dùng một policy khác? Dùng IANA hay Windows timezone ID, và backfill/default nào áp dụng cho Store hiện hữu? Không hard-code một timezone trước quyết định này.
5. **Debt payment note/reference:** MVP có cần optional note/reference text cho Customer/Supplier Debt Payment không? Nếu có, cần giới hạn độ dài và note có thuộc immutable audit record/fingerprint không?
6. **Correction sau unallocated debt payment:** Nếu Customer đã trả nợ ở cấp Customer rồi sau đó Return/Sale Void làm obligation giảm thấp hơn tổng tiền đã thu, hoặc Store đã trả nợ Supplier rồi Purchase Void làm supplier obligation giảm thấp hơn tổng đã trả, behavior nào được phép? D-046 cấm customer credit/supplier advance; D-047 không allocation theo invoice; không được clamp balance, xóa payment hoặc tự suy diễn allocation. Cần chốt một trong các business direction được review (ví dụ reject correction gây balance âm, hoặc bổ sung explicit actual-money refund/recovery flow với scope được approve).

Câu 6 là blocker domain cho việc phối hợp Slice 5 với correction hiện có. Các lựa chọn schema, query, EF mapping, SQL lock resource và index thông thường được giải quyết trong technical design; không phải Product Owner question.

Tài liệu đề xuất: [Technical Breakdown Slice 5 v0.1](docs/architecture/technical-breakdown-slice-5-v0.1.md) — `PROPOSED / PENDING PRODUCT OWNER APPROVAL`.

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
- Thiết bị/khổ giấy pilot và lựa chọn browser print, local print agent hay printer service.
- Rule window, threshold và điều kiện dữ liệu đủ tin cậy cho C14; value/willingness-to-pay vẫn cần kiểm chứng.
- Chi tiết cấu hình Identity/Auth + secure HttpOnly cookie; deployment topology, backup, monitoring, secrets và CI/CD. Authentication direction đã chốt tại Step 11.
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
