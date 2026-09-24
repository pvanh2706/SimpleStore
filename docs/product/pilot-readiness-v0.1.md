# Pilot Readiness v0.1

## Trạng thái

`APPROVED`

Product Owner đã approve phạm vi và gate Pilot Readiness tại D-077–D-084 ngày 2026-09-24. Approval này định nghĩa công việc cần hoàn tất để đạt M7; nó không xác nhận implementation đã bắt đầu, không đánh dấu `M7 — Pilot-ready`, và không có nghĩa pilot hoặc production release đã bắt đầu.

## 1. Mục tiêu M7

Đưa SimpleStore từ trạng thái MVP business implementation Slice 0–6 đã hoàn tất sang trạng thái có thể triển khai, vận hành, hỗ trợ và đánh giá an toàn trong pilot thực tế.

M7 tập trung vào:

- hoàn tất các MVP MUST còn thiếu trước pilot;
- giảm rủi ro deployment, account bootstrap, dữ liệu, backup/restore, support và printing;
- thiết lập release gate có evidence;
- chuẩn bị Store onboarding, support/contact và validation plan;
- giữ ranh giới rõ giữa readiness evidence và product-value validation.

## 2. Trạng thái hiện tại

- Slice 0–6 đã hoàn tất; Slice 6 là `APPROVED / COMPLETED — D-076`.
- Pilot Readiness decisions D-077–D-084 là `APPROVED`.
- Pilot Readiness implementation: `NOT STARTED`.
- M7 — Pilot-ready: `NOT ACHIEVED`.
- Pilot: `NOT STARTED`.
- Production readiness: `NOT DECLARED`.
- C14 product value và willingness-to-pay: `NOT VALIDATED`.

Các readiness blocker dưới đây không revoke approval hoặc completion của business implementation Slice 0–6.

## 3. Approved decisions D-077–D-084

### D-077 — Pilot deployment topology

Pilot đầu dùng topology đơn giản: Windows Server + IIS + ASP.NET Core backend + Vue SPA + SQL Server, với một deployable application và một database theo kiến trúc hiện tại. Có thể phục vụ một hoặc vài Store tenant theo isolation hiện có. Không yêu cầu Kubernetes, orchestration, HA, distributed services, multi-region hoặc autoscaling.

### D-078 — Production account provisioning

Phải có production-safe workflow để bootstrap Owner đầu tiên; Owner quản lý Cashier ở mức tạo, disable và reset/set lại thông tin đăng nhập. Production không dựa vào `DevelopmentOwnerSeeder` hoặc DB manipulation như normal workflow. Backend giữ authorization boundary và Store isolation; không mở enterprise IAM hoặc role designer.

### D-079 — Inventory pilot completeness

Hoàn tất C4 MVP MUST: reasoned Stock Adjustment, Stocktake và stocktake difference recording. Mọi thay đổi tạo immutable/explainable `InventoryMovement`, cập nhật `InventoryBalance` atomically, audit actor/time/reason, scope Store/Main Warehouse và bảo vệ Owner-only cho mutation nhạy cảm.

### D-080 — Backup / restore readiness

Có automated SQL Server backup, schedule, retention, storage, restore runbook và ít nhất một restore drill thành công. Sau restore phải chứng minh application start và đọc được dữ liệu; file backup chưa được restore-test không đủ evidence.

### D-081 — Observability and support baseline

Có persistent searchable logs, end-to-end `traceId` correlation, DB-aware health/readiness, log retention và support runbook cho log/trace/health/DB/deployed version/escalation/recovery. Không bắt buộc observability platform lớn nếu giải pháp đơn giản đáp ứng pilot.

### D-082 — Printer pilot certification

Chọn một khổ giấy và một đến hai target printer/model/configuration để test thực tế. Browser print là default strategy nếu đạt checklist về layout, tiếng Việt, monetary fields, Return/Void/reprint và failure recovery mà không làm mất Completed transaction.

### D-083 — Pilot release gate

Mỗi pilot release phải có backend restore/build/tests, frontend frozen install/build/tests, migration verification, critical real E2E, production-like deployment smoke, backup/restore check khi có schema/data risk, recorded version/SHA và rollback/recovery instruction. CI tiếp tục là automated gate; reliable local/release Playwright evidence được chấp nhận mà chưa bắt buộc GitHub Actions chạy real E2E.

### D-084 — Pilot validation plan

Validation tách riêng Core MVP operational validation và C14 experiment validation. Quantitative evidence phải kết hợp interview/observation; telemetry chỉ hỗ trợ và không tự chứng minh value hoặc recommendation success.

## 4. Current blockers

### PR-BLOCKER-01 — Production account provisioning

Production hiện chưa có approved implemented workflow bootstrap Owner và quản lý Cashier phù hợp D-078.

### PR-BLOCKER-02 — Inventory MUST gap

C4 MVP MUST còn thiếu Stock Adjustment, Stocktake và stocktake difference recording theo D-079.

### PR-BLOCKER-03 — Backup/restore

Chưa có documented automated backup, retention, restore runbook và restore-drill evidence theo D-080.

### PR-BLOCKER-04 — Production deployment runbook

Topology Windows Server/IIS đã được chọn tại D-077 nhưng production deployment, versioning, rollback và recovery procedure chưa hoàn thiện/evidence.

### PR-BLOCKER-05 — Observability/support

Ứng dụng có health/logging foundation nhưng chưa có đầy đủ persistent-log, DB-aware readiness, retention và pilot support runbook evidence theo D-081.

### PR-BLOCKER-06 — Printer certification

Browser print đã implement nhưng chưa chọn/certify target paper size và một đến hai real printer configurations theo D-082.

