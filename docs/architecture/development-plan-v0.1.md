# Development Plan v0.1

- **Step:** 11 — Development Plan / Technical Design Breakdown v0.1
- **Trạng thái:** `APPROVED`
- **Người phê duyệt:** Product Owner

## Mục tiêu

Ghi nhận kế hoạch phát triển MVP theo vertical slice và technical breakdown đủ để bắt đầu code, dựa trên Step 1–10 đã APPROVED.

Step này được phép mô tả:

- implementation phases / vertical slices;
- engineering foundation;
- solution/repo structure;
- stack/version direction;
- testing direction;
- authentication direction;
- migrations direction;
- technical breakdown cho Slice 1 — Setup + Product;
- tenancy foundation;
- import policy.

Không tự mở rộng functional scope đã duyệt.

---

## Development Strategy — APPROVED

Không build theo module completeness kiểu:

Product hết → Inventory hết → Sales hết → Reporting hết.

Phải build theo vertical slice end-to-end.

Thứ tự:

1. Foundation
2. Setup + Product
3. Purchase → Inventory
4. Sale → Payment → Print
5. Return / Void
6. Debt + End-of-day
7. Understand & Act
8. Pilot

Mỗi slice phải tạo outcome chạy thật qua:

Vue → API → DB.

---

## Phase 0 — Engineering Foundation

Tạo nền tảng tối thiểu:

- .NET solution;
- Vue 3 app;
- cấu trúc Api / Application / Domain / Infrastructure;
- SQL Server + EF Core migrations;
- authentication cơ bản;
- global error handling;
- logging;
- configuration/secrets;
- test projects;
- CI build tối thiểu.

Mục tiêu:

Source có thể build, test và deploy trước khi có nhiều nghiệp vụ.

Không xây framework nội bộ lớn hoặc abstraction chưa có nhu cầu.

---

## Slice 1 — Setup + Product

Outcome:

Owner tạo được Store, Main Warehouse, sản phẩm, import danh mục/tồn đầu và xem được tồn có nguồn.

Domain/technical concepts cần implement:

- Store
- Warehouse
- User
- Product
- InventoryMovement
- InventoryBalance

---

## Slice 2 — Purchase → Inventory

Thêm:

- Supplier
- Purchase
- PurchaseLine
- Payment
- Moving Weighted Average
- Supplier Debt

Outcome:

Purchase Completed cập nhật nhất quán:

- tồn;
- inventory value;
- average cost;
- công nợ NCC.

---

## Slice 3 — Sale → Payment → Print

Thêm:

- Sale
- SaleLine
- Payment
- OperationId / Idempotency
- Negative Stock policy
- Receipt / print flow

Outcome:

Cashier có thể bán hàng thật.

Phải test:

- double click;
- timeout after commit;
- retry;
- concurrent sale;
- negative stock;
- printer failure.

Milestone:

SimpleStore có thể demo như POS thật.

---

## Slice 4 — Return / Void / Recovery

Thêm:

- Return
- ReturnLine
- Restock / No Restock
- Void
- Reversal
- Audit

Phải test:

- partial return;
- multiple returns;
- return quá số lượng;
- retry return;
- void authorization;
- purchase void limitation.

---

## Slice 5 — Debt + End-of-day

Hoàn thiện:

- Customer
- Customer debt payment
- Supplier debt payment
- End-of-day query
- Revenue vs Collected
- Gross Profit

Outcome:

Owner trả lời được:

- hôm nay bán bao nhiêu;
- đã thu bao nhiêu;
- khách còn nợ bao nhiêu;
- NCC còn nợ bao nhiêu;
- lãi gộp khoảng bao nhiêu.

---

## Slice 6 — Understand & Act

Thêm:

- “Hôm nay cửa hàng thế nào?”
- C14 experiment nguy cơ sắp hết hàng.

Không AI.

Không dashboard lớn.

Không generic rule engine framework.

Outcome:

Kiểm chứng giá trị khác biệt so với POS thông thường.

---

## Definition of Done cho mỗi Slice — APPROVED

Một slice chỉ Done khi:

- happy path chạy end-to-end;
- failure/recovery path quan trọng chạy được;
- authorization đúng;
- transaction consistency đúng;
- automated tests cho business rules quan trọng;
- Vue → API → DB chạy thật;
- log/error đủ để debug;
- không phá domain invariant/architecture decision đã APPROVED.

Không chấp nhận “backend xong nhưng frontend chưa dùng được” là hoàn thành slice.

---

## Testing Strategy — APPROVED

Ba tầng:

- Domain Unit Tests
- Application / Integration Tests
- một số End-to-End critical flows

Không đặt mục tiêu 100% coverage.

Ưu tiên test:

- Moving Weighted Average;
- InventoryBalance;
- idempotency;
- negative stock;
- Return;
- debt;
- concurrent Sale/Purchase.

---

## Milestones — APPROVED

- M0 — Skeleton deploy được
- M1 — Có dữ liệu hàng & tồn
- M2 — Nhập hàng đúng cost/tồn/nợ
- M3 — Bán hàng thật được
- M4 — Return/Void an toàn
- M5 — Cuối ngày hiểu được tiền/lãi/nợ
- M6 — Differentiator experiment hoạt động
- M7 — Pilot-ready

Không estimate ngày cứng tại Step 11.

Estimate sau khi từng slice có technical breakdown đủ rõ.

---

## Technical Design Principle — APPROVED

Technical Design làm just enough, per vertical slice.

Trước mỗi slice:

Domain behavior
→ DB changes
→ API contract
→ UI flow
→ Test cases
→ Implement

Không thiết kế toàn bộ SQL/API/frontend cho tất cả slice trước khi code.

---

## Trạng thái tiếp theo

**Ready to begin implementation — Slice 0 Engineering Foundation**

Sau đó: **Slice 1 — Setup + Product**.

Step 11 phê duyệt kế hoạch, không có nghĩa source đã được triển khai hoặc các milestone đã đạt. Phase 0 và Slice 0 cùng chỉ Engineering Foundation. Các yêu cầu integrity/idempotency của Step 8–10 áp dụng ngay khi operation tương ứng được triển khai, không chờ tới Slice 3.

## Tài liệu liên quan

- [Technical Breakdown Slice 0–1](technical-breakdown-slice-0-1-v0.1.md)
- [Architecture v0.1](architecture-v0.1.md)
- [Domain Model v0.1](domain-model-v0.1.md)
- [MVP Functional Scope v0.1](../capabilities/mvp-functional-scope-v0.1.md)
- [MVP User Flows v0.1](../ux/mvp-user-flows-v0.1.md)

Giữ nguyên Step 1–10 đã APPROVED; không triển khai code trong lần cập nhật tài liệu này.
