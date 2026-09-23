# Technical Breakdown Slice 6 v0.1 — Understand & Act

## Trạng thái

`APPROVED FOR IMPLEMENTATION`

Product Owner đã approve tài liệu này tại D-073 ngày 2026-09-23, trên reviewed baseline `daffb39c8f4d75f8bae0d83ba12be0484ce99280`. Approval authorize sequencing Stage 6A/6B nhưng không có nghĩa implementation đã bắt đầu; Slice 6 implementation vẫn `NOT STARTED`.

Baseline đã inspect: `9f57a37ef97ab9f8f672b5309a42141ea6e267b8`, tại đó `Slice 5 — Debt + End-of-day` là `APPROVED / COMPLETED` theo D-060.

## Mục tiêu và trình tự

Slice 6 thêm một Owner experience trả lời nhanh “Hôm nay cửa hàng thế nào?”, giải thích được con số và thử nghiệm duy nhất một attention hypothesis: nguy cơ sắp hết hàng. Trình tự review/implementation tiếp tục là:

`Domain behavior → DB changes → API contract → UI flow → Test cases → Implementation stages`

Mọi quyết định D-001–D-060 tiếp tục được bảo toàn. D-069–D-072 đã resolve S6-Q1–S6-Q4; không còn known Product Owner blocker cho technical semantics. D-073 approve Technical Breakdown và implementation sequence mà không reopen D-061–D-072.

## 1. Trace Product Owner decisions

| Decision | Technical consequence |
| --- | --- |
| D-061 | Owner-only `/today`, default landing cho Owner, current Store-local business date, không date picker |
| D-062 | Summary tối thiểu; reuse Revenue/Collected/Gross Profit Slice 5; new-debt-created tách khỏi ending debt |
| D-063 | Typed `Summary → Vì sao? → Dữ liệu nguồn`; không parse text và không report builder |
| D-064 | C14 dùng đúng 7 completed Store-local business days, net Sale/Return/Void quantity, denominator 7 |
| D-065 | Fixed threshold `DaysOfCover <= 3`, factual stock state, risk state và insufficient-data behavior |
| D-066 | `Cần chú ý` tối đa 3 item, factual trước rồi lowest DaysOfCover, evidence mỏng và full list |
| D-067 | Product/Purchase transition chỉ giảm thao tác; Owner vẫn quyết định Supplier/quantity/commit |
| D-068 | C14-specific minimum immutable measurement; click không đồng nghĩa validated value |
| D-069 | New debt created dùng direct unpaid obligation theo transaction; same-day correction only; standalone DebtPayment excluded |
| D-070 | SaleCount đếm same-day Completed Sale không bị same-day Void; Return không giảm count |
| D-071 | Full-history risk coverage và earlier factual state với recent positive net-sales evidence |
| D-072 | C14 candidate set chỉ gồm active Product |

## 2. Scope

### 2.1 In scope

- Owner-only Today landing cho current Store-local business date.
- C12 summary: Revenue, net Collected, Estimated Gross Profit, Sale count và new Customer/Supplier debt created.
- C13 explainability từ summary/attention tới typed source evidence và existing detail khi phù hợp.
- C14 deterministic low-stock-risk experiment duy nhất.
- Current inventory + 7 completed-business-day sales velocity evidence.
- Tối đa 3 attention items trên Today và full attention list/detail.
- Product detail và Create Purchase transition với Product preselection hint.
- Minimum C14-specific experiment measurement.
- Domain/query, SQL Server integration, authorization/isolation, frontend và real E2E verification.

### 2.2 Explicitly out of scope

- AI, machine learning, forecasting engine, seasonality hoặc demand planning.
- Reorder quantity recommendation, Supplier recommendation, automatic Purchase hoặc auto-order.
- BI dashboard lớn, report builder hoặc historical Today dashboard.
- Generic rule engine, generic alert subsystem, notification center, background alerts.
- Push notification, email alert hoặc scheduled alert delivery.
- Analytics platform, generic event tracking, telemetry product hoặc data warehouse.
- New financial source of truth, accounting close/reopen, General Ledger hoặc net profit.
- Per-Store/per-Product C14 threshold configuration.
- Cashier-specific financial Today variant.

## 3. Repository baseline đã inspect

Baseline hiện có:

- `Store.TimeZoneId` là canonical IANA timezone và `BusinessDateWindow.Resolve` tạo hai independent local-midnight UTC boundaries.
- `GetEndOfDayReportUseCase` + `ISlice5BRepository.GetEndOfDayAsync` đã chốt Revenue, Collected, actual refunds, ending debt, Supplier payments và historical-cost Estimated Gross Profit.
- `Sale`, `Purchase`, `CustomerReturn`, `SaleVoid`, `PurchaseVoid`, payments và debt payments giữ timestamps/source references cần cho event-date projection.
- `SaleLine` giữ Product snapshot, quantity, price, historical unit cost và `CostReliability`.
- `ReturnLine` giữ original SaleLine, Product, quantity và restock-independent returned quantity.
- `InventoryBalance` là current materialized state theo Store + Main Warehouse + Product; `InventoryMovement` là audit ledger.
- `Product.CreatedAt` và `Store.CreatedAt` là authoritative coverage timestamps cho exact D-071 sufficiency gate.
- Owner-only reporting convention dùng backend role authorization; frontend `ownerOnly` chỉ hỗ trợ navigation UX.
- Purchase Create hiện yêu cầu Owner tự chọn Supplier, Product, quantity và unit price; preselection phải đi qua flow này, không bypass API rules.
- Chưa có experiment-event persistence hoặc generic analytics abstraction.

