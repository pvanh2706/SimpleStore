# PR-B Windows 11 IIS deployment exercise — 2026-09-27

## Outcome

`PARTIAL — IIS PREREQUISITES INSTALLED; IIS DEPLOYMENT AND SMOKE NOT EXERCISED`

This was a local Windows 11 Pro exercise, **not Windows Server** and not pilot production. It does not close PR-BLOCKER-04/05 or approve PR-B.

## Machine and reviewed artifact

- `Win32_OperatingSystem`: Microsoft Windows 11 Pro, version `10.0.26200`, 64-bit; registry `DisplayVersion=25H2`, `CurrentBuild=26200`, `UBR=9550`. `Get-ComputerInfo.WindowsProductName` and registry `ProductName` reported the compatibility label `Windows 10 Pro`; the OS caption/build identify Windows 11.
- Initial PowerShell token was not elevated. `Get-WindowsOptionalFeature -Online` required elevation. Standard Windows UAC elevation succeeded for inspection, feature installation, Hosting Bundle installation and scheduled-task registration; no UAC bypass was used.
- Initial `ASPNETCORE_ENVIRONMENT` environment variable was unset. Initial free space was `70,552,518,656` bytes on C: and `99,105,878,016` bytes on D:; these are partitions of the same physical disk.
- Initial repo worktree clean. Reviewed `HEAD` and `origin/master`: `00e11d4c1a33be90cb04648c9fecb78007f9f30b`. The artifact was built before the later evidence/tooling commits.
- .NET SDK `10.0.101`; Node `24.21.0` from the [official Node release archive](https://nodejs.org/download/release/latest-v24.x/) (ZIP SHA-256 `158f7685b44de51f6c0df1d153526cbcd3e1bc739a8dfc607721cef75de9e541` checked against official `SHASUMS256.txt`); pnpm `10.15.1`. The first local build invoked pnpm under the machine's Node 22 shim and was discarded. The retained rebuild ran pnpm under Node 24 without engine warnings.
- `New-ReleaseArtifact.ps1` generated `SimpleStore-0.1.0-00e11d4c1a33.zip`, SHA-256 `21837728b1c94e0504a6cd1882663821c6e26e0be8d6319317044ae46231004e`, matching its `.sha256` file. Manifest: version `0.1.0`, exact commit above, 109 files, `productionRequiresNode=false`. Prebuilt SPA `wwwroot/index.html`, backend publish DLL, IIS `web.config` and Windows migration bundle were present. Artifact and build tools remain outside Git under `D:\SimpleStorePilotTest`.

## IIS and Hosting Bundle

- Initial IIS/WAS feature states were Disabled; `WebAdministration`, IIS Manager, `appcmd.exe`, full IIS ASP.NET Core Module and W3SVC were absent. IIS Express existed but was not treated as full IIS.
- Enabled only `IIS-WebServerRole`, `IIS-WebServer`, `IIS-CommonHttpFeatures`, `IIS-StaticContent`, `IIS-DefaultDocument`, `IIS-HttpErrors`, `IIS-Security`, `IIS-RequestFiltering`, `IIS-HttpLogging`, `IIS-ManagementConsole`, `WAS-WindowsActivationService`, `WAS-ProcessModel` and `WAS-ConfigurationAPI`. All were verified Enabled; the feature operation reported no restart needed. `WebAdministration`, IIS Manager and `appcmd.exe` became available; W3SVC/WAS were Running.
- Installed Microsoft's [ASP.NET Core 10 Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-10.0.12-windows-hosting-bundle-installer) `10.0.12` after enabling IIS. Installer SHA-256 `ccc5b497c6f179d4acf996ff6eb7d015959bc85f87dde4ce5ee5c426847576e1`; Authenticode signature Valid, signer `.NET, Microsoft Corporation`. ASP.NET Core Module V2 exists at `%ProgramFiles%\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll`, file version `20.0.26234.12`; `Microsoft.AspNetCore.App 10.0.12` is installed. Installer exit `3010` requested a system restart; WAS/W3SVC were restarted and W3SVC was Running, but a full Windows restart was not performed.

## Deployment boundary

- The next command to create a dedicated IIS site/app pool, least-privilege ACL, localhost self-signed certificate and protected app-pool environment configuration was rejected by automatic command approval with `blocked by policy`. No site/app pool, certificate binding or IIS application configuration was created. The affected IIS topology subtask stopped at that point.
- No `Install-SimpleStoreRelease.ps1` deployment occurred. Artifact checksum/manifest checks above are build-side only; app-pool stop, installer migration, release install, IIS path switch and app-pool start remain untested.
- No IIS SPA/API route, `/health/*`, login, authenticated version/session/Store/product, external structured application log or ProblemDetails trace correlation evidence exists from this exercise. No production certificate validation is claimed.
- The explicit migration bundle was run separately against the isolated `SimpleStorePilotIisTest` SQL database for the backup exercise; this is **not** evidence that the IIS installer completed its migration phase.

## Current boundary

`IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW`. PR-BLOCKER-03/04/05 remain open. IIS deployment, protected runtime configuration, local HTTPS, authenticated IIS smoke, application logs, rollback and failure-safety exercises remain pending. Another human operator runbook exercise remains pending.
