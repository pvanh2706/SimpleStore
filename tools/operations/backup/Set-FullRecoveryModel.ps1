[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9_.()\\,-]+$')]
    [string]$ServerInstance,
    [Parameter(Mandatory)]
    [string]$DatabaseName,
    [Parameter(Mandatory)]
    [string]$BackupRoot
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Backup.Common.ps1')
Assert-SimpleStoreDatabaseName $DatabaseName
Assert-SqlCmdAvailable
$BackupRoot = Resolve-SafeBackupRoot $BackupRoot
New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

$query = @"
SET NOCOUNT ON;
IF DB_ID(N'$DatabaseName') IS NULL THROW 51000, 'Database not found.', 1;
IF (SELECT recovery_model_desc FROM sys.databases WHERE name = N'$DatabaseName') <> N'FULL'
    ALTER DATABASE [$DatabaseName] SET RECOVERY FULL;
IF (SELECT recovery_model_desc FROM sys.databases WHERE name = N'$DatabaseName') <> N'FULL'
    THROW 51001, 'FULL recovery model was not applied.', 1;
SELECT recovery_model_desc FROM sys.databases WHERE name = N'$DatabaseName';
"@
Invoke-IntegratedSqlCmd $ServerInstance $query
Write-BackupOperationEvent $BackupRoot $DatabaseName @{
    event = 'RecoveryModelConfigured'
    recoveryModel = 'FULL'
    result = 'Success'
    nextRequiredAction = 'Run a verified full backup before scheduling transaction-log backups.'
}
