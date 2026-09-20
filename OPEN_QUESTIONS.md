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

- Cashier bị hạn chế cụ thể thế nào trong từng luồng sửa/hủy/điều chỉnh của MVP?
- Pilot thực tế có cần nhiều barcode cho một sản phẩm không (SHOULD có điều kiện tại C2)?
- Template import cố định cuối cùng cần những trường bắt buộc nào? Step 7 đã chốt validation bắt buộc, barcode trùng, kiểu số và báo dòng lỗi; không xây importer tổng quát.

Phạm vi functional scope tham chiếu: [MVP Functional Scope v0.1](docs/capabilities/mvp-functional-scope-v0.1.md).

### Sau Step 8 — các quyết định còn cần làm rõ

Step 8 đã APPROVED 6 user flows và các nguyên tắc identity/idempotency, timeout recovery, Completed bất biến, Purchase consistency, Return validation và Reprint. Các câu hỏi sau không mở lại các nguyên tắc đó:

- Có chốt áp dụng giới hạn chỉ Owner được Void transaction Completed trong pilot không? Step 8 cho phép giới hạn này; chưa suy diễn thành quyền Void cho Cashier.
- Import áp dụng chính sách nhận toàn bộ file hay từng phần? Dù chọn cách nào, không được có trạng thái import nửa vời mà người dùng không biết; phải báo rõ kết quả để xử lý an toàn.
- Khổ giấy/thiết bị pilot và ngưỡng/cửa sổ dữ liệu C14 vẫn cần làm rõ như các câu hỏi ở trên; ví dụ trong user flows không chốt các lựa chọn này.

Tài liệu: [MVP User Flows v0.1](docs/ux/mvp-user-flows-v0.1.md). Chưa chốt database schema, API contract, UI/wireframe chi tiết hoặc architecture implementation ở Step 8.

## Đã giải quyết tại Step 9 — APPROVED

### Phương pháp giá vốn — D-012 / Decision A

**Đã quyết định:** Moving Weighted Average — Bình quân gia quyền di động.

SaleLine snapshot giá vốn tại Sale Completed; không tính lại lịch sử từ CurrentCost. Return dùng cost basis SaleLine gốc; restock phục hồi quantity/value phù hợp. Purchase reversal đảo đúng quantity/value contribution của transaction gốc theo business semantics. Không đưa lot/FIFO/serial/expiry costing vào MVP.

### Payment / Debt behavior — D-013 / Decision B

**Đã quyết định:** Payment là tiền thực thu/thực trả, domain cho phép nhiều Payment liên quan một transaction. Khách trả nợ sau và cửa hàng trả nợ NCC sau đều phải ghi nhận Payment và giảm Outstanding Debt.

Customer/Supplier debt được suy ra và giải thích từ transaction, payment, returns/reversals/adjustments phù hợp; không sửa số nợ tùy ý. Customer chỉ cần đủ để nhận diện người đang nợ; không mở CRM hoặc generic accounting ledger.

## Ranh giới chưa chốt sau Step 9

- Permission chi tiết, giới hạn Void, chính sách import, thiết bị pilot và các câu hỏi kiểm chứng người dùng/C14 ở trên vẫn mở.
- Việc Sale Payment, Customer Debt Payment, Purchase Payment và Supplier Debt Payment dùng chung abstraction hay không được quyết định ở Architecture/Technical Design.
- Cách cache/materialize current stock hoặc outstanding balance chưa chốt; mọi lựa chọn phải giữ nguồn nghiệp vụ audit/explain được.
- Chưa thiết kế SQL/database schema, API contracts, EF Core entities, frontend model hoặc architecture implementation chi tiết.

Bước tiếp theo: **Step 10 — Architecture**. Tham chiếu: [Domain Model v0.1](docs/architecture/domain-model-v0.1.md). Tài liệu Step 1–8 giữ nguyên như lịch sử phê duyệt; hai quyết định trên là trạng thái hiện hành.
