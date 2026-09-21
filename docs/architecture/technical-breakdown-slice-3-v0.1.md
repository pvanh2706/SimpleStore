# Technical Breakdown — Slice 3 v0.1

- **Slice:** 3 — Sale → Payment → Print
- **Trạng thái:** `APPROVED FOR IMPLEMENTATION`
- **Ngày Product Owner approval:** 2026-09-21
- **Approved decisions:** D-023–D-030
- **Ranh giới:** Tài liệu này chốt technical direction cho Slice 3; không thay đổi Step 1–11, Architecture v0.1, Domain Model v0.1 hoặc scope MVP đã `APPROVED`.

## Outcome

Sau Slice 3, Cashier hoặc Owner có thể:

1. tìm Product theo tên/SKU hoặc scan/tìm barcode;
2. thêm Product vào Cart và thay đổi quantity;
3. xem giá và tổng preview;
4. khai báo một hoặc nhiều Payment `Cash`/`Transfer`;
5. chọn hoặc tạo Customer tối thiểu khi bán còn nợ;
6. Complete Sale an toàn và không tạo double Sale khi double-submit/retry;
7. làm InventoryBalance giảm đúng và có InventoryMovement giải thích được;
8. giữ price snapshot và cost snapshot lịch sử trên SaleLine;
9. xem Sale Completed authoritative;
10. render receipt để print/reprint mà print failure không thay đổi Sale.

`Cart ≠ Sale`. Cart chỉ là interaction state phía frontend. Database không có Sale Draft; Sale chỉ trở thành business fact khi `CompleteSale` commit thành công và được tạo trực tiếp ở trạng thái `Completed`.

## Explicit scope

### Trong Slice 3

- Sale và SaleLine.
- actual SalePayment.
- minimal Customer identity.
- checkout UI và transient Cart.
- Product search, SKU và barcode input/search qua API pagination hiện có.
- cart quantity editing; một Product xuất hiện một lần trong Sale.
- multiple payment entries `Cash`/`Transfer`.
- paid/outstanding calculation.
- `CompleteSale` với OperationId/idempotency và ambiguous-result recovery.
- inventory deduction, InventoryMovement âm và cost snapshot.
- `AllowNegativeStock` setting, Owner-only update và audit record tối thiểu.
- Sale list/detail cần cho completed result và Reprint.
- printable receipt, print after completion và Reprint.
- Owner/Cashier authorization.
- domain, SQL Server integration, frontend và critical real E2E tests.

### Không thuộc Slice 3

- Return, Void, refund hoặc correction workflow.
- Customer debt repayment sau Sale hoặc Supplier debt repayment.
- full Customer management/CRM, loyalty, segmentation, credit limit hoặc customer statement phức tạp.
- discount, promotion hoặc price override.
- invoice/HĐĐT implementation.
- C14, end-of-day report hoặc generic accounting ledger.
- advanced printer agent/service hoặc printer orchestration.
- multi-warehouse hoặc multi-branch.

Không thêm placeholder behavior làm người dùng hiểu nhầm Return/Void đã tồn tại. Nếu UI cần chừa vị trí, control phải disabled và không tạo API/domain behavior.

## Approved decisions applied

| Decision | Technical consequence |
| --- | --- |
| D-023 | Không có persisted Draft; `CompleteSale` tạo Sale Completed atomically; Completed immutable và không hard-delete. |
| D-024 | Backend đọc `Product.SalePrice`, tính tiền và snapshot giá; request không chứa authoritative price/total/cost; chưa có discount. |
| D-025 | Nhiều actual Payment, chỉ Cash/Transfer; không overpayment; outstanding được suy ra. |
| D-026 | Customer optional khi paid đủ, required khi còn nợ; Customer chỉ là identity tối thiểu. |
| D-027 | Sale, payments, inventory balance/movement, cost snapshot và BusinessOperation commit trong một transaction; inventory locks deterministic. |
| D-028 | Backend enforce `AllowNegativeStock`; Owner-only setting/audit; negative-stock cost fallback có reliability state; không retroactive revaluation. |
| D-029 | Client-generated OperationId, request fingerprint, exact retry và immutable ambiguous attempt snapshot. |
| D-030 | Print là hậu xử lý của Sale Completed; failure không rollback/re-complete; Owner/Cashier bán và Reprint, chỉ Owner đổi negative-stock policy. |

## Domain model direction