## 4. Today business-date authority

### 4.1 Current date

Backend lấy `nowUtc` từ application clock, load Store trong current-user scope, convert instant sang `Store.TimeZoneId`, rồi lấy `DateOnly` của Store-local time. Browser `new Date()` và OS/application-server local timezone không quyết định business date.

Today endpoint không nhận `date`. Nếu client truyền historical date thì contract không sử dụng; historical navigation đi tới Slice 5 End-of-day.

### 4.2 Today half-open window

Với Store-local `today`:

- `todayWindow = BusinessDateWindow.Resolve(today, Store.TimeZoneId)`;
- mọi event-date metric dùng `[todayWindow.StartUtc, todayWindow.EndUtc)`;
- event đúng `StartUtc` thuộc hôm nay;
- event đúng `EndUtc` thuộc ngày sau.

Hai local-midnight boundaries được resolve độc lập; không cộng fixed 24 giờ. Invalid/ambiguous midnight tiếp tục dùng typed Store/business-date error của Slice 5.

### 4.3 Seven completed business days

Cửa sổ C14 không chứa current/in-progress day:

- first completed date: `today - 7 days`;
- last completed date: `today - 1 day`;
- `velocityStartUtc = Resolve(today - 7).StartUtc`;
- `velocityEndUtc = Resolve(today).StartUtc`;
- query interval: `[velocityStartUtc, velocityEndUtc)`.

Đây là đúng 7 Store-local calendar dates dù UTC duration có thể khác 168 giờ qua DST. Denominator luôn là `7`, không phải số ngày có Sale.

## 5. Shared reporting projection và Today summary

### 5.1 Không tạo financial definition thứ hai

Không để Today controller tự viết lại Revenue/Collected/COGS SQL. Refactor proposal sau approval:

1. Tách Slice 5 calculation thành shared application reporting component, ví dụ `IDailyFinancialProjection`.
2. Component nhận `storeId`, `BusinessDateWindow`, trả typed daily financial data + source evidence khi được yêu cầu.
3. `GetEndOfDayReportUseCase` tiếp tục dùng component này và giữ response contract hiện hữu.
4. `GetTodayUseCase` dùng chính component đó cho current Store-local window.

Refactor này chỉ chia sẻ projection/calculation; không thay source-of-truth, không persist daily snapshot và không làm Today phụ thuộc HTTP call tới EOD endpoint.

### 5.2 Reused metrics

- **Sales Revenue:** Completed Sale trong Today window trừ Return và Sale Void event trong Today window, đúng D-048/D-049 và Slice 5.
- **Net Collected:** Sale Payments + Customer Debt Payments − actual Customer Refunds theo event timestamp trong Today window, đúng D-053.
- **Estimated Gross Profit:** net Revenue − historical SaleLine COGS, gồm exact Return restock value và Sale Void reversal, với worst `CostReliability` như Slice 5.

Today response có thể trình bày headline gọn nhưng không đổi calculation hoặc label Estimated.

### 5.3 New debt created — D-069 exact projection

Metric này là direct unpaid obligation do transaction mới trong Today window tạo ra, không phải current/ending outstanding debt và không phải net movement của aggregate debt.

Per-Sale Customer contribution:

1. Chọn Sale có `CompletedAt` trong `[startUtc, endUtc)`.
2. `BaseCustomerDebt = max(Sale.TotalAmount - sum(DirectSalePayments tied to Sale), 0)`.
3. Nếu Sale có SaleVoid với `VoidedAt` trong cùng window, contribution của Sale là `0`.
4. Nếu có Return của chính Sale với `CompletedAt` trong cùng window, `SameDayReturnObligationReduction = sum(Return.TotalReturnAmount)`. Toàn bộ `TotalReturnAmount` là correction của obligation do original Sale tạo ra, không phụ thuộc Return đã refund bằng tiền bao nhiêu.
5. `SaleCustomerDebtCreated = 0` nếu có same-day SaleVoid; nếu không, `SaleCustomerDebtCreated = max(BaseCustomerDebt - SameDayReturnObligationReduction, 0)`.
6. `CustomerDebtCreated = sum(SaleCustomerDebtCreated)`.

Per-Purchase Supplier contribution:

1. Chọn Purchase Completed có `CompletedAt` trong Today window.
2. `BaseSupplierDebt = max(Purchase.TotalAmount - sum(DirectPurchasePayments tied to Purchase), 0)`.
3. Nếu Purchase có PurchaseVoid với `VoidedAt` trong cùng window, contribution là `0`; nếu không, contribution là `BaseSupplierDebt`.
4. `SupplierDebtCreated = sum(PurchaseSupplierDebtCreated)`.

Hard boundaries:

