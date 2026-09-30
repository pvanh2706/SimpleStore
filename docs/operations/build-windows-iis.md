# Build SimpleStore và triển khai lên IIS

Tài liệu này dùng cho repository SimpleStore trên Windows. Frontend Vue và backend ASP.NET Core được phục vụ chung bởi **một** IIS site. Có thể build riêng từng phần để kiểm tra, nhưng một lần cập nhật IIS phải dùng artifact release đầy đủ để giữ đúng version, manifest, checksum và migration bundle. Quy trình triển khai và xử lý lỗi chi tiết nằm trong [deployment runbook](deployment-runbook-v0.1.md).

## Điều kiện chung

- Mở PowerShell tại thư mục gốc repository. Máy build cần .NET SDK theo `global.json`, Node.js 24+, pnpm 10.15.1+ và Git. Windows PowerShell 5.1 dùng `powershell.exe`; nếu đã cài PowerShell 7 có thể dùng `pwsh`.
- Trước khi đóng gói release, kiểm tra `git status --short` rỗng và dùng commit đã review. `New-ReleaseArtifact.ps1` từ chối working tree có thay đổi nếu không truyền `-AllowDirty`; không dùng tùy chọn đó cho release triển khai.
- Máy IIS cần Hosting Bundle phù hợp, site/app pool đã tạo, app pool đặt **No Managed Code**, HTTPS, cấu hình SQL được bảo vệ và quyền truy cập theo [runbook](deployment-runbook-v0.1.md). `sqlcmd` cần có trên máy chạy bước kiểm tra migration. Node.js và pnpm chỉ cần trên máy build.
- Các lệnh dưới đây chạy từ thư mục gốc. `artifacts/` được Git bỏ qua.

## 1. Build đầy đủ để đưa lên IIS

Chạy trong PowerShell thông thường trên máy build:

```powershell
git status --short
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\release\New-ReleaseArtifact.ps1 -Version 0.1.0
```

Script tự restore .NET tool/dependency, cài pnpm theo lockfile, build frontend, `dotnet publish` backend Release, chép `dist` vào `wwwroot`, tạo EF migration bundle cho Windows x64 rồi xuất:

```text
artifacts\SimpleStore-<version>-<12-ký-tự-SHA>.zip
artifacts\SimpleStore-<version>-<12-ký-tự-SHA>.zip.sha256
artifacts\SimpleStore-<version>-<12-ký-tự-SHA>\artifact-manifest.json
```

ZIP là đơn vị triển khai. Không chép riêng `dist` hay DLL vào một release đang chạy; như vậy manifest và khả năng đối chiếu/rollback sẽ không còn đúng.

### Cài artifact lên site IIS đã có

Các lệnh cài đặt cần chạy trong **PowerShell mở bằng quyền Administrator** trên máy IIS. Ví dụ dưới đây dùng site thử nghiệm cục bộ; thay tên site, đường dẫn, SQL target và HTTPS URL theo môi trường thực tế. Kiểm tra backup mới, migration hiện tại và quyết định phục hồi trước khi chạy installer. Không đưa connection string hay mật khẩu vào Git, log hoặc command line.

```powershell
$sha = (git rev-parse HEAD).Trim()
$archivePath = (Resolve-Path ".\artifacts\SimpleStore-0.1.0-$($sha.Substring(0,12)).zip").Path
$checksumPath = "$archivePath.sha256"
$expectedHash = ((Get-Content -LiteralPath $checksumPath -Raw).Trim() -split '\s+')[0]
$actualHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
if ($actualHash -ine $expectedHash) { throw 'Release checksum không khớp.' }

& .\tools\operations\deploy\Get-MigrationState.ps1 `
  -ServerInstance 'localhost\SQLEXPRESS' -DatabaseName 'SimpleStorePilotIisTest'
& .\tools\operations\backup\Test-BackupFreshness.ps1 `
  -DatabaseName 'SimpleStorePilotIisTest' -BackupRoot 'D:\SimpleStorePilotTest\backups'

$connection = Read-Host 'SQL connection string được bảo vệ' -AsSecureString
& .\tools\operations\deploy\Install-SimpleStoreRelease.ps1 `
  -ArchivePath $archivePath `
  -ChecksumFile $checksumPath `
  -InstallRoot 'D:\SimpleStorePilotTest' `
  -IisSiteName 'SimpleStore-Pilot-Test' `
  -AppPoolName 'SimpleStore-Pilot-Test' `
  -ConnectionString $connection `
  -Operator $env:USERNAME