Các field dưới đây là conceptual minimum. Naming có thể điều chỉnh cho nhất quán codebase, nhưng không được đổi semantics.

### Sale

- `Id`
- `StoreId`
- `WarehouseId`
- `CustomerId` nullable
- `Status = Completed`
- `TotalAmount`
- `PaidAmount` được tính từ SalePayments trong domain/read result
- `OutstandingAmount = TotalAmount - PaidAmount`
- `CompletedAt`
- `CompletedByUserId`
- `CreatedAt`

Không cần Draft status hoặc API create/update Draft. Completed Sale immutable; không hard-delete. Nếu persist `Status`, Slice 3 chỉ tạo giá trị `Completed` để giữ extension point sạch cho Return/Void relationships sau này, không tạo lifecycle chưa được duyệt.

### SaleLine

- `Id`
- `StoreId`
- `SaleId`
- `ProductId`
- identifying snapshot tối thiểu để lịch sử/receipt không đổi khi Product đổi sau này: `ProductName`, `ProductSku`, `ProductUnit`
- `Quantity`
- `UnitSalePrice`
- `LineAmount`
- `UnitCostAtSale`
- `CostReliability`

Một Product chỉ xuất hiện một lần trong Sale. Domain từ chối duplicate ProductId và database có unique constraint `(SaleId, ProductId)`. Frontend cũng ngăn duplicate nhưng backend/database là authority.

SaleLine phải đủ để:

- tính revenue và gross profit lịch sử mà không đọc lại current Product price/cost;
- render Sale detail/receipt lịch sử;
- cho Slice 4 tham chiếu `OriginalSaleLine` khi Return.

### Cost reliability

Cost snapshot cần phân biệt tối thiểu:

- `Reliable`: có last known AverageCost authoritative;
- `Estimated`: dùng `Product.ReferencePurchaseCost` fallback;
- `Unavailable`: không có cost source đáng tin.

Không dùng điều kiện `AverageCost > 0` như tín hiệu duy nhất rằng cost có hay không, vì zero có thể là cost hợp lệ. Implementation phải dựa trên cost source/history hoặc metadata rõ ràng.

Nếu không có cost source, Sale vẫn có thể hoàn tất khi policy cho phép; technical representation có thể dùng numeric zero để tương thích ledger hiện tại nhưng phải lưu `CostReliability = Unavailable`. Reporting không được diễn giải trường hợp này thành hàng có giá vốn chắc chắn bằng zero hoặc gross profit đáng tin. Không cập nhật lại SaleLine khi Purchase xảy ra sau.

### SalePayment

- `Id`
- `StoreId`
- `SaleId`
- `Amount`
- `Method`: `Cash` hoặc `Transfer`
- `OccurredAt`

SalePayment là tiền thực nhận. Không model cash tendered/change, QR gateway, provider reference hoặc payment orchestration trong Slice 3.

### Customer

- `Id`
- `StoreId`
- `Name`
- `Phone` nullable
- `CreatedAt`
- `UpdatedAt` nếu persistence convention hiện tại cần

Slice 3 chỉ cần create/search/select Customer tối thiểu. Không thêm arbitrary `Debt` field. Customer outstanding là projection từ Completed Sales, SalePayments và các effect ở slice sau.

### Store setting và audit

- `Store.AllowNegativeStock`, default `false`.
- Chỉ Owner được thay đổi.
- Mỗi thay đổi tạo audit record tối thiểu chứa StoreId, old/new value, ChangedByUserId và ChangedAt.

Audit này có thể là entity cụ thể cho Store setting; không tạo generic audit framework.

### BusinessOperation

Tái sử dụng BusinessOperation foundation hiện có với operation type `CompleteSale`, request fingerprint, StoreId, Completed status và SaleId result reference. Operation result và mọi business effect phải cùng commit/rollback.

## Persistence and database integrity

Migration Slice 3 dự kiến chỉ tạo/thay đổi schema cần cho scope trên:

- add `Store.AllowNegativeStock` với default `false` và setting audit table;
- Customers;
- Sales;
- SaleLines;
- SalePayments;
- thêm `Sale` vào InventoryMovementType/source conventions nếu cần.

Database constraints/index direction:

