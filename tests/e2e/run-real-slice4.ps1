$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$runId = [Guid]::NewGuid().ToString('N')
$databaseName = "SimpleStoreE2E_$runId"
$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=$databaseName;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
$infrastructureProject = Join-Path $repositoryRoot 'src\backend\SimpleStore.Infrastructure'
$apiProject = Join-Path $repositoryRoot 'src\backend\SimpleStore.Api'

$env:ConnectionStrings__SimpleStore = $connectionString
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'https://localhost:7237;http://localhost:5237'
$env:DevelopmentOwner__Email = "owner-$runId@example.test"
$env:DevelopmentOwner__Password = "E2E-$runId!aA1"
$env:SIMPLESTORE_E2E_EMAIL = $env:DevelopmentOwner__Email
$env:SIMPLESTORE_E2E_PASSWORD = $env:DevelopmentOwner__Password
$env:SIMPLESTORE_E2E_RUN_ID = $runId

Push-Location $repositoryRoot
try {
    dotnet ef database update --project $infrastructureProject --startup-project $apiProject --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Could not migrate temporary database '$databaseName'." }

    pnpm --dir tests/e2e exec playwright test --config playwright.real.config.ts --grep 'Slice 4 real correction flows'
    if ($LASTEXITCODE -ne 0) { throw 'The real Slice 4 Playwright flows failed.' }
}
finally {
    dotnet ef database drop --force --project $infrastructureProject --startup-project $apiProject --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { Write-Warning "Could not drop temporary database '$databaseName'." }
    Pop-Location
}
