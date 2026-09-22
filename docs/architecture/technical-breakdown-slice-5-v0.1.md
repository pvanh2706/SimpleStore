# Technical Breakdown Slice 5 v0.1 — Debt + End-of-day

## Trạng thái

`PROPOSED / PENDING PRODUCT OWNER APPROVAL`

Tài liệu này chưa phải authorization để implement. Không được tạo migration, sửa production code hoặc bắt đầu Stage 5A/5B trước khi Product Owner approve Technical Breakdown và giải quyết các Open Questions chặn domain/authorization.

## Mục tiêu và quy trình

Slice 5 hoàn thiện vòng vận hành tối thiểu về công nợ và câu hỏi cuối ngày mà không biến SimpleStore thành hệ thống kế toán. Trình tự bắt buộc sau approval:

`Domain behavior → DB changes → API contract → UI flow → Test cases → Implement`

Các quyết định D-001–D-050 tiếp tục được bảo toàn, đặc biệt:

- Completed Sale/Purchase/Return là immutable; correction dùng Return/Void/Reversal có audit.
- Payment chỉ là tiền thực nhận/thực trả; Payment không đồng nghĩa Revenue hoặc Debt.
- Critical mutation atomic, idempotent, có timeout/retry recovery.
- Mọi dữ liệu và query phải Store-scoped; backend là authorization boundary.
- Historical SaleLine cost snapshot là nguồn tính COGS lịch sử.
- End-of-day là query theo business date, không phải accounting close.

## 1. Scope

### In scope

- Customer debt balance/list/query.
- Customer debt payment: partial, full và multiple payments.
- Supplier debt balance/list/query.
- Supplier debt payment: partial, full và multiple payments.
- End-of-day summary theo business/local date của Store.
- Revenue và Collected là hai metrics độc lập.
- Supplier payments trong business date.
- Customer outstanding debt tại cutoff.
- Supplier outstanding debt tại cutoff.
- Estimated Gross Profit từ net Sale revenue và historical SaleLine cost snapshot.
- Recovery cho double-click, same OperationId retry, timeout after commit và concurrent payment.

### Out of scope

- Accounting ledger / General Ledger.
- Formal accounting closing, Close Day/Reopen Day hoặc period lock.
- Cashbook đầy đủ hoặc carry-forward cash closing balance.
- Invoice-level debt allocation, FIFO settlement hoặc statement reconciliation theo invoice.
- Debt aging.
- Customer credit balance, supplier advance, advance payment hoặc prepaid balance.
- Financial statements.
- Operating expenses và net profit.
- BI dashboard.
- AI.
- New Return/Purchase-return/cash-correction framework ngoài các correction đã approve.

## 2. Repository baseline đã inspect

Breakdown này dựa trên source tại baseline Slice 4 `5876082a`:

- `SalePayment`, `PurchasePayment` và `ReturnRefundPayment` là ba immutable actual-money record gắn với transaction tương ứng.
- Cả ba reuse `PaymentMethod` hiện chỉ có `Cash` và `Transfer`, amount `decimal(18,2)`, actor và UTC `DateTimeOffset`.
- `SalePayment` bắt buộc `SaleId`; `PurchasePayment` bắt buộc `PurchaseId`; `ReturnRefundPayment` bắt buộc `ReturnId`. Không bảng nào phù hợp để gắn debt payment cấp Customer/Supplier mà không tạo invoice allocation giả.
- `BusinessOperation.OperationId` là global primary key. Repo đã thống nhất transaction-owned SQL application lock `SimpleStore:BusinessOperation:{OperationId}` và exact fingerprint retry.
- Supplier outstanding hiện được query từ Completed Purchase trừ PurchasePayment, đồng thời loại Purchase đã Void. Customer read model chưa có aggregate debt cấp Customer.
- Sale/Return/Void projections đã giữ original facts và tính active net values; Purchase Void giữ original payments nhưng loại active purchase contribution.
- `Store` chưa có timezone configuration. Timestamps hiện dùng `DateTimeOffset` từ `TimeProvider.GetUtcNow()`.
- Existing route convention là resource routes dưới `/api/...`, mutation dạng subresource/action, `ProblemDetails` có stable `code`, Owner/Cashier qua ASP.NET authorization.
- Supplier/Purchase hiện Owner-only theo D-021; Customers/Sales read và Sale checkout cho Owner/Cashier; Return và Sale/Purchase Void Owner-only theo D-033.

### Payment model direction

Không migrate ba bảng payment lịch sử sang một bảng generic trong Slice 5. Việc đó tạo migration/risk lớn nhưng không mang lại business value cần thiết. Đề xuất reuse:

- `PaymentMethod` và cùng validation/precision/timestamp/audit semantics;
- `BusinessOperation` và global OperationId lock;
- một entity/bảng mới dùng chung cho **hai loại debt payment**, thay vì tạo hai abstraction gần như giống nhau.

Tên logical trong tài liệu: `DebtPayment`. Exact class name có thể giữ tên này khi implement.

## 3. Domain semantics

### 3.1 Thuật ngữ

- `Obligation`: giá trị giao dịch còn hiệu lực mà Customer phải trả hoặc Store phải trả Supplier.
- `Actual payment`: immutable fact rằng tiền đã thực sự vào/ra.
- `Outstanding debt`: derived value từ obligation, payment và correction history tại một cutoff.
- `CurrentOutstandingDebt`: outstanding được backend recompute trong transaction sau khi đã lấy party debt lock.
- `DebtPayment`: actual money ở cấp Customer hoặc Supplier, không allocation vào Sale/Purchase cụ thể.
- `Business date`: ngày local của Store được chuyển thành một UTC half-open interval `[startUtc, endUtc)`.