- Customer/Supplier standalone `DebtPayment` luôn bị loại, kể cả occurred cùng ngày và bất kể xảy ra trước hay sau Return/Void; không FIFO, invoice allocation hoặc aggregate allocation ngầm.
- Same-day Return/Void chỉ giảm contribution của actual `OriginalSaleId`/`OriginalPurchaseId`; không net correction vào transaction khác.
- Nhiều same-day Return của cùng original Sale cộng toàn bộ `Return.TotalReturnAmount`, sau đó floor contribution của Sale tại `0`; phần correction vượt `BaseCustomerDebt` không chuyển sang Sale khác, không tạo credit và không tạo negative new-debt-created.
- D-056 vẫn giữ nguyên authoritative aggregate semantics của Return: actual aggregate debt reduction, required actual refund và không hidden Customer credit. Riêng projection D-069, `RefundAmount` **không** là input của new-debt-created: trường này chỉ tiếp tục phục vụ cash refund, Net Collected, aggregate correctness theo D-056 và Return explainability. Không lấy phần Return chưa refund làm transaction-local obligation correction.
- Correction khác business date không tham gia contribution của Today, không rewrite historical result và không tạo negative metric ở correction date.
- Cross-day correction vẫn hiện trong Revenue/Collected/COGS/correction explanation theo event-date semantics tương ứng, không overload new-debt-created.

Ví dụ bắt buộc: Sale `1,000,000`, direct Sale payment `0`, standalone Customer Debt Payment `1,000,000`, rồi same-day Return `300,000` với `RefundAmount` có thể là `300,000` thì Customer debt created của Sale vẫn là `700,000`. Standalone payment không đổi base/contribution; refund không quyết định Return obligation correction.

Explainability per Sale hiển thị original total, direct Sale payments, `BaseCustomerDebt`, tổng same-day Return obligation correction bằng `Return.TotalReturnAmount`, same-day Void exclusion và final contribution. Standalone DebtPayment không được allocate vào Sale; `RefundAmount` có thể xuất hiện trong Return/cash evidence nhưng không được trình bày như input quyết định contribution. Explainability per Purchase tương ứng hiển thị original total, direct Purchase payments, base, same-day Void exclusion và final contribution.

### 5.4 Sale count — D-070 exact projection

`SaleCount`:

```text
COUNT(Sale completed in [startUtc, endUtc)
      WHERE NOT EXISTS SaleVoid for that Sale in [startUtc, endUtc))
```

- Partial Return và full Return không giảm count.
- Same-day SaleVoid loại đúng original Sale khỏi count.
- Sale từ ngày trước bị Void hôm nay không tạo `-1` và không thuộc candidate Completed Sale hôm nay.
- Cross-day Void không rewrite historical count của ngày Sale.
- Explainability có hai typed sets: counted Completed Sales và same-day-voided Sales excluded from count. Excluded Sale mang contribution `0`, không phải negative count row.

## 6. Explainability — Summary → Vì sao? → Dữ liệu nguồn

### 6.1 Typed evidence

Evidence dùng enum/discriminator, không dùng localized message làm logic:

- `Sale`
- `CustomerReturn`
- `SaleVoid`
- `SalePayment`
- `CustomerDebtPayment`
- `ActualCustomerRefund`
- `Purchase`
- `PurchasePayment`
- `SupplierDebtPayment`
- `HistoricalCogs`
- `ProductInventory`

Một source row tối thiểu gồm `sourceType`, `sourceId`, `occurredAt`, typed contribution (`amount` hoặc `quantity`) và metadata trình bày tối thiểu. Frontend map `sourceType + sourceId` sang existing detail route khi route tồn tại; không build URL từ message.

### 6.2 Financial explanations

- Revenue: groups/totals cho Sale, Return, Sale Void và source transaction references.
- Collected: Sale Payment, Customer Debt Payment, Actual Customer Refund với direction rõ.
- Estimated Gross Profit: net Revenue, historical COGS, reliability và source Sale/Return/Void lines.
- New debt created: per-transaction direct unpaid obligation, full `Return.TotalReturnAmount` same-day obligation correction, Void exclusion và final nonnegative contribution theo D-069; standalone DebtPayment không xuất hiện như allocated source và `RefundAmount` không phải calculation input.
- Sale count: typed counted Sale và same-day-voided excluded Sale theo D-070; Return chỉ là related correction evidence, không thay count.

Pagination được dùng cho source list nếu vượt page size; Today summary response không nhúng toàn bộ lịch sử.

### 6.3 C14 explanation

Product detail explanation gồm:

- Product identity và current Main Warehouse stock;
- Store timezone;
- seven completed business dates và exact UTC boundaries;
- net sold quantity;
- `AverageDailySales = NetSoldQuantity / 7`;
- `DaysOfCover` khi average > 0;
- classification và data-sufficiency status;
- typed source Sale/Return/SaleVoid quantity rows hoặc daily grouped evidence.

Không copy transaction data sang analytics snapshot chỉ để giải thích.

## 7. C14 deterministic rule

### 7.1 Net sold quantity

Query trực tiếp approved transaction history trong `[velocityStartUtc, velocityEndUtc)`:

- `+ SaleLine.Quantity` khi parent Sale `CompletedAt` trong window;
- `- ReturnLine.Quantity` khi parent Return `CompletedAt` trong window, bất kể Restock/NoRestock;
- `- original SaleLine.Quantity` khi SaleVoid `VoidedAt` trong window;
- Purchase, Opening Balance, Purchase Void và inventory adjustment không tham gia velocity.

Event-date projection này làm correction của prior Sale có thể tạo negative contribution trong window. Tổng `NetSoldQuantity <= 0` không tạo risk vì `AverageDailySales > 0` là điều kiện bắt buộc. D-069/D-070 định nghĩa new-debt-created và SaleCount riêng; không suy hai metric đó từ C14 quantity.

### 7.2 Current stock

