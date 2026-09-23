# Technical Breakdown Slice 6 v0.1 — Understand & Act

## Trạng thái

`DRAFT / PENDING PRODUCT OWNER REVIEW`

Product Owner đã approve product decisions D-061–D-068 ngày 2026-09-23. Tài liệu này là technical proposal chưa được Product Owner approve, không bắt đầu Slice 6 implementation và không approve staging 6A/6B.

Baseline đã inspect: `9f57a37ef97ab9f8f672b5309a42141ea6e267b8`, tại đó `Slice 5 — Debt + End-of-day` là `APPROVED / COMPLETED` theo D-060.

## Mục tiêu và trình tự

Slice 6 thêm một Owner experience trả lời nhanh “Hôm nay cửa hàng thế nào?”, giải thích được con số và thử nghiệm duy nhất một attention hypothesis: nguy cơ sắp hết hàng. Trình tự review/implementation tiếp tục là:

`Domain behavior → DB changes → API contract → UI flow → Test cases → Implementation stages`

Mọi quyết định D-001–D-060 tiếp tục được bảo toàn. Các điểm chưa được D-061–D-068 quyết định đủ chính xác nằm trong `OPEN_QUESTIONS.md`; draft này không tự invent câu trả lời.

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
- `Product.CreatedAt` và `Store.CreatedAt` có thể hỗ trợ data-coverage checks, nhưng exact sufficiency vẫn là Product Owner question S6-Q3.
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

### 5.3 New debt created — semantic gate

Metric này là obligation created, không phải current/ending outstanding debt và không phải DebtPayment cash movement.

Các source có thể query:

- Customer: Completed credit Sale trong Today window, Sale payments gắn trực tiếp với Sale, Return/Sale Void correction evidence.
- Supplier: Completed Purchase trong Today window, Purchase payments gắn trực tiếp với Purchase, Purchase Void evidence.
- Standalone `DebtPayment` không có invoice allocation theo D-047.

Exact cross-day correction và standalone DebtPayment treatment chưa được D-062 quyết định đủ để chốt công thức. S6-Q1 phải được Product Owner resolve trước Technical Breakdown approval. Không implement một aggregate allocation ngầm và không cho field này reuse ending debt.

### 5.4 Sale count — semantic gate

Candidate sources là Completed Sale, Sale Void và Return. Gross count, currently-active cohort và event-day-net count cho kết quả khác nhau, đặc biệt khi correction xảy ra ở business date khác. Partial/full Return behavior cũng chưa được approve.

S6-Q2 phải được resolve trước khi `saleCount` contract/test oracle trở thành final. Draft API giữ field dự kiến nhưng đánh dấu semantic blocked; implementation không bắt đầu với assumption tạm.

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
- New debt created: source obligation transactions theo semantic sau khi S6-Q1 được quyết định.
- Sale count: exact included/excluded source transactions theo S6-Q2.

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

Event-date projection này làm correction của prior Sale có thể tạo negative contribution trong window. Tổng `NetSoldQuantity <= 0` không tạo risk vì `AverageDailySales > 0` là điều kiện bắt buộc. Cách correction ảnh hưởng new-debt-created và Sale count là câu hỏi riêng, không được suy từ C14.

### 7.2 Current stock

Current stock đọc `InventoryBalance.QuantityOnHand` cho current Store + Main Warehouse + Product. Missing balance được xử lý như zero current stock trong query projection, nhưng chỉ được attention nếu data/evidence policy sau S6-Q3 cho phép. Không cộng lại movement history để tạo current state cạnh tranh.

### 7.3 Data sufficiency

Response cần typed status dự kiến:

- `Sufficient`
- `InsufficientHistory`
- `NoRecentSalesEvidence`

Exact transition giữa các status bị chặn bởi S6-Q3. `Store.CreatedAt`, `Product.CreatedAt` và seven-day boundaries có sẵn, nhưng draft không tự chọn “Product tồn tại đủ ngày” hay “có N ngày bán”. Factual stock-out/negative-stock có thể có sufficiency khác risk; Product Owner phải chốt.

### 7.4 Classification

Sau khi data-sufficiency gate được resolve:

1. `CurrentStock < 0` + required evidence → `NegativeStock` factual attention.
2. `CurrentStock == 0` + required evidence → `OutOfStock` factual attention.
3. `CurrentStock > 0`, `AverageDailySales > 0`, sufficient data, `DaysOfCover <= 3` → `LowStockRisk`.
4. Các trường hợp còn lại không tạo strong attention; insufficient state có thể hiển thị neutral explanatory state ở full view.

`DaysOfCover` dùng decimal calculation, không round trước comparison. `== 3` phải flag. Rounding chỉ để presentation sau classification.

### 7.5 Ordering và limit

Deterministic ordering proposal không thêm business priority ngoài D-066:

1. factual category trước risk category;
2. trong factual category: Product name normalized ordinal, rồi ProductId;
3. trong risk category: exact DaysOfCover ascending, rồi Product name normalized ordinal, rồi ProductId.

Today lấy first 3 sau ordering. `totalAttentionCount` là tổng trước limit; nếu `> 3`, UI hiển thị `Xem tất cả X mặt hàng`. Alphabetical/ProductId tie-break chỉ đảm bảo stable presentation, không tuyên bố severity khác.

### 7.6 Product lifecycle gate

Product inactive có tồn hoặc sales history nhưng replenishment action có thể mâu thuẫn `IsActive = false`. S6-Q4 phải chốt active-only hay factual exception trước khi candidate query final.

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
    "evaluationStatus": "Sufficient",
    "totalCount": 0,
    "items": []
  }
}
```

`saleCount` and new-debt-created fields remain blocked by S6-Q1/S6-Q2. Contract is not final until those questions are resolved.

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
  "dataSufficiency": "Sufficient"
}
```

For factual zero/negative stock, `daysOfCover` may be null; UI must not represent factual state as forecast.

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

- `TodayOpened`: Today data successfully rendered for Owner.
- `SignalShown`: one qualifying C14 Product item actually rendered, one event per rendered item and render occurrence.
- `WhyOpened`: Owner opened C14 explanation for a Product.
- `PurchaseDraftStarted`: Owner invoked `Tạo phiếu nhập` from that Product's C14 flow.

Client-generated EventId protects network retry only; a later genuine interaction creates a new EventId. Event count is descriptive telemetry, not business outcome.

### 13.2 Non-interpretation rule

No automated code labels C14 validated from CTR/count. Pilot/research must separately evaluate discovery, trust, decision influence, continued usage and willingness-to-pay. No dashboard beyond technical/pilot extraction explicitly approved later.

## 14. Error handling, performance and operational behavior

- Invalid Store timezone/business window uses stable typed code; no fallback to browser/server timezone.
- Explanation metric, event type and Product-reference validation use stable `ProblemDetails.code`.
- Financial source/evidence pagination is bounded.
- C14 aggregates only 7 completed days, current Store and Main Warehouse.
- Exact decimal values drive threshold/order; display rounding is separate.
- No background job/cache is required for MVP. If query performance is insufficient, inspect SQL/query plan before adding indexes or cache.
- Experiment-event write is isolated from business mutation; no failure may rollback/read-block Today or create/commit Purchase.
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
- Sale exactly at start included; exactly at end excluded.
- Sale Payment, Customer Debt Payment and actual refund component consistency.
- Return Restock/NoRestock and Sale Void historical COGS behavior unchanged.
- Empty Today produces zero financial summary and appropriate reliability.
- Sale-count tests follow S6-Q2 resolution; no placeholder assumption.
- Customer/Supplier new-debt tests follow S6-Q1 resolution, including same-day/cross-day correction and standalone DebtPayment.

### 15.3 C14 query/domain tests

- Exact Sale start/end for seven-day velocity window.
- Current-day Sale excluded.
- SaleLine quantity increases net sold.
- Return quantity decreases net sold for Restock and NoRestock.
- Sale Void reverses original quantity by Void event date.
- Purchase/Opening/PurchaseVoid/adjustment do not affect velocity.
- zero/negative net velocity yields no DaysOfCover risk.
- negative stock factual state.
- zero stock factual state.
- insufficient Store/Product history per S6-Q3.
- `DaysOfCover == 3` flags.
- `DaysOfCover < 3` flags.
- `DaysOfCover > 3` does not flag.
- decimal comparison occurs before display rounding.
- deterministic factual/risk ordering and ProductName/ProductId ties.
- Today preview max 3 and full count/list consistent.
- no signal when rule is not satisfied.
- inactive Product behavior follows S6-Q4 resolution.

