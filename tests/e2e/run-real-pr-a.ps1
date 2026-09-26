$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$runId = [Guid]::NewGuid().ToString('N')
$databaseName = "SimpleStoreE2E_$runId"
$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=$databaseName;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
$infrastructureProject = Join-Path $repositoryRoot 'src\backend\SimpleStore.Infrastructure'
$apiProject = Join-Path $repositoryRoot 'src\backend\SimpleStore.Api'
$ownerEmail = "owner-$runId@example.test"
$ownerPassword = "E2E-$runId!aA1"

$env:ConnectionStrings__SimpleStore = $connectionString
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ASPNETCORE_URLS = 'https://localhost:7237;http://localhost:5237'
$env:SIMPLESTORE_E2E_EMAIL = $ownerEmail
$env:SIMPLESTORE_E2E_PASSWORD = $ownerPassword
$env:SIMPLESTORE_E2E_RUN_ID = $runId
$env:DevelopmentOwner__Email = $null
$env:DevelopmentOwner__Password = $null

Push-Location $repositoryRoot
try {
    dotnet ef database update --project $infrastructureProject --startup-project $apiProject --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw "Could not migrate temporary database '$databaseName'." }

    $bootstrapOutput = $ownerPassword | dotnet run --project $apiProject --configuration Release --no-build --no-launch-profile -- bootstrap-owner --email $ownerEmail --password-stdin
    if ($LASTEXITCODE -ne 0) { throw 'The production Owner bootstrap command failed.' }
    $bootstrapEvidence = $bootstrapOutput | Select-Object -Last 1 | ConvertFrom-Json
    if ($bootstrapEvidence.result -ne 'Success' -or $bootstrapEvidence.normalizedOwnerIdentity -ne $ownerEmail.ToUpperInvariant()) {
        throw 'The bootstrap command did not return the expected non-secret evidence.'
    }
    if (($bootstrapOutput -join "`n").Contains($ownerPassword)) {
        throw 'The bootstrap command exposed its input secret.'
    }

    pnpm --dir tests/e2e exec playwright test --config playwright.real.config.ts --grep 'Pilot Readiness PR-A real critical flow'
    if ($LASTEXITCODE -ne 0) { throw 'The real Pilot Readiness PR-A Playwright flow failed.' }
}
finally {
    dotnet ef database drop --force --project $infrastructureProject --startup-project $apiProject --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { Write-Warning "Could not drop temporary database '$databaseName'." }
    Pop-Location
}