Current stock đọc `InventoryBalance.QuantityOnHand` cho current Store + Main Warehouse + active Product. Missing balance được xử lý như zero current stock trong query projection, nhưng D-071 vẫn yêu cầu recent positive net-sales evidence trước factual attention. Không cộng lại movement history để tạo current state cạnh tranh.

### 7.3 Data sufficiency

Evaluation dùng typed dimensions thay vì localized text:

- `historyCoverage`: `FullSevenCompletedDays` khi cả `Store.CreatedAt <= velocityStartUtc` và `Product.CreatedAt <= velocityStartUtc`; nếu không là `PartialObservation`.
- `recentSalesEvidence`: `PositiveNetSold` khi `NetSoldQuantity > 0`; nếu không là `NoPositiveNetSold`.
- `riskEvaluation`: `InsufficientFullHistory` khi coverage partial; nếu full history thì `Eligible` khi positive net sold, ngược lại `NoPositiveSalesEvidence`. `recentSalesEvidence` vẫn giữ evidence state riêng cho factual evaluation trong partial window.

Zero-sale days vẫn thuộc full observation và có quantity `0`; không yêu cầu Sale ở đủ 7 ngày hoặc N ngày. Store/Product created đúng `velocityStartUtc` được tính full coverage. Không giả định data trước `Store.CreatedAt`/`Product.CreatedAt`.

Factual stock attention không cần full history. Active Product với current stock `<= 0` và `PositiveNetSold` trong observable part của same 7-day window có thể flag dù Product mới tạo sau start. Current stock `<= 0` + `NoPositiveNetSold` không tạo attention.

### 7.4 Classification

Sau active-product filter:

1. `CurrentStock < 0` + `PositiveNetSold` → `NegativeStock` factual attention, kể cả partial history.
2. `CurrentStock == 0` + `PositiveNetSold` → `OutOfStock` factual attention, kể cả partial history.
3. `CurrentStock > 0`, full history, `AverageDailySales > 0`, `DaysOfCover <= 3` → `LowStockRisk`.
4. Các trường hợp còn lại không tạo strong attention; insufficient state có thể hiển thị neutral explanatory state ở full view.

`DaysOfCover` dùng decimal calculation, không round trước comparison. `== 3` phải flag. Rounding chỉ để presentation sau classification.

### 7.5 Ordering và limit

Deterministic ordering proposal không thêm business priority ngoài D-066:

1. factual category trước risk category;
2. trong factual category: Product name normalized ordinal, rồi ProductId;
3. trong risk category: exact DaysOfCover ascending, rồi Product name normalized ordinal, rồi ProductId.

Today lấy first 3 sau ordering. `totalAttentionCount` là tổng trước limit; nếu `> 3`, UI hiển thị `Xem tất cả X mặt hàng`. Alphabetical/ProductId tie-break chỉ đảm bảo stable presentation, không tuyên bố severity khác.

### 7.6 Active-only Product candidates

D-072 filter `Product.IsActive = true` trước mọi C14 classification. Inactive Product không xuất hiện trong Today preview, full list, detail/action entry hoặc experiment `SignalShown`, dù stock âm/zero, recent sales hoặc low DaysOfCover. Inactive-inventory anomaly detection là capability khác ngoài Slice 6.

## 8. Read model/query approach

### 8.1 Proposed application boundary

- `GetTodayUseCase`: resolves Store/current window, calls shared financial projection, summary projections và attention preview.
- `GetTodayExplanationUseCase`: validates typed metric and returns paged source evidence.
- `GetAttentionListUseCase`: full deterministic C14 results.
- `GetAttentionDetailUseCase`: one Store-scoped Product with formula/source evidence.
- `RecordC14ExperimentEventUseCase`: validates narrow event contract and appends immutable measurement row.

Repository queries remain read-only (`AsNoTracking`) except experiment event append. No read query obtains mutation locks or changes business state.

### 8.2 Query composition

Prefer SQL aggregation grouped by Product and typed event tables. Avoid loading all Store history into memory. Source detail can use separate paged queries. All joins include `StoreId`; source reference lookup must not leak cross-Store existence.

The Today orchestration may run independent read projections concurrently only if DbContext lifetime/query implementation supports it safely; otherwise run sequentially. Correctness and one semantic source are more important than parallel query micro-optimization.

### 8.3 Consistency model

Today is a live query, not accounting snapshot/close. Independent read statements may observe newly committed transactions between queries under normal read-committed semantics. Proposal should prefer one repository projection or explicit read transaction if cross-card point-in-time consistency is required during implementation review; do not persist duplicated totals.

## 9. Proposed DB changes — chưa tạo migration

### 9.1 Financial/C14 read model

No summary, daily snapshot, forecast or alert table is proposed. Existing transactional tables and `InventoryBalance` remain authoritative.

Query-plan review may justify additive indexes such as Store/time/Product paths for Sale/Return/Void source joins. Any index must be based on SQL Server execution evidence and included in a reviewed additive migration only after Technical Breakdown approval; this draft creates none.

### 9.2 Minimal `C14ExperimentEvents`

D-068 measurement cannot survive page/session boundaries reliably without server persistence. Proposal is one narrow append-only table/domain type, not a generic analytics framework:

- `Id` / client-generated `EventId` — unique idempotency key;
- `StoreId` — required tenant scope;
- `ActorUserId` — required Owner actor;
- `EventType` — closed enum/string: `TodayOpened`, `SignalShown`, `WhyOpened`, `PurchaseDraftStarted`;
- `ProductId` — nullable only for `TodayOpened`, required for the other three types;
- `BusinessDate` — Store-local Today date attached by backend, not trusted from browser;
- `OccurredAt` — UTC application-clock timestamp;
- optional `AttentionKind` closed enum for shown/why/purchase events, if needed to distinguish factual/risk during pilot analysis.

Constraints/index proposal:

- PK/unique `Id` prevents retry duplicate;
- FK Store and Actor; optional Store-scoped Product reference with restrict behavior;
- check/validation for Product requirement by event type;
- index `(StoreId, OccurredAt)` and `(StoreId, EventType, OccurredAt)`;
- immutable after insert; no update/delete UI;
- no JSON payload, generic event name or arbitrary properties.

Recording failure must not block Today read or Purchase business flow. Frontend may show no user-facing business error for best-effort experiment telemetry, but implementation must log/debug failure and never fabricate success evidence.

## 10. Draft API contract

All endpoints below are Owner-only and Store-scoped. Names are proposal, not implemented contract.

### 10.1 `GET /api/today`

No date parameter.

```json
{
  "businessDate": "2026-09-23",
  "timeZoneId": "Asia/Ho_Chi_Minh",
  "startUtc": "2026-09-22T17:00:00Z",
  "endUtc": "2026-09-23T17:00:00Z",
  "summary": {
    "salesRevenue": 0,
    "netCollected": 0,
    "estimatedGrossProfit": {
      "amount": 0,
      "costReliability": "Reliable"
    },
    "saleCount": 0,
    "customerDebtCreated": 0,
    "supplierDebtCreated": 0
  },
  "attention": {
    "riskEvaluationStatus": "Sufficient",
    "totalCount": 0,
    "items": []
  }
}
```

`saleCount` follows D-070. `customerDebtCreated`/`supplierDebtCreated` follow D-069 and never use standalone DebtPayment; `customerDebtCreated` also never uses `RefundAmount` as a calculation input. `riskEvaluationStatus` is typed (`Sufficient`, `InsufficientStoreHistory`, `PartiallyInsufficientProductHistory`) so UI does not infer sufficiency from text.

### 10.2 `GET /api/today/explanations/{metric}`

Allowed typed metric values are a closed set such as `revenue`, `collected`, `estimated-gross-profit`, `sale-count`, `customer-debt-created`, `supplier-debt-created`. Response contains summary total, component totals, reliability when relevant and paged typed sources.

Unknown metric returns typed validation error. Frontend never sends/branches on localized label.

### 10.3 `GET /api/today/attention`

Returns full ordered list, evaluation coverage and seven-day window. Pagination may be added for bounded responses without changing ordering.

Attention item proposal:

```json
{
  "productId": "...",
  "productName": "Coca",
  "sku": "COCA",
  "unit": "chai",
  "kind": "LowStockRisk",
  "currentStock": 6,
  "netSoldQuantity": 28,
  "averageDailySales": 4,
  "daysOfCover": 1.5,
  "historyCoverage": "FullSevenCompletedDays",
  "recentSalesEvidence": "PositiveNetSold",
  "riskEvaluation": "Eligible"
}
```

Only active Products are returned. For factual zero/negative stock, `daysOfCover` may be null and `historyCoverage` may be `PartialObservation`; `PositiveNetSold` remains required. UI must not represent factual state as forecast.

### 10.4 `GET /api/today/attention/{productId}`

Returns the item plus exact Store-local dates/UTC boundaries, calculation inputs, typed daily/source evidence and allowed navigation targets. Cross-Store/nonexistent Product returns Store-scoped not-found.

### 10.5 `POST /api/experiments/c14/events`

```json
{
  "eventId": "client-generated-guid",
  "eventType": "WhyOpened",
  "productId": "..."
}
```

Backend derives Store, actor, business date and occurred time. Same `eventId` + same normalized identity is idempotent; reuse with different identity returns typed conflict. This endpoint records no business transaction and cannot mutate Product, stock or Purchase.

## 11. UI flows

### 11.1 Owner login/default routing

- `/today` is `ownerOnly`.
- Authenticated Owner with Store and no explicit safe redirect lands on `/today`.
- Cashier never defaults to `/today`; current operational route remains until a separate approved Cashier landing decision changes it.
- Explicit authorized redirect after login remains honored.
- Direct Cashier URL is blocked by router UX and backend returns authorization failure.

### 11.2 Today page

- Header: `Hôm nay cửa hàng thế nào?`, Store-local date and timezone.
- No date picker.
- Summary cards: Revenue, Net Collected, Estimated Gross Profit + reliability, Sale count, Customer/Supplier new debt.
- Each metric offers `Vì sao?` leading to typed explanation/breakdown.
- Link to historical End-of-day is explicit and separate.
- `Cần chú ý` displays max 3 ordered items, count/link to full list, or narrow neutral/insufficient wording.

Unavailable cost remains visibly estimated/unavailable as Slice 5; Today must not relabel it as accounting profit.

### 11.3 Attention detail

- Show Product, classification, stock, exact 7 completed dates, net quantity, average/day, days of cover and sufficiency.
- `Xem vì sao` reveals formula and source evidence, not a prose-only assertion.
- `Xem sản phẩm` navigates existing Product detail.
- `Tạo phiếu nhập` navigates to `/purchases/new?productId={id}` and records `PurchaseDraftStarted` interaction.

