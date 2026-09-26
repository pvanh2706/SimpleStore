# SimpleStore SQL Server Backup and Restore Runbook v0.1

## Status and D-090 contract

This runbook implements operational tooling for D-090 outside the SimpleStore application. The mandatory baseline is SQL Server `FULL` recovery, nightly full backup, transaction-log backup every 15 minutes, a usable 14-day recovery chain, weekly full backups retained for 8 weeks, restricted storage outside the live database failure domain, detectable job failures, and an actual isolated full+log restore proof.

`RESTORE VERIFYONLY` is early validation only and never counts as an actual restore drill.

## 1. Identities, storage, and prerequisites

- Run backup jobs under a restricted Windows/SQL Server service identity using integrated authentication. No credential is stored in repository scripts.
- Install Microsoft `sqlcmd` and ensure the service identity can connect and perform the approved backup operations.
- The SQL Server service identity needs write access to the configured backup destination; the operator/service identity needs the minimum required read/retention access.
- Backup destination must be a separate protected volume/server/share or a protected volume with an external replicated copy. A folder on the live DB disk alone does not satisfy D-090.
- Restrict ACLs to backup operators/services; do not allow public write. Enable infrastructure encryption at rest where supported and protect network transport/share credentials.
- Test task/job commands interactively under the exact scheduled identity before enabling schedules.

## 2. Initialize FULL recovery and first chain

```powershell
pwsh .\tools\operations\backup\Set-FullRecoveryModel.ps1 `
  -ServerInstance '<sql-instance>' `
  -DatabaseName '<database>' `
  -BackupRoot '<separate-protected-backup-root>'

pwsh .\tools\operations\backup\Invoke-SimpleStoreBackup.ps1 `
  -ServerInstance '<sql-instance>' `
  -DatabaseName '<database>' `
  -BackupRoot '<separate-protected-backup-root>' `
  -BackupType Full
```

Changing to FULL does not establish a usable log chain until the first successful full backup. Both commands must succeed and the operation JSONL plus actual backup file must be retained.

## 3. Backup schedules

Use SQL Server Agent where available or Windows Task Scheduler when Agent is unavailable:

- full: nightly at an environment-approved quiet UTC/local time;
- transaction log: every 15 minutes, continuously;
- retention: daily after a successful full and freshness check;
- freshness check: at least every 15 minutes after the expected log job, with failure surfaced to the operator.

Job commands call `Invoke-SimpleStoreBackup.ps1 -BackupType Full|Log`. Configure PowerShell to fail the task/job when the script returns non-zero. Capture stdout/stderr and preserve `<database>-operations.jsonl`. Configure SQL Agent/Task Scheduler history and an environment-specific notification/escalation channel; script existence alone is not monitoring.

The native backup script:

- validates database/root input;
- refuses a non-FULL database;
- creates deterministic `<database>_FULL|LOG_<UTC>.bak|trn` names;
- runs native backup with checksum and uses compression when the SQL Server edition supports it;
- runs `RESTORE VERIFYONLY ... WITH CHECKSUM`;
- writes timestamp/type/file/size/verification/result without credentials;
- throws/returns non-zero on failure.

## 4. Freshness and failure detection

```powershell
pwsh .\tools\operations\backup\Test-BackupFreshness.ps1 `
  -DatabaseName '<database>' `
  -BackupRoot '<backup-root>'
```

Defaults fail with exit code `2` when the latest full is older than 26 hours or the latest log is older than 20 minutes. Operators must inspect:

- latest successful full filename/time and job result;
- latest successful log filename/time and job result;
- last log age against the 15-minute policy;
- operation JSONL failures and scheduler/Agent failure history;
- storage capacity/ACL/replication status.

Any stale/missing log backup is an incident; do not wait for the nightly review.

## 5. Conservative retention

```powershell
# Review first
pwsh .\tools\operations\backup\Invoke-BackupRetention.ps1 `
  -DatabaseName '<database>' `
  -BackupRoot '<backup-root>' `
  -WhatIf

# Execute only after review
pwsh .\tools\operations\backup\Invoke-BackupRetention.ps1 `
  -DatabaseName '<database>' `
  -BackupRoot '<backup-root>' `
  -Confirm
