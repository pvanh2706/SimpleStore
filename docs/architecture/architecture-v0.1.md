# Architecture v0.1

- **Step:** 10
- **Trạng thái:** `APPROVED`
- **Ngày phê duyệt:** 2026-09-21
- **Người phê duyệt:** Product Owner
- **Bước tiếp theo:** Step 11 — Development Plan / Technical Design Breakdown

## 1. Mục tiêu và ranh giới

Tài liệu này ghi nhận kiến trúc cấp cao cho MVP SimpleStore dựa trên các quyết định Step 1–9 đã `APPROVED`.

Kiến trúc phải hỗ trợ vòng vận hành của một cửa hàng, ưu tiên tính nhất quán và khả năng giải thích của dữ liệu, đồng thời giữ deployment đủ đơn giản cho MVP.

Step 10 quyết định:

- high-level system architecture;
- backend structure và module boundaries;
- transaction boundary và idempotency strategy;
- inventory ledger, materialized balance, costing và concurrency behavior;
- negative stock behavior;
- reporting/read model direction;
- printing và HĐĐT integration boundaries;
- authentication/authorization direction;
- deployment direction.

Step 10 chưa thiết kế:

- SQL schema đầy đủ;
- API contracts cụ thể;
- EF Core entity mapping chi tiết;
- frontend component tree hoặc wireframe/UI chi tiết;
- cơ chế locking/isolation/concurrency cụ thể;
- production infrastructure phức tạp.

## 2. Architecture style

MVP dùng **Modular Monolith**, không dùng microservices.

```text
Vue 3 SPA
    ↓ HTTPS/JSON
ASP.NET Core Backend
    ↓
SQL Server
```

SimpleStore có một backend deployable và một database. Code được chia boundary rõ theo domain/module, nhưng mỗi module không phải một microservice và không có distributed transaction giữa các module trong MVP.

Các module conceptual có thể gồm:

- Catalog;
- Purchasing;
- Sales;
- Inventory;
- Payments;
- Customers;
- Suppliers;
- Reporting;
- Identity.

Module boundary dùng để giữ business concepts và ownership rõ ràng, không phải để tạo thêm deployment unit hoặc distributed infrastructure.

## 3. Backend structure

Backend định hướng gần Clean Architecture nhưng tránh tạo layer hoặc abstraction chỉ vì hình thức:

```text
SimpleStore.Api
SimpleStore.Application
SimpleStore.Domain
SimpleStore.Infrastructure
```

### SimpleStore.Domain

- Chứa domain concepts, invariants và business rules đã được xác định ở Step 9.
- Không phụ thuộc EF Core, HTTP/API hoặc Vue.
- Không chứa integration detail của máy in hay HĐĐT.

### SimpleStore.Application

Chứa và điều phối các application use case, ví dụ:

- `CompleteSale`;
- `CompletePurchase`;
- `CreateReturn`;
- `VoidTransaction`;
- `RecordCustomerPayment`;
- `RecordSupplierPayment`;
- `GetTodayStoreSituation`.

Application layer điều phối transaction, authorization cần thiết, domain behavior và persistence boundary; không biến thành nơi chứa UI behavior.

### SimpleStore.Infrastructure

- EF Core và SQL Server persistence;
- implementation của repository/persistence boundary khi cần;
- integration adapters;
- external HĐĐT adapter nếu được bổ sung sau MVP core;
- implementation kỹ thuật cho các external side effect phù hợp.

### SimpleStore.Api

- HTTP boundary giữa Vue và application use cases;
- nhận request, authentication context và operation identity;
- trả response phù hợp;
- không thay thế backend business rules bằng validation ở frontend.

Dependency direction cụ thể sẽ được làm rõ ở Step 11; nguyên tắc hiện tại là Domain không phụ thuộc Infrastructure/API/Vue.

## 4. Persistence direction

MVP dùng:

- SQL Server;
- EF Core.

Không cần trong MVP:

- MongoDB;
- Redis bắt buộc;
- event store;
- Elasticsearch;
- message broker cluster;
- CQRS infrastructure lớn;
- data warehouse riêng.

Một relational database và local database transaction phù hợp với các business operation cần consistency mạnh của SimpleStore.

## 5. Critical transaction boundary

Critical business operation nên nằm trong **một local SQL database transaction** khi có thể.

Ví dụ `CompleteSale` về mặt conceptual:

```text
BEGIN TRANSACTION
  validate operation identity
  create Sale
  create SaleLines
  create Payments
  create InventoryMovements
  update InventoryBalance
  persist operation result
COMMIT
```

Nếu operation thất bại trước commit thì toàn bộ thay đổi phải rollback. Không được báo Sale thành công khi inventory, payment hoặc cost-related state liên quan đang ở trạng thái cập nhật một phần.

Transaction boundary này áp dụng cho business state trong SQL Server. External side effects như in phiếu hoặc gọi HĐĐT không nằm trong core Sale transaction.

