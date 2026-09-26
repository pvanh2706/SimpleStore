[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0',
    [string]$OutputRoot,
    [switch]$AllowDirty,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot 'artifacts'
}
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

foreach ($command in @('dotnet', 'git', 'node', 'pnpm')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required build command '$command' was not found."
    }
}

$nodeVersion = (& node --version).Trim().TrimStart('v')
if ([int]($nodeVersion.Split('.')[0]) -lt 24) {
    throw "Node.js 24 or newer is required on the build machine. Found $nodeVersion."
}

$commitSha = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $commitSha -notmatch '^[0-9a-f]{40}$') {
    throw 'Unable to resolve the Git commit SHA.'
}
if (-not $AllowDirty -and (& git -C $repositoryRoot status --porcelain)) {
    throw 'The worktree is dirty. Commit reviewed changes or pass -AllowDirty for a non-release exercise.'
}

$shortSha = $commitSha.Substring(0, 12)
$releaseId = "SimpleStore-$Version-$shortSha"
$stagingRoot = Join-Path $OutputRoot $releaseId
$applicationRoot = Join-Path $stagingRoot 'app'
$archivePath = Join-Path $OutputRoot "$releaseId.zip"
$checksumPath = "$archivePath.sha256"

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
foreach ($path in @($stagingRoot, $archivePath, $checksumPath)) {
    if (Test-Path -LiteralPath $path) {
        if (-not $Force) {
            throw "Output '$path' already exists. Use -Force only after verifying the target."
        }
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}
New-Item -ItemType Directory -Path $applicationRoot -Force | Out-Null

Push-Location $repositoryRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed.' }
    & dotnet restore 'SimpleStore.sln'
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & pnpm install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) { throw 'Frozen frontend install failed.' }
    & pnpm --dir 'src/frontend/simplestore-web' build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend production build failed.' }

    & dotnet publish 'src/backend/SimpleStore.Api/SimpleStore.Api.csproj' `
        --configuration Release `
        --no-restore `
        --output $applicationRoot `
        "/p:Version=$Version" `
        "/p:VersionPrefix=$Version" `
        "/p:InformationalVersion=$Version+$commitSha" `
        "/p:RepositoryCommit=$commitSha"
    if ($LASTEXITCODE -ne 0) { throw 'Backend Release publish failed.' }

    $webRoot = Join-Path $applicationRoot 'wwwroot'
    New-Item -ItemType Directory -Path $webRoot -Force | Out-Null
    Copy-Item -Path 'src/frontend/simplestore-web/dist/*' -Destination $webRoot -Recurse -Force

    $migrationBundle = Join-Path $applicationRoot 'tools\simplestore-migrate.exe'
    New-Item -ItemType Directory -Path (Split-Path $migrationBundle) -Force | Out-Null
    & dotnet restore 'src/backend/SimpleStore.Api/SimpleStore.Api.csproj' --runtime win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Windows runtime restore for the migration bundle failed.' }
    & dotnet ef migrations bundle `
        --project 'src/backend/SimpleStore.Infrastructure/SimpleStore.Infrastructure.csproj' `
        --startup-project 'src/backend/SimpleStore.Api/SimpleStore.Api.csproj' `
        --configuration Release `
        --runtime win-x64 `
        --output $migrationBundle `
        --force
    if ($LASTEXITCODE -ne 0) { throw 'EF migration bundle creation failed.' }

    $stagingUri = [Uri]::new($stagingRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar)
    $files = Get-ChildItem -LiteralPath $applicationRoot -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            [ordered]@{
                path = [Uri]::UnescapeDataString(
                    $stagingUri.MakeRelativeUri([Uri]::new($_.FullName)).ToString())
                length = $_.Length
                sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }
    $manifest = [ordered]@{
        schemaVersion = 1
        application = 'SimpleStore'
        applicationVersion = $Version
        commitSha = $commitSha
        builtAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        target = 'Windows Server + IIS / win-x64 migration bundle'
        productionRequiresNode = $false
        migrationBundle = 'app/tools/simplestore-migrate.exe'
        files = @($files)
    }
    $manifest | ConvertTo-Json -Depth 6 |
        Set-Content -LiteralPath (Join-Path $stagingRoot 'artifact-manifest.json') -Encoding utf8

    Compress-Archive -Path (Join-Path $stagingRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal
    $archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$archiveHash *$(Split-Path $archivePath -Leaf)" |
        Set-Content -LiteralPath $checksumPath -Encoding ascii

    [ordered]@{
        artifact = $archivePath
        sha256 = $archiveHash
        checksumFile = $checksumPath
        applicationVersion = $Version
        commitSha = $commitSha
    } | ConvertTo-Json
}
finally {
    Pop-Location
}
