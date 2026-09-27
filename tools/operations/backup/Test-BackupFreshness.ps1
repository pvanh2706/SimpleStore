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
$operationPath = Join-Path $BackupRoot "$DatabaseName-operations.jsonl"
$backupAttempts = @()
if (Test-Path -LiteralPath $operationPath -PathType Leaf) {
    $backupAttempts = @(Get-Content -LiteralPath $operationPath | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_)
    } | ForEach-Object {
        $_ | ConvertFrom-Json
    } | Where-Object {
        $_.event -eq 'NativeBackup' -and $_.database -eq $DatabaseName
    })
}
$verifiedFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($attempt in $backupAttempts | Where-Object {
    $_.result -eq 'Success' -and $_.verification -eq 'RESTORE VERIFYONLY + CHECKSUM passed'
}) {
    [void]$verifiedFiles.Add([string]$attempt.backupFile)
}
$pattern = '^' + [regex]::Escape($DatabaseName) + '_(FULL|LOG)_(\d{8}T\d{6}Z)\.(bak|trn)$'
$backups = Get-ChildItem -LiteralPath $BackupRoot -File | ForEach-Object {
    $match = [regex]::Match($_.Name, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success -and $verifiedFiles.Contains($_.Name)) {
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
$latestFullAttempt = $backupAttempts | Where-Object backupType -eq 'Full' |
    Sort-Object { [DateTimeOffset]::Parse($_.timestampUtc) } | Select-Object -Last 1
$latestLogAttempt = $backupAttempts | Where-Object backupType -eq 'Log' |
    Sort-Object { [DateTimeOffset]::Parse($_.timestampUtc) } | Select-Object -Last 1
$now = [DateTimeOffset]::UtcNow
$fullFresh = $null -ne $latestFull -and $null -ne $latestFullAttempt -and
    $latestFullAttempt.result -eq 'Success' -and $latestFullAttempt.backupFile -eq $latestFull.Name -and
    ($now - $latestFull.Timestamp).TotalHours -le $MaximumFullAgeHours
$logFresh = $null -ne $latestLog -and $null -ne $latestLogAttempt -and
    $latestLogAttempt.result -eq 'Success' -and $latestLogAttempt.backupFile -eq $latestLog.Name -and
    ($now - $latestLog.Timestamp).TotalMinutes -le $MaximumLogAgeMinutes
$result = [ordered]@{
    timestampUtc = $now.ToString('O')
    database = $DatabaseName
    latestFull = if ($null -eq $latestFull) { $null } else { $latestFull.Name }
    latestFullAgeHours = if ($null -eq $latestFull) { $null } else { [Math]::Round(($now - $latestFull.Timestamp).TotalHours, 2) }
    latestLog = if ($null -eq $latestLog) { $null } else { $latestLog.Name }
    latestLogAgeMinutes = if ($null -eq $latestLog) { $null } else { [Math]::Round(($now - $latestLog.Timestamp).TotalMinutes, 2) }
    fullFresh = $fullFresh
    logFresh = $logFresh
    latestFullAttemptResult = if ($null -eq $latestFullAttempt) { $null } else { $latestFullAttempt.result }
    latestLogAttemptResult = if ($null -eq $latestLogAttempt) { $null } else { $latestLogAttempt.result }
    result = if ($fullFresh -and $logFresh) { 'Healthy' } else { 'StaleOrMissing' }
}
$result | ConvertTo-Json
if (-not ($fullFresh -and $logFresh)) {
    exit 2
}
