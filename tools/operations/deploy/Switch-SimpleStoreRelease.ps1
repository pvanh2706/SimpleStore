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
function Wait-AppPoolState([string]$ExpectedState) {
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    do {
        $state = [string](Get-WebAppPoolState -Name $AppPoolName).Value
        if ($state -eq $ExpectedState) { return }
        Start-Sleep -Milliseconds 250
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "Application pool '$AppPoolName' did not reach '$ExpectedState' within 30 seconds (last state: '$state')."
}
if ($PSCmdlet.ShouldProcess($IisSiteName, "Switch IIS physical path to '$applicationPath'")) {
    $state = [string](Get-WebAppPoolState -Name $AppPoolName).Value
    if ($state -notin @('Stopped', 'Stopping')) {
        Stop-WebAppPool -Name $AppPoolName
    }
    Wait-AppPoolState 'Stopped'
    Set-ItemProperty -Path "IIS:\Sites\$IisSiteName" -Name physicalPath -Value $applicationPath
    Start-WebAppPool -Name $AppPoolName
    Wait-AppPoolState 'Started'
}