```

The script examines only top-level files matching the exact configured database/pattern/extensions. It never recursively deletes a directory. It keeps:

- all fulls in the current 14-day recovery window;
- the latest full at or before the 14-day cutoff as the recovery-chain anchor;
- every log at/after that anchor;
- the newest full in each week across the 8-week window;
- the newest full unconditionally.

When no anchor exists, it keeps all log backups and reports `ConservativeNoAnchorKeepLogs` rather than guessing. Operators must review the dry run, weekly selections, chain continuity, storage replication, and actual restore evidence before approving deletions. File timestamps encoded in names are UTC.

## 6. Select a recovery point and chain

1. Define the required UTC recovery point and incident boundary.
2. Select the newest verified full at or before that point.
3. Use `RESTORE HEADERONLY`/`RESTORE FILELISTONLY` and operation records to validate identity and metadata.
4. Select every ordered log backup after that full through the recovery point; detect any gap before starting.
5. Record source backup identifiers/hashes, source schema/application version, target, operator, and start time.
6. Restore only to an isolated separate database/server target. Never overwrite the source during a drill.

## 7. Actual isolated restore

Execute reviewed SQL equivalent to:

```sql
RESTORE DATABASE [<isolated-target>]
FROM DISK = N'<full-backup>'
WITH NORECOVERY, MOVE N'<data-logical-name>' TO N'<isolated-data-path>',
     MOVE N'<log-logical-name>' TO N'<isolated-log-path>', CHECKSUM;

RESTORE LOG [<isolated-target>] FROM DISK = N'<log-1>' WITH NORECOVERY, CHECKSUM;
-- Repeat every ordered log, with no gaps.
RESTORE LOG [<isolated-target>] FROM DISK = N'<final-log>'
WITH STOPAT = '<UTC recovery point>', RECOVERY, CHECKSUM;
```

Use actual logical file names discovered with `RESTORE FILELISTONLY`; do not paste placeholders. Then:

1. run `DBCC CHECKDB ([<isolated-target>]) WITH NO_INFOMSGS`;
2. confirm database is online and expected tables/schema/migration history exist;
3. read critical known non-secret records/counts and reconcile the chosen recovery point;
4. configure the exact intended SimpleStore artifact to the isolated restored database using protected temporary configuration;
5. start the app, verify `/health/live`, `/health/ready`, authenticated `/api/system/version`, and read-only Store/product/data smoke;
6. record source identifiers, full/log ordered chain, recovery point, source schema/version, isolated target, start/end/duration, operator, checks, application smoke, and outcome;
7. remove/isolate the restored copy only under the drill environment's reviewed cleanup process.

## 8. Restore versus application rollback

Application rollback changes the deployed application artifact only after schema compatibility review. Database restore changes data/schema to a selected recovery point and follows the isolated proof/recovery authorization above. EF `Down()` is not the normal rollback strategy. Use a forward fix or a reviewed verified restore when the current schema is incompatible with an older app.

## 9. Current drill status

`LOCAL ACTUAL FULL + LOG RESTORE DRILL — PASS`

The retained [2026-09-26 local restore evidence](evidence/pr-b-local-restore-drill-2026-09-26.md) proves native full/log creation, checksum/verify, ordered isolated-database restore, DB integrity/data checks, and restored-artifact readiness/authenticated read smoke. The exercise used LocalDB/SQL Express on a development workstation and local same-failure-domain storage. Pilot-infrastructure schedule, separate protected storage, alerting, elapsed retention, and actual operator exercise remain pending; PR-B evidence is therefore incomplete and PR-BLOCKER-03 remains open until sufficient reviewed environment evidence exists.