- giữ primary key GUID hiện tại;
- composite principal/alternate keys `(StoreId, Id)` khi cần;
- Sale `(StoreId, WarehouseId)` → Warehouse cùng Store;
- Sale `(StoreId, CustomerId)` → Customer cùng Store khi CustomerId có giá trị;
- SaleLine `(StoreId, SaleId)` → Sale cùng Store;
- SaleLine `(StoreId, ProductId)` → Product cùng Store;
- SalePayment `(StoreId, SaleId)` → Sale cùng Store;
- InventoryMovement tiếp tục có composite Store/Product/Warehouse constraints;
- CompletedByUserId và setting ChangedByUserId tham chiếu Identity user với delete `Restrict` nếu mapping sạch như audit FKs hiện có;
- unique `(SaleId, ProductId)`;
- indexes cho Sale list theo Store/completed time, Customer lookup và source/reference lookup;
- check constraints: quantity > 0, unit sale price >= 0, line amount >= 0, total >= 0, payment amount > 0, cost snapshot >= 0;
- Store scope không đến từ request body và không dựa vào frontend filter.

Migration thuộc Infrastructure, phải được review để không recreate/drop business tables ngoài ý muốn. Production không tự migrate khi startup.

## Pricing, totals and precision

Tái sử dụng project conventions:

- Quantity: `decimal(18,3)`.
- `UnitSalePrice`, LineAmount, TotalAmount và payment money: `decimal(18,2)`.
- `UnitCostAtSale`/AverageCost: `decimal(18,4)`.
- `LineAmount = Round(Quantity × UnitSalePrice, 2, MidpointRounding.AwayFromZero)`.
- `Sale.TotalAmount = sum(LineAmount)`.
- `PaidAmount = sum(SalePayments.Amount)`.
- `OutstandingAmount = TotalAmount - PaidAmount`.

Backend/domain là authority. Frontend preview áp dụng cùng convention cho UX nhưng response Completed là kết quả authoritative.

`CompleteSale` không nhận SalePrice, LineAmount, TotalAmount hoặc UnitCostAtSale từ frontend. Product price thay đổi trước completion được phản ánh bằng price hiện hành tại lúc backend complete; sau completion, SaleLine snapshot không đổi.

## CompleteSale command boundary

Command chỉ biểu diễn business intention tối thiểu:

- `OperationId`
- `CustomerId` nullable
- lines: `ProductId + Quantity`
- payments: `Amount + Method`

Không nhận StoreId, WarehouseId, authoritative price, total, inventory quantity hoặc cost từ frontend.

### Transaction sequence

`CompleteSale` chạy trong một local SQL transaction:

1. resolve authenticated user, role và StoreId server-side;
2. resolve Main Warehouse;
3. acquire transaction-owned application lock cho OperationId;
4. normalize request và tính fingerprint;
5. kiểm tra BusinessOperation hiện có;
6. load active Products cùng Store;
7. validate lines, duplicate Product và payment method/amount;
8. lock InventoryBalances theo ProductId tăng dần bằng cùng SQL locking strategy của Slice 2;
9. đọc authoritative Product.SalePrice và cost sources trong transaction;
10. tính LineAmount, Total, Paid và Outstanding authoritative;
11. validate overpayment và Customer rule;
12. enforce `AllowNegativeStock` cho toàn bộ lines;
13. resolve UnitCostAtSale + CostReliability;
14. tạo Sale trực tiếp Completed và SaleLines snapshots;
15. tạo actual SalePayments;
16. giảm InventoryBalances và tạo InventoryMovements âm nguồn SaleLine;
17. tạo BusinessOperation Completed với SaleId result reference;
18. `SaveChanges`;
19. commit; bất kỳ lỗi nào rollback toàn bộ.

Không trả success trước commit. Không được tồn tại Sale, Payment, movement hoặc balance update một phần.

### Request fingerprint

Fingerprint phải bao gồm normalized business intention:

- operation type `CompleteSale`;
- CustomerId hoặc null;
- ProductId + Quantity, normalized theo deterministic ProductId order;
- Payment Method + Amount, normalized deterministically.

Fingerprint không chứa frontend preview price/cost vì request không được gửi các field đó. Cùng OperationId nhưng fingerprint khác trả `409 idempotency-key-reused` trước khi tạo thêm effect.

## Payment and Customer debt rule

Validation authoritative:

```text
0 <= sum(Payments) <= Sale.TotalAmount
Outstanding = Sale.TotalAmount - sum(Payments)
```

