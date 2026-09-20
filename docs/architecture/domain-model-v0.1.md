# Domain Model v0.1

- **Step:** 9 — Domain Model v0.1
- **Trạng thái:** `APPROVED`
- **Ngày phê duyệt:** 2026-09-20
- **Người phê duyệt:** Product Owner
- **Bước tiếp theo:** Step 10 — Architecture

## Mục tiêu và ranh giới

Ghi nhận conceptual domain model nghiệp vụ của MVP dựa trên Step 1–8 đã APPROVED: domain concepts, relationships, domain invariants, costing behavior và payment/debt behavior.

Các tên và thành phần dưới đây mô tả khái niệm nghiệp vụ. Đây không phải database ERD, SQL/database schema, API contract, EF Core entities, frontend model hoặc architecture implementation chi tiết.

Giữ nguyên tài liệu và quyết định Step 1–8; Step 9 ghi nhận các lựa chọn đã được Product Owner phê duyệt tiếp theo, gồm Decision A và Decision B. Các phát biểu “chưa chọn phương pháp giá vốn” trong tài liệu trước phản ánh trạng thái tại thời điểm phê duyệt; quyết định hiện hành là Decision A dưới đây.

Không tự thêm feature. C7 tiếp tục chỉ là integration point; C14 vẫn là experiment chưa validated về value/willingness-to-pay.

## Core Domain Concepts

### Store, Warehouse và User

| Concept | Ý nghĩa và ranh giới MVP |
| --- | --- |
| Store | MVP có 1 Store, 1 Warehouse chính và nhiều User. |
| Warehouse | Đại diện kho chính của cửa hàng; chưa hỗ trợ multi-warehouse phức tạp. |
| User | Có Owner và Cashier; permission chi tiết được làm rõ ở bước phù hợp sau này. |

### Product

Các khái niệm tối thiểu: **SKU / mã hàng, Barcode, Unit, Sale Price, Current/reference purchase cost**.

`CurrentCost` của Product không được dùng để tính lại lợi nhuận giao dịch quá khứ. Giá vốn dùng cho SaleLine phải được snapshot tại thời điểm Sale Completed.

Ví dụ SaleLine:

| Khái niệm | Giá trị |
| --- | --- |
| Product | Coca |
| Qty | 2 |
| SalePrice | 12.000 |
| UnitCostAtSale | 8.000 |

Nếu giá vốn hiện tại đổi sau này, cost snapshot và lãi gộp lịch sử của giao dịch cũ không thay đổi.

### InventoryMovement — nguồn giải thích tồn kho

Không chỉ lưu một con số `Product.Stock` rồi sửa trực tiếp. Nguồn giải thích tồn kho là **InventoryMovement**.

Một movement tối thiểu biểu diễn được:

| Thành phần nghiệp vụ | Ý nghĩa |
| --- | --- |
| Product | Sản phẩm có biến động tồn. |
| Warehouse | Kho có biến động tồn. |
| Quantity delta (+/-) | Mức tăng hoặc giảm số lượng. |
| Source/Reason | Nguồn và lý do biến động. |
| Source Transaction | Giao dịch nghiệp vụ gây ra biến động. |
| Time | Thời điểm biến động. |
| User | Người thực hiện. |
| Inventory value / cost basis cần thiết | Cơ sở giá trị tồn kho/giá vốn để giải thích biến động. |

Ví dụ:

| Quantity delta | Nguồn |
| --- | --- |
| +50 | Purchase |
| -2 | Sale |
| +1 | Customer Return có restock |
| -3 | Stock Adjustment |

**Conceptually: Current Stock = tổng InventoryMovement hợp lệ.**

Có thể cache/materialize tồn hiện tại để tối ưu hiệu năng sau này, nhưng movement ledger phải cho phép giải thích nguồn của tồn. Step 9 chưa chốt cách lưu hoặc cơ chế materialization.

### Purchase và PurchaseLine

| Concept | Thành phần nghiệp vụ |
| --- | --- |
| Purchase | Supplier; PurchaseLine[]; total/amount; paid amount; outstanding debt; status. |
| PurchaseLine | Product; Quantity; Purchase Price. |

Khi Purchase Completed: **Purchase → InventoryMovement (+) → Cost update → Supplier debt/payment state**.

Chuỗi trên thể hiện hệ quả nghiệp vụ, không chốt thứ tự thực thi kỹ thuật. Purchase chỉ Completed khi các hệ quả liên quan nhất quán.

Completed Purchase là immutable. Nếu nhập sai sau Completed, không sửa âm thầm lịch sử; sử dụng Void/Reverse phù hợp rồi tạo nghiệp vụ mới nếu cần.

### Sale và SaleLine

| Concept | Thành phần nghiệp vụ |
| --- | --- |
| Sale | SaleLine[]; Customer? (optional); Payment[]; Total; Paid Amount; Outstanding Debt; Status. |
| SaleLine | Product; Quantity; Sale Price; UnitCostAtSale / CostBasis. |