Không dùng field `Customer.Debt` hoặc `Supplier.Debt` có thể chỉnh tay làm source of truth.

### 3.2 Customer debt calculation

Tại cutoff `T`, với một Customer trong một Store:

```text
ActiveSaleContribution(sale, T) =
    0, nếu Sale đã Void trước T
    Sale.TotalAmount
      - sum(Return.TotalReturnAmount completed trước T)
      - sum(SalePayment.Amount occurred trước T)
      + sum(ReturnRefundPayment.Amount occurred trước T), ngược lại

CustomerOutstandingDebt(customer, T) =
    sum(ActiveSaleContribution cho Customer, completed trước T)
      - sum(CustomerDebtPayment.Amount occurred trước T)
```

Giải thích:

- Completed Sale tạo obligation.
- SalePayment là tiền đã thu tại checkout và giảm obligation còn lại.
- Return giảm Sale obligation; actual refund làm tiền đang giữ giảm nên được cộng trở lại vào phần obligation chưa settle theo công thức D-036.
- Sale Void làm active debt contribution của Sale bằng zero; original Sale/Payment vẫn là historical facts.
- Customer debt payment giảm aggregate debt của Customer nhưng không tạo Revenue và không gắn tới Sale cụ thể.
- Invariant sau mọi mutation liên quan phải là `CustomerOutstandingDebt >= 0`; không clamp âm về zero vì clamp sẽ che customer credit/overpayment mà D-046 cấm.

Query current và historical-as-of phải dùng cùng semantic. Nếu dữ liệu vi phạm invariant, API/report trả typed data-integrity error và log đủ điều tra; không âm thầm sửa hoặc clamp.

### 3.3 Supplier debt calculation

Tại cutoff `T`, với một Supplier trong một Store:

```text
ActivePurchaseContribution(purchase, T) =
    0, nếu Purchase đã Void trước T
    Purchase.TotalAmount
      - sum(PurchasePayment.Amount paid trước T), ngược lại

SupplierOutstandingDebt(supplier, T) =
    sum(ActivePurchaseContribution cho Supplier, completed trước T)
      - sum(SupplierDebtPayment.Amount occurred trước T)
```

- Completed Purchase tạo supplier obligation.
- PurchasePayment tại CompletePurchase và SupplierDebtPayment sau đó đều là actual money out.
- Purchase Void làm active obligation/payment contribution của Purchase bằng zero theo D-039; không xóa payment lịch sử và không tạo fake money movement.
- Invariant sau mọi mutation liên quan là `SupplierOutstandingDebt >= 0`; không clamp và không tạo supplier advance.

### 3.4 Source of truth và performance

MVP đề xuất query/projection trực tiếp từ indexed transaction/payment/correction history. Không thêm mutable debt-balance column trong Stage 5A.

Nếu profiling sau này chứng minh cần materialized balance:

- transaction history vẫn là business source of truth;
- balance phải update trong cùng SQL transaction với mutation;
- phải có deterministic rebuild từ history;
- phải có consistency check so materialized với recomputed value;
- mismatch không được tự động ghi đè history.

Việc materialize là future optimization, không phải acceptance criterion Slice 5.

## 4. Domain flows

Mỗi flow giữ format đã approve: `User Action → System Behavior → Business Outcome → Failure Path → Recovery Path`.

### 4.1 Customer debt payment — partial

**User Action** → Owner/Cashier có quyền mở Customer Debt, thấy outstanding authoritative gần nhất, nhập amount nhỏ hơn outstanding, chọn Cash/Transfer và confirm.

**System Behavior** → Client tạo một OperationId và immutable attempt snapshot. Backend resolve Store/User, lấy global operation lock, kiểm tra exact retry, lấy Customer debt lock, recompute current outstanding, đối chiếu `ExpectedOutstandingAmount`, validate `0 < Amount <= CurrentOutstandingDebt`, tạo `DebtPayment` purpose `CustomerDebtCollection`/direction `MoneyIn` và `BusinessOperation` trong cùng transaction.

**Business Outcome** → Actual collected tăng đúng amount; customer outstanding giảm đúng amount; Revenue không đổi; response trả payment và outstanding before/after authoritative.

**Failure Path** → Invalid amount/method, Customer không thuộc Store, balance stale, concurrent mutation làm amount vượt outstanding, forbidden, operation-lock timeout hoặc DB failure. Không payment/operation partial.

**Recovery Path** → Deterministic 4xx/409 hiển thị lỗi và reload latest outstanding. Với timeout/408/relevant 5xx/operation-lock-timeout, giữ nguyên snapshot + OperationId, query operation status và retry exact request; không phát sinh payment thứ hai.

### 4.2 Customer debt payment — full

**User Action** → Chọn “Thu toàn bộ” từ outstanding đang hiển thị.

**System Behavior** → Gửi exact displayed amount kèm `ExpectedOutstandingAmount`; backend vẫn recompute dưới lock, không tin nút “full” phía client.

**Business Outcome** → Outstanding về chính xác `0.00`, không tạo credit balance; actual collected tăng nhưng Revenue không đổi.

