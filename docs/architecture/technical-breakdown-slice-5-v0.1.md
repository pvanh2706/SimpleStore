# Technical Breakdown Slice 5 v0.1 — Debt + End-of-day

## Trạng thái

`APPROVED FOR IMPLEMENTATION`

Product Owner final-approved tài liệu ngày 2026-09-22 tại D-057. Implementation phải tuân thủ D-043–D-058. Stage 5A được Product Owner `APPROVED / COMPLETED` ngày 2026-09-23 tại D-058, approved baseline `49098e94c4f44acf3762f3e33ce6be3dfc00758f`. Stage 5B giữ `NOT STARTED / PLANNED` và chỉ bắt đầu theo implementation sequencing/review process của project; toàn bộ Slice 5 chưa completed.

## Mục tiêu và quy trình

Slice 5 hoàn thiện vòng vận hành tối thiểu về công nợ và câu hỏi cuối ngày mà không biến SimpleStore thành hệ thống kế toán. Trình tự bắt buộc sau approval:

`Domain behavior → DB changes → API contract → UI flow → Test cases → Implement`

Các quyết định D-001–D-057 tiếp tục được bảo toàn, đặc biệt:

- Completed Sale/Purchase/Return là immutable; correction dùng Return/Void/Reversal có audit.
- Payment chỉ là tiền thực nhận/thực trả; Payment không đồng nghĩa Revenue hoặc Debt.
- Critical mutation atomic, idempotent, có timeout/retry recovery.
- Mọi dữ liệu và query phải Store-scoped; backend là authorization boundary.
- Historical SaleLine cost snapshot là nguồn tính COGS lịch sử.
- End-of-day là query theo business date, không phải accounting close.
- Owner/Cashier authorization, net Collected, IANA Store timezone, immutable Note và negative-debt correction behavior tuân theo D-051–D-056.

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
- Theo D-056, Return giảm aggregate Customer debt trước và tạo exact actual refund cho phần Return obligation reduction vượt current aggregate debt. Sale Void không phải refund và bị reject nếu loại active Sale contribution làm aggregate debt âm.

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
- Theo D-056, Purchase Void bị reject nếu loại active Purchase contribution làm aggregate Supplier debt âm; không tự tạo supplier refund hoặc advance.

### 3.4 D-056 — correction trên aggregate debt

#### Customer Return

Sau khi validate remaining returnable quantity/value và existing actual refund history theo Slice 4, backend tính dưới Customer debt lock + Sale correction lock:

```text
ReturnObligationReduction = authoritative proposed Return value
CurrentAggregateCustomerDebt = derived Customer debt trước proposed Return

DebtReduction = min(ReturnObligationReduction, CurrentAggregateCustomerDebt)
RequiredActualRefund = ReturnObligationReduction - DebtReduction
EndingCustomerDebt = CurrentAggregateCustomerDebt - DebtReduction
```

Do các input không âm, `RequiredActualRefund >= 0` và `EndingCustomerDebt >= 0`. Ví dụ debt `30,000`, Return value `50,000` ⇒ debt reduction `30,000`, required refund `20,000`, ending debt `0`.

Đây là **một authoritative aggregate calculation**, không phải amount cộng thêm vào refund calculator Slice 4. Existing SalePayments, prior Returns, prior actual ReturnRefundPayments, CustomerDebtPayments và mọi active Sale của Customer đều tham gia derived `CurrentAggregateCustomerDebt`. Backend vẫn validate refund snapshot bằng actual payment history và remaining SaleLine financial cap trước khi tính proposed reduction. Exact `RequiredActualRefund` trở thành `Return.RefundAmount` và đúng một actual `ReturnRefundPayment` khi > 0; khi bằng 0 không tạo refund row.

D-056 mở rộng nguyên tắc obligation-first D-036 từ original Sale state sang Customer aggregate state khi có unallocated debt payments. Không double refund, không sửa historical payment và không allocation CustomerDebtPayment vào Sale. Return, lines, refund, inventory effects và BusinessOperation vẫn commit/rollback atomically.

