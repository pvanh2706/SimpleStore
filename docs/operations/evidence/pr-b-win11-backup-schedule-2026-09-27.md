# PR-B Windows 11 backup schedule and restore exercise — 2026-09-27

## Outcome

`PASS — LOCAL SCHEDULED FULL/LOG AND ISOLATED RESTORE; PILOT EVIDENCE INCOMPLETE`

This was an isolated Windows 11 Pro technical exercise. SQL Server Agent for Express was Disabled; Windows Task Scheduler was used. No customer or production database was used. It does not close PR-BLOCKER-03.

## Instance, storage and migration

- SQL Server `localhost\SQLEXPRESS`: SQL Server 2022 Express Edition 64-bit `16.0.1000.6`, EngineEdition `4`; SQL service `NT SERVICE\MSSQL$SQLEXPRESS`. The current operator was SQL `sysadmin`; no user databases existed on this instance before the exercise.
- Source database `SimpleStorePilotIisTest` was created solely for this exercise. The reviewed artifact's `simplestore-migrate.exe` ran through `Invoke-DatabaseMigration.ps1` with a process-scoped, integrated-security connection after confirming the exact database target. Latest migration: `20260926023259_ImplementPilotReadinessPrA`. The migration was outside IIS because IIS site creation stopped; it must not be counted as an installer pass.
- The first migration invocation used an incomplete local connection builder, was interrupted, and wrote no migration evidence. A database query confirmed only the new test database existed on `SQLEXPRESS` and no `SimpleStore` database existed there. The corrected invocation verified its target before running and succeeded. No other database was selected for backup or restore.
- Data files for this instance are under `D:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA`. Backup files are under the dedicated `D:\SimpleStorePilotTest\backups` folder, outside the data directory and Git. The folder ACL excludes `BUILTIN\Users` and grants the SQL service, scheduled identity and administrators the required access.
- C: and D: are partitions on the same physical NVMe disk (`Disk 0`, `N100 pro 512G`). **Separate failure-domain storage remains unmet**; this local exercise does not satisfy D-090 protected external storage/replication.

## Schedule and actual job runs

- `Set-FullRecoveryModel.ps1` verified source recovery model `FULL`.
- Task `SimpleStore-Pilot-Test-Full`: daily `02:00` Asia/Bangkok; `LOCAL SERVICE` service-account logon; calls a local, ACL-restricted wrapper around repository `Invoke-SimpleStoreBackup.ps1 -BackupType Full`.
- Task `SimpleStore-Pilot-Test-Log`: every 15 minutes, beginning `00:00` Asia/Bangkok (`PT15M`); same identity and wrapper with `-BackupType Log`. Task actions contain no passwords or connection strings. The wrapper, operation JSONL and backup files remain outside Git.
- `LOCAL SERVICE` has `db_backupoperator` on the isolated database, and `CREATE DATABASE` in `master` because SQL Server requires it for the script's `RESTORE VERIFYONLY`. This latter grant is broader than backup alone and needs an identity/privilege review before pilot use. SQL Server Express did not use backup compression; checksum and verification remained enabled.
- First forced runs at `09:05` local created backup files but both tasks returned `1`: `RESTORE VERIFYONLY` lacked the extra SQL permission. The operations JSONL recorded `NativeBackup` Failure for both types. After granting the required permission, forced runs at `09:06` local returned `0` for both tasks. Full `SimpleStorePilotIisTest_FULL_20260927T020605Z.bak` and log `SimpleStorePilotIisTest_LOG_20260927T020607Z.trn` each recorded `Success` and `RESTORE VERIFYONLY + CHECKSUM passed` in JSONL. The next nightly full was `2026-09-28 02:00` local; the next log run was `2026-09-27 09:15` local at inspection.
- A known probe marker `37e10813-1230-4d18-91ff-675e120dba6f` was inserted into the test source **after** the successful scheduled Full. The log task was triggered again at `09:07:15` local, returned `0`, and produced `SimpleStorePilotIisTest_LOG_20260927T020715Z.trn` with successful checksum/verification evidence.
- `Test-BackupFreshness.ps1` after the successful chain reported Full age `0.07` hours, log age `2.86` minutes, both latest attempts `Success`, result `Healthy` at `2026-09-27T02:10:06Z`. A real defect was found: the prior implementation counted backup filenames even when the operation JSONL recorded failed verification. Commit `2bf3a29` requires matching verified success records and marks a latest failed attempt unhealthy. A controlled fixture returned `Healthy` for two successful records and `StaleOrMissing` with exit `2` after a later failed log attempt. The failed scheduled runs above supplied real non-zero/JSONL evidence.
- `Invoke-BackupRetention.ps1 -RecoveryChainDays 14 -WeeklyFullRetentionWeeks 8 -WhatIf` ran without deleting files. It reported `ConservativeNoAnchorKeepLogs` with no expired anchor. Eight weeks of elapsed history, retention deletions, off-host copy and alert delivery cannot be claimed from this exercise.

## Isolated restore

- Selected the successful scheduled Full at `02:06:05Z`, then ordered log backups at `02:06:07Z` and `02:07:15Z`; `msdb.dbo.backupset` showed continuous LSN boundaries (`...316800001 → ...339200001 → ...359200001`). Failed earlier task files were excluded.
- Restored to a **new** `SimpleStorePilotIisRestore` database with separate MDF/LDF files under `D:\SimpleStorePilotTest\restore-data`. Full used `NORECOVERY`, first log used `NORECOVERY`, final log used `RECOVERY`; all used `CHECKSUM`, and the source database was not overwritten.
- `DBCC CHECKDB ... WITH NO_INFOMSGS` completed without error; restored database was `ONLINE` with `FULL` recovery. Latest EF migration matched the source. The post-Full probe marker was present in both restored and source databases, proving the ordered log restore carried the later change.
- Application startup/live/ready/version/authenticated read smoke against this newly restored database was **not exercised**, because the IIS site/app pool was blocked. The previous [local restore drill](pr-b-local-restore-drill-2026-09-26.md) contains separate restored-artifact application smoke, but does not substitute for this chain's IIS smoke.

## Remaining evidence

Pilot separate-failure-domain storage, durable schedule observation, failure alert routing, real 14-day/8-week retention history, restored-app IIS smoke, and an exercise by **another human operator** remain pending. Product Owner review and PR-BLOCKER-03 closure remain pending.
