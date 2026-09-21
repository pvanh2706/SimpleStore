# Technical Breakdown — Slice 4 v0.1

- **Slice:** 4 — Return / Void / Recovery
- **Trạng thái:** `PROPOSED / PENDING PRODUCT OWNER APPROVAL`
- **Ngày:** 2026-09-22
- **Approved business/technical decisions:** D-033–D-040
- **Implementation status:** `NOT STARTED`
- **Source of truth hiện hành:** Architecture v0.1, Domain Model v0.1, Development Plan v0.1 và các decision `APPROVED` trong `DECISIONS.md`.
- **Ranh giới:** Tài liệu này đề xuất implementation design để review; chưa cho phép bắt đầu production implementation và không thay đổi Step 1–11 hoặc Slice 0–3 đã approved.

## Outcome

Sau Slice 4, Owner có thể thực hiện ba correction flow có audit và recovery rõ ràng.

### Return

1. mở một Completed Sale chưa Void;
2. xem sold, previously returned và returnable quantity theo từng SaleLine;
3. chọn một hoặc nhiều line, nhập quantity và chọn Restock/NoRestock;
4. xem server-calculated return value, debt reduction và refund due;
5. chọn Cash/Transfer khi thực sự phải refund;
6. Complete Return atomically;
7. thấy inventory, financial projection và Return history nhất quán;
8. retry không tạo double Return.

### Sale Void

1. mở Sale chưa Void và chưa có Return;
2. nhập mandatory reason;
3. Void Sale;
4. giữ original Sale và original SalePayments;
5. đảo inventory effect và loại Sale khỏi active business contribution;
6. retry không tạo double Void.

### Purchase Void

1. mở Completed Purchase;
2. gửi yêu cầu Void với mandatory reason;
3. hệ thống reverse khi có trustworthy reversal basis và không có downstream dependency;
4. nếu không chứng minh an toàn, hệ thống reject rõ ràng và không mutate một phần;
5. original Purchase và original PurchasePayments vẫn tồn tại.

## Explicit scope

### Trong Slice 4

- Return, ReturnLine và ReturnRefundPayment.
- Link đến OriginalSale và OriginalSaleLine.
- Per-line Restock/NoRestock.
- Return financial/debt projection và actual refund.
- Cumulative quantity/value caps và deterministic rounding residual.
- Return idempotency, concurrency và recovery.
- Typed SaleVoid với reason/audit và full inventory reversal.
- Typed PurchaseVoid safe path.
- Purchase reversal basis cho Purchase được Complete từ Slice 4 trở đi.
- Conservative Purchase Void dependency check dựa trên persisted movement ordering/evidence.
- Reversal/audit relationships và read models.
- Owner-only mutation APIs và UI; Cashier read behavior giữ nguyên.
- Domain, SQL Server integration, frontend và critical real local E2E tests.

### Ngoài Slice 4

- Edit Completed Sale, Purchase hoặc Return.
- Hard-delete transaction.
- Void Return.
- Supplier return workflow.
- Arbitrary retroactive Purchase revaluation hoặc costing replay engine.
- Generic accounting ledger, generic correction/audit framework hoặc generic permission matrix.
- Customer/Supplier debt repayment.
- End-of-day, discount/promotion, C14, HĐĐT.
- Multi-warehouse, multi-branch hoặc Slice 5 implementation.

## Approved decisions applied

- D-033 — Owner-only Return/Sale Void/Purchase Void; backend authorization boundary.
- D-034 — Return được tạo trực tiếp Completed, immutable và tham chiếu original Sale/lines.
- D-035 — cumulative return cap và serialized Sale correction.
- D-036 — historical sale-price basis, obligation-first refund semantics và financial rounding cap.
- D-037 — explicit Restock choice, historical cost basis và inbound valuation safety.
- D-038 — typed Sale Void giữ original history, không fake refund.
- D-039 — conservative Purchase Void với exact pre-state reversal basis.
- D-040 — atomic idempotent operations, correction locks, shared inventory locks và audit.

## Architecture and project boundaries

Giữ Modular Monolith và dependency direction hiện tại:

- `SimpleStore.Domain`: typed Return/Void entities, value rules và pure calculations.
- `SimpleStore.Application`: commands/results/use cases, authorization orchestration qua current user, fingerprint và repository contracts.
- `SimpleStore.Infrastructure`: EF Core mappings, SQL Server transaction/applock, movement ordering queries và persistence.
- `SimpleStore.Api`: thin HTTP boundary, role attributes và typed ProblemDetails hiện có.
- Vue SPA: interaction state, server-backed context/preview, immutable attempt snapshot và display; không quyết định Store scope hoặc authoritative money/inventory values.

