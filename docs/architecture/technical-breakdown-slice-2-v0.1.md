# Technical Breakdown — Slice 2 v0.1

- **Slice:** 2 — Purchase → Inventory
- **Trạng thái:** `APPROVED / IMPLEMENTED`
- **Ngày Product Owner approval:** 2026-09-21
- **Business decisions:** D-018–D-021 giữ nguyên `APPROVED`.
- **Verification:** Implementation đã qua các vòng review và hardening; CI backend/frontend pass tại commit `f68c8a111a5538be8e51cf8ff0e323e7ce2f41c0`.

## Outcome và ranh giới

Owner quản lý Supplier tối thiểu, lập/sửa Purchase Draft và Complete Purchase. Completion cập nhật PurchasePayment, InventoryMovement, InventoryBalance, Moving Weighted Average, reference purchase cost và supplier outstanding trong một SQL transaction.

Không thuộc Slice 2: Sale, Customer, Return, Purchase Void/Reverse, trả nợ NCC sau purchase, generic accounting ledger, multi-warehouse/multi-branch hoặc C14.

## Domain behavior

- Supplier store-scoped, không hard-delete; deactivate thay delete.
- Purchase lifecycle chỉ `Draft → Completed`; Completed immutable.
- Draft giữ Supplier và lines; chưa tạo actual PurchasePayment.
- Một Product chỉ xuất hiện một lần trong Purchase.
- Quantity `decimal(18,3)`, UnitPrice/Money `decimal(18,2)`, AverageCost `decimal(18,4)`.
- `LineAmount = Round(Quantity × UnitPrice, 2, AwayFromZero)`; total là tổng LineAmount.
- Payment chỉ được tạo khi Complete, amount > 0, method Cash/Transfer và tổng payment không vượt total.
- Outstanding được suy ra từ Completed Purchase total trừ actual payments.

## Completion transaction

`CompletePurchase` chạy trong một local SQL transaction:

1. lấy StoreId từ authenticated user;
2. khóa OperationId bằng transaction-owned SQL Server application lock;
3. kiểm tra fingerprint/idempotency result;
4. load Draft và validate Supplier/Product cùng Store, còn active;
5. khóa từng InventoryBalance bằng `UPDLOCK, HOLDLOCK` theo ProductId tăng dần;
6. tính total/payment authoritative;
7. tạo PurchasePayment và InventoryMovement nguồn `PurchaseLine`;
8. cập nhật balance và Product.ReferencePurchaseCost;
9. chuyển Purchase sang Completed;
10. lưu BusinessOperation Completed cùng transaction;
11. commit hoặc rollback toàn bộ.

Moving Weighted Average:

```text
Q1 = Q0 + Qp
V1 = V0 + LineAmount
AverageCost1 = Round(V1 / Q1, 4, AwayFromZero)
```

Không có constraint `QuantityOnHand >= 0`; Slice 3 vẫn có thể triển khai negative-stock policy đã duyệt.

## Idempotency và timeout recovery

Client tạo GUID OperationId. Backend fingerprint normalized PurchaseId và payment entries bằng SHA-256.

- ID mới: xử lý và commit business result + BusinessOperation atomically.
- Cùng ID/fingerprint đã Completed: trả Purchase cũ, không nhân đôi stock/payment/debt.
- Cùng ID nhưng fingerprint khác: `409 idempotency-key-reused`.
- `GET /api/operations/{operationId}` store-scoped trả status/type/result reference; unknown trả 404 typed ProblemDetails.

Vue snapshot bất biến OperationId + payments ngay khi gửi. Khi network/timeout hoặc `409 operation-lock-timeout` cho kết quả mơ hồ, UI khóa payments, query operation status và chỉ cho retry đúng cùng ID/payload. Nếu status là Completed sau lost response, UI tải lại Purchase đã commit. Lỗi 4xx xác định (bao gồm `idempotency-key-reused`, validation errors và `purchase-already-completed`) được hiển thị trực tiếp và không được suy diễn thành success từ operation status.

## Persistence constraints

- Supplier và Purchase có composite principal key `(StoreId, Id)` khi cần.
- Purchase `(StoreId, SupplierId)` tham chiếu Supplier cùng Store.
- PurchaseLine `(StoreId, PurchaseId)` và `(StoreId, ProductId)` bảo vệ cùng Store.
- unique `(PurchaseId, ProductId)`.
- PurchasePayment `(StoreId, PurchaseId)` bảo vệ đúng Purchase/Store.
- audit user FKs dùng `Restrict`.
- check constraints cho quantity, unit price, line amount, total và payment amount.
- BusinessOperation dùng OperationId unique/primary key.

Migration nằm trong Infrastructure; production không tự migrate khi startup.

## HTTP/UI boundary

- Owner-only Supplier create/update/deactivate/list/detail.
- Owner-only Purchase create/update/list/detail/complete.
- Owner-only operation status.
- API không nhận StoreId để scope; backend resolve từ authenticated user.
- Vue có Supplier list/editor và Purchase list phân trang phía server (`pageSize=20`). Filter/search reset về trang 1 và được giữ khi đổi trang.
- Purchase Draft editor tìm Supplier theo tên/điện thoại và Product theo tên/SKU/barcode qua API search phân trang; không tải toàn bộ catalog. Reference hiện có của Draft được giữ riêng để edit vẫn hiển thị đúng ngoài page tìm kiếm hiện tại.
- Vue có completion payment review và completed detail với recovery semantics như phần idempotency ở trên.

## Verification direction

- Domain tests cho lifecycle, validation, rounding, totals, payment/debt và immutability.
- SQL Server integration tests cho authorization, Store isolation/constraints, atomic completion/rollback, costing, payment/debt, idempotency và concurrent completion.
- Vitest/Vue Test Utils cho duplicate line, preview totals/payment/outstanding, owner navigation, immutable UI và OperationId retry.
- Local Playwright chạy Vue → API → SQL Server cho critical Slice 2 flow; SQL Server integration tests tiếp tục là primary CI database verification.

## Quyết định liên quan

- D-012 — Moving Weighted Average.
- D-013 — Payment & Debt Model.
- D-014 — transaction/idempotency, inventory ledger/concurrency.
- D-016 — Store tenancy foundation.
- D-018–D-021 — `APPROVED`: Slice 2 lifecycle, precision, unique Product line và Owner-only permission.
