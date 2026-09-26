# SimpleStore Windows/IIS Deployment Runbook v0.1

## Status and boundary

This runbook implements the approved PR-B topology: one SimpleStore ASP.NET Core application on Windows Server/IIS, with the prebuilt Vue SPA served from the same application and SQL Server as the data store. Node.js is required only on the build machine and must not be installed as a production runtime dependency.

Deployment is not PR-B approval, Pilot Ready, or M7 completion. A real reviewed deployment exercise and the remaining PR-B evidence are separate gates.

## 1. Required roles and prerequisites

- Build operator: repository access, Git, .NET SDK from `global.json`, Node.js 24+, pnpm 10.15.1+.
- Deployment operator: authorized Windows/IIS access and permission to deploy reviewed artifacts; must not use a day-to-day application account for server work.
- Migration identity: least-privilege SQL identity allowed to apply the reviewed EF migration set.
- Runtime app-pool identity: read/execute on the selected release only, write on the external log directory, and only the required application SQL permissions.
- Windows Server with IIS, HTTPS certificate, ASP.NET Core Module installed through the matching .NET Hosting Bundle, and outbound/inbound network rules needed for SQL Server and pilot clients.
- SQL Server reachable from the app-pool identity or protected configured SQL identity.

Verify the Hosting Bundle after IIS is installed. Restart IIS after installing/upgrading the Hosting Bundle. OpenAPI remains Development-only and is not a production verification endpoint.

## 2. Server layout and ACLs

Recommended configurable layout:

```text
C:\SimpleStore\
  releases\
    <version>-<short-sha>\
      app\
  deployment-evidence\
  logs\
```

The application default production log path is `%ProgramData%\SimpleStore\Logs\simplestore-.json`. Set `OperationalLogging__Path` to `C:\SimpleStore\logs\simplestore-.json` if the layout above is preferred. In either case logs are outside versioned releases. Backup storage is configured separately and must not be placed under this deployment tree.

ACL rules:

- administrators/deployment operators: modify `releases` and `deployment-evidence`;
- app-pool identity: read/execute the active `app` directory;
- app-pool identity: modify only the configured log directory;
- no public write access; no application write permission on release binaries;
- backup service identity and backup ACLs are defined in the backup/restore runbook.

## 3. Production configuration

Use protected IIS/server environment configuration; do not commit production values or put secrets in the release archive. Required names:

| Setting | Production requirement |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__SimpleStore` | Protected SQL Server connection string; never record in evidence/logs |
| `OperationalLogging__Enabled` | `true` |
| `OperationalLogging__Path` | Absolute path outside `releases` |
| `OperationalLogging__RetentionDays` | `14` by default; configurable |
| `OperationalLogging__FileSizeLimitBytes` | `52428800` by default; configurable |
| `Readiness__SqlTimeoutSeconds` | `5` default, bounded by application to 1–30 seconds |

Restrict access to IIS configuration and any environment store containing the connection string. Never echo configuration values, include them in tickets/evidence, or pass them in a process command line. HTTPS is mandatory; configure the certificate/binding and redirect HTTP to HTTPS at IIS/network level as well as in the application.

The release build passes version metadata through MSBuild and writes the same application version/commit SHA to assembly metadata and `artifact-manifest.json`. Runtime startup logs and authenticated `GET /api/system/version` use those embedded values.

## 4. Build one reviewed artifact

From a clean reviewed commit on the build machine:

```powershell
pwsh .\tools\release\New-ReleaseArtifact.ps1 -Version 0.1.0
```

The script verifies tools and Node 24+, restores the local `dotnet-ef` tool, restores/builds the frozen frontend, publishes the backend Release output, copies Vue `dist` into publish `wwwroot`, builds a Windows x64 EF migration bundle, records a file-level manifest, creates one ZIP, and writes its SHA-256 checksum. Output is under ignored `artifacts/`; generated artifacts must not be committed.

Copy only the ZIP and matching `.sha256` to the controlled deployment staging location. Record transport/source and verify the hash again on the server.

## 5. One-time IIS setup

1. Create an application pool for SimpleStore.
2. Set `.NET CLR version` to `No Managed Code` and use a dedicated least-privilege identity.
3. Create the IIS site with an initial reviewed release path and bind the approved hostname/HTTPS certificate.
4. Disable anonymous directory browsing. The application itself keeps health endpoints anonymous and requires cookie authentication elsewhere.
5. Apply the configuration and ACL boundary from sections 2–3.
6. Confirm the production server has the .NET Hosting Bundle/runtime but does not need Node.js, pnpm, or frontend source.

## 6. Per-release deployment procedure

1. Record operator, UTC start time, candidate SHA/version, previous release, IIS site/app pool, safe SQL target identifier, and rollback candidate in the deployment evidence template.
2. Verify the artifact SHA-256 against its supplied checksum.
3. Confirm the artifact manifest SHA/version match the approved candidate.
4. Query and record the current migration using `Get-MigrationState.ps1` under an integrated authorized identity.
5. Confirm backup freshness and the release-specific DB recovery decision before any schema change.
6. Drain traffic as appropriate and stop the app pool.
7. Run the explicit reviewed migration bundle. Normal application startup never applies migrations.
8. Install the versioned release without deleting previous releases, logs, config, backups, or evidence.
9. Point the IIS site at the new versioned `app` directory and start the app pool.
10. Verify `GET /health/live` returns only `200 Healthy`.
11. Verify `GET /health/ready` and compatibility `GET /health` return only `200 Healthy`.
12. Securely obtain a smoke-test application credential and run the read-only smoke script. It validates authenticated `/api/system/version`, session, Store, and product read without creating Sale/Purchase data.
13. Confirm version endpoint SHA/version match the manifest and startup log.
14. Search JSON logs for the smoke request trace/correlation and confirm the log path is external to the release.
15. Query and record migration state after deployment.
16. Complete outcome, UTC end/duration, checks, and reviewer fields. A failed migration, readiness, version mismatch, or authenticated smoke fails the deployment.

Reviewed automation entry point:

```powershell
$connection = Read-Host 'Protected SQL connection string' -AsSecureString
pwsh .\tools\operations\deploy\Install-SimpleStoreRelease.ps1 `
  -ArchivePath '<artifact.zip>' `
  -ChecksumFile '<artifact.zip.sha256>' `
  -InstallRoot 'C:\SimpleStore' `
  -IisSiteName '<site-name>' `
  -AppPoolName '<pool-name>' `
  -ConnectionString $connection `
  -Operator '<operator-id>'
```