Completed Sale immutable. Mỗi SaleLine giữ cost snapshot để tính lãi gộp lịch sử, không lấy CurrentCost mới để tính lại giao dịch cũ.

Customer là optional đối với Sale nói chung; khi bán nợ cần đủ thông tin xác định người đang nợ.

### Customer

MVP hỗ trợ bán nợ nên cần Customer tối thiểu, đủ xác định người đang nợ. Ví dụ: **Name; Phone hoặc simple identifier nếu cần**.

Không xây CRM.

### Supplier

Supplier có thông tin nhận diện/liên hệ tối thiểu cần thiết.

Supplier outstanding debt không phải số tùy ý được sửa trực tiếp. Phải giải thích được từ **Purchase debt − Supplier payments ± Returns/Reversals phù hợp**, theo tác động của nghiệp vụ tương ứng. Chi tiết payment/debt nằm ở Decision B; không mở thêm workflow trả hàng NCC phức tạp.

### Return và ReturnLine

| Concept | Thành phần nghiệp vụ |
| --- | --- |
| Return | OriginalSale; ReturnLine[]; Refund/payment effect; Status. |
| ReturnLine | OriginalSaleLine; Quantity; Restock Yes/No; CostBasis. |

ReturnLine phải tham chiếu **OriginalSaleLine**, không chỉ Product. Return sử dụng cost basis của SaleLine gốc.

**Return Quantity ≤ Sold Quantity − Previously Returned Quantity.**

| Restock | Hệ quả |
| --- | --- |
| Yes | Tạo InventoryMovement tăng tồn; phục hồi quantity và inventory value theo cost basis phù hợp của giao dịch gốc. |
| No | Không tăng tồn. |

Completed Return immutable. Lựa chọn restock phải do người dùng xác định theo user flow đã duyệt; hệ thống không tự đoán.

### Void / Reversal

Quan hệ nghiệp vụ: **Original Transaction → Reversal/Void Transaction**.

Original transaction vẫn tồn tại và audit được; không hard-delete Completed transaction. Giữ liên hệ tới giao dịch gốc và lý do audit theo Step 8.

Không xây generic correction framework quá lớn trong MVP.

### StockAdjustment

StockAdjustment đại diện việc điều chỉnh tồn có lý do.

Khi hoàn tất: **StockAdjustment → InventoryMovement**.

Không sửa trực tiếp số tồn mà không có movement/source.

## Relationships — conceptual structure

Bảng thể hiện quan hệ nghiệp vụ, không quy định bảng, khóa ngoại, aggregate hoặc class implementation.

| Concept | Quan hệ nghiệp vụ |
| --- | --- |
| Store | Bối cảnh của Warehouse, User, Product, Customer, Supplier và các giao dịch MVP. |
| Purchase | Gắn Supplier, gồm PurchaseLine; khi Completed tạo movement tăng tồn và cập nhật giá vốn/payment/debt. |
| PurchaseLine | Xác định Product, quantity và purchase price của dòng nhập. |
| Sale | Gồm SaleLine, có Customer optional và cho phép nhiều Payment liên quan; gây biến động tồn khi hoàn tất. |
| SaleLine | Gắn Product và giữ cost basis tại thời điểm bán. |
| Return | Gắn OriginalSale, gồm ReturnLine và có refund/payment effect. |
| ReturnLine | Gắn OriginalSaleLine; tạo movement tăng tồn khi restock. |
| StockAdjustment | Khi hoàn tất tạo InventoryMovement có lý do. |
| InventoryMovement | Gắn Product, Warehouse và nguồn giao dịch/lý do; giải thích quantity và value. |
| Payment | Tiền thực thu/thực trả liên quan transaction và behavior trả nợ; không đồng nghĩa debt. |
| Reversal / Audit relationships | Liên kết giao dịch đảo với original transaction còn được giữ và audit được. |

## Decision A — Costing Method — APPROVED

MVP sử dụng **Moving Weighted Average — Bình quân gia quyền di động**.

### Ví dụ nhập và bán

| Thành phần | Số lượng × đơn giá | Giá trị |
| --- | --- | --- |
| Tồn trước nhập | 10 × 10.000 | 100.000 |
| Nhập thêm | 20 × 13.000 | 260.000 |
| Sau nhập | 30 × 12.000 | 360.000 |

Giá vốn bình quân mới: **(10 × 10.000 + 20 × 13.000) / 30 = 12.000**.

Khi bán, SaleLine snapshot **UnitCostAtSale = 12.000**. Giá vốn lịch sử của sale không thay đổi khi giá vốn hiện tại thay đổi sau này.

### Return Costing

Return sử dụng cost basis của SaleLine gốc. Nếu hàng quay lại kho, quantity và inventory value được phục hồi theo cost basis phù hợp của giao dịch gốc.

