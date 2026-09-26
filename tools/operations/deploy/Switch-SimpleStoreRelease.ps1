[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [string]$ReleasePath,
    [Parameter(Mandatory)]
    [string]$IisSiteName,
    [Parameter(Mandatory)]
    [string]$AppPoolName,
    [Parameter(Mandatory)]
    [switch]$SchemaCompatibilityReviewed
)

$ErrorActionPreference = 'Stop'
if (-not $SchemaCompatibilityReviewed) {
    throw 'Application rollback requires explicit reviewed schema compatibility.'
}
$releasePath = [IO.Path]::GetFullPath($ReleasePath)
$applicationPath = Join-Path $releasePath 'app'
if (-not (Test-Path -LiteralPath (Join-Path $applicationPath 'SimpleStore.Api.dll') -PathType Leaf)) {
    throw 'The requested release does not contain a SimpleStore application artifact.'
}
Import-Module WebAdministration
if ($PSCmdlet.ShouldProcess($IisSiteName, "Switch IIS physical path to '$applicationPath'")) {
    Stop-WebAppPool -Name $AppPoolName
    Set-ItemProperty -Path "IIS:\Sites\$IisSiteName" -Name physicalPath -Value $applicationPath
    Start-WebAppPool -Name $AppPoolName
}
