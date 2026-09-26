[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$DatabaseName,
    [Parameter(Mandatory)]
    [string]$BackupRoot,
    [ValidateRange(14, 365)]
    [int]$RecoveryChainDays = 14,
    [ValidateRange(8, 104)]
    [int]$WeeklyFullRetentionWeeks = 8
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Backup.Common.ps1')
Assert-SimpleStoreDatabaseName $DatabaseName
$BackupRoot = Resolve-SafeBackupRoot $BackupRoot
if (-not (Test-Path -LiteralPath $BackupRoot -PathType Container)) {
    throw "Backup root '$BackupRoot' does not exist."
}

$pattern = '^' + [regex]::Escape($DatabaseName) + '_(FULL|LOG)_(\d{8}T\d{6}Z)\.(bak|trn)$'
$backups = Get-ChildItem -LiteralPath $BackupRoot -File | ForEach-Object {
    $match = [regex]::Match($_.Name, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
        [pscustomobject]@{
            File = $_
            Type = $match.Groups[1].Value.ToUpperInvariant()
            Timestamp = [DateTimeOffset]::ParseExact(
                $match.Groups[2].Value,
                'yyyyMMddTHHmmssZ',
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::AssumeUniversal)
        }
    }
}

$now = [DateTimeOffset]::UtcNow
$chainCutoff = $now.AddDays(-$RecoveryChainDays)
$weeklyCutoff = $now.AddDays(-7 * $WeeklyFullRetentionWeeks)
$fulls = @($backups | Where-Object Type -eq 'FULL' | Sort-Object Timestamp)
$logs = @($backups | Where-Object Type -eq 'LOG' | Sort-Object Timestamp)
$anchor = $fulls | Where-Object Timestamp -le $chainCutoff | Select-Object -Last 1

$keep = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($full in $fulls | Where-Object Timestamp -ge $chainCutoff) {
    [void]$keep.Add($full.File.FullName)
}
if ($null -ne $anchor) {
    [void]$keep.Add($anchor.File.FullName)
    foreach ($log in $logs | Where-Object Timestamp -ge $anchor.Timestamp) {
        [void]$keep.Add($log.File.FullName)
    }
} else {
    foreach ($log in $logs) { [void]$keep.Add($log.File.FullName) }
}

$calendar = [Globalization.CultureInfo]::InvariantCulture.Calendar
$weekly = $fulls | Where-Object { $_.Timestamp -lt $chainCutoff -and $_.Timestamp -ge $weeklyCutoff } |
    Group-Object {
        $week = $calendar.GetWeekOfYear(
            $_.Timestamp.UtcDateTime,
            [Globalization.CalendarWeekRule]::FirstFourDayWeek,
            [DayOfWeek]::Monday)
        '{0:D4}-W{1:D2}' -f $_.Timestamp.Year, $week
    }
foreach ($group in $weekly) {
    $weeklyFull = $group.Group | Sort-Object Timestamp | Select-Object -Last 1
    [void]$keep.Add($weeklyFull.File.FullName)
}
if ($fulls.Count -gt 0) {
    [void]$keep.Add(($fulls | Select-Object -Last 1).File.FullName)
}

$candidates = @($backups | Where-Object { -not $keep.Contains($_.File.FullName) })
$deleted = 0
foreach ($candidate in $candidates) {
    $resolvedCandidate = [IO.Path]::GetFullPath($candidate.File.FullName)
    if (-not $resolvedCandidate.StartsWith(
        $BackupRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to delete outside configured backup root: '$resolvedCandidate'."
    }
    if ($PSCmdlet.ShouldProcess($resolvedCandidate, 'Delete expired SimpleStore backup')) {
        Remove-Item -LiteralPath $resolvedCandidate -Force
        $deleted++
    }
}

Write-BackupOperationEvent $BackupRoot $DatabaseName @{
    event = 'BackupRetention'
    recoveryChainDays = $RecoveryChainDays
    weeklyFullRetentionWeeks = $WeeklyFullRetentionWeeks
    anchorFull = if ($null -eq $anchor) { $null } else { $anchor.File.Name }
    matchedFiles = $backups.Count
    deletionCandidates = $candidates.Count
    deletedFiles = $deleted
    result = if ($null -eq $anchor) { 'ConservativeNoAnchorKeepLogs' } else { 'Success' }
}