Không thêm MediatR, SharedKernel, generic correction engine hoặc generic repository abstraction. Có thể tạo `ISlice4Repository` và các helper nhỏ cho correction applock/fingerprint khi duplication thực sự xuất hiện.

## Domain model direction

### Return

Conceptual fields:

- `Id`
- `StoreId`
- `OriginalSaleId`
- `Status = Completed`
- `TotalReturnAmount`
- `RefundAmount`
- `CompletedByUserId`
- `CreatedAt`
- `CompletedAt`
- collection `Lines`
- optional collection `RefundPayments` với tối đa một row trong Slice 4

Không có Draft API/status. Factory `Return.Complete(...)` validate non-empty unique lines, positive quantities, financial totals, refund rule và immutable completed state. Không expose mutator sửa Completed Return.

### ReturnLine

Conceptual fields:

- `Id`
- `StoreId`
- `ReturnId`
- `OriginalSaleLineId`
- `ProductId`
- `Quantity` — decimal(18,3), > 0
- `Restock` — explicit bool/enum, không có unspecified persisted state; wire command nên dùng nullable/required field để phân biệt “chưa chọn” với explicit NoRestock
- `UnitSalePriceBasis` — decimal(18,2), từ OriginalSaleLine
- `ReturnLineAmount` — decimal(18,2), authoritative rounded/capped amount
- `UnitCostBasis` — decimal(18,4), từ OriginalSaleLine.UnitCostAtSale
- `RestockedInventoryValue` — decimal(18,2), 0 khi NoRestock

Unique `(ReturnId, OriginalSaleLineId)` bảo đảm một original line chỉ xuất hiện một lần trong một Return. Nhiều Return khác nhau vẫn được tham chiếu cùng OriginalSaleLine trong cumulative cap.

### ReturnRefundPayment

Actual money outflow dùng entity riêng, không reuse SalePayment money-in:

- `Id`
- `StoreId`
- `ReturnId`
- `Amount` — decimal(18,2), > 0
- `Method` — Cash hoặc Transfer
- `OccurredAt`
- `PerformedByUserId`

Slice 4 tạo đúng một row khi `RefundDueNow > 0`, với amount do backend tính. Khi refund bằng 0 không có row. Không có arbitrary refund amount từ client và không có refund debt payable.

### SaleVoid

Typed entity, không phải generic correction:

- `Id`
- `StoreId`
- `OriginalSaleId`
- `Reason`
- `VoidedByUserId`
- `VoidedAt`
- `CreatedAt`

Unique `OriginalSaleId` bảo vệ one-void-per-Sale ở database. Reason được trim, required và đề xuất max 500 ký tự. Original Sale không bị sửa/xóa; `IsVoided` là derived relationship.

### PurchaseLineReversalBasis

Technical/audit entity được tạo cho mỗi PurchaseLine trong cùng CompletePurchase transaction, sau khi balance đã lock và trước inventory mutation:

- `Id`
- `StoreId`
- `PurchaseId`
- `PurchaseLineId`
- `ProductId`
- `WarehouseId`
- `QuantityBefore`
- `InventoryValueBefore`
- `AverageCostBefore`
- `HasAverageCostBefore`
- `ReferencePurchaseCostBefore` nullable
- `ReferencePurchaseCostApplied`
- `PurchaseMovementId`
- `PurchaseMovementSequence`
- `CapturedAt`

Unique `PurchaseLineId`; vì Purchase hiện đã enforce one Product per Purchase, mỗi affected Product có đúng một basis. Basis là immutable evidence, không biến Completed Purchase thành mutable business transaction.

`ReferencePurchaseCostBefore/Applied` cho phép tránh để một voided Purchase tiếp tục làm cost fallback. Safe Void chỉ restore previous reference cost khi current value vẫn bằng applied value và không có later Purchase movement; nếu evidence không khớp thì reject thay vì overwrite trạng thái mới.

### PurchaseVoid

- `Id`
- `StoreId`
- `OriginalPurchaseId`
- `Reason`
- `VoidedByUserId`
- `VoidedAt`
- `CreatedAt`

Unique `OriginalPurchaseId`. Original Purchase/lines/payments và reversal basis được giữ immutable.

### BusinessOperation

Mở rộng typed operation names:

- `CreateReturn`
- `VoidSale`
- `VoidPurchase`

`ResultReference` trỏ lần lượt tới Return, SaleVoid hoặc PurchaseVoid. Existing status endpoint tiếp tục trả `OperationType` + `ResultReference`; authorization/store scope giữ nguyên. Không đổi semantics của CompleteSale/CompletePurchase.