- `Outstanding = 0`: Customer optional.
- `Outstanding > 0`: CustomerId required, Customer phải tồn tại trong authenticated Store.
- Customer Store khác phải bị che bằng store-scoped lookup/not-found semantics; database FK bảo vệ thêm.
- zero payments là credit sale toàn phần và bắt buộc Customer.
- mỗi Payment entry phải > 0; không tạo zero/negative Payment rows.

Không implement `RecordCustomerDebtPayment` trong Slice 3. Outstanding Customer chỉ là derived read information để Slice 5 tiếp tục.

## Negative-stock and cost semantics

### AllowNegativeStock = false

Sau khi giữ lock trên tất cả balances, backend kiểm tra từng line:

```text
available = QuantityOnHand
shortage = max(0, requestedQuantity - available)
```

Nếu có shortage:

- reject toàn bộ operation;
- không tạo Sale/Payment/movement;
- không thay đổi balance;
- typed ProblemDetails phải liệt kê ProductId và shortage quantity đủ để UI giải thích;
- OperationId sau deterministic rejection không được ghi Completed; business intention sửa đổi dùng OperationId mới.

### AllowNegativeStock = true

- Không cần Owner approve từng Sale.
- Balance được phép âm và movement vẫn ghi đầy đủ.
- UnitCostAtSale ưu tiên last known AverageCost; nếu không có thì dùng ReferencePurchaseCost; nếu vẫn không có thì đánh dấu Unavailable.
- InventoryMovement quantity/value delta âm theo resolved cost basis và cùng rounding convention.
- SaleLine giữ snapshot và reliability state; Purchase sau không sửa historical SaleLine hoặc Sale movement.

InventoryBalance logic phải xử lý an toàn khi quantity trước/sau transaction bằng hoặc nhỏ hơn zero: không chia cho zero và không retroactively revalue Sale cũ. InventoryValue tiếp tục giải thích được từ tổng movement; last known usable cost được giữ cho lần bán tiếp theo theo D-028.

## Concurrency semantics

Mọi mutation InventoryBalance từ Purchase và Sale phải đi qua cùng locking contract `(StoreId, WarehouseId, ProductId)`.

- lock ProductId theo deterministic ascending order;
- giữ lock tới khi transaction commit/rollback;
- Sale và Purchase không có lock namespace riêng có thể bỏ qua nhau;
- rowversion/concurrency handling là lớp bảo vệ bổ sung, không thay thế balance lock;
- transaction thua race rollback toàn bộ side effects và trả typed conflict/shortage phù hợp.

Ví dụ tồn = 1, hai Cashier đồng thời bán 1 khi negative stock tắt: chỉ transaction lấy/giữ balance lock trước được complete; transaction sau đọc state đã commit và bị shortage. Không được có hai Sale Completed dựa trên cùng snapshot tồn 1.

Sale nhiều Product và concurrent Sale/Purchase phải không deadlock do khác lock order; cả hai path dùng cùng deterministic ProductId order.

## Idempotency and recovery

### Backend

- OperationId mới: xử lý một operation mới.
- Cùng OperationId + cùng fingerprint đã Completed: trả Sale cũ với `WasAlreadyCompleted`, không duplicate effect.
- Cùng OperationId + payload khác: `409 idempotency-key-reused`.
- Operation lock timeout: `409 operation-lock-timeout`; kết quả vẫn ambiguous từ phía client.
- operation status endpoint store-scoped trả status/type/result reference; unknown trả typed 404.

### Frontend

Checkout tạo immutable attempt snapshot gồm OperationId, CustomerId, lines/quantities và payments ngay trước request đầu tiên.

- disable Complete ngay khi submit để chặn double click;
- network error, HTTP 408 hoặc `operation-lock-timeout`: khóa Cart/Customer/Payments, query operation status và giữ exact snapshot;
- Completed: load authoritative Sale từ resultReference rồi chuyển sang completion/receipt;
- chưa xác định: cho retry đúng cùng OperationId + exact payload;
- không sinh OperationId mới khi attempt cũ còn ambiguous;
- deterministic 4xx như validation, negative-stock rejection, `idempotency-key-reused` hoặc already-completed conflict hiển thị trực tiếp và không suy diễn thành success từ status;
- chỉ sau deterministic failure mới cho sửa business intention và tạo OperationId mới.

Không copy component Purchase một cách máy móc; có thể tái sử dụng nhỏ các pure concepts/helpers nếu duplication thực sự xuất hiện, nhưng không tạo generic operation framework lớn trong Slice 3.