Nếu OriginalSale không có Customer (chỉ hợp lệ khi Sale đã thanh toán đủ theo D-026), Sale đó không thể có CustomerDebtPayment. Return giữ nguyên authoritative sale-level D-036 calculation; không invent Customer hoặc Customer lock. Preview có thể trả `currentAggregateCustomerDebt = null` để phân biệt path này, còn required actual refund vẫn do backend tính từ original Sale payment/refund history.

#### Sale Void

Vì Sale có Return không được Void, candidate active contribution là original Sale obligation trừ original SalePayments. Dưới Customer debt lock:

```text
HypotheticalOutstandingAfterVoid =
    CurrentAggregateCustomerDebt - ActiveSaleDebtContribution
```

Nếu kết quả `< 0`, reject `customer-debt-would-become-negative`; response có current outstanding, hypothetical result/excess và suggestion dùng Return/refund flow phù hợp. Không clamp, không auto-create refund và không commit inventory/void/operation effect.

#### Purchase Void

Dưới Supplier debt lock:

```text
HypotheticalOutstandingAfterVoid =
    CurrentAggregateSupplierDebt - ActivePurchaseDebtContribution
```

Nếu kết quả `< 0`, reject `supplier-debt-would-become-negative`. Không tạo Supplier Advance hoặc implicit Supplier Refund; toàn bộ Purchase Void fail trước commit. Supplier refund/recovery flow nằm ngoài Slice 5.

### 3.5 Source of truth và performance

MVP đề xuất query/projection trực tiếp từ indexed transaction/payment/correction history. Không thêm mutable debt-balance column trong Stage 5A.

Nếu profiling sau này chứng minh cần materialized balance:

- transaction history vẫn là business source of truth;
- balance phải update trong cùng SQL transaction với mutation;
- phải có deterministic rebuild từ history;
- phải có consistency check so materialized với recomputed value;
- mismatch không được tự động ghi đè history.

Việc materialize là future optimization, không phải acceptance criterion Slice 5.

Stage 5A implementation review tại D-058 chốt rõ hai query semantic không được collapse:

- **Current/authoritative debt:** đọc toàn bộ committed transaction/payment/correction history, không dùng `TimeProvider.GetUtcNow()` hoặc clock instant khác làm artificial upper bound. DebtPayment, Return, Sale Void và Purchase Void dùng semantic này dưới party-level serialization.
- **Historical/as-of debt:** giữ strict `event timestamp < cutoff`. Boundary event đúng tại cutoff thuộc interval sau; semantic này chủ ý bảo toàn half-open `[startUtc, endUtc)` reporting cho Stage 5B.

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

**Failure Path** → Nếu balance đã giảm trước khi commit, request bị reject `customer-debt-changed` hoặc `debt-payment-exceeds-outstanding`, không tự đổi amount.

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

**System Behavior** → UI giữ immutable `OperationId + PartyId + Purpose + Amount + Method + ExpectedOutstandingAmount + normalized Note`. Client gọi `GET /api/operations/{operationId}`. Nếu Completed đúng type, load committed DebtPayment; nếu not found/unknown sau transient failure, retry exact snapshot; nếu same ID khác fingerprint/type, reject.

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

### 4.7 Customer Return sau aggregate debt payment

**User Action** → Owner chọn Return lines/quantities/Restock state và yêu cầu server preview.

**System Behavior** → Preview validate Slice 4 returnable/financial history rồi derive Customer aggregate debt. Response trả `ReturnObligationReduction`, `CurrentAggregateCustomerDebt`, `DebtReduction`, `RequiredActualRefund` và refund-method requirement. Preview không reserve state. Complete command dùng immutable correction intention, lấy Customer debt lock trước SaleCorrection/inventory locks, recompute toàn bộ values và chỉ dùng final authoritative result.

**Business Outcome** → Return giảm debt tới tối đa zero; excess trở thành đúng một actual ReturnRefundPayment. Ví dụ debt `30`, Return `50` ⇒ debt reduction `30`, refund `20`, ending debt `0`. Restock/NoRestock và historical-cost behavior không đổi.

**Failure Path** → Concurrent debt payment/Return/Void làm preview stale; refund method thiếu/thừa; refund snapshot/history inconsistent; return cap thay đổi hoặc transaction failure. Không tạo debt âm, double refund hay partial Return.