### InventoryMovement

Thêm explicit movement types theo codebase convention:

- `ReturnRestock`
- `SaleVoid`
- `PurchaseVoid`

Mỗi movement có Store/Warehouse/Product, signed quantity/value delta, cost basis, typed source id, actor và occurred time. NoRestock không tạo movement. Không mutation balance nào thiếu movement.

Để Purchase dependency evidence không dựa vào timestamp hoặc current balance equality, đề xuất thêm database-generated monotonic `LedgerSequence bigint IDENTITY` và index `(StoreId, WarehouseId, ProductId, LedgerSequence)`. Existing rows nhận sequence chỉ để query/order kỹ thuật; không dùng sequence backfill để suy đoán trustworthy legacy pre-state. New Purchase basis lưu exact movement id/sequence tạo cùng transaction.

## Persistence and database integrity

Migration Slice 4 dự kiến:

- tạo `Returns`, `ReturnLines`, `ReturnRefundPayments`;
- tạo `SaleVoids`, `PurchaseVoids`, `PurchaseLineReversalBases`;
- mở rộng InventoryMovement type và thêm ledger ordering evidence;
- không recreate/drop các bảng Slice 1–3;
- không auto-run production migration khi startup.

Store-consistency dùng composite alternate/principal keys như các entity hiện tại:

- Return `(StoreId, OriginalSaleId)` → Sale `(StoreId, Id)`;
- ReturnLine `(StoreId, ReturnId)` → Return và `(StoreId, OriginalSaleLineId)` → SaleLine;
- ReturnLine `(StoreId, ProductId)` → Product;
- Refund `(StoreId, ReturnId)` → Return;
- SaleVoid `(StoreId, OriginalSaleId)` → Sale;
- reversal basis `(StoreId, PurchaseId/PurchaseLineId/ProductId/WarehouseId)` → đúng principals;
- PurchaseVoid `(StoreId, OriginalPurchaseId)` → Purchase.

SaleLine và PurchaseLine cần alternate key `(StoreId, Id)` nếu hiện chưa có để composite FK enforce Store consistency. Tất cả transaction/audit/user FKs dùng `DeleteBehavior.Restrict`; không cascade-delete business history.

Database constraints/indexes tối thiểu:

- Return totals/refund >= 0; refund <= total returned where applicable;
- ReturnLine quantity > 0, price/cost/value >= 0;
- RefundPayment amount > 0;
- unique `(ReturnId, OriginalSaleLineId)`;
- unique SaleVoid.OriginalSaleId và PurchaseVoid.OriginalPurchaseId;
- unique reversal basis PurchaseLineId;
- indexes cho Return history by OriginalSale/CompletedAt, original line aggregation, source movement, and Purchase dependency checks.

Database constraints hỗ trợ integrity nhưng cumulative cap/refund/dependency vẫn phải được tính dưới correction/inventory locks trong transaction.

## Return quantity and amount rules

Với mỗi OriginalSaleLine, query only Completed Returns của cùng Store/Sale:

```text
PreviouslyReturnedQuantity = sum(previous ReturnLine.Quantity)
RemainingQuantity = OriginalQuantity - PreviouslyReturnedQuantity

require RequestedQuantity > 0
require RequestedQuantity <= RemainingQuantity
```

Duplicate OriginalSaleLine trong command hoặc line không thuộc OriginalSale bị reject trước mutation. Validation được chạy lại sau khi giữ SaleCorrection lock; frontend state không có authority.

Financial amount:

```text
PreviouslyReturnedFinancialValue = sum(previous ReturnLine.ReturnLineAmount)
RemainingFinancialValue = OriginalLineAmount - PreviouslyReturnedFinancialValue

candidate = Round(
    RequestedQuantity * OriginalUnitSalePrice,
    2,
    MidpointRounding.AwayFromZero)

if RequestedQuantity == RemainingQuantity:
    ReturnLineAmount = RemainingFinancialValue
else:
    ReturnLineAmount = min(candidate, RemainingFinancialValue)
```

Require remaining values nonnegative; any corrupted/ambiguous aggregate causes conflict, không clamp âm. Kết quả bảo đảm cumulative quantity/value không vượt original và full cumulative Return đóng đúng OriginalLineAmount kể cả rounding residual.

## Return refund and Sale financial projection

Các con số trước current Return:

```text
OriginalCollected = sum(Original SalePayments)
PreviousRefunds = sum(actual ReturnRefundPayments của prior Returns)
NetCashHeldBeforeRefund = OriginalCollected - PreviousRefunds
```