## 6. Decision A — Transaction và idempotency strategy

Critical operation dùng **client-generated `OperationId` / `IdempotencyKey`**.

Áp dụng ít nhất cho:

- `CompleteSale`;
- `CompletePurchase`;
- `CreateReturn`;
- `VoidTransaction`;
- `RecordDebtPayment`.

Một operation record về mặt conceptual có thể giữ:

- `OperationId`;
- `OperationType`;
- `RequestFingerprint`;
- `Status`;
- `ResultReference`;
- `CreatedAt`;
- `CompletedAt`.

Đây không phải SQL schema được phê duyệt; danh sách chỉ mô tả thông tin kiến trúc cần có để thực hiện behavior.

### Retry behavior

- Cùng `OperationId` đã `Completed`: trả lại kết quả cũ, không tạo business transaction mới.
- `OperationId` mới: xử lý operation mới.
- Cùng `OperationId` nhưng request identity/payload khác: từ chối vì không phải retry hợp lệ.
- Business intention thay đổi sau validation failure: client dùng `OperationId` mới.

Business changes và operation result phải commit **atomically trong cùng SQL transaction**. Không được commit business data trước rồi mới lưu trạng thái idempotency.

### Timeout after commit

```text
DB commit thành công
→ response bị mất hoặc timeout
→ client retry cùng OperationId
→ backend nhận ra Completed
→ trả lại kết quả cũ
```

Disable button ở frontend chỉ là UX protection. Backend là idempotency boundary.

## 7. Decision B — Inventory ledger và materialized balance

### InventoryMovement

`InventoryMovement` là immutable/explainable ledger. Mọi thay đổi tồn phải có movement và source có thể truy vết.

### InventoryBalance

`InventoryBalance` là materialized operational state để đọc và cập nhật tồn hiện tại hiệu quả. Về mặt conceptual, balance giữ:

- Product;
- Warehouse;
- QuantityOnHand;
- InventoryValue;
- AverageCost.

`InventoryMovement` là nguồn giải thích/audit. `InventoryBalance` là fast current state. Hai phần phải được cập nhật atomically trong cùng transaction.

Việc reconciliation hoặc rebuild balance nếu cần sẽ được thiết kế sau; Step 10 chưa chốt job, table hoặc API cho việc đó.

### Moving Weighted Average

Costing tiếp tục tuân theo Step 9:

- dùng Moving Weighted Average;
- Purchase cập nhật Quantity, InventoryValue và AverageCost;
- Sale dùng AverageCost hiện hành tại `CompleteSale` để snapshot cost basis vào SaleLine;
- Return dùng cost basis của SaleLine gốc.

### Inventory concurrency

Concurrent inventory mutation trên cùng `Product + Warehouse` phải được serialized hoặc kiểm soát concurrency sao cho không có lost update và không tính cost từ cùng một balance cũ.

Với operation có nhiều sản phẩm, balance phải được xử lý/lock theo deterministic order, ví dụ `ProductId` tăng dần, để giảm nguy cơ deadlock.

Step 10 chốt behavior này nhưng chưa chốt implementation bằng row locking, isolation level hay optimistic concurrency/retry. Quyết định kỹ thuật cụ thể thuộc Step 11.

### Purchase reversal limitation

MVP không hỗ trợ arbitrary retroactive Purchase Void khi đã có downstream inventory movements làm việc đảo trực tiếp không còn an toàn cho Moving Weighted Average.

- Chỉ cho Void/Reverse trực tiếp khi không có downstream dependency khiến costing không còn an toàn.
- Nếu đã có dependency, từ chối direct void và báo rõ lý do.
- Không xây retroactive costing/revaluation engine trong MVP.

Giới hạn này làm rõ cách tuân thủ Step 9: reversal chỉ được thực hiện khi hệ thống có thể phản ánh đúng business semantics về quantity và value, không đơn giản đảo quantity bất chấp các movement sau đó.

## 8. Decision C — Negative stock policy

SimpleStore hỗ trợ bán làm tồn kho âm theo cấu hình cấp Store:

```text
AllowNegativeStock
```

Giá trị mặc định là `false`. Chỉ Owner được thay đổi setting này và việc thay đổi phải audit được.

### Khi AllowNegativeStock = false

Nếu Sale làm `QuantityOnHand` âm:

- `CompleteSale` bị từ chối;
- response phải chỉ rõ sản phẩm thiếu và số lượng thiếu;
- backend enforce rule, không chỉ frontend.

### Khi AllowNegativeStock = true

- Sale được phép hoàn tất dù tồn bị âm;
- `InventoryMovement` vẫn ghi đầy đủ;
- `InventoryBalance` có thể âm;
- không yêu cầu Owner approve từng Sale;
- UI có thể cảnh báo Sale sẽ làm tồn âm.

### Cost behavior khi tồn âm