### 15.4 SQL Server integration/security tests

- Aggregation translates/runs against SQL Server, not EF InMemory.
- Store A cannot read Store B Today, explanations, inventory or Product evidence.
- Owner allowed; Cashier/anonymous forbidden for every Slice 6 endpoint.
- Main Warehouse/current InventoryBalance selected correctly.
- Source references cannot cross Store.
- query uses half-open boundaries at SQL precision.
- shared financial projection regression against Slice 5 integration cases.
- proposed additive indexes/migration, if approved, upgrade safely from Slice 5 schema.

### 15.5 Explainability/navigation tests

- Revenue/Collected/Gross Profit components reconcile exactly to headline.
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
- event-write failure does not mutate/block Product, inventory or Purchase.
- UI emits `TodayOpened`, rendered `SignalShown`, `WhyOpened`, `PurchaseDraftStarted` at defined interactions.

### 15.7 Frontend and real E2E

- Owner default login landing `/today`; explicit redirect preserved.
- Cashier not routed to/allowed Today.
- Today has no date picker and shows Store date/timezone.
- summary labels/reliability and explanation flows.
- attention max 3, full-count link, factual/risk/insufficient/neutral states.
- Product detail and preselected Purchase transition with no Supplier/quantity recommendation.
- real E2E: seed 7 completed Store-local days, Sale/Return/Void history and stock; verify summary, explanation, attention, action transition and measurement rows through Vue → API → SQL Server.
- full Slice 1–5 backend/frontend/real-flow regressions remain green; no test deletion/skip to force green.

## 16. Proposed implementation staging — chưa approved

### Stage 6A — Today/C12/C13 reporting foundation

Proposal only:

- shared daily financial projection reused by EOD/Today;
- current Store-local date/window service orchestration;
- Today summary and typed financial explanations;
- Owner `/today` route/default landing;
- Sale count/new-debt semantics only after S6-Q1/S6-Q2 resolution;
- domain/SQL integration/frontend tests.

### Stage 6B — C14/action/measurement/E2E

Proposal only:

- seven-completed-day C14 projection after S6-Q3/S6-Q4 resolution;
- attention preview/list/detail and evidence;
- Product/Purchase transition without recommendation/automation;
- narrow immutable C14 experiment events and migration if approved;
- full frontend, SQL Server integration and real local E2E/regression.

Stage 6A/6B names, contents and sequence are not authorized implementation sequencing until Product Owner approves this Technical Breakdown.

## 17. Definition of Done proposal

- D-061–D-068 traceable in implementation/tests.
- S6-Q1–S6-Q4 resolved and incorporated before Technical Breakdown approval.
- Today uses Store-local current date, no historical picker and no duplicate Slice 5 financial semantics.
- Summary and source evidence reconcile.
- C14 formula/window/classification/order/sufficiency are deterministic and explainable.
- Owner-only authorization and Store isolation are backend-enforced.
- Action transition never decides Supplier/quantity or creates/commits Purchase automatically.
- Experiment events are narrow, immutable and non-transactional to business flow.
- Domain/query, SQL Server integration, frontend and real local critical E2E pass; Slice 1–5 regressions pass.
- No AI, forecast/replenishment engine, BI dashboard, generic rule/alert/analytics platform.
- Product Owner separately reviews/approves Technical Breakdown and implementation stages before code begins.

## 18. Open Product Owner gates

- S6-Q1 — new-debt-created correction and unallocated DebtPayment semantics.
- S6-Q2 — exact Sale count with Void/Return.
- S6-Q3 — exact 7-day sufficiency and factual-stock evidence requirement.
- S6-Q4 — active/inactive Product eligibility for C14.

Các câu hỏi nằm trong [`OPEN_QUESTIONS.md`](../../OPEN_QUESTIONS.md). Tài liệu này phải được cập nhật theo decision mới trước khi chuyển khỏi `DRAFT / PENDING PRODUCT OWNER REVIEW`.

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

**Current gate:** `DRAFT / PENDING PRODUCT OWNER REVIEW`. Slice 6 implementation is `NOT STARTED`. No Technical Breakdown approval decision exists.