### 11.4 Purchase preselection

Create Purchase treats query ProductId as a convenience hint:

- load via Store-scoped active Product query;
- preselect Product identity only;
- leave Supplier unselected;
- require Owner to enter quantity and unit price; do not prefill a “recommended” quantity;
- submission still uses existing Purchase Draft API/domain validation;
- invalid/inactive/cross-Store Product never leaks data and cannot bypass normal search/business rules.

No Purchase is created by merely navigating from C14.

## 12. Authorization and Store isolation

- Today summary, explanations, C14 list/detail and event endpoint: `[Authorize(Roles = Owner)]`.
- Use `ICurrentUser`/Store guard before every query/write.
- Every financial, Product, InventoryBalance, Sale/Return/Void and event query filters current `StoreId`.
- Main Warehouse comes from current Store setup, never client input.
- ActorUserId/StoreId/time/business date on experiment event come from authenticated server context.
- Frontend visibility/default route is not the security boundary.
- Cross-Store Product/source/event references return scoped not-found or authorization-safe response without revealing existence.

## 13. Experiment measurement semantics

### 13.1 Event meaning

- `TodayOpened`: một successful user-visible Today page presentation/view instance; không emit lại do reactive render, computed recalculation, loading transition, unrelated state update hoặc refresh dữ liệu trong cùng view instance.
- `SignalShown`: một active Product attention item thực sự được trình bày visible trong view; mỗi `ProductId + AttentionKind` emit tối đa một lần trong một Today view instance.
- `WhyOpened`: explicit user action mở C14 explanation cho Product.
- `PurchaseDraftStarted`: explicit user action chọn `Tạo phiếu nhập` từ Product's C14 flow.

Frontend tạo per-view ephemeral exposure guard:

- boolean/identity cho `TodayOpened`;
- `Set<ProductId + AttentionKind>` cho `SignalShown`;
- chỉ add/emit sau successful data state và item thuộc visible presented list;
- không emit từ generic DOM/Vue render hook;
- reactive re-render, parent re-render hoặc unrelated state update không clear guard;
- page reload, navigation away/back hoặc independently mounted/loaded Today view tạo guard mới và event identities mới.

Không cần analytics-session framework hoặc persisted view session. Client-generated EventId bảo vệ network retry của một exposure/action; same EventId retry không duplicate DB row. Một later genuine view/action dùng EventId mới. Event count là descriptive telemetry, không phải business outcome.

### 13.2 Non-interpretation rule

No automated code labels C14 validated from CTR/count. Pilot/research must separately evaluate discovery, trust, decision influence, continued usage and willingness-to-pay. No dashboard beyond technical/pilot extraction explicitly approved later.

## 14. Error handling, performance and operational behavior

- Invalid Store timezone/business window uses stable typed code; no fallback to browser/server timezone.
- Explanation metric, event type and Product-reference validation use stable `ProblemDetails.code`.
- Financial source/evidence pagination is bounded.
- C14 aggregates only 7 completed days, current Store and Main Warehouse.
- Exact decimal values drive threshold/order; display rounding is separate.
- No background job/cache is required for MVP. If query performance is insufficient, inspect SQL/query plan before adding indexes or cache.
- Experiment-event write is isolated from business mutation; failure không block Today presentation, Product navigation hoặc Purchase flow và không được ảnh hưởng correctness của business transaction.
- Logs include correlation, typed error and Store-safe identifiers without sensitive arbitrary payload.

## 15. Test plan

### 15.1 Domain/application time-window tests

- Store-local Today derived from UTC clock in `Asia/Ho_Chi_Minh` and `America/New_York`.
- Browser/server timezone difference does not change business date.
- Today exact `[startUtc, endUtc)` boundaries.
- Seven completed dates exclude current day.
- DST spring/fall windows use independent midnight conversion, not fixed 24/168 hours.
- invalid/ambiguous Store timezone boundary returns stable error.

### 15.2 Today summary consistency

- Same Store/date data gives identical Revenue, net Collected, Gross Profit amount and `CostReliability` through Today and Slice 5 EOD projection.
- Sale Payment, Customer Debt Payment and actual refund component consistency.
- Return Restock/NoRestock and Sale Void historical COGS behavior unchanged.
- Empty Today produces zero financial summary and appropriate reliability.
- Sale Completed today → SaleCount `+1`; Sale exactly at start included; exactly at end excluded.
- Sale Completed today + partial Return today → count remains `1`.
- Sale Completed today + full Return today → count remains `1`.
- Sale Completed today + same-day SaleVoid → count `0`, with typed exclusion evidence and no negative row.
- Sale yesterday + SaleVoid today → Today count unchanged, no `-1`, historical count not rewritten.
- Customer base debt is `SaleTotal - DirectSalePayments`; Supplier base debt is `PurchaseTotal - DirectPurchasePayments`.
- Sale `1,000`, direct Sale payments `800`, same-day Return `100` → Customer debt created `100`.
- Sale `1,000`, direct Sale payments `800`, same-day Return `500` → Customer debt created `0`, không negative/credit.
- Sale `1,000`, direct Sale payments `0`, standalone Customer Debt Payment `1,000`, rồi same-day Return `300` (kể cả `RefundAmount = 300`) → Customer debt created `700`.
- Cùng dữ liệu trên nhưng standalone Customer Debt Payment xảy ra sau Return → Customer debt created vẫn `700`.
- Customer base debt `1,000`, hai same-day Return `200` và `300` của cùng original Sale → Customer debt created `500`.
- Prior-day Sale, Today Return `300` → không tạo `-300` Today và không rewrite new-debt-created của ngày hôm qua.
- Cùng một `Return.TotalReturnAmount` với các `RefundAmount` khác nhau tạo cùng new-debt-created contribution; full `Return.TotalReturnAmount` chỉ giảm original Sale cùng business date.
- Same-day SaleVoid/PurchaseVoid zeroes only original transaction contribution; standalone Customer/Supplier DebtPayment before or after the Void does not change that result.
- Standalone Customer/Supplier DebtPayment, including same-day payment, never reduces new-debt-created and is never allocated in explanation.
- Cross-day Return/SaleVoid/PurchaseVoid neither rewrites original-day new debt nor creates negative new debt on correction date.
- Multiple same-day Returns/corrections remain bounded by original transaction contribution; no hidden credit or negative total.