### Purchase Reversal

Khi reverse Purchase, không chỉ giảm quantity: phải đảo đúng **inventory quantity/value contribution** của transaction gốc theo business semantics.

Không kéo lot/FIFO/serial/expiry costing complexity vào MVP. Step 9 không chốt thuật toán implementation chi tiết cho reversal.

## Decision B — Payment & Debt Model — APPROVED

### Payment

Payment chỉ đại diện **tiền thực sự đã nhận hoặc đã trả**. Payment không đồng nghĩa Debt.

Domain model phải cho phép **nhiều Payment liên quan tới một transaction**.

Ví dụ Sale Total = 1.000.000:

| Thành phần | Số tiền |
| --- | --- |
| Payment — Cash | 300.000 |
| Payment — Transfer | 500.000 |
| Paid | 800.000 |
| Outstanding Debt | 200.000 |

UI MVP ban đầu có thể đơn giản hơn; domain không khóa vào một payment duy nhất. Step này không thiết kế UI thanh toán.

### Customer Debt

Customer Debt được suy ra từ **Credit Sales − Customer Debt Payments − Returns/Reversals phù hợp**.

Đây là quan hệ nghĩa vụ và các khoản làm giảm nợ; phải phản ánh đúng tiền đã thu trên sale, không coi toàn bộ giá trị sale đã thu một phần là khoản nợ mới.

Behavior bắt buộc: **Customer trả nợ sau → ghi nhận Payment → giảm Outstanding Debt**.

Không sửa `Customer.Debt` bằng một con số tùy ý.

### Supplier Debt

Supplier Debt được suy ra từ **Purchase obligations − Supplier Payments − Purchase Reversals/adjustments phù hợp**.

Behavior bắt buộc: **Cửa hàng trả nợ NCC sau → ghi nhận Payment → giảm Outstanding Debt**.

Số dư phải giải thích được từ nghĩa vụ mua hàng và các khoản thực trả/tác động đảo phù hợp; không chỉnh tay số nợ tùy ý.

### Payment Modeling Boundary

Không biến Payment/Debt thành generic accounting ledger đầy đủ. MVP chỉ cần đủ để trả lời chính xác:

- Bán bao nhiêu.
- Đã thu bao nhiêu.
- Khách còn nợ bao nhiêu.
- Nhập bao nhiêu.
- Đã trả NCC bao nhiêu.
- Còn nợ NCC bao nhiêu.

Conceptually có thể có **Sale Payment, Customer Debt Payment, Purchase Payment, Supplier Debt Payment**. Đây là các vai trò nghiệp vụ; implementation có dùng chung abstraction hay không sẽ quyết định ở Architecture/Technical Design, không chốt tại Step 9.

Completed transaction bất biến và behavior trả nợ sau cùng phải được bảo đảm: Payment phát sinh sau giải thích sự thay đổi outstanding balance, không cho phép sửa âm thầm nội dung nghiệp vụ lịch sử của transaction Completed.

## Domain Invariants — APPROVED

1. Completed Sale / Purchase / Return là immutable.
2. Không hard-delete transaction nghiệp vụ quan trọng.
3. Mọi thay đổi tồn kho phải tạo InventoryMovement.
4. InventoryMovement quan trọng phải truy được source/reason.
5. SaleLine phải giữ cost snapshot dùng để tính lãi gộp lịch sử.
6. Return phải tham chiếu Sale/OriginalSaleLine gốc.
7. Return không vượt số lượng thực tế còn được phép trả.
8. Payment chỉ đại diện tiền thực thu/thực trả.
9. Debt phải giải thích được từ transaction và payment, không phải số tùy ý được chỉnh tay.
10. Void/Reverse không xóa transaction gốc.
11. Purchase/Sale/Return phải tuân thủ consistency/idempotency principles đã APPROVED ở Step 8.
12. “Hôm nay cửa hàng thế nào?” và các số liệu tổng hợp là projection/derived information từ domain data, không phải một nguồn sự thật độc lập.
13. Current stock hoặc outstanding balance có thể được cache/materialize để tối ưu sau này, nhưng nguồn nghiệp vụ phải vẫn audit/explain được.

Nguyên tắc Step 8 tiếp tục áp dụng: critical operation có identity, retry không tạo business transaction mới; timeout phải kiểm tra trạng thái operation; lỗi in không rollback sale Completed.

## Liên quan và bước tiếp theo

- [MVP Scope v0.1](../product/mvp-scope-v0.1.md)
- [MVP Functional Scope v0.1](../capabilities/mvp-functional-scope-v0.1.md)
- [MVP User Flows v0.1](../ux/mvp-user-flows-v0.1.md)
- [Decision Log](../../DECISIONS.md)
- [Project State](../../PROJECT_STATE.md)
- [Open Questions](../../OPEN_QUESTIONS.md)

**Bước tiếp theo: Step 10 — Architecture.**