**Recovery Path** → Reload authoritative preview/context. Ambiguous Complete giữ exact OperationId/intention và dùng Slice 4 operation recovery; committed Return trả result cũ.

### 4.8 Sale Void với aggregate Customer debt

**User Action** → Owner yêu cầu Void Sale hợp lệ theo Slice 4.

**System Behavior** → Backend lấy Customer debt lock và SaleCorrection lock, recompute current aggregate debt và hypothetical balance sau khi loại active Sale contribution.

**Business Outcome** → Void chỉ commit nếu hypothetical debt `>= 0`; inventory/history/audit effects giữ nguyên semantics Slice 4.

**Failure Path** → Nếu hypothetical debt âm, reject `customer-debt-would-become-negative` trước mọi effect. Không auto-refund, không clamp và không tạo operation Completed.

**Recovery Path** → UI hiển thị current debt/excess và hướng dẫn dùng Return/refund flow phù hợp. Exact retry/recovery của một Void đã commit vẫn giữ semantics D-040.

### 4.9 Purchase Void với aggregate Supplier debt

**User Action** → Owner yêu cầu safe Purchase Void.

**System Behavior** → Ngoài eligibility/costing evidence Slice 4, backend lấy Supplier debt lock, recompute current debt và hypothetical balance sau Void.

**Business Outcome** → Void chỉ commit nếu hypothetical Supplier debt `>= 0` và mọi inventory/costing guard đều pass.

**Failure Path** → Hypothetical debt âm trả `supplier-debt-would-become-negative`; không tạo advance, supplier refund, Void hoặc inventory reversal.

**Recovery Path** → UI giải thích payment history đã làm Void không an toàn. Supplier refund/recovery workflow không được tự tạo trong Slice 5.

### 4.10 Concurrent debt payment và correction

Customer debt ban đầu `100`; CustomerDebtPayment `80` và Return obligation reduction `50` chạy đồng thời:

- payment thắng lock trước ⇒ debt còn `20`; Return recompute, giảm debt `20`, yêu cầu actual refund `30`, ending debt `0`;
- Return thắng lock trước ⇒ debt còn `50`, refund `0`; payment `80` recompute latest state và bị stale/overpayment reject.

Sale Void đồng thời CustomerDebtPayment và Purchase Void đồng thời SupplierDebtPayment cũng serialize trên party lock. Mọi serial order phải tạo một outcome hợp lệ hoặc typed rejection; không outcome nào được để debt âm và không dựa frontend balance.

### 4.11 End-of-day query

**User Action** → Owner chọn một business date.

**System Behavior** → Backend resolve Store timezone, chuyển local date thành `[startUtc, endUtc)`, query Store-scoped projections và trả metric + component breakdown + cost reliability. Query không mutate state hoặc tạo “day close”.

**Business Outcome** → Owner phân biệt Revenue, actual Collected, ending debts, Supplier Payments và Estimated Gross Profit.

**Failure Path** → Invalid date/timezone, unauthorized hoặc data-integrity inconsistency.

**Recovery Path** → Typed error; user sửa date hoặc Owner sửa Store timezone theo workflow được approve. Không fallback ngầm sang UTC/server-local timezone.