## HTTP/API direction

Route naming có thể điều chỉnh theo conventions hiện tại; conceptual boundary:

- `GET /api/products?search=...&isActive=true&page=...&pageSize=...`
- `GET /api/customers?search=...&page=...&pageSize=...`
- `POST /api/customers`
- `POST /api/sales/complete`
- `GET /api/sales?page=...&pageSize=...`
- `GET /api/sales/{id}`
- `GET /api/operations/{operationId}`
- Owner-only GET/PUT endpoint cho `AllowNegativeStock`.

API controllers là thin HTTP boundary; Application use cases điều phối; Domain giữ invariants; Infrastructure chứa EF/SQL locking. Không cần MediatR, generic tenant framework, accounting ledger hoặc printer service.

Authorization:

- Owner và Cashier: Product search cần cho checkout, CompleteSale, Sale detail/list cần cho flow và Reprint.
- Customer create/search cần cho credit Sale; store-scoped backend authorization bắt buộc.
- Owner only: read/update negative-stock policy nếu UI setting chỉ dành Owner; update luôn Owner-only.
- Cashier Store A không đọc/complete/reprint dữ liệu Store B.

## UI direction

Tái sử dụng Vue Router, Pinia khi shared state thực sự cần, Tailwind và component patterns hiện tại; không thiết kế visual system mới.

### Checkout

- server-side paginated Product search theo name/SKU/barcode; không tải toàn bộ catalog;
- barcode input/search hoạt động với Product ngoài page đầu;
- Cart transient, một row mỗi Product;
- quantity edit/remove;
- price/line/total preview, có nhãn rõ đây là preview;
- multiple Cash/Transfer payments và outstanding preview;
- Customer search/create/select khi outstanding > 0;
- warning rõ khi store cho negative stock, nhưng backend vẫn là authority;
- Complete button chống double-submit và khóa exact attempt khi ambiguous.

### Completion

- hiển thị Sale success và authoritative totals;
- render receipt data từ Sale Completed response/detail;
- `Print receipt` gọi browser print boundary;
- `New Sale` tạo Cart/OperationId mới chỉ sau khi Sale hiện tại đã xác định Completed;
- print failure hiển thị riêng, không biến Sale thành failed và không gọi CompleteSale lại.

### Sale detail

- immutable lines, price snapshots, payments, paid/outstanding và Customer;
- Reprint dùng Sale detail hiện có;
- không expose internal cost/profit trên customer receipt;
- không có Return/Void action hoạt động trong Slice 3.

### Owner setting

- AllowNegativeStock toggle;
- giải thích khi tắt sẽ reject toàn bộ Sale thiếu tồn, khi bật balance có thể âm và cost/profit có thể estimated;
- mutation Owner-only, hiển thị success/failure rõ;
- không tạo settings platform chung.

## Receipt and print boundary

Printable receipt lấy duy nhất từ Sale Completed authoritative và tối thiểu có:

- Store name;
- Sale identifier;
- completed time;
- cashier identity/display value phù hợp;
- lines: Product snapshot, quantity, UnitSalePrice, LineAmount;
- TotalAmount;
- Payments theo method/amount;
- Outstanding nếu có;
- Customer nếu có.

Không in UnitCostAtSale, CostReliability hoặc profit trên receipt khách hàng.

Flow:

```text
CompleteSale commit
→ Vue nhận/load Sale Completed
→ render printable receipt
→ browser print
```

Browser print cancel/failure/unknown không rollback Sale, không gọi CompleteSale lần nữa và không khẳng định giấy chắc chắn đã ra. Reprint chỉ fetch/render/print Sale hiện có; không mutate Sale, inventory, payments hoặc BusinessOperation.

## Testing requirements

### Domain tests

- totals và rounding nhiều lines;
- authoritative price snapshot và lịch sử không đổi;
- UnitCostAtSale + CostReliability từ AverageCost/reference/unavailable;
- duplicate Product rejected;
- multiple Payments và paid/outstanding;
- overpayment/invalid payment rejected;
- credit Sale requires Customer; fully paid Customer optional;
- Completed immutable;
- negative-stock allowed/disallowed cost behavior;
- no divide-by-zero around zero/negative balance transitions.

### SQL Server integration tests

