[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9_.()\\,-]+$')]
    [string]$ServerInstance,
    [Parameter(Mandatory)]
    [string]$DatabaseName,
    [Parameter(Mandatory)]
    [string]$BackupRoot,
    [Parameter(Mandatory)]
    [ValidateSet('Full', 'Log')]
    [string]$BackupType
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Backup.Common.ps1')
Assert-SimpleStoreDatabaseName $DatabaseName
Assert-SqlCmdAvailable
$BackupRoot = Resolve-SafeBackupRoot $BackupRoot
New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

$utcTimestamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$typeToken = $BackupType.ToUpperInvariant()
$extension = if ($BackupType -eq 'Full') { '.bak' } else { '.trn' }
$backupFile = Join-Path $BackupRoot "${DatabaseName}_${typeToken}_${utcTimestamp}${extension}"
if (Test-Path -LiteralPath $backupFile) {
    throw "Backup target '$backupFile' already exists."
}
$sqlPath = $backupFile.Replace("'", "''")
$backupCommand = if ($BackupType -eq 'Full') { "BACKUP DATABASE [$DatabaseName]" } else { "BACKUP LOG [$DatabaseName]" }
$query = @"
SET NOCOUNT ON;
IF DB_ID(N'$DatabaseName') IS NULL THROW 51000, 'Database not found.', 1;
IF (SELECT recovery_model_desc FROM sys.databases WHERE name = N'$DatabaseName') <> N'FULL'
    THROW 51001, 'Database must use FULL recovery model.', 1;
DECLARE @compression nvarchar(20) = CASE
    WHEN CONVERT(int, SERVERPROPERTY('EngineEdition')) = 4 THEN N''
    ELSE N', COMPRESSION'
END;
DECLARE @backup nvarchar(max) = N'$backupCommand TO DISK = N''$sqlPath'' WITH INIT, CHECKSUM, STATS = 10' + @compression + N';';
EXEC sys.sp_executesql @backup;
RESTORE VERIFYONLY FROM DISK = N'$sqlPath' WITH CHECKSUM;
"@

$started = [DateTimeOffset]::UtcNow
try {
    Invoke-IntegratedSqlCmd $ServerInstance $query
    if (-not (Test-Path -LiteralPath $backupFile -PathType Leaf)) {
        throw 'SQL Server reported success but the backup file is not visible to the operator context.'
    }
    Write-BackupOperationEvent $BackupRoot $DatabaseName @{
        event = 'NativeBackup'
        backupType = $BackupType
        backupFile = [IO.Path]::GetFileName($backupFile)
        startedAtUtc = $started.ToString('O')
        verification = 'RESTORE VERIFYONLY + CHECKSUM passed'
        compression = 'Used when supported by SQL Server edition'
        sizeBytes = (Get-Item -LiteralPath $backupFile).Length
        result = 'Success'
    }
}
catch {
    Write-BackupOperationEvent $BackupRoot $DatabaseName @{
        event = 'NativeBackup'
        backupType = $BackupType
        backupFile = [IO.Path]::GetFileName($backupFile)
        startedAtUtc = $started.ToString('O')
        verification = 'Failed or not reached'
        result = 'Failure'
        failureType = $_.Exception.GetType().Name
    }
    throw
}