Sau khi tính current line amounts:

```text
CumulativeReturnedValue = PreviousReturnedValue + CurrentReturnValue
NetSaleObligation = OriginalSaleTotal - CumulativeReturnedValue
RefundDueNow = max(0, NetCashHeldBeforeRefund - NetSaleObligation)

NetCashHeld = NetCashHeldBeforeRefund - RefundDueNow
Outstanding = max(0, NetSaleObligation - NetCashHeld)
```

Backend computes all values. Nếu refund > 0, RefundMethod phải là Cash/Transfer; nếu refund = 0, method phải null (đề xuất reject non-null bằng typed validation để fingerprint/intention không mơ hồ). Không nhận refund amount, outstanding hoặc totals từ client.

Sale detail/list projection sau Slice 4 giải thích:

- `OriginalTotalAmount`
- `TotalReturnedAmount`
- `NetSaleAmount`
- `OriginalCollectedAmount`
- `TotalRefundedAmount`
- `NetCollectedAmount`
- `OutstandingAmount`
- `IsVoided`, optional Void reason/actor/time
- Return history và per-line returnable state khi cần

Sale không Void:

```text
NetSaleAmount = OriginalTotalAmount - TotalReturnedAmount
NetCollectedAmount = OriginalCollectedAmount - TotalRefundedAmount
OutstandingAmount = max(0, NetSaleAmount - NetCollectedAmount)
```

Sale đã Void có active `NetSaleAmount`, active collection contribution và `OutstandingAmount` bằng 0. Original totals/payments vẫn hiển thị như historical facts. Không gọi active collection contribution là physical cash balance: Sale Void không tạo money-out và không giả lập refund.

## Return restock valuation

Original issued evidence lấy từ SaleLine snapshot và original Sale InventoryMovement:

```text
OriginalIssuedInventoryValue =
    Round(OriginalQuantity * UnitCostAtSale, 2, AwayFromZero)
```

Implementation nên verify movement source/value tương ứng và dùng exact absolute issued value làm cap. Với current Restock:

```text
PreviouslyRestockedValue = sum(previous ReturnLine.RestockedInventoryValue where Restock)
RemainingIssuedValue = OriginalIssuedInventoryValue - PreviouslyRestockedValue
candidate = Round(RequestedQuantity * UnitCostAtSale, 2, AwayFromZero)
```

- NoRestock: `RestockedInventoryValue = 0`, không mutate balance/movement.
- Restock partial: `min(candidate, RemainingIssuedValue)`.
- Chỉ khi cumulative restocked quantity bằng toàn bộ OriginalQuantity mới dùng exact `RemainingIssuedValue` để đóng residual. Nếu đã có NoRestock, quantity đó không được return/restock lần nữa nên không được dùng để phục hồi value cho hàng không quay lại kho.
- Cumulative restored value tuyệt đối không vượt original issued value.

Apply inbound balance transition dùng chung cho Purchase/Return/SaleVoid:

```text
Q1 = Q0 + InboundQuantity
V1 = V0 + InboundInventoryValue

QuantityOnHand = Q1
InventoryValue = V1

if Q1 > 0 && V1 >= 0:
    AverageCost = Round(V1 / Q1, 4, AwayFromZero)
    HasAverageCost = true
else if Q1 > 0 && V1 < 0:
    preserve numeric AverageCost
    HasAverageCost = false
else:
    preserve AverageCost
    preserve HasAverageCost
```

Nên đổi tên/refactor method domain nhỏ như `ReceiveInbound(...)` chỉ nếu Purchase/Return/Void thực sự chia sẻ cùng invariant; không tạo inventory framework mới. Không clamp InventoryValue, không persist negative authoritative AverageCost và không revalue historical Sale.

## CreateReturn command and preview boundary

Mutation command chỉ chứa business intention:

```text
OperationId
OriginalSaleId
Lines[]:
    OriginalSaleLineId
    Quantity
    Restock
RefundMethod? // Cash | Transfer
```

Không nhận StoreId, WarehouseId, ProductId, price, cost, line amount, refund amount, outstanding hoặc inventory value.

Để UI có server-calculated preview, đề xuất read-only `POST /api/returns/preview` với OriginalSaleId/lines (và optional refund method không ảnh hưởng amount). Preview trả authoritative calculation tại thời điểm đọc nhưng không reserve state; `POST /api/returns` luôn recompute dưới locks và có thể reject nếu concurrent correction đã thay đổi returnable state.

Fingerprint canonical gồm:

- OriginalSaleId;
- lines sort theo OriginalSaleLineId;
- exact decimal quantity canonical invariant culture;
- Restock flag;
- normalized RefundMethod hoặc null.

Line order từ UI không làm thay đổi fingerprint. Duplicate ids không được normalize away; phải validation fail.

## CreateReturn transaction and lock order

```text
1. resolve authenticated Owner + Store server-side
2. begin SQL transaction (ReadCommitted)
3. acquire OperationId applock
4. check BusinessOperation type + fingerprint
5. acquire transaction-owned SaleCorrection applock by OriginalSaleId
6. load Store-scoped OriginalSale, lines, SaleVoid, prior Returns/refunds
7. validate not voided, line ownership, cumulative quantities and values
8. calculate authoritative Return lines, obligation and RefundDueNow
9. validate RefundMethod against calculated refund
10. lock affected InventoryBalances sorted by ProductId for Restock lines
11. re-read/revalidate authoritative correction state if repository query caching could be stale
12. create Return + ReturnLines
13. create ReturnRefundPayment only when RefundDueNow > 0
14. apply inbound balance changes and positive ReturnRestock movements
15. add completed BusinessOperation
16. SaveChanges
17. commit
```

Operation lock precedes correction lock; correction lock precedes inventory locks; inventory locks always sort ProductId. Các Slice 2/3 inventory mutations đã lock theo ProductId nên ordering này tránh reverse lock order. Lock timeout là transient/ambiguous outcome; frontend giữ exact attempt và query operation status.

## Sale Void command and transaction

Command:

```text
OperationId
SaleId
Reason
```

Reason trim, required, max length; fingerprint chứa SaleId + exact normalized reason. Client không gửi financial/inventory amount.

Transaction:

```text
resolve Owner/Store
begin transaction
operation lock + existing operation/fingerprint check
SaleCorrection lock
load Store-scoped Sale, lines, returns and existing SaleVoid
reject if any Completed Return or already voided (unless exact idempotent result)
lock all affected balances by sorted ProductId
load/validate original Sale movements
create SaleVoid
for every SaleLine restore exact original issued quantity/value
create positive SaleVoid movement linked to original line/void
apply inbound Q/V/HasAverageCost safety
create BusinessOperation
commit
```

Sale Void uses exact original Sale movement value where available and verifies it against historical line basis; no current Product cost. Vì Sale có Return bị reject, full original issued value can close exactly. A concurrent Return and Void serialize on the same SaleCorrection resource; exactly one business outcome may commit.

Void does not add RefundPayment, delete original Payment or imply cash physically left the store.

## Purchase completion reversal-basis capture

CompletePurchase Slice 4 giữ current behavior và thêm capture trong existing atomic transaction:

```text
operation lock
load Draft Purchase
lock balances sorted ProductId
load products for update
for each line in ProductId order:
    snapshot balance pre-state and Product.ReferencePurchaseCost
    create Purchase movement with database ledger sequence
    apply ReceivePurchase and ReferencePurchaseCost
    persist PurchaseLineReversalBasis linked to movement
complete Purchase + payments + BusinessOperation
commit
```

Basis và Purchase effect phải commit/rollback cùng nhau. Nếu không tạo đầy đủ basis cho mọi line thì CompletePurchase fail; không tạo partially reversible new Purchase.

Migration không backfill guessed reversal basis cho legacy Purchases. Legacy Void mặc định reject `purchase-void-reversal-basis-unavailable`; chỉ một future explicitly reviewed proof path mới có thể nới, không suy đoán từ current balance.

## Purchase Void eligibility and transaction

Command:

```text
OperationId
PurchaseId
Reason
```

All conditions required:

- Store-scoped Purchase exists and is Completed;
- not already voided;
- reason valid;
- every line has complete trustworthy reversal basis;
- basis links exact original Purchase movement;
- no later InventoryMovement sequence exists for any `(StoreId, WarehouseId, ProductId)` after that line's Purchase movement;
- current balance equals the deterministic post-Purchase state implied by basis plus original Purchase movement;
- current ReferencePurchaseCost still equals captured applied value;
- all evidence/order is unambiguous.

Current-state equality là secondary integrity check, không thay movement dependency evidence. Any unsafe line rejects the entire Purchase Void.

Transaction:

```text
resolve Owner/Store
begin transaction
operation lock + idempotency check
PurchaseCorrection lock
load Purchase, lines, payments, basis, existing PurchaseVoid
lock balances sorted ProductId
requery/revalidate later movement evidence under locks
validate current post-state and reference-cost evidence
create PurchaseVoid
for each line create negative PurchaseVoid reversal movement
restore QuantityOnHand, InventoryValue, AverageCost, HasAverageCost exactly
restore ReferencePurchaseCostBefore
create BusinessOperation
commit
```

