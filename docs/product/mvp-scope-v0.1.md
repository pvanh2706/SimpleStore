# MVP Scope v0.1

- **Step:** 6 — MVP Scope
- **Trạng thái:** `APPROVED`
- **Ngày phê duyệt:** 2026-09-20
- **Người phê duyệt:** Product Owner

## MVP Definition

MVP phải đủ để một cửa hàng tạp hóa nhỏ có thể vận hành thật, không phải chỉ là demo CRUD.

End-to-end mục tiêu:

Khởi tạo cửa hàng → tạo/import hàng → nhập hàng → bán + thanh toán + in → tồn / tiền / giá vốn tự cập nhật → xử lý trả hoặc sửa một giao dịch → đối soát cuối ngày → chủ cửa hàng trả lời được “Hôm nay cửa hàng thế nào?” → biết ít nhất một việc đáng chú ý.

## MVP Boundary

- 1 cửa hàng.
- 1 kho chính.
- 1 chủ cửa hàng.
- Một số nhân viên bán hàng.
- Chưa hỗ trợ multi-branch/chuỗi cửa hàng đầy đủ.
- Chưa làm kế toán đầy đủ.

## Capability Scope

### CORE

| Mã | Capability |
| --- | --- |
| C1 | Sales & Checkout |
| C2 | Product & Pricing Management |
| C3 | Purchasing & Supplier Management |
| C4 | Inventory Operations |
| C8 | Transaction Integrity |
| C9 | Inventory Confidence |
| C10 | Financial & Profit Integrity |
| C15 | Store Setup & Data Onboarding |
| C17 | Device & Peripheral Integration |

### CORE-LITE

| Mã | Capability |
| --- | --- |
| C5 | Returns & Exception Handling |
| C6 | Money & Debt Management |
| C11 | Auditability & Safe Recovery |
| C16 | User & Permission Management |
| C18 | Operational Resilience |

### DIFFERENTIATOR-LITE / EXPERIMENT

| Mã | Capability | Phạm vi MVP |
| --- | --- | --- |
| C12 | Business Situation Understanding | Tối thiểu hỗ trợ outcome: “Hôm nay cửa hàng thế nào?” |
| C13 | Explainable Business Insights | Giải thích một số số liệu quan trọng bằng ngôn ngữ dễ hiểu. |
| C14 | Attention & Decision Support | Chỉ là experiment mỏng; ưu tiên rule-based/deterministic. Chưa cần AI. |

C14 được đưa vào MVP ở mức experiment, không đồng nghĩa nhu cầu, value hoặc willingness-to-pay đã được validated. Differentiator Hypothesis liên quan vẫn cần kiểm chứng.

### LIMITED / OUT OF BUILD SCOPE

| Mã | Capability | Ranh giới MVP |
| --- | --- | --- |
| C19 | External Integration | Chỉ các integration thực sự cần cho MVP. |
| C7 | E-Invoice & Tax Compliance | Chỉ chừa integration point để sau này gọi service API HĐĐT riêng của Product Owner. Không xây capability HĐĐT nội bộ trong MVP. |

Việc C7 có trong Capability Map và JTBD đã duyệt không tạo yêu cầu xây capability HĐĐT nội bộ. MVP cũng không yêu cầu triển khai kết nối service này ngay.

## Important Principles

- Không làm “mỗi capability một ít”; MVP phải tạo thành một vòng vận hành hoàn chỉnh.
- DO và TRUST phải đủ chắc trước khi đẩy mạnh UNDERSTAND & ACT.
- C8–C11 không phải backend nice-to-have; đây là nền tảng TRUST, kể cả khi C11 được giới hạn ở mức CORE-LITE.
- Không cần full offline trong MVP.
- Resilience bắt buộc phải xử lý timeout, double-submit, retry, lỗi thiết bị hoặc external API mà không tạo double sale hoặc dữ liệu nửa vời.
- Không cần AI recommendation engine trong MVP; ưu tiên rule engine/deterministic logic nếu phù hợp.

## Explicitly Out of MVP

- CRM đầy đủ.
- Loyalty phức tạp.
- Marketing automation.
- Website/e-commerce.
- Multi-branch đầy đủ.
- Kế toán đầy đủ.
- AI chatbot.
- Forecasting phức tạp.
- Dashboard BI lớn.
- Phân quyền enterprise.
- Full offline synchronization.

## Quan hệ với các quyết định đã duyệt

- Tài liệu này chốt phạm vi MVP ở Step 6, không thay đổi các quyết định Step 1–5 đã `APPROVED`.
- Capability Map mô tả các khả năng của sản phẩm; tài liệu này xác định mức độ đưa từng capability vào MVP.
- Phê duyệt phạm vi không có nghĩa MVP đã được triển khai hoặc C14 đã được validated.
- Không tự mở rộng phạm vi MVP khi làm rõ nghiệp vụ hoặc thiết kế tiếp theo.

## Liên quan

- [Capability Map v0.1](../capabilities/capability-map-v0.1.md)
- [Product Principles v0.1](product-principles-v0.1.md)
- [Product Model DO → TRUST → UNDERSTAND & ACT](product-model-do-trust-understand-act.md)
- [Decision Log](../../DECISIONS.md)
- [Project State](../../PROJECT_STATE.md)
- [Open Questions](../../OPEN_QUESTIONS.md)
