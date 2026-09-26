[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9_.()\\,-]+$')]
    [string]$ServerInstance,
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z0-9_-]+$')]
    [string]$DatabaseName
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required and must be available on PATH.'
}

$query = @"
SET NOCOUNT ON;
IF DB_ID(N'$DatabaseName') IS NULL THROW 51000, 'Database not found.', 1;
SELECT TOP (1) MigrationId
FROM [$DatabaseName].[dbo].[__EFMigrationsHistory]
ORDER BY MigrationId DESC;
"@
& sqlcmd -S $ServerInstance -d master -E -b -V 16 -h -1 -W -Q $query
if ($LASTEXITCODE -ne 0) {
    throw "Unable to read migration state for '$DatabaseName'."
}