Không reverse AverageCost algebraically. Reversal movement deltas equal exact restored-before minus current state, and source links PurchaseVoid/original PurchaseLine. Original PurchasePayments stay historical; supplier outstanding/list/detail projections exclude voided Purchase contribution without creating fake payment.

## Idempotency and recovery

Backend semantics cho cả ba mutations:

- new OperationId → attempt mới;
- same OperationId + same normalized fingerprint + Completed → load typed existing result;
- same OperationId + different operation type/payload → `idempotency-key-reused`;
- operation result và all business effects commit atomically;
- database unique constraints are defense-in-depth, không thay operation/correction locks.

`BusinessOperation.OperationId` hiện là khóa chính toàn cục, nên operation applock resource nên được thống nhất thành `SimpleStore:BusinessOperation:{OperationId}` cho CompletePurchase, CompleteSale, CreateReturn, VoidSale và VoidPurchase. Điều này serialize việc reuse cùng GUID xuyên operation type và cho phép trả `idempotency-key-reused` deterministically thay vì race tới unique constraint. Thay đổi helper này không đổi fingerprint/result semantics Slice 2–3 và phải có regression tests.

Frontend giữ immutable attempt snapshot:

- Return: OperationId + OriginalSaleId + normalized Lines + RefundMethod/null;
- Void: OperationId + target id + normalized Reason.

Network error, HTTP 408, relevant 5xx và `operation-lock-timeout` là ambiguous/transient: query `/api/operations/{operationId}`, load result by type/reference if Completed, otherwise expose retry using exact snapshot. Không unlock form payload để reuse same ID và không tạo new ID khi old outcome ambiguous. Deterministic 4xx được hiển thị trực tiếp, không suy diễn success từ unrelated operation status.

## HTTP/API direction

Exact route names có thể theo existing controller conventions; proposed boundary:

### Return and Sale read

```text
POST /api/returns/preview
POST /api/returns
GET  /api/returns/{returnId}
GET  /api/sales/{saleId}/return-context
GET  /api/sales/{saleId} // extended correction projection/history
```

### Void

```text
POST /api/sales/{saleId}/void
POST /api/purchases/{purchaseId}/void
```

### Recovery

```text
GET /api/operations/{operationId}
```

Mutation endpoints Owner-only. Cashier retains Sale reads needed for operations/reprint but sees no correction mutation. Purchase access remains Owner-only. StoreId never accepted as data-scope authority; cross-store targets use not-found/store-scoped semantics.

## Typed error contracts

Return:

- `return-sale-not-found`
- `sale-already-voided`
- `return-lines-required`
- `duplicate-return-line`
- `return-line-not-from-sale`
- `invalid-return-quantity`
- `return-quantity-exceeds-remaining`
- `return-financial-state-invalid`
- `refund-method-required`
- `refund-method-not-applicable`
- `invalid-refund-method`

Sale Void:

- `sale-already-voided`
- `sale-has-returns`
- `void-reason-required`
- `void-reason-too-long`

Purchase Void:

- `purchase-already-voided`
- `purchase-void-reversal-basis-unavailable`
- `purchase-void-reversal-basis-invalid`
- `purchase-void-downstream-inventory-dependency`
- `purchase-void-reference-cost-dependency`

Shared:

- `idempotency-key-reused`
- `operation-lock-timeout`
- `concurrent-update`

ProblemDetails giữ format hiện tại. Frontend branch theo stable `code`, không parse human-readable detail.

## UI direction

Không redesign visual system.

### Sale detail

- Giữ original Sale/receipt/payment history.
- Hiển thị returned amount, net amount, refunded amount, outstanding, Return history và Void state/reason.
- Owner thấy `Trả hàng` khi còn returnable và Sale chưa Void; thấy `Hủy giao dịch` khi chưa Return/chưa Void.
- Cashier có thể xem permitted Sale state/reprint nhưng không thấy mutation actions.

### Return screen

- Original Sale và từng OriginalSaleLine.
- Sold, previously returned, returnable quantity.
- Requested quantity và required Restock/NoRestock choice.
- Server preview cho return value, debt reduction, RefundDueNow.
- Refund method chỉ xuất hiện/required khi refund > 0.
- Cảnh báo preview có thể thay đổi do concurrent correction; backend final response là authoritative.
- Sau success load Return/Sale authoritative; không optimistic mutate history.

### Purchase detail

