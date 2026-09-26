[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$DatabaseName,
    [Parameter(Mandatory)]
    [string]$BackupRoot,
    [ValidateRange(1, 72)]
    [int]$MaximumFullAgeHours = 26,
    [ValidateRange(15, 120)]
    [int]$MaximumLogAgeMinutes = 20
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Backup.Common.ps1')
Assert-SimpleStoreDatabaseName $DatabaseName
$BackupRoot = Resolve-SafeBackupRoot $BackupRoot
$pattern = '^' + [regex]::Escape($DatabaseName) + '_(FULL|LOG)_(\d{8}T\d{6}Z)\.(bak|trn)$'
$backups = Get-ChildItem -LiteralPath $BackupRoot -File | ForEach-Object {
    $match = [regex]::Match($_.Name, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
        [pscustomobject]@{
            Name = $_.Name
            Type = $match.Groups[1].Value.ToUpperInvariant()
            Timestamp = [DateTimeOffset]::ParseExact(
                $match.Groups[2].Value,
                'yyyyMMddTHHmmssZ',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::AssumeUniversal)
        }
    }
}
$latestFull = $backups | Where-Object Type -eq 'FULL' | Sort-Object Timestamp | Select-Object -Last 1
$latestLog = $backups | Where-Object Type -eq 'LOG' | Sort-Object Timestamp | Select-Object -Last 1
$now = [DateTimeOffset]::UtcNow
$fullFresh = $null -ne $latestFull -and ($now - $latestFull.Timestamp).TotalHours -le $MaximumFullAgeHours
$logFresh = $null -ne $latestLog -and ($now - $latestLog.Timestamp).TotalMinutes -le $MaximumLogAgeMinutes
$result = [ordered]@{
    timestampUtc = $now.ToString('O')
    database = $DatabaseName
    latestFull = if ($null -eq $latestFull) { $null } else { $latestFull.Name }
    latestFullAgeHours = if ($null -eq $latestFull) { $null } else { [Math]::Round(($now - $latestFull.Timestamp).TotalHours, 2) }
    latestLog = if ($null -eq $latestLog) { $null } else { $latestLog.Name }
    latestLogAgeMinutes = if ($null -eq $latestLog) { $null } else { [Math]::Round(($now - $latestLog.Timestamp).TotalMinutes, 2) }
    fullFresh = $fullFresh
    logFresh = $logFresh
    result = if ($fullFresh -and $logFresh) { 'Healthy' } else { 'StaleOrMissing' }
}
$result | ConvertTo-Json
if (-not ($fullFresh -and $logFresh)) {
    exit 2
}