**Failure Path** → Nếu balance đã giảm trước khi commit, request bị reject `debt-balance-changed` hoặc `debt-payment-exceeds-outstanding`, không tự đổi amount.

**Recovery Path** → Reload authoritative balance. User xác nhận một intention mới với OperationId mới; exact retry của intention cũ vẫn dùng ID cũ.

### 4.3 Supplier debt payment — partial

**User Action** → User có quyền mở Supplier Debt, nhập amount nhỏ hơn outstanding, chọn method và confirm.

**System Behavior** → Cùng idempotency flow nhưng lấy Supplier debt lock, recompute Supplier outstanding và tạo `DebtPayment` purpose `SupplierDebtSettlement`/direction `MoneyOut`.

**Business Outcome** → Actual supplier payment tăng, supplier outstanding giảm; Purchase value không đổi.

**Failure Path** → Supplier cross-store/not found, invalid/stale/overpayment, forbidden, timeout hoặc transaction failure.

**Recovery Path** → Typed error reload balance; ambiguous outcome query operation và exact retry.

### 4.4 Supplier debt payment — full

**User Action** → Chọn “Trả toàn bộ”.

**System Behavior** → Backend validate exact current outstanding dưới Supplier debt lock.

**Business Outcome** → Outstanding về `0.00`; không tạo supplier advance/prepayment.

**Failure Path** → Concurrent payment/correction làm displayed balance stale.

**Recovery Path** → Không auto-pay amount mới; reload và yêu cầu user confirm intention mới.

### 4.5 Retry sau timeout

**User Action** → User submit, client không nhận được response do timeout/network failure.

**System Behavior** → UI giữ immutable `OperationId + PartyId + Purpose + Amount + Method + ExpectedOutstandingAmount`. Client gọi `GET /api/operations/{operationId}`. Nếu Completed đúng type, load committed DebtPayment; nếu not found/unknown sau transient failure, retry exact snapshot; nếu same ID khác fingerprint/type, reject.

**Business Outcome** → At-most-one committed debt payment cho một OperationId; user thấy result đã commit thay vì tạo payment lần hai.

**Failure Path** → UI mất local snapshot hoặc user cố sửa payload với ID cũ.

**Recovery Path** → Không reuse ID với payload đã sửa. Result Completed được load theo result reference; payload mismatch trả `idempotency-key-reused`.

### 4.6 Concurrent debt payments

Ví dụ Customer debt hiện tại `600,000`; request A và B có OperationId khác nhau, mỗi request `400,000`.

**User Action** → Hai tab/device submit gần đồng thời.

**System Behavior** → Cả hai lấy global operation lock riêng, nhưng serialize trên cùng Customer debt lock. Request thắng commit, debt còn `200,000`; request sau recompute và reject vì `400,000 > 200,000` hoặc expected balance stale.

**Business Outcome** → Tổng committed không vượt `600,000`; không thể thành `800,000`.

**Failure Path** → Lock timeout hoặc stale balance.

**Recovery Path** → Retry exact request không bypass validation; UI reload balance `200,000` và yêu cầu intention mới.

### 4.7 End-of-day query

**User Action** → Owner chọn một business date.

**System Behavior** → Backend resolve Store timezone, chuyển local date thành `[startUtc, endUtc)`, query Store-scoped projections và trả metric + component breakdown + cost reliability. Query không mutate state hoặc tạo “day close”.

**Business Outcome** → Owner phân biệt Revenue, actual Collected, ending debts, Supplier Payments và Estimated Gross Profit.

**Failure Path** → Invalid date/timezone, unauthorized hoặc data-integrity inconsistency.

**Recovery Path** → Typed error; user sửa date hoặc Owner sửa Store timezone theo workflow được approve. Không fallback ngầm sang UTC/server-local timezone.

### 4.8 End-of-day với các event chính

Metrics đề xuất dùng **event-date basis**; correction ảnh hưởng ngày correction xảy ra, không âm thầm rewrite report của ngày Sale/Purchase gốc:

| Event trong business date | Sales Revenue | Collected | Supplier Payments | Ending debt | Estimated GP |
|---|---:|---:|---:|---|---:|
| Normal Sale | `+Sale.Total` | `+SalePayments` | — | phần chưa thu tăng Customer debt | `+Revenue - historical COGS` |
| Credit Sale | `+Sale.Total` | chỉ actual SalePayments | — | unpaid amount tăng Customer debt | `+Revenue - historical COGS` |
| Old customer debt collected | `0` | `+CustomerDebtPayment` | — | Customer debt giảm | `0` |
| Return | `-Return.TotalReturnAmount` | `-actual RefundPayment` | — | obligation giảm; refund/payment history điều chỉnh debt theo formula | đảo Revenue; chỉ Restock đảo historical COGS, NoRestock giữ cost đã issue |
| Sale Void | `-OriginalSale.Total` tại ngày Void | không tự tạo cash out; không đổi Collected nếu không có actual refund | — | active Sale contribution về 0 | đảo original Revenue và historical COGS |
| Purchase | `0` | `0` | `+PurchasePayments` | unpaid amount tăng Supplier debt | `0` |
| Supplier debt payment | `0` | `0` | `+SupplierDebtPayment` | Supplier debt giảm | `0` |
| Safe Purchase Void | `0` | `0` | không tự đảo actual payments | active Purchase contribution về 0 | `0` |