### 15.3 C14 query/domain tests

- Exact Sale start/end for seven-day velocity window.
- Current-day Sale excluded.
- SaleLine quantity increases net sold.
- Return quantity decreases net sold for Restock and NoRestock.
- Sale Void reverses original quantity by Void event date.
- Purchase/Opening/PurchaseVoid/adjustment do not affect velocity.
- Store/Product `CreatedAt <= velocityStartUtc` gives full seven-day observation; zero-sale days count and denominator remains `7`.
- One/few Sale days across seven completed dates still use denominator `7`.
- zero/negative net velocity yields no DaysOfCover risk.
- Product created after velocityStart yields `PartialObservation` and no strong LowStockRisk.
- Store created after velocityStart yields insufficient full history and no LowStockRisk.
- active recent Product + positive net sold + stock `== 0` yields factual OutOfStock even without full Product history.
- active Product + positive net sold + stock `< 0` yields factual NegativeStock even without full Product history.
- stock `<= 0` + no positive net sold yields no factual attention.
- `DaysOfCover == 3` flags.
- `DaysOfCover < 3` flags.
- `DaysOfCover > 3` does not flag.
- decimal comparison occurs before display rounding.
- deterministic factual/risk ordering and ProductName/ProductId ties.
- Today preview max 3 and full count/list consistent.
- no signal when rule is not satisfied.
- inactive Product never appears in factual/risk attention or C14 action even with stock `<= 0`, recent sales or low DaysOfCover.

### 15.4 SQL Server integration/security tests

- Aggregation translates/runs against SQL Server, not EF InMemory.
- Store A cannot read Store B Today, explanations, inventory or Product evidence.
- Owner allowed; Cashier/anonymous forbidden for every Slice 6 endpoint.
- Main Warehouse/current InventoryBalance selected correctly.
- Source references cannot cross Store.
- query uses half-open boundaries at SQL precision.
- shared financial projection regression against Slice 5 integration cases.
- D-069 Sale/direct-payment/Return/standalone-payment matrix in 15.2 executes against SQL Server and reconciles summary with per-Sale explanation, including payment-before-Return, payment-after-Return, multiple Return and cross-day cases.
- proposed additive indexes/migration, if approved, upgrade safely from Slice 5 schema.

### 15.5 Explainability/navigation tests

- Revenue/Collected/Gross Profit components reconcile exactly to headline.
- New-debt explanation exposes original total, direct payments, base, full same-day `Return.TotalReturnAmount` correction, Void exclusion and final contribution; UI never derives contribution from `RefundAmount` or standalone DebtPayment.
- typed source kinds/ids map to correct existing detail routes when available.
- no client logic parses title/detail/localized message.
- C14 evidence reconciles quantity/window/formula.
- Product and Purchase links preserve only authorized Store-scoped Product identity.
- Purchase preselection leaves Supplier/quantity decision to Owner and does not auto-create draft.

### 15.6 Experiment-event tests

- four closed event types and Product-required rules.
- Store, actor, business date and timestamp derived server-side.
- exact same EventId retry does not duplicate.
- EventId reuse with different identity rejects.
- immutable row; no update/delete endpoint.
- Cashier/cross-Store Product rejected.
- event-write failure does not mutate/block Today, Product navigation, inventory or Purchase flow.
- one successful visible Today view emits one `TodayOpened`; reactive/loading/unrelated rerenders do not duplicate it.
- initial visible Product attention emits exactly one `SignalShown`.
- reactive component/parent rerender keeps exactly one SignalShown for the same Product/attention identity.
- unrelated state update keeps exactly one SignalShown.
- multiple different visible Products emit one SignalShown per Product/attention identity.
- genuine reload/navigation-away-and-back/new independently loaded view can emit new exposure identities.
- network retry reuses exact same EventId and persists one row.
- `WhyOpened` and `PurchaseDraftStarted` emit only from explicit user actions.

### 15.7 Frontend and real E2E

