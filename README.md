# SimpleStore

SimpleStore là ứng dụng hỗ trợ chủ cửa hàng tạp hóa nhỏ tại Việt Nam vận hành và hiểu tình hình kinh doanh. Repository hiện có **Slice 0 — Engineering Foundation**; chưa có chức năng nghiệp vụ Product hoặc Inventory.

## Cấu trúc

- `src/backend/`: ASP.NET Core modular monolith với các project Api, Application, Domain và Infrastructure.
- `src/frontend/simplestore-web/`: Vue 3 SPA dùng TypeScript, Vite, Vue Router, Pinia và Tailwind CSS.
- `tests/`: xUnit domain/integration tests và Playwright E2E foundation.

## Yêu cầu phát triển

- .NET SDK 10
- Node.js 24 LTS
- pnpm 10
- SQL Server hoặc SQL Server LocalDB cho phát triển cục bộ

## Backend

```powershell
dotnet restore SimpleStore.sln
dotnet build SimpleStore.sln
dotnet test SimpleStore.sln
dotnet run --project src/backend/SimpleStore.Api
```

Connection string `ConnectionStrings:SimpleStore` có thể được override bằng environment variable hoặc user secrets. Migration production là bước deployment explicit; ứng dụng không tự migrate khi startup.

```powershell
dotnet user-secrets set "ConnectionStrings:SimpleStore" "<connection-string>" --project src/backend/SimpleStore.Api
dotnet ef database update --project src/backend/SimpleStore.Infrastructure --startup-project src/backend/SimpleStore.Api
```

## Frontend

```powershell
corepack enable
pnpm install
pnpm --dir src/frontend/simplestore-web dev
pnpm --dir src/frontend/simplestore-web build
pnpm --dir src/frontend/simplestore-web test
```

Playwright foundation nằm tại `tests/e2e`. Cài browser bằng `pnpm --dir tests/e2e exec playwright install chromium` trước khi chạy `pnpm test:e2e`.

## Tài liệu

- `PROJECT_STATE.md`: trạng thái hiện tại của dự án.
- `DECISIONS.md`: nhật ký quyết định sản phẩm.
- `OPEN_QUESTIONS.md`: các câu hỏi cần làm rõ.
- `docs/product/product-vision-v0.1.md`: Product Vision v0.1 đã được phê duyệt.
- `docs/product/problem-definition-v0.1.md`: Problem Definition v0.1 đã được phê duyệt.
- `docs/`: tài liệu Product/BA, nghiên cứu, UX và kiến trúc.