### PR-BLOCKER-07 — Release/pilot operational checklist

CI và real E2E evidence hiện có nhưng chưa có formal pilot release checklist, production-like deployment smoke và rollback/recovery gate theo D-083.

## 5. Pilot readiness gates

Các gate này chỉ được đóng khi có implementation và evidence review được; decision approval không tự đánh dấu gate complete.

1. Account provisioning gate — production-safe Owner bootstrap và Cashier lifecycle hoạt động, authorization/Store isolation verified.
2. Inventory completeness gate — Stock Adjustment + Stocktake MVP complete với atomic balance, immutable movement và audit evidence.
3. Deployment gate — documented Windows Server/IIS procedure, config/secrets handling, version identification, smoke, rollback và recovery được verify.
4. Data protection gate — automated backup/schedule/retention/storage và successful restore drill có application read verification.
5. Supportability gate — persistent searchable logs, trace correlation, DB-aware health/readiness, retention và support runbook được verify.
6. Printer gate — target paper/printer certification checklist pass trên thiết bị thực.
7. Release gate — D-083 checklist pass cho candidate release và exact version/commit SHA được ghi nhận.
8. Pilot operations gate — Store onboarding/data preparation plan cùng support/contact/escalation process sẵn sàng.
9. Validation gate — Core MVP và C14 validation plan, evidence collection và interview/observation method sẵn sàng.

## 6. Definition of Pilot Ready

M7 chỉ được đề xuất `Pilot-ready` khi toàn bộ điều kiện dưới đây có evidence và được review. Trạng thái hiện tại của mọi item là `NOT COMPLETE / EVIDENCE PENDING`:

- [ ] Production account provisioning hoạt động.
- [ ] C4 Stock Adjustment + Stocktake MVP hoàn tất.
- [ ] Deployment runbook hoàn tất.
- [ ] Production-like deployment smoke pass.
- [ ] SQL Server backup automated và restore drill pass.
- [ ] Persistent logs và support runbook sẵn sàng.
- [ ] Health/readiness phản ánh kết nối DB.
- [ ] Target printer/paper certification pass.
- [ ] Current migrations được verify trên deployment/upgrade path phù hợp.
- [ ] Critical release regression và real E2E pass.
- [ ] Pilot Store onboarding/data preparation plan sẵn sàng.
- [ ] Pilot support/contact/escalation process sẵn sàng.
- [ ] Pilot validation plan sẵn sàng.

Không được đổi checkbox chỉ dựa trên planned work hoặc document approval; mỗi item cần evidence tương ứng.

## 7. Pilot validation plan

### A. Core MVP operational validation

Đánh giá bằng usage evidence kết hợp observation/interview:

- onboarding và chuẩn bị dữ liệu Store có thực hiện được;
- Product/import đủ dùng;
- Sale nhanh và đáng tin;
- printer hoạt động trên target setup;
- Purchase, inventory, debt, Return, Void và EOD hỗ trợ vận hành thật;
- recovery/support xử lý được sự cố pilot;
- dữ liệu và explainability tạo được niềm tin.

### B. C14 experiment validation

Đánh giá riêng:

- Owner có phát hiện signal;
- signal có nói điều Owner chưa biết;
- Owner có tin evidence;
- signal có ảnh hưởng quyết định nhập hàng;
- Owner có tiếp tục sử dụng;
- willingness-to-pay có tồn tại.

`TodayOpened`, `SignalShown`, `WhyOpened`, `PurchaseDraftStarted` chỉ là quantitative supporting evidence. Không suy diễn `click == value validated` hoặc `PurchaseDraftStarted == recommendation succeeded`; kết luận phải kết hợp interview/observation.

## 8. State distinctions

| State | Ý nghĩa | Trạng thái hiện tại |
|---|---|---|
| MVP implementation completed | Business implementation Slice 0–6 đã qua approval gate; không đồng nghĩa operational readiness. | Có — Slice 6 đóng tại D-076. |
| Pilot Ready | Toàn bộ Definition of Pilot Ready có reviewed evidence và M7 được explicit xác nhận. | Chưa. |
| Pilot started | Một pilot Store thực tế đã onboarding và bắt đầu sử dụng theo support/validation plan. | Chưa. |
| Pilot validated | Pilot evidence đủ để kết luận riêng về core operational fit và các hypothesis được đánh giá. | Chưa. |
| Production ready | Deployment, security, operations, data protection, support và release criteria cho production được explicit review/approve. | Chưa được tuyên bố. |

`Pilot Ready`, `Pilot started`, `Pilot validated` và `Production ready` là các gate khác nhau; không trạng thái nào được suy ra tự động từ trạng thái trước đó.

## 9. Non-goals

Pilot Readiness A–H không authorize:

- Kubernetes, container orchestration, HA cluster, distributed services, multi-region hoặc autoscaling platform;
- enterprise IAM, social/OAuth login, large invitation platform hoặc permission designer;
- warehouse management system, multi-warehouse stocktake, batch/lot/serial hoặc complex inventory approval;
- in-app backup product/framework;
- mandatory ELK/Grafana/Application Insights/Sentry/distributed tracing platform;
- local print agent/printer service khi browser print đạt certification;
- full continuous deployment;
- AI/ML, forecasting, replenishment engine, Supplier/quantity recommendation hoặc auto-order;
- tuyên bố C14 value/willingness-to-pay đã validated trước pilot evidence.

## 10. Next step

Tạo Technical Breakdown cho Pilot Readiness, review các implementation stage/evidence plan và nhận Product Owner approval trước khi bắt đầu implementation. Không tự tạo Technical Breakdown approval decision trong tài liệu này.