Một Sale/Purchase và correction cùng ngày net đúng trong ngày. Correction của giao dịch ngày cũ xuất hiện ở ngày correction. Vì EOD không phải accounting close, query past date vẫn deterministic theo event timestamps và không phụ thuộc current Product cost.

## 5. Payment model

### 5.1 Proposed `DebtPayment`

```text
DebtPayment
- Id: Guid
- StoreId: Guid
- OperationId: Guid
- Direction: MoneyIn | MoneyOut
- Purpose: CustomerDebtCollection | SupplierDebtSettlement
- CustomerId: Guid?  // exactly one party reference
- SupplierId: Guid?
- Amount: decimal(18,2)
- Method: Cash | Transfer
- OccurredAt: DateTimeOffset (UTC instant)
- PerformedByUserId: Guid
- Note/Reference: not included until Open Question is decided
```

Invariants:

- CustomerDebtCollection ⇒ `Direction = MoneyIn`, CustomerId required, SupplierId null.
- SupplierDebtSettlement ⇒ `Direction = MoneyOut`, SupplierId required, CustomerId null.
- Amount positive, maximum two decimals, never exceeds current outstanding under lock.
- Store/party composite FK prevents cross-store reference.
- Completed payment immutable; no direct edit/hard-delete.
- OperationId unique and maps to one completed BusinessOperation result.
- No SaleId/PurchaseId/InvoiceId because D-047 forbids invoice allocation requirement.

### 5.2 Reversal/correction

Slice 5 không thêm generic payment edit/delete/reversal operation. Nếu user nhập sai một completed debt payment, không được sửa row hoặc xóa lịch sử. Một explicit debt-payment reversal/correction cần business rules, authorization, actual money semantics và approval riêng; hiện ngoài scope.

Nếu DB transaction fail trước commit, không có payment. Nếu timeout after commit, recovery trả payment đã commit. Đây không phải reversal.

## 6. Concurrency, atomicity và idempotency

### 6.1 Lock strategy cho SQL Server + EF Core

Reuse transaction-owned `sp_getapplock` pattern hiện tại:

```text
SimpleStore:BusinessOperation:{OperationId}
SimpleStore:CustomerDebt:{StoreId}:{CustomerId}
SimpleStore:SupplierDebt:{StoreId}:{SupplierId}
```

Canonical order cho mutation:

1. begin local SQL transaction;
2. acquire BusinessOperation lock;
3. check existing operation/type/fingerprint;
4. acquire party debt lock;
5. acquire SaleCorrection/PurchaseCorrection lock nếu operation là correction;
6. acquire InventoryBalance locks theo ProductId deterministic order nếu có inventory effect;
7. recompute authoritative debt và validate;
8. persist all effects + BusinessOperation;
9. commit.

Để debt linearizable, các operation làm thay đổi debt phải tham gia cùng party lock:

- Customer: CompleteSale có Customer, CreateReturn, VoidSale, CustomerDebtPayment.
- Supplier: CompletePurchase, VoidPurchase, SupplierDebtPayment.

Đây là extension kỹ thuật cho existing use cases, không làm Completed transaction mutable. Toàn repo phải giữ cùng lock order để tránh deadlock.

Read Committed + exclusive transaction-owned application lock đủ để serialize mutation của một party; không dựa riêng vào frontend, EF tracking hoặc rowversion. Unique constraints là defense-in-depth.

### 6.2 Exact retry

Fingerprint canonical gồm:

```text
operation type + Store-resolved party id + amount G29 + method
+ expected outstanding G29
```

Note chỉ tham gia fingerprint nếu được Product Owner approve và model hóa.

- Same OperationId + same fingerprint + Completed → trả DebtPayment cũ và current committed result, `WasAlreadyRecorded = true`.
- Same OperationId + different type/payload → `idempotency-key-reused`.
- BusinessOperation, DebtPayment và debt-validation outcome commit atomically.
- Double-click reuse cùng immutable attempt/OperationId.
- Hai OperationId khác nhau luôn là hai intentions và phải qua current debt validation riêng.

### 6.3 Stale UI balance

Mutation request bắt buộc gửi `ExpectedOutstandingAmount` từ read response. Backend so với recomputed balance dưới lock:

- mismatch → `debt-balance-changed` với latest outstanding trong ProblemDetails extension;
- amount lớn hơn latest → `debt-payment-exceeds-outstanding`;
- không tự co amount hoặc tự biến “full” thành amount mới.

UI reload balance, giải thích rằng công nợ vừa thay đổi và yêu cầu user xác nhận intention mới với OperationId mới. Exact ambiguous retry không bị stale-check lại theo state sau commit vì idempotency result được resolve trước validation.

## 7. Proposed DB changes — chưa tạo migration

### 7.1 New table `DebtPayments`

- PK `Id`.
- `StoreId` required; FK `Stores` Restrict.
- `OperationId` required, unique.
- `Direction`/`Purpose` string tối đa 32.
- nullable `CustomerId` và `SupplierId` với composite Store FK, Restrict.
- `Amount decimal(18,2)` + check `Amount > 0`.
- `Method` string tối đa 32.
- `OccurredAt datetimeoffset`.
- `PerformedByUserId` FK `AspNetUsers`, Restrict.
- check constraint enforce đúng một party và Direction/Purpose hợp lệ.

Indexes:

- unique `OperationId`;
- `(StoreId, CustomerId, OccurredAt)` filtered `CustomerId IS NOT NULL`;
- `(StoreId, SupplierId, OccurredAt)` filtered `SupplierId IS NOT NULL`;
- `(StoreId, Purpose, OccurredAt)` cho EOD;
- `PerformedByUserId` cho audit.

`BusinessOperationTypes` cần thêm `RecordCustomerDebtPayment` và `RecordSupplierDebtPayment`. `ResultReference` trỏ `DebtPayment.Id`; không tạo generic FK từ polymorphic result reference.

### 7.2 Store timezone — conditional on Open Question

Current schema không có timezone. Đề xuất thêm `Stores.TimeZoneId` required, tối đa 128, dùng IANA hoặc Windows ID theo runtime/deployment strategy được approve. Migration/backfill value không được hard-code trước Product Owner decision về source/default.

Nếu pilot onboarding bắt buộc chọn timezone, existing Store cần explicit backfill/deployment configuration đã review; API không được silently dùng server local timezone.

### 7.3 Index additions cho derived queries

Chỉ thêm sau khi kiểm tra generated SQL/query plan, dự kiến:

- `SalePayments(StoreId, OccurredAt)` include `SaleId, Amount, Method`.
- `ReturnRefundPayments(StoreId, OccurredAt)` include `ReturnId, Amount, Method`.
- `PurchasePayments(StoreId, PaidAt)` include `PurchaseId, Amount, Method`.
- `Sales(StoreId, CompletedAt, CustomerId)` hoặc giữ/tune existing indexes theo plan.
- `Purchases(StoreId, CompletedAt, SupplierId)`.
- `Returns(StoreId, CompletedAt)` include `OriginalSaleId, TotalReturnAmount`.
- `SaleVoids(StoreId, VoidedAt)` include `OriginalSaleId`.
- `PurchaseVoids(StoreId, VoidedAt)` include `OriginalPurchaseId`.

Không duplicate transaction totals/costs trong report table. Không tạo daily snapshot/close table trong Slice 5.

### 7.4 Migration safety khi implementation được approve

- Chỉ additive schema/index changes đã review; không recreate/drop business tables.
- Generate migration trong Infrastructure nhưng production apply vẫn explicit deployment step.
- Migration test từ Slice 4 schema/data có Sale/Purchase/Return/Void/payment history.
- Snapshot/model diff phải được review để bảo đảm không có unrelated changes.

## 8. API contract proposal

Routes theo conventions hiện tại:

```text
GET  /api/customers/debts?search=&page=&pageSize=
GET  /api/customers/{customerId}/debt
POST /api/customers/{customerId}/debt-payments

GET  /api/suppliers/debts?search=&page=&pageSize=
GET  /api/suppliers/{supplierId}/debt
POST /api/suppliers/{supplierId}/debt-payments

GET  /api/reports/end-of-day?date=YYYY-MM-DD
GET  /api/operations/{operationId} // extend existing result types
```

`StoreId` không nhận từ request làm authority; resolve từ authenticated user.

### 8.1 Debt query response

```json
{
  "partyId": "guid",
  "partyName": "...",
  "asOf": "2026-09-22T16:59:59.9999999+00:00",
  "outstandingAmount": 600000.00,
  "currency": "VND"
}
```

List chỉ cần identity + outstanding + asOf, mặc định `outstandingAmount > 0`, pagination giống repo. Không expose invoice allocation. Currency field có thể omit nếu repo chưa model multi-currency; VND display là UI concern và không mở multi-currency scope.

### 8.2 Record debt payment request

```json
{
  "operationId": "guid",
  "amount": 400000.00,
  "method": "Cash",
  "expectedOutstandingAmount": 600000.00
}
```

`Note/reference` chưa có trong contract trước khi Open Question được quyết định.

Response 200 cho first commit và exact retry, thống nhất với mutation convention hiện tại:

```json
{
  "id": "guid",
  "partyId": "guid",
  "direction": "MoneyIn",
  "purpose": "CustomerDebtCollection",
  "amount": 400000.00,
  "method": "Cash",
  "occurredAt": "2026-09-22T10:00:00+00:00",
  "performedByUserId": "guid",
  "outstandingBefore": 600000.00,
  "outstandingAfter": 200000.00,
  "wasAlreadyRecorded": false
}
```

Exact retry trả cùng payment/result reference và `wasAlreadyRecorded = true`; không tạo timestamp/id mới.

### 8.3 End-of-day response

```json
{
  "businessDate": "2026-09-22",
  "timeZoneId": "approved-store-timezone-id",
  "startUtc": "...",
  "endUtc": "...",
  "salesRevenue": 10000000.00,
  "collected": {
    "salePayments": 7000000.00,
    "customerDebtPayments": 1000000.00,
    "customerRefunds": 0.00,
    "netAmount": 8000000.00
  },
  "customerOutstandingDebtAtEnd": 2000000.00,
  "supplierPayments": {
    "purchasePayments": 3000000.00,
    "supplierDebtPayments": 500000.00,
    "totalAmount": 3500000.00
  },
  "supplierOutstandingDebtAtEnd": 4000000.00,
  "estimatedGrossProfit": {
    "netSalesRevenue": 10000000.00,
    "historicalCogs": 7500000.00,
    "amount": 2500000.00,
    "costReliability": "Reliable"
  }
}
```