### 4.12 End-of-day với các event chính

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
- Note: string? // optional, max 250, canonical trimmed value
```

Invariants:

- CustomerDebtCollection ⇒ `Direction = MoneyIn`, CustomerId required, SupplierId null.
- SupplierDebtSettlement ⇒ `Direction = MoneyOut`, SupplierId required, CustomerId null.
- Amount positive, maximum two decimals, never exceeds current outstanding under lock.
- Store/party composite FK prevents cross-store reference.
- Completed payment immutable; no direct edit/hard-delete.
- OperationId unique and maps to one completed BusinessOperation result.
- No SaleId/PurchaseId/InvoiceId because D-047 forbids invoice allocation requirement.
- Note trim đầu/cuối; null, empty và whitespace-only canonical thành null/“không có note”; normalized Note immutable và tham gia fingerprint.
- Không có `Reference` field riêng trong Slice 5.

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
8. với Return, recompute exact debt reduction/refund; với Void, validate hypothetical ending debt không âm;
9. persist all effects + BusinessOperation;
10. commit.

Để debt linearizable, các operation làm thay đổi debt phải tham gia cùng party lock:

- Customer: CompleteSale có Customer, CreateReturn, VoidSale, CustomerDebtPayment.
- Supplier: CompletePurchase, VoidPurchase, SupplierDebtPayment.

Đây là extension kỹ thuật cho existing use cases, không làm Completed transaction mutable. Toàn repo phải giữ cùng lock order để tránh deadlock.

Read Committed + exclusive transaction-owned application lock đủ để serialize mutation của một party; không dựa riêng vào frontend, EF tracking hoặc rowversion. Unique constraints là defense-in-depth.

Return preview là read-only và không reserve lock/state sau khi response. CompleteReturn phải lấy locks và recompute authoritative aggregate values. Existing Slice 4 preview/intention binding, stale-preview invalidation và immutable retry tiếp tục áp dụng; client-provided preview totals không phải authority.

### 6.2 Exact retry

Fingerprint canonical gồm:

```text
operation type
+ StoreId
+ Store-resolved PartyId
+ amount G29
+ normalized PaymentMethod
+ expected outstanding G29
+ normalized Note (null/empty/whitespace => canonical null)
```

Note được trim/canonical-normalize trước cả persistence và fingerprint. Cùng OperationId với `" ghi chú "` rồi retry `"ghi chú"` là cùng normalized payload; cùng ID với Note khác là `idempotency-key-reused`.

- Same OperationId + same fingerprint + Completed → trả DebtPayment cũ và current committed result, `WasAlreadyRecorded = true`.
- Same OperationId + different type/payload → `idempotency-key-reused`.
- BusinessOperation, DebtPayment và debt-validation outcome commit atomically.
- Double-click reuse cùng immutable attempt/OperationId.
- Hai OperationId khác nhau luôn là hai intentions và phải qua current debt validation riêng.

### 6.3 Stale UI balance

Mutation request bắt buộc gửi `ExpectedOutstandingAmount` từ read response. Backend so với recomputed balance dưới lock:

- mismatch → `customer-debt-changed` hoặc `supplier-debt-changed` với latest outstanding trong ProblemDetails extension;
- amount lớn hơn latest → `debt-payment-exceeds-outstanding`;
- không tự co amount hoặc tự biến “full” thành amount mới.

UI reload balance, giải thích rằng công nợ vừa thay đổi và yêu cầu user xác nhận intention mới với OperationId mới. Exact ambiguous retry không bị stale-check lại theo state sau commit vì idempotency result được resolve trước validation.

### 6.4 Payment/correction race acceptance

- Customer debt `100`, concurrent payment `80` + Return reduction `50`: nếu payment trước thì Return refund `30`; nếu Return trước thì payment bị stale/overpayment reject. Ending debt không âm.
- CustomerDebtPayment + SaleVoid: serialize trên Customer lock; Void reject nếu order đó tạo hypothetical negative debt, hoặc later payment validate balance sau Void.
- SupplierDebtPayment + PurchaseVoid: serialize trên Supplier lock; Purchase Void reject nếu order đó tạo negative debt, hoặc later payment validate balance sau Void.
- Error/lock timeout không được commit partial refund, payment, Void, inventory effect hoặc BusinessOperation.

## 7. Proposed DB changes — chưa tạo migration

### 7.1 New table `DebtPayments`

- PK `Id`.
- `StoreId` required; FK `Stores` Restrict.
- `OperationId` required, unique.
- `Direction`/`Purpose` string tối đa 32.
- nullable `CustomerId` và `SupplierId` với composite Store FK, Restrict.
- `Amount decimal(18,2)` + check `Amount > 0`.
- `Method` string tối đa 32.
- `Note` nullable `nvarchar(250)`; persisted value đã trim, empty/whitespace canonical thành null.
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

### 7.2 Store timezone — approved requirement D-054

Current schema chưa có timezone. Proposed migration sau implementation approval thêm `Stores.TimeZoneId`:

- required, tối đa 128 ký tự;
- canonical persisted value là valid IANA timezone ID;
- existing Store backfill/default `Asia/Ho_Chi_Minh`;
- Store onboarding/settings cho phép cấu hình timezone;
- không persist Windows timezone ID như canonical value.

.NET/Windows runtime có thể cần conversion/mapping compatibility khi resolve IANA ID; đó là implementation detail ở boundary. API không được silently fallback sang browser timezone, server local timezone hoặc UTC khi configuration invalid.

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
  "expectedOutstandingAmount": 600000.00,
  "note": "Thu nợ ca sáng"
}
```

