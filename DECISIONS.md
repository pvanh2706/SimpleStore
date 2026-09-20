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