Main debt metrics là **ending balance tại `endUtc`**, không phải debt created during day. Slice 5 UI không cần “debt created during day”; nếu thêm sau này phải dùng tên riêng, không overload `CustomerOutstandingDebt`/`SupplierOutstandingDebt`.

### 8.4 Validation và typed errors

Validation:

- OperationId non-empty.
- Amount positive, tối đa hai decimals.
- Method chỉ `Cash`/`Transfer` hiện tại.
- ExpectedOutstandingAmount nonnegative, tối đa hai decimals.
- ISO `YYYY-MM-DD` valid.
- Party phải thuộc current Store.

Stable error codes dự kiến:

- `customer-not-found`, `supplier-not-found` (cross-store dùng not-found semantics).
- `customer-has-no-outstanding-debt`, `supplier-has-no-outstanding-debt`.
- `invalid-payment-amount`, `invalid-payment-precision`, `invalid-payment-method`.
- `debt-payment-exceeds-outstanding`.
- `debt-balance-changed` với `latestOutstandingAmount` extension.
- `idempotency-key-reused`, `operation-lock-timeout`, `concurrent-update`.
- `invalid-business-date`, `store-timezone-not-configured`, `invalid-store-timezone`.
- `customer-debt-state-invalid`, `supplier-debt-state-invalid`, `return-financial-state-invalid`.

Status convention: validation 400; auth 401/403; not found 404; state/concurrency/idempotency conflict 409; unexpected 500. UI branch theo `code`, không parse title/detail.

## 9. End-of-day metric semantics

### 9.1 Sales Revenue

Cho window `[startUtc, endUtc)`:

```text
SalesRevenue =
    sum(Sale.TotalAmount where Sale.CompletedAt in window)
  - sum(Return.TotalReturnAmount where Return.CompletedAt in window)
  - sum(OriginalSale.TotalAmount where SaleVoid.VoidedAt in window)
```

Return và Sale Void mutually exclusive theo D-035 nên không double subtract cùng Sale. Sale Void không dựa vào current product/sale price. Debt payment không xuất hiện trong Revenue.

### 9.2 Collected

```text
GrossCollected =
    sum(SalePayment.Amount where OccurredAt in window)
  + sum(CustomerDebtPayment.Amount where OccurredAt in window)

CustomerRefunds =
    sum(ReturnRefundPayment.Amount where OccurredAt in window)

NetCollected = GrossCollected - CustomerRefunds
```

Proposed API trả components và `netAmount`; UI wording cuối cùng phụ thuộc Open Question. Sale Void không tự làm giảm Collected vì Void không phải actual refund theo D-038. Debt creation không tăng Collected.

### 9.3 Customer Outstanding Debt

`CustomerOutstandingDebtAtEnd` là tổng aggregate debt của mọi Customer tại cutoff `endUtc`, dùng formula mục 3.2 và lịch sử **as-of cutoff**. Không dùng current/latest void/return/payment xảy ra sau cutoff khi query ngày cũ.

### 9.4 Supplier Payments

```text
SupplierPayments =
    sum(PurchasePayment.Amount where PaidAt in window)
  + sum(SupplierDebtPayment.Amount where OccurredAt in window)
```

Purchase Void không tạo money inflow và không âm thầm subtract actual supplier payment. Nếu sau này có actual supplier refund, đó là một money event mới cần approval/model riêng.

### 9.5 Supplier Outstanding Debt

`SupplierOutstandingDebtAtEnd` là total outstanding của mọi Supplier tại `endUtc`, dùng formula mục 3.3; không phải purchase value hoặc debt created in day.

### 9.6 Estimated Gross Profit

Historical COGS amount cho một SaleLine:

```text
Round(quantity × UnitCostAtSale, 2, AwayFromZero)
```

Return dùng persisted historical basis và final residual semantics đã approve. COGS chỉ được đảo khi hàng thực sự Restock: `ReturnLine.RestockedInventoryValue` đã được tính từ `UnitCostAtSale`, bị cap theo exact original issued value và bằng `0` cho NoRestock. NoRestock không đưa hàng/value về inventory nên original issued cost vẫn nằm trong COGS; nếu revenue đã return toàn bộ, Estimated Gross Profit có thể âm và phản ánh mất mát hàng hóa ở mức gross estimate. Sale Void đảo inventory đầy đủ nên dùng exact original historical issued value. Window projection:

```text
HistoricalCOGS =
    COGS của Sales completed trong window
  - sum(ReturnLine.RestockedInventoryValue của Returns completed trong window)
  - historical COGS của original Sales voided trong window

EstimatedGrossProfit = SalesRevenue - HistoricalCOGS
```

Không dùng current Product AverageCost/ReferencePurchaseCost. Không tự phân loại NoRestock thành một operating-expense ledger; Slice 5 chỉ giữ cost đã issue trong estimated COGS và không tuyên bố đây là net/accounting profit.

Response tổng hợp `CostReliability` theo worst state `Unavailable > Estimated > Reliable` từ SaleLine/Return basis tham gia window. Nếu có `Unavailable`, UI vẫn ghi “Lãi gộp ước tính” và cảnh báo dữ liệu giá vốn chưa đủ tin cậy; không trình bày như accounting profit chính xác.

### 9.7 Empty day

Tất cả flow metrics trong ngày bằng `0.00`; ending debt vẫn là balance as-of endUtc và có thể khác zero do lịch sử trước đó. Không trả 404 cho ngày rỗng.

## 10. Business date và timezone