- Owner default login landing `/today`; explicit redirect preserved.
- Cashier not routed to/allowed Today.
- Today has no date picker and shows Store date/timezone.
- summary labels/reliability and explanation flows.
- attention max 3, full-count link, full/partial-history, factual/risk/no-positive-evidence/neutral states and active-only filter.
- Product detail and preselected Purchase transition with no Supplier/quantity recommendation.
- real E2E: seed exact Today Sale/Purchase/direct payments/standalone DebtPayments/same-day and cross-day corrections, including the `1,000,000 - 0 - 300,000 = 700,000` required case; verify D-069/D-070 summary and explanations and prove refund/payment timing does not change new-debt-created.
- real E2E: seed 7 completed Store-local days, partial Store/Product history, active/inactive Products, Sale/Return/Void quantity and stock; verify D-071/D-072 attention, action transition and deduplicated measurement rows through Vue → API → SQL Server.
- full Slice 1–5 backend/frontend/real-flow regressions remain green; no test deletion/skip to force green.

## 16. Approved implementation staging — D-073

### Stage 6A — Today/C12/C13 reporting foundation

`IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`:

- shared daily financial projection reused by EOD/Today;
- current Store-local date/window service orchestration;
- Today summary and typed financial explanations;
- Owner `/today` route/default landing;
- D-069 new-debt-created và D-070 SaleCount projections/explainability;
- domain/SQL integration/frontend tests.

Implementation handoff: EOD và Today dùng chung `DailyFinancialProjection`; Today business date được resolve từ injected `TimeProvider` + canonical Store IANA timezone; `GET /api/today` và bounded `GET /api/today/explanations/{metric}` là Owner-only; frontend `/today` không có date picker/C14 placeholder. Local verification pass Domain 78/78, SQL Server integration 82/82, frontend 89/89 và backend/frontend Release build; không có schema/migration change. Trạng thái này không phải Product Owner approval.

### Stage 6B — C14/action/measurement/E2E

`APPROVED FOR IMPLEMENTATION / NOT STARTED`:

- D-071 exact full-history/factual sufficiency và D-072 active-only C14 projection;
- attention preview/list/detail and evidence;
- Product/Purchase transition without recommendation/automation;
- narrow immutable C14 experiment events và additive migration cần thiết cho approved design;
- full frontend, SQL Server integration and real local E2E/regression.

Stage 6A/6B names, contents and order là approved implementation sequence theo D-073. Approval này không tuyên bố stage nào đã bắt đầu/completed và không thay thế implementation review sau khi code được thực hiện.

## 17. Definition of Done proposal

- D-061–D-072 traceable in implementation/tests.
- S6-Q1–S6-Q4 resolutions D-069–D-072 incorporated without reopening approved scope.
- Today uses Store-local current date, no historical picker and no duplicate Slice 5 financial semantics.
- Summary and source evidence reconcile.
- C14 formula/window/classification/order/sufficiency are deterministic and explainable.
- Owner-only authorization and Store isolation are backend-enforced.
- Action transition never decides Supplier/quantity or creates/commits Purchase automatically.
- Experiment events are narrow, immutable and non-transactional to business flow; per-view exposure guards prevent reactive duplicate `TodayOpened`/`SignalShown`.
- Domain/query, SQL Server integration, frontend and real local critical E2E pass; Slice 1–5 regressions pass.
- No AI, forecast/replenishment engine, BI dashboard, generic rule/alert/analytics platform.
- Technical Breakdown và staging sequence đã được Product Owner approve tại D-073; implementation vẫn phải thực hiện, verify và review theo từng stage.

## 18. Resolved Product Owner questions và review gate

- S6-Q1 resolved by D-069 — per-transaction new-debt-created, same-day correction only, no standalone allocation.
- S6-Q2 resolved by D-070 — same-day Completed Sale excluding same-day Void; Return does not reduce count.
- S6-Q3 resolved by D-071 — exact full-history risk gate and earlier factual state with positive evidence.
- S6-Q4 resolved by D-072 — active-only C14 candidates.

Không còn known Product Owner semantic blocker trong [`OPEN_QUESTIONS.md`](../../OPEN_QUESTIONS.md). S6-Q1–S6-Q4 đã resolved trước khi Product Owner approve Technical Breakdown và staging tại D-073.

## 19. Related decisions

- D-010–D-017 — transaction history, payment/debt, costing, idempotency, Store/Main Warehouse và testing foundation.
- D-023–D-032 — Sale/payment/Customer/inventory/cost/recovery và Slice 3 approval.
- D-033–D-042 — Return/Void/recovery và Slice 4 approval.
- D-043–D-060 — Debt/EOD semantics, Stage 5A/5B và final Slice 5 approval.
- D-061 — Owner Today landing/current Store-local date.
- D-062 — Today summary/new-debt distinction.
- D-063 — typed Summary → Why → Source evidence.
- D-064 — 7 completed-business-day velocity.
- D-065 — fixed threshold, factual/risk/insufficient states.
- D-066 — intentionally thin attention UI.
- D-067 — information-to-action without auto-decision.
- D-068 — measurable but still-unvalidated C14 experiment.
- D-069 — exact per-transaction new-debt-created and correction boundaries.
- D-070 — exact SaleCount with same-day Void and Return behavior.
- D-071 — exact LowStockRisk/factual data sufficiency.
- D-072 — active-only C14 Product candidates.
- D-073 — Technical Breakdown Slice 6 và Stage 6A/6B implementation sequence approval.

**Current gate:** Technical Breakdown `APPROVED FOR IMPLEMENTATION` tại D-073; Stage 6A `IMPLEMENTED / PENDING PRODUCT OWNER REVIEW`; Stage 6B `NOT STARTED`. Next step là Product Owner review Stage 6A implementation; không có D-074/implementation approval trong handoff này.
