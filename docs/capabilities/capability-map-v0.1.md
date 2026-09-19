# Capability Map v0.1

- **Trạng thái:** `APPROVED`
- **Ngày phê duyệt:** 2026-09-20
- **Người phê duyệt:** Product Owner

## Capability Map

```text
FOUNDATION (C15–C19) hỗ trợ xuyên suốt

DO (C1–C7) → TRUST (C8–C11) → UNDERSTAND & ACT (C12–C14)
```

### A. DO — Vận hành cửa hàng

- **C1 — Sales & Checkout**
- **C2 — Product & Pricing Management**
- **C3 — Purchasing & Supplier Management**
- **C4 — Inventory Operations**
- **C5 — Returns & Exception Handling**
- **C6 — Money & Debt Management**
- **C7 — E-Invoice & Tax Compliance**

### B. TRUST — Làm dữ liệu đáng tin

- **C8 — Transaction Integrity**
- **C9 — Inventory Confidence**
- **C10 — Financial & Profit Integrity**
- **C11 — Auditability & Safe Recovery**

Nguyên tắc quan trọng: Một nghiệp vụ phải cập nhật nhất quán các hệ quả liên quan như tồn kho, tiền, công nợ, giá vốn và lợi nhuận. Không để từng module có “sự thật riêng”.

### C. UNDERSTAND & ACT — Hiểu và hành động

- **C12 — Business Situation Understanding**
- **C13 — Explainable Business Insights**
- **C14 — Attention & Decision Support**

C14 liên quan Differentiator Hypothesis “Cho tôi biết điều gì cần chú ý và nên làm gì tiếp theo”.

C14 được phê duyệt là một phần của Capability Map. Nhu cầu và willingness-to-pay cho outcome này vẫn chưa được xác nhận; hypothesis chưa được validated.

### D. FOUNDATION — Nền móng

- **C15 — Store Setup & Data Onboarding**
- **C16 — User & Permission Management**
- **C17 — Device & Peripheral Integration**
- **C18 — Operational Resilience**
- **C19 — External Integration**

## Nguyên tắc kèm theo Capability Map

- Capability không đồng nghĩa Feature hoặc màn hình.
- Capability Map trả lời: “SimpleStore cần có khả năng gì?”
- Capability Map chưa quyết định capability nào thuộc MVP.
- MVP Scope sẽ được quyết định ở bước sau.
- Không tổ chức Capability Map đơn giản thành POS / Kho / CRM / Báo cáo / Kế toán / AI.
- AI không phải capability cấp cao. AI chỉ là một possible implementation mechanism cho các capability như Explainable Business Insights hoặc Attention & Decision Support.
- Nếu rule engine hoặc cách khác phù hợp hơn AI thì có thể sử dụng cách đó.

## Liên quan

- [`docs/product/product-model-do-trust-understand-act.md`](../product/product-model-do-trust-understand-act.md)
- [`docs/product/product-principles-v0.1.md`](../product/product-principles-v0.1.md)
- [`docs/product/jobs-to-be-done-v0.1.md`](../product/jobs-to-be-done-v0.1.md)