- Owner thấy Void request action cho Completed Purchase chưa Void.
- Eligibility hint chỉ informational; backend có thể reject vì missing basis/downstream dependency.
- Hiển thị explicit dependency/basis error và giữ original history.

### Recovery UI

Reuse Slice 3 immutable snapshot/retry pattern. Disable inputs while submitting/ambiguous; status Completed loads result reference; deterministic validation unlocks form only for a new safe intention/OperationId.

## Authorization and tenancy

- Return preview/mutation and Sale/Purchase Void mutations require Owner.
- Cashier does not gain Purchase access or correction permission.
- Existing Sale read/reprint authorization remains Owner/Cashier.
- Every repository query includes resolved StoreId.
- Backend resolves Store/Warehouse/User; client does not select scope.
- Composite FKs prevent cross-store original/line/product references.
- Direct URL/API calls by Cashier must return forbidden even when UI hides actions.

## Testing requirements

### Domain tests

- Return references OriginalSale and every ReturnLine references its OriginalSaleLine.
- Non-empty, unique lines; positive quantity.
- Partial Return and multiple Returns inputs.
- Cumulative quantity cap calculation.
- Completed Return immutable.
- ReturnLine `AwayFromZero` amount rounding and cap.
- Final exact remaining Return closes original financial residual.
- Cumulative returned value never exceeds OriginalLineAmount.
- Restock uses UnitCostAtSale; NoRestock value is zero.
- Historical zero cost remains valid.
- Restock inventory residual cap/full close.
- Inbound Q/V/HasAverageCost cases, including negative residual value.
- Refund calculation: fully paid, partially paid, refund zero and refund positive.
- Mandatory/normalized Void reason.

### SQL Server integration tests

Return:

- Owner allowed; Cashier forbidden.
- Cross-store Sale and SaleLine rejected/not found; database FKs reject invalid Store combinations.
- Partial and multiple Returns; over-return rejected.
- Concurrent Returns cannot exceed quantity/value caps.
- Return vs Sale Void race yields exactly one valid outcome.
- Same OperationId concurrent creates one Return; reused ID/different payload conflicts.
- Refund zero creates no row; Cash/Transfer refund creates exact authoritative row.
- Credit Return reduces outstanding before refund.
- Restock changes stock/value/movement; NoRestock leaves inventory untouched.
- Negative inventory Restock transition and negative residual InventoryValue guard.
- Any failure rolls back Return/refund/movement/balance/operation.

Sale Void:

- normal Owner Void; Cashier forbidden; reason required.
- original Sale/lines/payments retained.
- exact inventory quantity/value reversal and positive movement.
- inbound AverageCost safety including zero/negative residual states.
- derived active net contribution/outstanding zero while history remains.
- double Void and exact operation retry semantics.
- Sale with Return cannot Void; Return after Void rejected.
- timeout/operation-lock recovery and Return/Void concurrency.

Purchase Void:

- newly Completed Purchase writes basis atomically.
- safe Void restores exact Q/V/AverageCost/HasAverageCost and reference cost.
- negative reversal movement exists; original Purchase/payments retained.
- supplier projection excludes voided obligation/payment contribution.
- downstream Sale, Purchase, ReturnRestock, SaleVoid or other later movement blocks Void.
- current balance equality after intervening net-zero movements does not bypass dependency check.
- one unsafe Product blocks multi-line Void.
- legacy/no/invalid basis rejected.
- same operation retry and concurrent Void protected.

Regression uses real SQL Server, not EF InMemory. Existing Product/import, Purchase, Sale, idempotency, Store isolation, shared locking, negative stock, `HasAverageCost`, print/reprint and authorization tests remain green.

### Frontend tests

- Returnable quantities and existing history display.
- Partial Return form and required Restock selection.
- Server preview/refund display; credit obligation reduced before refund.
- Refund method required only when refund > 0.
- Immutable snapshot retry/recovery, including operation-lock-timeout.
- Backend over-return/concurrent errors surfaced by stable code.
- Cashier has no Return/Void actions.
- Sale Void reason validation and history remains visible.
- Purchase Void dependency/basis rejection shown clearly.
- Return/Void success reloads authoritative original detail; does not mutate historical display locally.

### Real local E2E

Critical Return flow:

```text
Owner setup Product
→ Complete Sale
→ Return partial + Restock
→ verify inventory quantity/value increase
→ Sale detail shows Return/net amounts
→ second Return remaining quantity
→ attempt beyond sold quantity is rejected
```

Preferred Sale Void flow:

```text
Owner Complete Sale
→ Void with reason
→ verify inventory reversed
→ original Sale remains visible
→ Void state/reason visible
```

