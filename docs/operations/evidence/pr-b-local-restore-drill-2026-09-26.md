# PR-B Local Full + Log Restore Drill Evidence — 2026-09-26

## Outcome

`PASS — LOCAL TECHNICAL RESTORE DRILL`

This is real full + transaction-log restore evidence on an isolated temporary database. It is not evidence of the pilot server's backup storage, SQL Agent/Task Scheduler schedule, Windows Server/IIS deployment, or separate failure-domain storage, so it does not close PR-BLOCKER-03/04/05 and does not approve PR-B.

## Candidate and environment

- Implementation commit: `1284487939b26a7370b499d48d760e9335b011cc`
- Application version: `0.1.0`
- Artifact: `SimpleStore-0.1.0-1284487939b2.zip`
- Artifact SHA-256: `ec569ffae1b9db6c0ba87709a011ed6f57143ff1c8691c379746c310527e01e1`
- Manifest: 109 published files; commit/version matched; prebuilt SPA, IIS `web.config`, and `simplestore-migrate.exe` present; `productionRequiresNode=false`.
- OS: Windows 11 Pro `10.0.26200` (local development workstation, not Windows Server/IIS).
- SQL: LocalDB / SQL Server Express Edition 64-bit `15.0.4382.1`.
- Operator: Codex-assisted local technical exercise under the repository owner session.
- UTC start: `2026-09-26T15:11:25.8307205Z`
- UTC completion: `2026-09-26T15:11:35.1121728Z`
- Approximate duration: 9.3 seconds after artifact creation.

## Migration and chain

- Explicit migration bundle applied successfully to a clean temporary source database.
- Latest migration: `20260926023259_ImplementPilotReadinessPrA`; PR-B required no new EF migration.
- Recovery model changed/verified as `FULL`.
- Full backup: `SimpleStorePrBDrill_012e911394_FULL_20260926T151129Z.bak`
- Full SHA-256: `56cdf4cd517a323b524d77d1dd9affd27efe58ccd9af3fcdae51210b0d4ca4c7`
- Transaction-log backup: `SimpleStorePrBDrill_012e911394_LOG_20260926T151132Z.trn`
- Log SHA-256: `5b541f4db95b16fb2e0deb3306d15ffcd07177fab8c661d7552a862d78a5c18a`
- Native backup checksum and `RESTORE VERIFYONLY ... WITH CHECKSUM`: PASS for both backup types.
- Freshness check immediately after capture: full fresh, log fresh, result `Healthy`.
- SQL Express does not support backup compression; the script correctly omitted compression for EngineEdition 4 while retaining checksum and verification.

## Isolated restore proof

- Source: temporary `SimpleStorePrBDrill_012e911394`.
- Isolated target: separate temporary database `SimpleStorePrBRestore_012e911394` with separate MDF/LDF paths.
- Full restored with `NORECOVERY`, then ordered transaction log restored with final `RECOVERY`.
- `DBCC CHECKDB ... WITH NO_INFOMSGS`: PASS.
- Expected schema/table check: PASS.
- A Store created through the application after the full backup was readable after the log restore, proving the log backup—not the full alone—carried the expected post-full data.
- Source and restored temporary databases were dropped after evidence capture; backup files/evidence were retained locally outside Git.

## Restored application smoke

- The exact published artifact started against the restored database with `ASPNETCORE_ENVIRONMENT=Production`.
- `/health/live`: PASS.
- `/health/ready`: PASS.
- Authenticated `/api/system/version`: version/SHA/environment matched `0.1.0` / `1284487939b26a7370b499d48d760e9335b011cc` / `Production`.
- Authenticated session, Store read, and product-list read: PASS.
- Published Vue direct navigation: PASS.
- Unknown `/api/*` remained 404 and did not return SPA HTML: PASS.
- Structured JSON startup/request logs contained application version, exact SHA, and correlated `TraceId`: PASS.

## Limitations that remain open

- Backup files were on the same local workstation/failure domain for this technical exercise. D-090 still requires restricted separate storage/external replication in the pilot environment.
- Nightly full and 15-minute log schedules, 14-day live chain, 8-week weekly full retention, alert routing, and stale-job operator response were not exercised over real elapsed time.
- No Windows Server/IIS app pool, ASP.NET Core Module, protected production configuration, certificate binding, least-privilege ACL, IIS deployment, or IIS rollback exercise was available.
- The support/deployment/backup runbooks still require exercise/review by the actual pilot operator.

Therefore the exact stage state remains `IMPLEMENTED / EVIDENCE INCOMPLETE / PENDING PRODUCT OWNER REVIEW`.