`note` optional, tối đa 250 ký tự; backend trim và canonical-normalize empty/whitespace thành null. Không có `reference` field riêng.

Response 200 cho first commit và exact retry, thống nhất với mutation convention hiện tại:

```json
{
  "id": "guid",
  "partyId": "guid",
  "direction": "MoneyIn",
  "purpose": "CustomerDebtCollection",
  "amount": 400000.00,
  "method": "Cash",
  "note": "Thu nợ ca sáng",
  "occurredAt": "2026-09-22T10:00:00+00:00",
  "performedByUserId": "guid",
  "outstandingBefore": 600000.00,
  "outstandingAfter": 200000.00,
  "wasAlreadyRecorded": false
}
```

Exact retry trả cùng payment/result reference và `wasAlreadyRecorded = true`; không tạo timestamp/id mới.

### 8.3 Return preview/command extension cho aggregate debt

`POST /api/returns/preview` tiếp tục dùng route/intent Slice 4 và bổ sung response:

```json
{
  "returnObligationReduction": 50000.00,
  "currentAggregateCustomerDebt": 30000.00,
  "debtReduction": 30000.00,
  "requiredActualRefund": 20000.00,
  "refundMethodRequired": true
}
```

`currentAggregateCustomerDebt`/`debtReduction` nullable cho customerless fully-paid Sale; path đó giữ D-036 sale-level calculation. Với Sale có Customer, các field aggregate bắt buộc có giá trị.

`POST /api/returns` không nhận authoritative refund amount. Command bổ sung `expectedAggregateCustomerDebt` và `expectedRequiredActualRefund` từ preview như optimistic intention-consistency tokens; cả hai tham gia Return fingerprint. Backend recompute dưới locks:

- expected values match ⇒ validate `RefundMethod`, create exact backend-calculated refund và commit;
- mismatch do concurrent debt/correction ⇒ reject `return-refund-requirement-changed`, trả latest calculation context và không commit;
- exact retry của operation đã Completed resolve trước recompute và trả committed Return.

Như vậy frontend không quyết định money amount nhưng không bị buộc phải chấp nhận một refund amount đã đổi sau preview. Existing Return line/Restock/refund-method intention và Slice 4 recovery semantics giữ nguyên.

Sale/Purchase Void request contract không nhận refund/advance field. Negative-debt conflict trả current outstanding, hypothetical ending amount, excess và stable suggestion code/data; human-readable suggestion không được frontend parse để điều khiển logic.

### 8.4 End-of-day response

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

### 8.5 Validation và typed errors

Validation:

- OperationId non-empty.
- Amount positive, tối đa hai decimals.
- Method chỉ `Cash`/`Transfer` hiện tại.
- ExpectedOutstandingAmount nonnegative, tối đa hai decimals.
- Note sau trim tối đa 250 ký tự; null/empty/whitespace đều hợp lệ và canonical thành null.
- ISO `YYYY-MM-DD` valid.
- Party phải thuộc current Store.

Stable error codes dự kiến:

