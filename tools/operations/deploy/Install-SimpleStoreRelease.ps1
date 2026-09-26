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

$previousPhysicalPath = [string](Get-ItemPropertyValue -Path $sitePath -Name physicalPath)
$previousPathLeaf = Split-Path -Leaf $previousPhysicalPath.TrimEnd('\', '/')
$previousReleaseId = if ($previousPathLeaf -eq 'app') {
    Split-Path -Leaf (Split-Path -Parent $previousPhysicalPath.TrimEnd('\', '/'))
}
else {
    $previousPathLeaf
}

$started = [DateTimeOffset]::UtcNow
$phase = 'StopAppPool'
$appPoolStopped = $false
$migrationStarted = $false
$migrationSucceeded = $false
$releaseMoved = $false
$iisPhysicalPathChanged = $false
$newAppStartAttempted = $false
$failureEvidencePath = Join-Path $installRoot (
    "deployment-evidence\$releaseId-install-failed-$($started.ToString('yyyyMMddTHHmmssfffZ')).json")

try {
    Stop-WebAppPool -Name $AppPoolName
    $appPoolStopped = $true

    $phase = 'RunExplicitMigration'
    # Set this before invoking the bundle. If invocation is ambiguous or fails to
    # launch cleanly, recovery must still assume the database may have changed.
    $migrationStarted = $true
    $migrationEvidence = Join-Path $staging 'migration-evidence.json'
    & (Join-Path $PSScriptRoot 'Invoke-DatabaseMigration.ps1') `
        -MigrationBundle (Join-Path $staging $manifest.migrationBundle) `
        -ConnectionString $ConnectionString `
        -EvidencePath $migrationEvidence
    if ($LASTEXITCODE -ne 0) {
        throw 'Explicit migration step failed.'
    }
    $migrationSucceeded = $true

    $phase = 'MoveVersionedRelease'
    Move-Item -LiteralPath $staging -Destination $releasePath
    $releaseMoved = $true

    $phase = 'SwitchIisPhysicalPath'
    Set-ItemProperty -Path $sitePath -Name physicalPath -Value (Join-Path $releasePath 'app')
    $iisPhysicalPathChanged = $true

    $phase = 'StartCandidateApplication'
    $newAppStartAttempted = $true
    Start-WebAppPool -Name $AppPoolName

    $phase = 'WriteInstallEvidence'
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
        previousReleaseId = $previousReleaseId
        migrationResult = 'Success'
        result = 'Installed; health and authenticated smoke still required'
    } | ConvertTo-Json | Set-Content `
        -LiteralPath (Join-Path $installRoot "deployment-evidence\$releaseId-install.json") `
        -Encoding utf8
}
catch {
    $failure = $_
    $failedPhase = $phase
    $failedAt = [DateTimeOffset]::UtcNow
    $existingApplicationAutoRestarted = $false
    $pathStillPrevious = $false
    $poolStopEnforced = $false
    $recoveryAction = 'ApplicationPoolLeftStoppedForManualReviewedRecovery'

    try {
        $currentPhysicalPath = [string](Get-ItemPropertyValue -Path $sitePath -Name physicalPath)
        $pathStillPrevious = [string]::Equals(
            $currentPhysicalPath,
            $previousPhysicalPath,
            [StringComparison]::OrdinalIgnoreCase)
        $iisPhysicalPathChanged = -not $pathStillPrevious
    }
    catch {
        $currentPhysicalPath = $null
    }

    if (-not $migrationStarted -and $pathStillPrevious) {
        # The schema was not touched and IIS still targets the original release.
        # Only this fully proven pre-migration case may resume the existing app.
        try {
            if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Started') {
                Start-WebAppPool -Name $AppPoolName
                $existingApplicationAutoRestarted = $true
                $recoveryAction = 'ExistingApplicationRestartedAfterProvenPreMigrationFailure'
            }
            else {
                $recoveryAction = 'ExistingApplicationRemainedStartedAfterProvenPreMigrationFailure'
            }
        }
        catch {
            $recoveryAction = 'ExistingApplicationRestartFailed;ManualReviewedRecoveryRequired'
        }
    }
    else {
        # Once migration invocation begins, never resume either the previous or
        # candidate application automatically. A partially started pool is
        # stopped again so schema compatibility must be reviewed explicitly.
        try {
            if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Stopped') {
                Stop-WebAppPool -Name $AppPoolName
            }
            $poolStopEnforced = $true
        }
        catch {
            $recoveryAction = 'ApplicationPoolStopCouldNotBeConfirmed;ImmediateOperatorActionRequired'
        }
    }

    try {
        $finalAppPoolState = [string](Get-WebAppPoolState -Name $AppPoolName).Value
    }
    catch {
        $finalAppPoolState = 'Unknown'
    }

    $failureEvidence = [ordered]@{
        event = 'IisReleaseInstallFailed'
        startedAtUtc = $started.ToString('O')
        failedAtUtc = $failedAt.ToString('O')
        operator = $Operator
        failedPhase = $failedPhase
        releaseId = $releaseId
        applicationVersion = $manifest.applicationVersion
        commitSha = $manifest.commitSha
        artifactSha256 = $actualHash.ToLowerInvariant()
        iisSite = $IisSiteName
        appPool = $AppPoolName
        appPoolState = $finalAppPoolState
        appPoolStopInitiallyCompleted = $appPoolStopped
        appPoolStopEnforcedAfterFailure = $poolStopEnforced
        previousReleaseId = $previousReleaseId
        previousPathStillConfigured = $pathStillPrevious
        migrationAttempted = $migrationStarted
        migrationSucceeded = $migrationSucceeded
        releaseMoved = $releaseMoved
        iisPhysicalPathChanged = $iisPhysicalPathChanged
        newAppStartAttempted = $newAppStartAttempted
        existingApplicationAutoRestarted = $existingApplicationAutoRestarted
        failureType = $failure.Exception.GetType().FullName
        recoveryAction = $recoveryAction
        recoveryStatus = 'ManualReviewedRecoveryRequired'
    }

    $evidenceWriteFailed = $false
    try {
        $failureEvidence | ConvertTo-Json | Set-Content -LiteralPath $failureEvidencePath -Encoding utf8
    }
    catch {
        $evidenceWriteFailed = $true
    }

    $evidenceMessage = if ($evidenceWriteFailed) {
        'Failure evidence could not be written; preserve console and migration evidence immediately.'
    }
    else {
        "Failure evidence: '$failureEvidencePath'."
    }

    if ($migrationStarted) {
        throw "HIGH SEVERITY: release '$releaseId' failed during phase '$failedPhase' after migration was attempted. The application pool was not automatically restarted. Keep it stopped, inspect migration state, and select an explicit reviewed forward-fix, schema-compatible release switch, or verified database recovery path. $evidenceMessage"
    }

    if ($pathStillPrevious -and $finalAppPoolState -eq 'Started') {
        throw "Release '$releaseId' failed during phase '$failedPhase' before migration began. IIS still targets the previous release and the existing application is running; the candidate deployment remains failed and requires operator review. $evidenceMessage"
    }

    throw "HIGH SEVERITY: release '$releaseId' failed during phase '$failedPhase'. Safe automatic recovery could not be proven. The application pool was not automatically restarted; manual reviewed recovery is required. $evidenceMessage"
}