- Owner/Cashier authorization và Owner-only setting update;
- Store isolation cho Sale/Customer/operation status;
- cross-store Product/Customer rejected ở application và database constraints;
- CompleteSale creates one Completed Sale atomically;
- authoritative current SalePrice snapshot;
- InventoryBalance decrease và negative InventoryMovement source SaleLine;
- UnitCostAtSale snapshot/reliability;
- multiple Payments, paid/outstanding và credit Customer rule;
- AllowNegativeStock false returns product shortage details with no partial effect;
- AllowNegativeStock true permits negative balance;
- setting change audit;
- same OperationId exact retry returns same Sale;
- same ID/different payload conflicts;
- concurrent same OperationId creates one logical completion;
- concurrent different OperationIds against same Cart intention obey stock policy and have no partial loser effects;
- concurrent Sales on same Product with last unit;
- concurrent Sale + Purchase on same Product with final balance/value/movements consistent;
- multiple Products lock deterministic order;
- transaction rollback when any line/payment/customer/inventory validation fails.

Không dùng EF InMemory cho inventory/concurrency/idempotency behavior. Dùng WebApplicationFactory + SQL Server như existing integration foundation.

### Frontend tests

- Product ngoài first page có thể search/select;
- barcode search;
- duplicate Product prevention và Cart quantity/total preview;
- payment/outstanding preview;
- credit Sale requires Customer;
- double-click chỉ gửi một active attempt;
- same OperationId exact retry;
- network lost response, HTTP 408 và `operation-lock-timeout` recovery;
- `idempotency-key-reused` không bị coi là success;
- Cart/Customer/Payments immutable trong ambiguous state;
- Completed operation recovery load authoritative Sale;
- print failure vẫn giữ Sale success và cho Reprint;
- Reprint không gọi CompleteSale.

### Real E2E

Local Playwright critical flow, không mock backend/database:

```text
Product có tồn
→ Cashier login
→ tìm/scan Product
→ add Cart + quantity
→ payment
→ Complete Sale
→ inventory giảm
→ receipt render + browser print boundary được gọi
→ reopen Sale detail
→ Reprint không tạo Sale/effect mới
```

Ưu tiên existing Windows/LocalDB setup. Nếu browser/CI không thể chứng minh physical print ổn định, E2E chỉ xác minh printable receipt và `window.print` boundary; không gọi đó là kiểm chứng giấy thực tế. Không dựng printer service, Docker/Kubernetes hoặc orchestration lớn cho test này.

## Implementation sequence

1. Domain entities/value rules và domain tests.
2. EF configurations + reviewed Slice 3 migration, including Store setting/audit and Store-consistency FKs.
3. Application repository contracts/use cases, CompleteSale transaction/locking/idempotency.
4. Thin API endpoints, authorization và typed ProblemDetails.
5. SQL Server integration tests, đặc biệt concurrency Sale/Sale và Sale/Purchase.
6. Vue checkout/customer/payment/recovery flow.
7. Sale detail + printable receipt/Reprint boundary.
8. Frontend tests và real local E2E.
9. Full restore/build/test/migration verification, diff review và CI.

## Definition of Done

Slice 3 chỉ được đề nghị approval implementation khi:

- happy path Owner/Cashier chạy Vue → API → SQL Server;
- Sale chỉ tồn tại Completed và immutable;
- price/cost/payment/debt/inventory invariants được backend enforce;
- negative-stock policy và audit hoạt động;
- idempotency/ambiguous retry/concurrency không tạo duplicate hoặc partial state;
- print failure không rollback/re-complete Sale và Reprint không mutate;
- automated test requirements trọng yếu pass;
- critical real E2E chạy local;
- migration được review, không auto-run production startup;
- không có Sale Draft, Return/Void, debt repayment, discount hoặc scope Slice 4+.

## Quyết định liên quan

- D-010 — MVP User Flows, timeout recovery và print failure boundary.
- D-011–D-013 — domain invariants, Moving Weighted Average, Payment & Debt.
- D-014 — transaction/idempotency, inventory concurrency, negative-stock và external print boundary.
- D-016 — Store tenancy foundation.
- D-023–D-030 — `APPROVED`: Slice 3 Sale lifecycle, pricing, payment/customer, inventory/cost, negative stock, idempotency, printing và permissions.

Slice 0–2 giữ nguyên trạng thái. Tài liệu này không bắt đầu implementation Slice 3 và không thay đổi business decisions đã `APPROVED`.
