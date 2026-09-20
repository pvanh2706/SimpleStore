# Project State

## Giai đoạn hiện tại

**Product Discovery**

## Primary Persona

Chủ cửa hàng tạp hóa nhỏ tại Việt Nam, trực tiếp tham gia vận hành và chịu trách nhiệm ít nhất cho bán hàng, nhập hàng/tồn kho và kết quả kinh doanh — `APPROVED`.

## Phạm vi hiện tại

- Tìm hiểu người dùng, bối cảnh vận hành, vấn đề và nhu cầu.
- Ghi nhận câu hỏi mở, giả thuyết và bằng chứng nghiên cứu.
- Chuẩn bị cơ sở để Product Owner xem xét các quyết định sản phẩm.

## Tiến độ

### Step 1 — Hoàn thành

- Product Vision v0.1 — `APPROVED`.
- Problem Definition v0.1 — `APPROVED`.

### Step 2 — Hoàn thành

- Primary Persona v0.1 — `APPROVED`.
- Jobs To Be Done v0.1 — `APPROVED`.
- Differentiator Hypothesis “Cho tôi biết điều gì cần chú ý và nên làm gì tiếp theo” — ưu tiên cao, chưa được phê duyệt là JTBD.
- Cơ sở: [`docs/research/step-2-market-user-research.md`](docs/research/step-2-market-user-research.md).

### Step 3 — Hoàn thành

- Current User Journey v0.1 — `APPROVED`.
- Pain Point Map v0.1 — `APPROVED`.
- Product Model DO → TRUST → UNDERSTAND & ACT — `APPROVED`.
- Nguyên tắc: Nếu DO và TRUST chưa tốt thì UNDERSTAND & ACT không đáng tin.

### Step 4 — Hoàn thành

- Product Principles v0.1 gồm 6 principles — `APPROVED`.
- Product Principle Evaluation Checklist được lưu cùng tài liệu để hỗ trợ review feature.

### Step 5 — Hoàn thành

- Capability Map v0.1 gồm 19 capability thuộc DO, TRUST, UNDERSTAND & ACT và FOUNDATION — `APPROVED`.
- Capability Map chưa quyết định capability nào thuộc MVP.
- C14 — Attention & Decision Support đã được phê duyệt là capability; value và willingness-to-pay liên quan vẫn cần được kiểm chứng.

### Step 6 — Hoàn thành

- MVP Scope v0.1 — `APPROVED`.
- MVP phục vụ vận hành thật cho 1 cửa hàng, 1 kho chính, 1 chủ cửa hàng và một số nhân viên bán hàng.
- Vòng end-to-end: khởi tạo → tạo/import hàng → nhập hàng → bán + thanh toán + in → tồn/tiền/giá vốn tự cập nhật → trả hoặc sửa giao dịch → đối soát cuối ngày → hiểu tình hình hôm nay → biết ít nhất một việc đáng chú ý.
- CORE: C1, C2, C3, C4, C8, C9, C10, C15, C17.
- CORE-LITE: C5, C6, C11, C16, C18.
- DIFFERENTIATOR-LITE / EXPERIMENT: C12, C13, C14; C14 chỉ là experiment mỏng, ưu tiên deterministic/rule-based, nhu cầu và willingness-to-pay chưa validated.
- C19 chỉ gồm integration thực sự cần cho MVP. C7 chỉ chừa integration point cho service API HĐĐT riêng của Product Owner; không xây capability HĐĐT nội bộ.
- DO và TRUST phải đủ chắc; resilience phải ngăn double sale và dữ liệu nửa vời khi timeout, double-submit, retry hoặc lỗi thiết bị/external API. Không yêu cầu full offline.
- Tài liệu: [`docs/product/mvp-scope-v0.1.md`](docs/product/mvp-scope-v0.1.md).
- Các nội dung Step 1–5 ở trên ghi nhận phạm vi phê duyệt tại từng bước và được giữ nguyên; phạm vi MVP hiện tại được chốt tại Step 6.

### Step 7 — Hoàn thành

- Capability Decomposition / Functional Scope v0.1 — `APPROVED`.
- Functional scope theo capability, giữ rõ MUST/SHOULD, phạm vi tối thiểu, experiment và integration boundary.
- Mỗi feature trace tới Capability → JTBD / Pain Point → MVP Outcome; feature không trace được mặc định không đưa vào MVP tới khi chứng minh được lý do.
- 6 vertical slices: Setup + Product; Purchase → Inventory; Sale → Payment → Print; Return / Void → dữ liệu vẫn đúng; End-of-day → tiền + tồn + lãi gộp; “Hôm nay cửa hàng thế nào?” + 1 attention experiment.
- C14 chỉ thử nghiệm nguy cơ sắp hết hàng từ tồn hiện tại và tốc độ bán gần đây; nhu cầu/value/willingness-to-pay chưa validated.
- C7 không build: chỉ giữ domain sale đủ sạch cho việc ánh xạ sang service API HĐĐT hiện có sau này. C19 chỉ integration cần cho vòng MVP.
- Chưa thiết kế database schema, API contract, UI chi tiết hoặc architecture implementation; không thêm feature.
- Nội dung Step 1–6 đã APPROVED được giữ nguyên.
- Tài liệu: [`docs/capabilities/mvp-functional-scope-v0.1.md`](docs/capabilities/mvp-functional-scope-v0.1.md).

### Step 8 — Hoàn thành

- MVP User Flows v0.1 — `APPROVED`.
- 6 flows: Setup + Product; Purchase → Inventory; Sale → Payment → Print; Return / Void; End-of-day Reconciliation; “Hôm nay cửa hàng thế nào?”.
- Mỗi critical flow có Happy Path, Failure Path và Recovery Path; mô tả User Action → System Behavior → Business Outcome.
- Transaction Completed bất biến; correction có audit. Complete Sale, Complete Purchase, Create Return, Void Transaction cần identity riêng và idempotency.
- Timeout kiểm tra operation cũ: Completed trả kết quả cũ; Failed retry an toàn; Processing/Unknown không tùy tiện tạo operation mới.
- Purchase chỉ Completed khi tồn/giá vốn/công nợ NCC nhất quán. Return không vượt số còn được trả, restock phải do người dùng chọn.
- Printing là hậu xử lý: print lỗi không rollback sale Completed, phải cho Reprint.
- Doanh thu khác tiền thu; C14 chỉ signal nguy cơ sắp hết hàng có evidence, chưa validated value/willingness-to-pay.
- Giữ nguyên Step 1–7; không thêm feature hoặc thiết kế database/API/UI/architecture.
- Tài liệu: [`docs/ux/mvp-user-flows-v0.1.md`](docs/ux/mvp-user-flows-v0.1.md).

### Bước tiếp theo

**Step 9 — Domain Model**.

## Chưa thuộc phạm vi

- Phát triển frontend.
- Phát triển backend.
- Thiết kế hoặc triển khai database.
- Viết source code ứng dụng.

## Cập nhật gần nhất

2026-09-20 — Hoàn thành Step 8: MVP User Flows v0.1 được Product Owner phê duyệt; bước tiếp theo là Step 9 — Domain Model. Step 1–7 giữ nguyên.