```

Installer kiểm checksum/manifest, dừng app pool, chạy migration bundle rõ ràng, tạo thư mục release mới, chuyển IIS physical path và khởi động app pool. Release cũ được giữ lại. Nếu migration đã bắt đầu rồi bước sau thất bại, làm theo quy trình phục hồi trong [runbook](deployment-runbook-v0.1.md); không tự chuyển về bản cũ khi chưa kiểm tra tương thích schema.

Sau khi installer thành công, kiểm tra HTTPS và dùng tài khoản ứng dụng thử nghiệm được cấp hợp lệ cho smoke đọc:

```powershell
Invoke-WebRequest 'https://localhost:8443/health/live' -UseBasicParsing
Invoke-WebRequest 'https://localhost:8443/health/ready' -UseBasicParsing
Invoke-WebRequest 'https://localhost:8443/sales/new' -UseBasicParsing

$credential = Get-Credential
& .\tools\operations\deploy\Invoke-DeploymentSmoke.ps1 `
  -BaseUri ([uri]'https://localhost:8443') `
  -Credential $credential `
  -EvidencePath 'D:\SimpleStorePilotTest\deployment-evidence\latest-smoke.json'
```

Đối chiếu `commitSha` trong smoke với manifest và commit định triển khai. Smoke chỉ đăng nhập và đọc health, version, session, Store, Product; không tạo Sale/Purchase. Ghi lại release trước/sau, kết quả migration, health, version và đường dẫn evidence ngoài release.

## 2. Chỉ build frontend

Không cần .NET hay quyền quản trị IIS. Chạy tại thư mục gốc:

```powershell
pnpm install --frozen-lockfile
pnpm --dir src/frontend/simplestore-web build
pnpm --dir src/frontend/simplestore-web test
```

Output nằm ở `src/frontend/simplestore-web/dist/`. Lệnh `build` chạy kiểm tra TypeScript (`vue-tsc`) rồi Vite production build; `test` chạy toàn bộ Vitest frontend. Nếu chỉ sửa frontend nhưng muốn cập nhật IIS, vẫn dùng **mục 1** để tạo release đầy đủ: backend hiện là host phục vụ SPA, và release phải có một manifest/version thống nhất.

## 3. Chỉ build backend

Không cần Node.js/pnpm. Các lệnh sau chỉ restore/build/publish dự án API và các thư viện backend phụ thuộc:

```powershell
dotnet restore .\src\backend\SimpleStore.Api\SimpleStore.Api.csproj
dotnet build .\src\backend\SimpleStore.Api\SimpleStore.Api.csproj --configuration Release --no-restore
dotnet publish .\src\backend\SimpleStore.Api\SimpleStore.Api.csproj `
  --configuration Release --no-restore `
  --output .\artifacts\backend-only
```

Output publish ở `artifacts/backend-only/` dùng để kiểm tra backend riêng; nó **không** phải artifact triển khai IIS đầy đủ vì thiếu frontend build, release manifest/checksum và migration bundle. Nếu chỉ sửa backend nhưng muốn cập nhật IIS, dùng **mục 1**. Migration không tự chạy lúc khởi động ứng dụng; installer thực hiện bước đó rõ ràng.

## Khi gặp lỗi

- Build frontend: kiểm Node/pnpm đúng version, sau đó chạy lại `pnpm install --frozen-lockfile`; không sửa lockfile chỉ để vượt lỗi release.
- Build backend: kiểm `dotnet --version` với `global.json` và kết quả `dotnet restore` trước khi chạy `--no-restore`.
- Installer hoặc health/smoke thất bại: giữ evidence, log và artifact; kiểm trạng thái migration/app pool rồi theo [quy trình failure handling](deployment-runbook-v0.1.md#61-fail-safe-deployment-failure-handling). Không xóa release cũ hoặc chạy EF `Down()` để chữa lỗi triển khai.