Request nhận `date` là local calendar date, không nhận UTC date giả định. Backend:

1. load Store timezone configuration;
2. tạo local start `date 00:00` và local end `date+1 00:00` trong timezone đó;
3. convert từng boundary thành UTC instant;
4. query mọi event bằng half-open interval `[startUtc, endUtc)`.

Không dùng `CAST(timestamp AS date)` theo UTC và không dùng timezone của app server/browser. Half-open interval ngăn double count ở midnight và hỗ trợ ngày có DST length khác 24 giờ.

Tests bắt buộc:

- event ngay trước start không thuộc ngày;
- event đúng start thuộc ngày;
- event ngay trước end thuộc ngày;
- event đúng end thuộc ngày sau;
- timezone offset khác UTC;
- DST/ambiguous/invalid boundary nếu timezone được chọn có DST;
- browser timezone khác Store timezone không đổi kết quả.

Source/format/default của Store timezone là Open Question. Không hard-code `Asia/Bangkok` hoặc timezone cụ thể chỉ vì pilot hiện tại ở Việt Nam.

## 11. UI flow proposal

Không redesign visual system và không xây BI dashboard.

### 11.1 Customer Debt

- Trang/list tìm Customer có debt, hiển thị name/phone/current outstanding/as-of.
- Chọn Customer mở debt card và payment form.
- Amount; shortcut “Thu toàn bộ”; PaymentMethod Cash/Transfer.
- Confirm dialog nêu đây là tiền thực thu và không tạo Revenue.
- Submit một lần với immutable OperationId snapshot; disable double-click.
- Success hiển thị amount, method, occurredAt và outstanding authoritative mới.
- Ambiguous state hiển thị “đang kiểm tra kết quả”, query operation và cho exact retry.
- `debt-balance-changed`/overpayment reload latest outstanding, không auto-submit amount mới.

### 11.2 Supplier Debt

- Tương tự Customer nhưng wording “tiền thực trả Supplier”.
- List chỉ available cho role được phép; current proposal giữ Owner-only theo existing Supplier/Purchase boundary.
- Success không thay đổi Purchase value.

### 11.3 End-of-day

Một trang summary đơn giản:

- date picker theo Store local date;
- Revenue;
- Collected, kèm breakdown sale payments / old debt collection / refunds;
- Customer outstanding debt at end;
- Supplier payments, kèm purchase payment / old debt payment;
- Supplier outstanding debt at end;
- Estimated Gross Profit + cost reliability note.

Không chart builder, drill-down BI, saved dashboard hoặc accounting close. Có thể link sang existing Sale/Purchase/debt list nếu đơn giản, nhưng drill-down mới không phải blocker MVP.

## 12. Authorization proposal và Open Questions

Backend authorize mọi endpoint; UI hide action chỉ là UX.

| Capability | Conservative proposal trước PO decision | Cơ sở |
|---|---|---|
| View customer debt | Owner + Cashier | Existing Customer/Sale read là Owner + Cashier; vẫn là Open Question cho Slice 5 |
| Collect customer debt | Owner + Cashier | Operational cash collection, nhưng chưa được approve rõ; Open Question |
| View supplier debt | Owner only | Giữ D-021 Supplier/Purchase boundary |
| Pay supplier debt | Owner only | Không mở quyền nhạy cảm rộng hơn D-021; xác nhận trong Open Questions |
| View End-of-day | Owner only | Financial summary nhạy cảm; Open Question |
| View Estimated Gross Profit | Owner only | Financial result nhạy cảm; Open Question |

Cho tới khi approve, deny-by-default đối với role chưa rõ. Cashier direct URL tới Owner-only endpoint trả 403. Cross-store party/resource dùng Store-scoped not-found và không leak existence.

## 13. Test plan

### 13.1 Domain tests — Customer debt payment

- partial payment giảm debt đúng amount và không đổi Revenue;
- full payment về zero;
- multiple payments;
- zero, negative và >2 decimal rejected;
- overpayment rejected;
- no-outstanding rejected;
- Return/refund/SaleVoid formula as-of cutoff;
- voided Sale contribution zero nhưng original history giữ nguyên;
- state âm/corrupt không bị clamp.

### 13.2 Domain tests — Supplier debt payment

- partial, full, multiple;
- zero, negative, precision và overpayment;
- PurchasePayment + SupplierDebtPayment formula;
- Purchase Void active contribution zero;
- Purchase value không đổi;
- state âm/corrupt không bị clamp.

### 13.3 SQL Server integration tests — Customer

- allowed roles theo final decision; wrong role forbidden.
- Customer cross-store not found; composite FK defense.
- DebtPayment + BusinessOperation atomic commit/rollback.
- same OperationId exact retry trả cùng payment.
- same OperationId/different payload hoặc cross-operation type conflict.
- timeout after commit/status recovery.
- concurrent same OperationId tạo một row.
- concurrent different IDs với debt `600,000`, hai payment `400,000`: chỉ một commit.
- stale expected balance typed conflict và latest amount.
- concurrent payment với Return/SaleVoid/CompleteSale dùng shared party lock và không tạo negative debt.

### 13.4 SQL Server integration tests — Supplier

- partial/full/multiple và exact retry.
- cross-store, wrong role và Owner-only boundary theo final decision.
- concurrent payments không vượt debt.
- concurrent SupplierDebtPayment với CompletePurchase/PurchaseVoid serialize đúng.
- payment không sửa Purchase total/history.
- transaction failure không để payment hoặc BusinessOperation partial.