- `customer-not-found`, `supplier-not-found` (cross-store dùng not-found semantics).
- `customer-has-no-outstanding-debt`, `supplier-has-no-outstanding-debt`.
- `invalid-payment-amount`, `invalid-payment-precision`, `invalid-payment-method`.
- `debt-payment-exceeds-outstanding`.
- `customer-debt-changed`, `supplier-debt-changed` với `latestOutstandingAmount` extension.
- `customer-debt-would-become-negative` cho Sale Void; extensions gồm `currentOutstandingAmount`, `hypotheticalOutstandingAmount`, `excessAmount`, `suggestedActionCode = use-return-refund-flow`.
- `supplier-debt-would-become-negative` cho Purchase Void; cùng numeric context và `suggestedActionCode = supplier-refund-not-supported`.
- `return-refund-requirement-changed` với latest aggregate debt/debt reduction/required refund context.
- `return-financial-state-invalid` cho actual refund history/snapshot inconsistency; `refund-method-required` và `refund-method-not-applicable` tiếp tục dùng theo Slice 4.
- `invalid-note-length`.
- `idempotency-key-reused`, `operation-lock-timeout`, `concurrent-update`.
- `invalid-business-date`, `store-timezone-not-configured`, `invalid-store-timezone`.
- `customer-debt-state-invalid`, `supplier-debt-state-invalid`.

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

Theo D-053, API trả components và headline `Collected = netAmount`. Sale Void không tự làm giảm Collected vì Void không phải actual refund theo D-038. Debt creation không tăng Collected và Collected không được dùng để suy ra Revenue.

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

- `Asia/Ho_Chi_Minh` business date;
- ít nhất một non-Vietnam IANA timezone;
- event ngay trước start không thuộc ngày;
- event đúng start thuộc ngày;
- event ngay trước end thuộc ngày;
- event đúng end thuộc ngày sau;
- timezone offset khác UTC;
- DST/ambiguous/invalid boundary nếu timezone được chọn có DST;
- browser timezone khác Store timezone không đổi kết quả;
- application-server timezone khác Store timezone không đổi kết quả.

Theo D-054, persisted source là `Store.TimeZoneId`, canonical IANA ID; Store hiện hữu backfill/default `Asia/Ho_Chi_Minh`. Algorithm phải hỗ trợ timezone khác và không assume ngày luôn dài 24 giờ.

## 11. UI flow proposal

Không redesign visual system và không xây BI dashboard.

### 11.1 Customer Debt

- Trang/list tìm Customer có debt, hiển thị name/phone/current outstanding/as-of.
- Chọn Customer mở debt card và payment form.
- Amount; shortcut “Thu toàn bộ”; PaymentMethod Cash/Transfer; optional Note tối đa 250 ký tự.
- Confirm dialog nêu đây là tiền thực thu và không tạo Revenue.
- Submit một lần với immutable OperationId snapshot; disable double-click.
- Success hiển thị amount, method, occurredAt và outstanding authoritative mới.
- Ambiguous state hiển thị “đang kiểm tra kết quả”, query operation và cho exact retry.
- `customer-debt-changed`/overpayment reload latest outstanding, không auto-submit amount mới.

### 11.2 Supplier Debt

- Tương tự Customer nhưng wording “tiền thực trả Supplier”.
- List và payment action chỉ available cho Owner theo D-052.
- Success không thay đổi Purchase value.
- Optional Note dùng cùng trim/length/immutable-attempt behavior.

### 11.3 End-of-day

Một trang summary đơn giản:

- date picker theo Store local date;
- Revenue;
- headline Collected = NetCollected, kèm breakdown Sale payments / Customer debt collected / Customer refunds / Net collected;
- Customer outstanding debt at end;
- Supplier payments, kèm purchase payment / old debt payment;
- Supplier outstanding debt at end;
- Estimated Gross Profit + cost reliability note.

Không chart builder, drill-down BI, saved dashboard hoặc accounting close. Có thể link sang existing Sale/Purchase/debt list nếu đơn giản, nhưng drill-down mới không phải blocker MVP.

### 11.4 Return/Void correction UX extension

- Return preview hiển thị Return obligation reduction, current aggregate Customer debt, debt reduction và required actual refund.
- Nếu required refund > 0, Owner phải chọn Cash/Transfer; UI không cho Cashier thực hiện Return theo D-033.
- Nếu aggregate state đổi sau preview, `return-refund-requirement-changed` reload preview/context và yêu cầu Owner xác nhận lại; không auto-accept refund amount mới.
- `customer-debt-would-become-negative` giải thích Sale phải dùng Return/refund flow phù hợp thay vì Void; không hiển thị Void như đã thành công.
- `supplier-debt-would-become-negative` giải thích Supplier refund/recovery chưa được hỗ trợ; không tạo advance ngầm.

