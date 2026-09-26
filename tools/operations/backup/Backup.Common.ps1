Set-StrictMode -Version Latest

function Assert-SimpleStoreDatabaseName([string]$DatabaseName) {
    if ($DatabaseName -notmatch '^[A-Za-z0-9_-]+$') {
        throw 'DatabaseName may contain only letters, digits, underscore, and hyphen.'
    }
}

function Resolve-SafeBackupRoot([string]$BackupRoot) {
    $fullPath = [IO.Path]::GetFullPath($BackupRoot)
    $volumeRoot = [IO.Path]::GetPathRoot($fullPath)
    if ([string]::IsNullOrWhiteSpace($volumeRoot) -or $fullPath.TrimEnd('\', '/') -eq $volumeRoot.TrimEnd('\', '/')) {
        throw 'BackupRoot must be a dedicated directory, not a volume root.'
    }
    return $fullPath
}

function Assert-SqlCmdAvailable {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw 'sqlcmd is required and must be available on PATH.'
    }
}

function Invoke-IntegratedSqlCmd(
    [string]$ServerInstance,
    [string]$Query,
    [string]$Database = 'master') {
    & sqlcmd -S $ServerInstance -d $Database -E -b -V 16 -r 1 -Q $Query
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed with exit code $LASTEXITCODE."
    }
}

function Write-BackupOperationEvent(
    [string]$BackupRoot,
    [string]$DatabaseName,
    [hashtable]$Event) {
    $Event.timestampUtc = [DateTimeOffset]::UtcNow.ToString('O')
    $Event.database = $DatabaseName
    $line = $Event | ConvertTo-Json -Compress
    Add-Content -LiteralPath (Join-Path $BackupRoot "$DatabaseName-operations.jsonl") -Value $line -Encoding utf8
    Write-Output $line
}
