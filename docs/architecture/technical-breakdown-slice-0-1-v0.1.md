# Technical Breakdown — Slice 0–1 v0.1

- **Step:** 11 — Development Plan / Technical Design Breakdown v0.1
- **Trạng thái:** `APPROVED`
- **Người phê duyệt:** Product Owner

Ghi nhận định hướng kỹ thuật đã được Product Owner duyệt. Các phiên bản dưới đây là định hướng của quyết định, chưa pin patch version hoặc tạo dependency/lockfile. Không chốt JSON schema/route cụ thể và không triển khai code trong lần cập nhật này.

## Slice 0 — Engineering Foundation

### Backend stack

Định hướng:

- .NET 10 LTS
- ASP.NET Core
- EF Core 10
- SQL Server
- ASP.NET Core Identity/Auth
- Cookie authentication cho browser-based SPA

Không dùng JWT/localStorage làm mặc định cho SPA cùng site.

### Frontend stack

Định hướng:

- Vue 3.5 stable
- TypeScript
- Vite
- Vue Router
- Pinia
- Tailwind CSS
- shadcn-vue khi thực sự cần component
- pnpm
- Node 24 LTS

Không dùng Vue RC cho MVP.

---

### Repo / Solution Structure

Các đường dẫn tính từ repository root `SimpleStore/`:

| Đường dẫn | Thành phần |
| --- | --- |
| `SimpleStore.sln` | .NET solution |
| `src/backend/SimpleStore.Api/` | HTTP boundary |
| `src/backend/SimpleStore.Application/` | Application use cases |
| `src/backend/SimpleStore.Domain/` | Domain behavior |
| `src/backend/SimpleStore.Infrastructure/` | Persistence/infrastructure |
| `src/frontend/simplestore-web/` | Vue app |
| `tests/SimpleStore.Domain.Tests/` | Domain tests |
| `tests/SimpleStore.IntegrationTests/` | Integration tests |
| `tests/e2e/` | Critical E2E tests |
| `docs/` | Tài liệu |

Không tạo sớm:

- SharedKernel
- Common
- Abstractions
- Core
- BuildingBlocks

trừ khi nhu cầu thực tế xuất hiện.

---

### Backend Implementation Style — APPROVED

Dùng thin Controllers.

Controller:

- nhận HTTP request;
- lấy authentication context;
- gọi application use case;
- trả HTTP response.

Business logic nằm ở Application/Domain.

Không bắt buộc MediatR ngay từ đầu.

Có thể dùng trực tiếp:

- CompleteSaleUseCase
- CompletePurchaseUseCase
- CreateProductUseCase

Chỉ đánh giá MediatR khi có nhu cầu thực sự.

---

### Error Handling — APPROVED

Backend dùng ProblemDetails / typed error response.

Frontend không parse text message để điều khiển logic.

Ví dụ concept:

- error type/code: insufficient-stock;
- HTTP status phù hợp;
- message thân thiện cho user.

---

### Authentication Direction — APPROVED

Vue SPA cùng site sử dụng:

- ASP.NET Core Identity/Auth
- secure HttpOnly cookie

Flow:

Vue login
→ backend authenticate
→ set HttpOnly cookie
→ browser tự gửi cookie ở các request sau.

MVP role:

- Owner
- Cashier

Backend là authorization boundary.

---

### EF Core & Migration Direction — APPROVED

EF Core migrations đặt trong Infrastructure.

Development có thể chạy migration explicit.

Production:

migration là explicit deployment step.

Không mặc định app tự chạy migration production khi startup.

---

### Testing Foundation — APPROVED

Backend/domain:

- xUnit

Integration:

- xUnit
- WebApplicationFactory
- SQL Server test database

Không dùng EF InMemory để kiểm chứng inventory transaction/concurrency.

Frontend:

- Vitest
- Vue Test Utils

E2E:

- Playwright

Slice 0 chỉ dựng nền tảng.

---

## Decision D — Tenancy Foundation — APPROVED

Business MVP:

1 tenant/account
→ 1 Store
→ 1 Main Warehouse.

Nhưng shared deployment/database có thể phục vụ nhiều Store tenant.

Business data phải được scope theo Store/Tenant, ví dụ:

- Product
- Sale
- Purchase
- InventoryBalance
- InventoryMovement
- Customer
- Supplier

Không có multi-branch hoặc cross-store business feature trong MVP.

Không xây tenant management platform lớn.

Mục tiêu:

Tránh phải thêm StoreId/tenant scoping vào toàn hệ thống sau khi đã có dữ liệu/pilot thứ hai.

Phải test isolation:

User Store A không đọc/sửa dữ liệu Store B.

---

## Slice 1 — Setup + Product Technical Breakdown

### Store initialization

Khi tenant mới được tạo:

Create Store
→ Create Main Warehouse automatically
→ Associate Owner

Rule:

MVP Store có đúng một Main Warehouse.

User không cần tự cấu hình warehouse phức tạp khi onboarding.

---

### Product model tối thiểu

Product cần hỗ trợ:

- Store scope
- SKU
- Barcode optional
- Name
- Unit
- SalePrice
- ReferencePurchaseCost optional
- Status

Behavior:

- SKU là khái niệm bắt buộc nhưng có thể auto-generate nếu user không nhập;
- Barcode optional;
- Name required;
- Unit required;
- SalePrice required;
- ReferencePurchaseCost optional nếu chưa biết.

Không mở rộng pricing/promotions ngoài Step 7.

---

## Decision E — Initial Import Policy — APPROVED

Import dùng template cố định với hai giai đoạn: Validate/Preview, sau đó Confirm:

Validate
→ Preview
→ Confirm

Confirm là all-or-nothing transaction.

Flow:

Upload template
→ Parse + Validate
→ Preview
→ Nếu có lỗi: không import, hiển thị lỗi rõ
→ Nếu không lỗi: Confirm
→ DB transaction import toàn bộ.

Không partial import ở pilot đầu.

Validation phải báo rõ:

- dòng lỗi;
- trường lỗi;
- lý do lỗi;
- barcode/SKU duplicate phù hợp;
- kiểu dữ liệu;
- required fields.

---

### Opening Inventory — APPROVED

Template định hướng có thể gồm:

- SKU
- Barcode
- Tên hàng
- Đơn vị
- Giá bán
- Giá vốn đầu
- Tồn đầu

Nếu Opening Qty > 0 thì cần Opening Cost hợp lệ để xác định inventory value.

Khi Confirm import:

Product

- InventoryMovement(Type = OpeningBalance)
- InventoryBalance

được ghi atomically.

Không cập nhật trực tiếp Product.Stock.

Ví dụ:

Qty = 20
OpeningCost = 8.000

→ Movement +20 / +160.000
→ Balance Qty 20 / Value 160.000 / AvgCost 8.000

Opening inventory phải tuân thủ architecture ledger/materialized balance của Step 10.

---

### Slice 1 Use Cases / API Boundary — conceptual

Use case:

- InitializeStore
- CreateProduct
- UpdateProduct
- DeactivateProduct
- GetProducts
- GetProduct
- ValidateProductImport
- ConfirmProductImport
- GetInventoryBalance
- GetInventoryMovements

Không chốt JSON schema/route cụ thể tại Step 11.

---

### Slice 1 Definition of Done — APPROVED

Owner có thể:

- login;
- tạo Store;
- hệ thống tạo Main Warehouse;
- tạo Product bằng tay;
- tải/điền/import template;
- nhận validation rõ ràng;
- Confirm import;
- xem Product;
- xem tồn hiện tại;
- xem source OpeningBalance.

Automated tests tối thiểu chứng minh:

- duplicate barcode phù hợp bị chặn;
- import all-or-nothing;
- retry confirm không duplicate;
- opening stock tạo InventoryMovement;
- InventoryBalance nhất quán với movement;
- Store/Tenant isolation hoạt động;
- User Store A không đọc Product Store B.

---

## Quan hệ với các quyết định đã duyệt

- Business MVP vẫn là một Store và một Main Warehouse trong mỗi tenant/account. Shared deployment/database phục vụ nhiều tenant không tạo multi-branch hoặc cross-store business feature.
- InventoryMovement và InventoryBalance phải cập nhật atomically theo Step 10; OpeningBalance là nguồn giải thích tồn đầu.
- Retry Confirm import không được duplicate. Cơ chế cụ thể phải tuân thủ operation identity/idempotency của Step 10.
- Các chi tiết SQL schema, API contract, mapping và concurrency được làm just enough trước từng slice; các chi tiết Slice 2+ vẫn mở, không thiết kế toàn bộ trước khi code.
- C7 vẫn chỉ là integration point; C14 vẫn là experiment cần kiểm chứng value/willingness-to-pay.

## Tài liệu liên quan

- [Development Plan v0.1](development-plan-v0.1.md)
- [Architecture v0.1](architecture-v0.1.md)
- [Domain Model v0.1](domain-model-v0.1.md)
- [Open Questions](../../OPEN_QUESTIONS.md)

**Ready to begin implementation — Slice 0 Engineering Foundation**; sau đó **Slice 1 — Setup + Product**.