## 12. Authorization — final Product Owner matrix

Backend authorize mọi endpoint; UI hide action chỉ là UX.

| Capability | Owner | Cashier |
|---|---|---|
| View Customer debt | Yes | Yes |
| Record Customer Debt Payment | Yes | Yes |
| View Supplier debt | Yes | No |
| Record Supplier Debt Payment | Yes | No |
| View End-of-day | Yes | No |
| View Estimated Gross Profit | Yes | No |

D-051/D-052 là final authorization decision cho Slice 5. Backend enforce; UI visibility chỉ hỗ trợ UX. Cashier direct URL/API tới Owner-only endpoint trả authorization error theo convention hiện tại. Không tạo Cashier-specific reduced EOD dashboard. Cross-store party/resource dùng Store-scoped not-found và không leak existence.

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

- Owner và Cashier đều đọc/thu Customer debt; unauthorized/wrong role ngoài matrix bị chặn.
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
- Owner Supplier debt read/pay allowed; Cashier read/pay forbidden; cross-store bị chặn.
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

### 13.7 Authorization matrix tests

- Owner Customer debt read/pay allowed.
- Cashier Customer debt read/pay allowed.
- Cashier Supplier debt read forbidden.
- Cashier Supplier Debt Payment forbidden.
- Cashier End-of-day forbidden.
- Cashier Estimated Gross Profit forbidden, kể cả direct URL/API.
- Owner Supplier debt/payment, End-of-day và Estimated Gross Profit allowed.
- Không tồn tại Cashier reduced-EOD endpoint/view.

### 13.8 Debt Payment Note tests

- null accepted/persisted null;
- empty và whitespace-only canonical thành null;
- leading/trailing whitespace được trim;
- exactly 250 characters accepted;
- >250 sau trim rejected `invalid-note-length`;
- retry same OperationId với cùng normalized Note trả payment cũ;
- retry `" note "` rồi `"note"` là exact normalized retry;
- same OperationId với different normalized Note trả `idempotency-key-reused`;
- response/history trả immutable normalized Note; không có Reference field.

### 13.9 Timezone tests

- `Asia/Ho_Chi_Minh` local-date boundaries;
- một non-Vietnam IANA timezone;
- event ngay trước/đúng UTC boundaries;
- DST timezone với ngày 23/25 giờ chứng minh không assume 24 giờ;
- browser timezone khác Store không đổi kết quả;
- app-server timezone khác Store không đổi kết quả;
- invalid IANA configuration trả typed error, không silent fallback;
- existing Store migration backfill `Asia/Ho_Chi_Minh` khi implementation được approve.

### 13.10 Return + aggregate Customer debt tests

- debt `30`, Return `50` ⇒ debt reduction `30`, required refund `20`, ending debt `0`;
- debt `50`, Return `50` ⇒ refund `0`, ending debt `0`;
- debt `100`, Return `50` ⇒ refund `0`, ending debt `50`;
- debt `0`, Return `50` ⇒ refund `50`, ending debt `0`;
- previous partial Returns và exact financial residual;
- previous CustomerDebtPayments;
- original SalePayment + previous actual refund + debt-payment interaction, không double refund;
- refund snapshot/payment mismatch trả `return-financial-state-invalid`;
- preview expected aggregate/refund mismatch trả `return-refund-requirement-changed`, không commit;
- refund method required/not applicable theo final authoritative amount;
- Restock/NoRestock quantity, inventory và historical COGS không bị financial logic làm thay đổi;
- Return/refund/inventory/BusinessOperation atomic rollback và exact retry.

### 13.11 Sale/Purchase Void aggregate-debt tests

- Sale Void allowed khi hypothetical Customer debt `>= 0`;
- Sale Void rejected `customer-debt-would-become-negative` khi `< 0`, không refund/void/inventory effect;
- concurrent CustomerDebtPayment + Sale Void theo cả lock order outcomes; final debt không âm;
- Sale Void exact retry/timeout recovery không đổi.
- Purchase Void allowed khi hypothetical Supplier debt `>= 0` và costing guards pass;
- Purchase Void rejected `supplier-debt-would-become-negative` khi `< 0`, không advance/refund/void/inventory effect;
- concurrent SupplierDebtPayment + Purchase Void theo cả outcomes; final debt không âm;
- Purchase Void exact retry/recovery và existing safety/dependency guards không đổi.

