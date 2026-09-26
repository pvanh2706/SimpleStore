[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ArchivePath,
    [Parameter(Mandatory)]
    [string]$ChecksumFile,
    [Parameter(Mandatory)]
    [string]$InstallRoot,
    [Parameter(Mandatory)]
    [string]$IisSiteName,
    [Parameter(Mandatory)]
    [string]$AppPoolName,
    [Parameter(Mandatory)]
    [Security.SecureString]$ConnectionString,
    [Parameter(Mandatory)]
    [string]$Operator
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$archivePath = [IO.Path]::GetFullPath($ArchivePath)
$checksumFile = [IO.Path]::GetFullPath($ChecksumFile)
$installRoot = [IO.Path]::GetFullPath($InstallRoot)
$volumeRoot = [IO.Path]::GetPathRoot($installRoot)
if ($installRoot.TrimEnd('\', '/') -eq $volumeRoot.TrimEnd('\', '/')) {
    throw 'InstallRoot must be a dedicated directory, not a volume root.'
}
$archiveExists = Test-Path -LiteralPath $archivePath -PathType Leaf
$checksumExists = Test-Path -LiteralPath $checksumFile -PathType Leaf
if (-not $archiveExists -or -not $checksumExists) {
    throw 'The release archive or checksum file does not exist.'
}
if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
    throw 'The IIS WebAdministration module is required.'
}

$expectedHash = ((Get-Content -LiteralPath $checksumFile -Raw).Trim() -split '\s+')[0]
$actualHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
if (-not ([string]::Equals($expectedHash, $actualHash, [StringComparison]::OrdinalIgnoreCase))) {
    throw 'Artifact SHA-256 checksum verification failed.'
}

New-Item -ItemType Directory -Path (Join-Path $installRoot 'releases') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $installRoot 'deployment-evidence') -Force | Out-Null
$staging = Join-Path $installRoot ('.staging-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging | Out-Null
Expand-Archive -LiteralPath $archivePath -DestinationPath $staging
$manifestPath = Join-Path $staging 'artifact-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'Artifact manifest is missing.'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.commitSha -notmatch '^[0-9a-f]{40}$') {
    throw 'Artifact manifest commit SHA is invalid.'
}
$releaseId = "$($manifest.applicationVersion)-$($manifest.commitSha.Substring(0, 12))"
$releasePath = Join-Path (Join-Path $installRoot 'releases') $releaseId
if (Test-Path -LiteralPath $releasePath) {
    throw "Release '$releaseId' already exists."
}

Import-Module WebAdministration
$poolPath = "IIS:\AppPools\$AppPoolName"
$sitePath = "IIS:\Sites\$IisSiteName"
if (-not (Test-Path $poolPath) -or -not (Test-Path $sitePath)) {
    throw 'Configured IIS site or application pool does not exist.'
}
if ((Get-ItemPropertyValue -Path $poolPath -Name managedRuntimeVersion) -ne '') {
    throw "Application pool '$AppPoolName' must use No Managed Code."
}

$started = [DateTimeOffset]::UtcNow
Stop-WebAppPool -Name $AppPoolName
try {
    $migrationEvidence = Join-Path $staging 'migration-evidence.json'
    & (Join-Path $PSScriptRoot 'Invoke-DatabaseMigration.ps1') `
        -MigrationBundle (Join-Path $staging $manifest.migrationBundle) `
        -ConnectionString $ConnectionString `
        -EvidencePath $migrationEvidence
    if ($LASTEXITCODE -ne 0) {
        throw 'Explicit migration step failed.'
    }

    Move-Item -LiteralPath $staging -Destination $releasePath
    Set-ItemProperty -Path $sitePath -Name physicalPath -Value (Join-Path $releasePath 'app')
    Start-WebAppPool -Name $AppPoolName

    [ordered]@{
        event = 'IisReleaseInstalled'
        startedAtUtc = $started.ToString('O')
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        operator = $Operator
        releaseId = $releaseId
        applicationVersion = $manifest.applicationVersion
        commitSha = $manifest.commitSha
        artifactSha256 = $actualHash.ToLowerInvariant()
        iisSite = $IisSiteName
        appPool = $AppPoolName
        releasePath = $releasePath
        migrationResult = 'Success'
        result = 'Installed; health and authenticated smoke still required'
    } | ConvertTo-Json | Set-Content `
        -LiteralPath (Join-Path $installRoot "deployment-evidence\$releaseId-install.json") `
        -Encoding utf8
}
catch {
    if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Started') {
        Start-WebAppPool -Name $AppPoolName
    }
    throw
}
