# SimpleStore

SimpleStore là ứng dụng hỗ trợ chủ cửa hàng tạp hóa nhỏ tại Việt Nam vận hành và hiểu tình hình kinh doanh. Repository hiện có **Slice 1 — Setup + Product** chạy xuyên suốt Vue → API → Application/Domain → EF Core → SQL Server.

## Cấu trúc

- `src/backend/`: ASP.NET Core modular monolith với Api, Application, Domain và Infrastructure.
- `src/frontend/simplestore-web/`: Vue 3 SPA dùng TypeScript, Vite, Vue Router, Pinia và Tailwind CSS.
- `tests/`: xUnit domain/integration tests dùng SQL Server thật và Playwright E2E foundation.

## Yêu cầu phát triển

- .NET SDK 10
- Node.js 24 LTS
- pnpm 10
- SQL Server hoặc SQL Server LocalDB

## Chạy backend cục bộ

Ứng dụng không tự chạy migration khi startup. Cấu hình connection string và development Owner bằng user-secrets, sau đó apply migration rõ ràng:

```powershell
dotnet user-secrets set "ConnectionStrings:SimpleStore" "Server=(localdb)\MSSQLLocalDB;Database=SimpleStore;Trusted_Connection=True;TrustServerCertificate=True" --project src/backend/SimpleStore.Api
dotnet user-secrets set "DevelopmentOwner:Email" "owner@example.local" --project src/backend/SimpleStore.Api
dotnet user-secrets set "DevelopmentOwner:Password" "<strong-local-password>" --project src/backend/SimpleStore.Api
dotnet ef database update --project src/backend/SimpleStore.Infrastructure --startup-project src/backend/SimpleStore.Api
dotnet run --project src/backend/SimpleStore.Api
```

Development Owner chỉ được bootstrap trong môi trường `Development`; không có mật khẩu production hard-code. API dùng secure HttpOnly cookie và antiforgery header `X-CSRF-TOKEN`.

## Chạy frontend

```powershell
corepack enable
pnpm install
pnpm --dir src/frontend/simplestore-web dev
```

Vite chạy HTTPS để tương thích secure cookie và proxy `/api` tới `https://localhost:7237`.

```powershell
dotnet build SimpleStore.sln --configuration Release
dotnet test SimpleStore.sln --configuration Release
pnpm --dir src/frontend/simplestore-web build
pnpm --dir src/frontend/simplestore-web test
```

## Import sản phẩm

Slice 1 dùng CSV UTF-8 template cố định: `SKU,Barcode,Name,Unit,SalePrice,OpeningCost,OpeningQuantity`. Flow là Validate → Preview → Confirm; confirm chạy all-or-nothing và retry không tạo dữ liệu trùng. Sản phẩm không có tồn đầu vẫn được tạo `InventoryBalance` bằng 0.

## E2E

Playwright foundation nằm tại `tests/e2e`. Critical Slice 1 flow cần một SQL Server, API đã migrate, development Owner và frontend chạy cùng lúc. CI hiện kiểm chứng behavior bằng integration tests với SQL Server thật; chưa thêm orchestration E2E đầy đủ để tránh dựng deployment harness ngoài phạm vi Slice 1.

## Tài liệu

- `PROJECT_STATE.md`: trạng thái hiện tại.
- `DECISIONS.md`: quyết định sản phẩm APPROVED.
- `OPEN_QUESTIONS.md`: câu hỏi cần làm rõ.
- `docs/`: tài liệu Product/BA, UX và kiến trúc.