The install script validates the checksum and manifest, requires `No Managed Code`, stops the pool, invokes the migration bundle using a process-scoped environment value rather than a command-line secret, installs a new release, updates the IIS physical path, restarts, and records non-secret install evidence. Health and authenticated smoke are still mandatory after it returns. A failed state-changing phase writes `IisReleaseInstallFailed` evidence with the failed phase, candidate version/SHA, migration attempted/succeeded flags, release/path/start flags, previous release identifier, final pool state, UTC time, operator and `ManualReviewedRecoveryRequired`; it never records the connection string.

### 6.1 Fail-safe deployment failure handling

The installer tracks the app-pool stop, migration invocation/result, release move, IIS path switch and candidate start as separate phases.

- A failure before migration may restart the existing application only when the script proves both that migration was never invoked and that IIS still points to the exact previous physical path. If either fact is unknown, the pool stays stopped for reviewed recovery.
- As soon as migration invocation begins, database state is treated as potentially changed even when the bundle reports failure. The installer never automatically starts the previous release after that point. If a candidate start was partial, the installer attempts to stop the pool again.
- A failure after migration attempt is a high-severity availability/recovery event. Preserve the failed-install JSON, migration evidence, artifact and logs; do not delete staging or edit migration history to make an old release start.

For every failure after migration attempt:

1. Keep the application pool stopped and traffic unavailable.
2. Preserve deployment/migration evidence and record the incident.
3. Run `Get-MigrationState.ps1` using the authorized identity and determine the actual current schema state.
4. Compare the previous application artifact with that schema; application rollback is not database rollback.
5. Only after explicit compatibility review may an operator invoke `Switch-SimpleStoreRelease.ps1 -SchemaCompatibilityReviewed`.
6. If the previous application is not compatible, leave it stopped and use a reviewed forward fix/new compatible artifact or the verified database recovery procedure. Never use automatic EF `Down()`.

Do not rely on the installer to restart the old application after a migration attempt. The explicit release switch only changes IIS application files; health, version and authenticated read smoke remain separate mandatory checks before rollback can be called successful.

Read-only smoke:

```powershell
$credential = Get-Credential
pwsh .\tools\operations\deploy\Invoke-DeploymentSmoke.ps1 `
  -BaseUri 'https://<pilot-host>/' `
  -Credential $credential `
  -EvidencePath 'C:\SimpleStore\deployment-evidence\<release>-smoke.json'
```

Do not use automatic Sale/Purchase mutations for normal production smoke.

## 7. SPA and route behavior

ASP.NET Core serves static files from publish `wwwroot` and uses a Vue history fallback only for browser routes without a file extension. `/api/*`, `/health*`, and missing static assets never fall into `index.html`; unknown API/health paths return a problem/404. OpenAPI is mapped only in Development. Existing HTTPS, authentication, authorization, cookie, forced-password-change, and antiforgery behavior remains in force.

## 8. Application rollback

Application rollback is not database restore and must not invoke EF `Down()` as normal recovery.

1. Stop/drain the app pool and retain failed release evidence/logs.
2. Confirm a reviewed previous artifact is compatible with the current schema. If not compatible, stop and choose a forward fix or the verified database recovery process.
3. Switch the IIS physical path to the previous versioned `app` directory using `Switch-SimpleStoreRelease.ps1 -SchemaCompatibilityReviewed`.
4. Start the app pool.
5. Verify version, live, ready, authenticated read smoke, and logs.
6. Record operator/time/from-to SHA/schema/outcome.

If the database must be recovered, use the separate backup/restore runbook with an approved recovery point. Never casually down-migrate production, edit migration history, or delete data to make an old application start.

## 9. Current exercise status

`PRODUCTION-LIKE IIS SMOKE PENDING — ENVIRONMENT LIMITATION`

The current development workstation must not be represented as a Windows Server/IIS production-like exercise unless the IIS role, ASP.NET Core Module, administrator access, target certificate/binding, least-privilege app-pool identity, and protected target configuration are all available and the full procedure above is executed. Record actual evidence when that environment exists.