## 14. Approved implementation staging

### Stage 5A — Debt backend/domain/persistence/tests

**Implementation status:** `APPROVED / COMPLETED` tại D-058.

**Approved baseline:** `49098e94c4f44acf3762f3e33ce6be3dfc00758f`.

- Customer debt derived query.
- Supplier debt derived query.
- Shared DebtPayment domain model và persistence.
- Customer Debt Payment và Supplier Debt Payment use cases/APIs.
- Customer/Supplier debt locks, operation types, idempotency/fingerprint/recovery.
- Timeout/recovery và concurrent-operation handling.
- Extend existing debt-affecting mutations với canonical party lock order và D-056 correction integration cần thiết ở backend.
- Store timezone persistence/configuration foundation nếu Stage 5A schema foundation cần để Stage 5B tiếp tục an toàn.
- Reviewed schema/migration implementation và SQL Server/domain/API integration tests.

Stage 5A không tự mở rộng sang toàn bộ frontend hoặc End-of-day UI đã xếp tại Stage 5B.

### Stage 5B — End-of-day + frontend + E2E/recovery

**Implementation status:** `NOT STARTED / PLANNED`.

- Store configurable IANA timezone theo D-054.
- EOD aggregation/query và historical COGS reliability.
- Customer/Supplier debt UI.
- End-of-day summary UI.
- Immutable retry/recovery và stale-balance UX.
- Frontend tests, integration hardening và real local E2E.

Stage 5A/5B sequencing được approve tại D-057. Stage 5A implementation được final-approve tại D-058; Stage 5B vẫn planned, chưa được ghi nhận đã bắt đầu hoặc hoàn thành. Approval Stage 5A không đánh dấu toàn bộ Slice 5 completed.

## 15. Resolved Product Owner questions và approval gate

Sáu Product Owner questions chặn ban đầu đã được giải quyết đầy đủ:

- authorization: D-051/D-052;
- Collected presentation: D-053;
- Store timezone: D-054;
- Debt Payment Note/no Reference: D-055;
- correction sau unallocated debt payment: D-056.

Không còn Product Owner Open Question nào được biết đang chặn Slice 5. `OPEN_QUESTIONS.md` ghi các mục này là resolved. Product Owner đã cấp Technical Breakdown approval tại D-057 và Stage 5A Implementation Approval tại D-058. Stage 5A là `APPROVED / COMPLETED`; Stage 5B vẫn `NOT STARTED / PLANNED`, vì vậy toàn bộ Slice 5 chưa completed.

## 16. Definition of Done

- Partial/full/multiple Customer/Supplier debt payments chạy Vue → API → SQL Server.
- No overpayment được chứng minh dưới concurrency khác OperationId.
- Exact retry/timeout recovery không duplicate actual money record.
- Return dùng aggregate Customer state, tạo exact actual refund cho excess và không tạo hidden credit.
- Sale/Purchase Void reject khi hypothetical aggregate debt âm; không implicit refund/advance.
- Completed transaction/payment history immutable; Store isolation và final role policy backend-enforced.
- Debt derived từ history, không editable balance source of truth.
- EOD dùng configurable canonical IANA Store timezone và `[startUtc, endUtc)`.
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
- D-051–D-056 — approved authorization, net Collected, IANA timezone, immutable Note và correction/aggregate-debt behavior.
- D-057 — Technical Breakdown Slice 5 Approval và implementation sequencing.
- D-058 — Slice 5 Stage 5A Implementation Approval tại baseline `49098e94c4f44acf3762f3e33ce6be3dfc00758f`.

Tài liệu này là `APPROVED FOR IMPLEMENTATION` theo D-057. D-058 ghi nhận Stage 5A `APPROVED / COMPLETED`; Stage 5B vẫn `NOT STARTED / PLANNED` và toàn bộ Slice 5 chưa completed.