- Dùng last known average cost làm cost basis tạm thời.
- Nếu không có, fallback về reference purchase cost.
- Nếu vẫn không có cost đáng tin, đánh dấu cost/profit liên quan là chưa đủ tin cậy.
- Gross profit/profit liên quan phải được thể hiện là **ước tính** khi cost chưa đủ tin cậy.

Không retroactively revalue lịch sử khi nhập hàng sau. Negative stock là trạng thái cần attention/data-quality visibility, không phải một AI hoặc recommendation subsystem mới.

## 9. Reporting và read model direction

MVP không có analytics service riêng.

```text
Transactional SQL data
    ↓ read query / projection
“Hôm nay cửa hàng thế nào?”
```

Read query/projection có thể lấy từ:

- Sale;
- Payment;
- Debt source data;
- InventoryBalance;
- SaleLine cost snapshots.

Không cần data warehouse hoặc cache riêng nếu chưa có bằng chứng hiệu năng. Projection/read model là thông tin suy ra từ transactional source data, không trở thành nguồn sự thật độc lập.

### C14 attention experiment

C14 dùng deterministic rule/query, ví dụ kết hợp `InventoryBalance` và recent sales để phát hiện nguy cơ sắp hết hàng.

Không cần:

- AI service;
- vector database;
- agent;
- generic rule engine framework lớn.

Rule, window và threshold cụ thể vẫn cần được xác định và kiểm chứng ở bước phù hợp; Step 10 chỉ chốt architecture direction.

## 10. Printing boundary

`CompleteSale` và `PrintReceipt` là hai concern khác nhau:

```text
Backend CompleteSale thành công
→ Vue nhận Sale result
→ thực hiện print
```

Print failure:

- không rollback Sale `Completed`;
- hiển thị rõ Sale đã thành công;
- cho phép Reprint;
- database transaction không chờ máy in.

Browser print, local print agent hay printer service sẽ được quyết định ở Step 11 dựa trên thiết bị pilot.

## 11. HĐĐT integration boundary

C7 vẫn không được build trong MVP core. External HĐĐT service nằm ngoài core Sale database transaction.

```text
Sale Completed
→ Invoice Integration Adapter
→ existing HĐĐT service
```

External API timeout/failure không mặc định rollback Sale. Nếu integration được bổ sung sau này, invoice integration cần lifecycle/status riêng và retry behavior phù hợp.

Step 10 không chốt request mapping, provider contract hoặc cơ chế dispatch cụ thể.

## 12. Authentication và authorization direction

Backend là authorization boundary. Frontend có thể ẩn action để hỗ trợ UX nhưng không phải security boundary.

MVP có hai role:

- Owner;
- Cashier.

Ví dụ, action nhạy cảm như Void transaction `Completed` có thể giới hạn Owner only. `AllowNegativeStock` chỉ Owner được thay đổi.

Có thể dùng ASP.NET Core Identity/Auth hoặc cơ chế ASP.NET Core tương đương phù hợp. Permission matrix và implementation detail sẽ được quyết định ở Step 11.

## 13. Deployment direction

MVP ưu tiên deployment đơn giản phù hợp stack hiện có:

```text
Windows Server / IIS
├── Vue static files
├── ASP.NET Core API
└── SQL Server
```

MVP không cần:

- Kubernetes;
- Docker orchestration;
- microservices infrastructure;
- message broker cluster.

Mục tiêu là giảm operational complexity. Cấu hình môi trường, backup, monitoring, secrets, CI/CD và topology production cụ thể thuộc Development Plan/Technical Design.

## 14. Architectural principles

1. Modular Monolith trước microservices.
2. Reliability và consistency trước distributed complexity.
3. Critical operation dùng local database transaction khi có thể.
4. Backend enforce idempotency và authorization.
5. Ledger giải thích lịch sử; materialized balance phục vụ vận hành nhanh.
6. Concurrent mutation trên cùng business state phải được kiểm soát.
7. Không đưa AI hoặc distributed infrastructure vào nơi deterministic logic đủ dùng.
8. External side effect như printing/HĐĐT không làm core transaction trở nên mong manh.
9. Không xây retroactive costing/revaluation engine trong MVP.
10. Architecture phục vụ Product Model **DO → TRUST → UNDERSTAND & ACT**.

## 15. Kết luận

Architecture v0.1 giữ core transactional workflow trong một Modular Monolith và một SQL Server database để bảo vệ tính nhất quán. Idempotency, inventory ledger/materialized balance, concurrency control và negative-stock behavior tạo nền tảng TRUST. Reporting và C14 được giữ dưới dạng query/projection deterministic. Printing và HĐĐT nằm ngoài core Sale transaction để failure bên ngoài không phá business state đã commit.

Bước tiếp theo là **Step 11 — Development Plan / Technical Design Breakdown**. Step 11 sẽ chia implementation work và làm rõ các chi tiết kỹ thuật còn mở mà không tự mở rộng functional scope.
