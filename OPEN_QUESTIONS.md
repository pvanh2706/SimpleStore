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

- Với experiment C14, rule/tín hiệu tối thiểu nào giúp chủ cửa hàng nhận biết việc đáng chú ý, và bằng chứng nào đủ để đánh giá value/willingness-to-pay?
- Những integration C19 và thiết bị C17 cụ thể nào thực sự cần cho vòng vận hành MVP?
- Integration point C7 cần chừa dữ liệu và ranh giới trách nhiệm nào để sau này gọi service API HĐĐT riêng của Product Owner? Không xây capability HĐĐT nội bộ và không mặc định phải kết nối service ngay trong MVP.

Phạm vi đã chốt được ghi tại [MVP Scope v0.1](docs/product/mvp-scope-v0.1.md).