Không dùng EF InMemory để chứng minh locking/concurrency.

### 13.5 End-of-day tests

- cash Sale;
- credit Sale;
- mixed payment/debt;
- old debt collected today tăng Collected, không tăng Revenue;
- Return giảm Revenue và actual refund giảm Collected;
- Sale Void đảo Revenue/COGS theo event date, không fake refund;
- Purchase payment today và supplier old-debt payment today;
- Purchase created/completed ngày khác không tự thành supplier payment hôm nay;
- safe Purchase Void không fake reverse actual supplier money;
- ending Customer/Supplier debt dùng endUtc, không phải debt created during day;
- timezone start/end boundary và browser/server timezone khác Store;
- empty day: flow metrics zero, ending balances vẫn đúng;
- historical GP không đổi sau Product AverageCost/ReferencePurchaseCost thay đổi;
- Return Restock đảo exact historical inventory value; NoRestock không đảo COGS; Sale Void đảo full historical cost; rounding residual đúng;
- cost reliability Reliable/Estimated/Unavailable aggregation;
- Store A report không chứa Store B.

### 13.6 API/security/frontend/E2E

- ProblemDetails stable codes/status/extensions.
- unauthorized 401, wrong role 403, cross-store 404.
- Customer/Supplier list pagination/search/current amount.
- double click một request; form locked khi outcome ambiguous.
- operation recovery load committed result.
- stale balance reload + clear message; không auto-resubmit.
- End-of-day labels Revenue/Collected/Gross Profit không nhập nhằng.
- Real local E2E: create credit Sale → partial/full old-debt collection → EOD verify Revenue/Collected/debt.
- Real local E2E: credit Purchase → supplier payment → EOD verify supplier payment/debt.

## 14. Implementation staging proposal

### Stage 5A — Debt backend/domain/persistence/tests

- Debt formula/query services.
- Shared DebtPayment domain model.
- Customer/Supplier debt locks, operation types, idempotency/fingerprint/recovery.
- Extend existing debt-affecting mutations với canonical party lock order.
- Proposed schema/migration sau review.
- Thin debt APIs, authorization và SQL Server integration tests.

### Stage 5B — End-of-day + frontend + E2E/recovery

- Store timezone mechanism theo approved answer.
- EOD aggregation/query và historical COGS reliability.
- Customer/Supplier debt UI.
- End-of-day summary UI.
- Immutable retry/recovery và stale-balance UX.
- Frontend tests, integration hardening và real local E2E.

Việc chia 5A/5B chỉ là `PROPOSAL`. Không stage nào được phép implement cho tới khi Product Owner approve tài liệu và các Open Questions chặn.

## 15. Open Questions chặn approval/implementation

Chi tiết đầy đủ được ghi trong `OPEN_QUESTIONS.md`. Các câu hỏi Slice 5 thực sự cần Product Owner quyết định:

1. Cashier có được xem/thu Customer debt không, hay Owner-only?
2. Supplier debt payment và End-of-day/Estimated Gross Profit có chốt Owner-only không? Có cho Cashier xem phần summary không chứa profit không?
3. Collected trên UI dùng net sau refund làm headline hay hiển thị gross collected và refund tách riêng; proposal API giữ đủ components.
4. Store timezone lấy từ đâu, format nào và backfill/default cho Store hiện hữu ra sao?
5. Debt payment có cần optional note/reference text trong MVP không?
6. Sau khi đã có unallocated debt payment, Return/Sale Void/Purchase Void làm aggregate debt âm thì business behavior nào được phép, trong khi D-046 cấm credit/advance và Slice 5 không có payment allocation/refund-from-supplier workflow?

Câu 6 phải được giải quyết trước implementation vì nó ảnh hưởng invariant và concurrency của existing correction flows; không được clamp balance, xóa payment hoặc tự suy diễn allocation.

## 16. Definition of Done đề xuất sau approval

- Partial/full/multiple Customer/Supplier debt payments chạy Vue → API → SQL Server.
- No overpayment được chứng minh dưới concurrency khác OperationId.
- Exact retry/timeout recovery không duplicate actual money record.
- Completed transaction/payment history immutable; Store isolation và final role policy backend-enforced.
- Debt derived từ history, không editable balance source of truth.
- EOD dùng Store-local business date và `[startUtc, endUtc)`.
- Revenue khác Collected; supplier payments và ending debts có semantic rõ.
- Estimated Gross Profit chỉ dùng historical cost snapshot và phản ánh Return/Void.
- SQL Server integration, frontend tests và critical real local E2E pass; existing Slice 1–4 regression pass.
- Migration diff additive/reviewed, không auto-run production và không có unrelated schema changes.

## 17. Related decisions

- D-010–D-014 — immutable transactions, Payment/Debt, costing, idempotency và reporting projection.
- D-021 — Supplier/Purchase Owner-only boundary.
- D-025–D-030 — Sale payment, customer credit, historical cost và recovery.
- D-033–D-040 — Return/Void permission, financial effects, actual refund, safe Purchase Void và correction locking.
- D-043–D-050 — approved Slice 5 product scope/semantics.

Tài liệu này vẫn là `PROPOSED / PENDING PRODUCT OWNER APPROVAL`; không phải `APPROVED FOR IMPLEMENTATION`.
