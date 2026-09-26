[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$MigrationBundle,
    [Parameter(Mandatory)]
    [Security.SecureString]$ConnectionString,
    [Parameter(Mandatory)]
    [string]$EvidencePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$bundlePath = [IO.Path]::GetFullPath($MigrationBundle)
if (-not (Test-Path -LiteralPath $bundlePath -PathType Leaf)) {
    throw "Migration bundle '$bundlePath' was not found."
}

$started = [DateTimeOffset]::UtcNow
$plainConnection = [Net.NetworkCredential]::new('', $ConnectionString).Password
$previousConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__SimpleStore', 'Process')
try {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__SimpleStore', $plainConnection, 'Process')
    & $bundlePath
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "Migration bundle failed with exit code $exitCode."
    }

    $evidence = [ordered]@{
        event = 'ExplicitMigration'
        startedAtUtc = $started.ToString('O')
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        migrationBundle = [IO.Path]::GetFileName($bundlePath)
        result = 'Success'
    }
    $evidence | ConvertTo-Json | Set-Content -LiteralPath $EvidencePath -Encoding utf8
}
finally {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__SimpleStore', $previousConnection, 'Process')
    $plainConnection = $null
}
