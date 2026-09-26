[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri]$BaseUri,
    [Parameter(Mandatory)]
    [Management.Automation.PSCredential]$Credential,
    [Parameter(Mandatory)]
    [string]$EvidencePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$started = [DateTimeOffset]::UtcNow
$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()

function Invoke-Read([string]$Path) {
    Invoke-RestMethod -Uri ([uri]::new($BaseUri, $Path)) -Method Get -WebSession $session
}

$live = Invoke-Read '/health/live'
$ready = Invoke-Read '/health/ready'
$antiforgery = Invoke-Read '/api/security/antiforgery'
$headers = @{ 'X-CSRF-TOKEN' = $antiforgery.requestToken }
$loginBody = @{
    email = $Credential.UserName
    password = $Credential.GetNetworkCredential().Password
    rememberMe = $false
} | ConvertTo-Json
try {
    Invoke-RestMethod `
        -Uri ([uri]::new($BaseUri, '/api/auth/login')) `
        -Method Post `
        -WebSession $session `
        -Headers $headers `
        -ContentType 'application/json' `
        -Body $loginBody | Out-Null
}
finally {
    $loginBody = $null
}

$version = Invoke-Read '/api/system/version'
$authSession = Invoke-Read '/api/auth/session'
$store = Invoke-Read '/api/store/current'
$products = Invoke-Read '/api/products?page=1&pageSize=1'
if (-not $authSession.isAuthenticated) {
    throw 'Authenticated smoke did not establish a valid session.'
}

[ordered]@{
    event = 'ReadOnlyDeploymentSmoke'
    startedAtUtc = $started.ToString('O')
    completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    baseUri = $BaseUri.GetLeftPart([UriPartial]::Authority)
    live = [string]$live
    ready = [string]$ready
    applicationVersion = $version.applicationVersion
    commitSha = $version.commitSha
    environment = $version.environment
    authenticated = [bool]$authSession.isAuthenticated
    storeId = $store.id
    productReadSucceeded = ($null -ne $products)
    result = 'Success'
} | ConvertTo-Json | Set-Content -LiteralPath $EvidencePath -Encoding utf8