Purchase Void safe path có thể integration-test-heavy nếu browser orchestration không ổn định. Không mock correction success và không gọi API-only script là real Vue E2E. Nếu LocalDB/HTTPS/browser orchestration không phù hợp CI thì giữ documented local runner và báo rõ CI không chạy real E2E.

## Technical risks and review points

1. **Purchase dependency ordering:** current InventoryMovement chỉ có `OccurredAt` + GUID; không đủ chứng minh ordering khi timestamp trùng hoặc movements net về cùng balance. Proposed `LedgerSequence` + captured movement link giải quyết cho new Purchases; legacy mặc định reject.
2. **ReferencePurchaseCost reversal:** CompletePurchase hiện mutate Product.ReferencePurchaseCost ngoài balance. Breakdown đề xuất capture/restore previous value và reject khi current applied evidence không còn khớp; cần được review cùng migration/use case design.
3. **NoRestock residual:** NoRestock không restore inventory value và quantity đó không thể được return lần nữa. Chỉ close full inventory rounding residual khi cumulative restocked quantity thật sự bằng original sold quantity; không phân bổ value của discarded item sang item restocked.
4. **Void financial wording:** Sale/Purchase Void làm active business projection bằng zero nhưng không tạo actual money movement. UI/report phải tách historical paid/collected khỏi active contribution để không ngụ ý cash đã refund/thu hồi.
5. **Preview race:** server preview không reserve returnable state. Complete command luôn recompute dưới correction lock và có thể trả typed conflict sau concurrent Return/Void.
6. **Operation lock namespace:** current Slice 2/3 applock resources có operation-type prefix trong khi `BusinessOperation.OperationId` là global key. Slice 4 implementation nên hợp nhất resource theo OperationId như trên để cross-type reuse không race; exact-retry regression của CompletePurchase/CompleteSale phải tiếp tục pass.

Các điểm trên là implementation safety details trong phạm vi D-033–D-040, không phải business decision mới. Technical Breakdown vẫn cần Product Owner final approval trước implementation.

## Implementation sequence after approval

1. Domain entities/calculators và domain tests.
2. EF mappings, movement ordering evidence và reviewed migration.
3. Extend CompletePurchase atomic reversal-basis capture with regression tests.
4. Slice 4 repository, correction applocks, transaction/idempotency use cases.
5. SQL Server integration tests, ưu tiên races/dependency/rollback.
6. Thin APIs, typed errors và authorization.
7. Sale/Purchase read projections.
8. Vue Return/Void screens and immutable recovery.
9. Frontend tests and real local E2E.
10. Full restore/build/test/migration/diff/security review and CI.

## Definition of Done

Slice 4 chỉ được đề nghị implementation approval khi:

- Return, Sale Void và safe Purchase Void chạy thật Vue → API → SQL Server;
- Owner-only authorization và Store isolation được backend enforce;
- original Completed transactions/payments remain immutable/auditable;
- cumulative Return quantity/revenue/inventory-value caps giữ đúng dưới concurrency;
- refund là exact actual outflow và obligation/outstanding projection đúng;
- mọi inventory mutation có movement và dùng historical cost basis;
- Purchase Void chỉ chạy với trustworthy basis/no downstream dependency và exact restore;
- correction operation atomic/idempotent, ambiguous retry không duplicate;
- Return/Return và Return/Void races được SQL Server tests chứng minh;
- migration không recreate/drop business tables ngoài ý muốn và không auto-run production;
- regression Slice 1–3 pass;
- critical real local E2E được chạy/document;
- không implement debt repayment, end-of-day, generic ledger/correction framework hoặc Slice 5.

## Related decisions and documents

- D-010 — MVP User Flows, correction recovery và operation identity.
- D-011–D-013 — domain invariants, historical Return costing, Payment & Debt.
- D-014 — transaction/idempotency, inventory concurrency và Purchase reversal limitation.
- D-016 — Store tenancy foundation.
- D-023–D-032 — approved Slice 3 model/implementation foundation.
- D-033–D-040 — approved Slice 4 business/technical decisions.
- [Architecture v0.1](architecture-v0.1.md)
- [Domain Model v0.1](domain-model-v0.1.md)
- [Development Plan v0.1](development-plan-v0.1.md)
- [Technical Breakdown Slice 3 v0.1](technical-breakdown-slice-3-v0.1.md)

Slice 0–3 giữ nguyên trạng thái. Tài liệu này chỉ đưa Technical Breakdown Slice 4 tới `PROPOSED / PENDING PRODUCT OWNER APPROVAL`; Slice 4 production implementation chưa bắt đầu.
