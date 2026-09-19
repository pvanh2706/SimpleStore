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
