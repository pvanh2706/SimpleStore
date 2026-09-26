# SimpleStore Support Runbook v0.1

## Safety boundary

Normal support must never edit production database rows manually, delete `InventoryMovement`, delete `AccountLifecycleAudit`, change completed transactions, change passwords in SQL, edit Identity password hashes, modify migration history, or run unreviewed migrations. Escalate rather than bypass immutable business/audit records.

Never copy passwords, temporary passwords, cookies, antiforgery tokens, authorization headers, connection strings, SQL credentials, customer/payment request bodies, email, phone, or other unnecessary PII into logs, tickets, chat, or evidence.

## 1. First response and severity

| Severity | Working definition | Initial action |
|---|---|---|
| SEV-1 | Store cannot perform critical operations, suspected data loss/security breach, restore may be required | Stop unsafe changes, preserve logs/evidence, contact primary escalation immediately |
| SEV-2 | Material workflow degraded with no safe normal workaround | Capture version/trace/time/scope and escalate promptly |
| SEV-3 | Limited issue with safe workaround, no integrity/security risk | Record and triage in normal support window |

Environment-specific contacts remain placeholders until pilot operations assigns them:

- Primary support: `<name/channel/phone>`
- Technical escalation: `<name/channel/phone>`
- Product Owner: `<name/channel>`
- SQL/backup operator: `<name/channel/phone>`
- Security contact: `<name/channel/phone>`

## 2. Identify exact release

1. Ask for UTC/local occurrence time, affected workflow, safe user/store IDs, and ProblemDetails `traceId`; do not request bodies/secrets.
2. Authenticated call: `GET /api/system/version`.
3. Match `applicationVersion` and `commitSha` to `artifact-manifest.json`, install evidence, and startup JSON log.
4. Record environment label and current IIS physical release path without copying filesystem ACL/config secrets.

## 3. Health and logs

- `GET /health/live`: process only. `503` means process/host pipeline unavailable.
- `GET /health/ready`: includes bounded `SELECT 1` SQL connectivity. `503 Unhealthy` is intentionally generic; investigate server logs and SQL/platform state.
- `GET /health`: compatibility alias to readiness.

Logs are JSON rolling files at `OperationalLogging__Path`, outside versioned releases. Default production path is `%ProgramData%\SimpleStore\Logs\simplestore-.json`; retention defaults to 14 days and files also roll by size.

Search exact `TraceId` in the current/rolled JSON files. Request completion events include method, safe path/route, status, duration, UserId/StoreId when available, application version/SHA, and environment. Logs intentionally omit request/response bodies, cookies, tokens, authorization headers, connection strings, and credentials.

If a readiness check fails, correlate its trace/time and safe `FailureType` with SQL service/network/ACL status. Do not expose server/database details through the public health response.

## 4. Migration and database state

Use a read-only authorized operator identity:

```powershell
pwsh .\tools\operations\deploy\Get-MigrationState.ps1 `
  -ServerInstance '<instance>' -DatabaseName '<database>'
```

Compare the latest migration with the approved release manifest/evidence. Do not edit `__EFMigrationsHistory` or apply a migration not attached to a reviewed artifact.

## 5. Backup status

1. Run `Test-BackupFreshness.ps1` against the configured backup root.
2. Inspect Scheduler/SQL Agent result and `<database>-operations.jsonl`.
3. Confirm last full ≤26 hours and last log ≤20 minutes under defaults; the target schedule remains nightly/15 minutes.
4. Confirm storage/replication/ACL health and available capacity.
5. Follow [Backup and Restore Runbook](backup-restore-runbook-v0.1.md) for chain selection or recovery. `RESTORE VERIFYONLY` alone is never a drill.

## 6. Safe IIS restart

Restart only when evidence supports a process-level issue and restart will not hide an integrity/security incident:

1. record version/SHA, time, reason, health result, active requests if observable, and operator;
2. stop/drain and restart the named application pool through IIS administration;
3. check live, ready, version and authenticated read-only smoke;
4. verify startup log version/SHA and capture outcome;
5. escalate if readiness or smoke fails. Repeated restarts are not a fix.

## 7. Evidence capture

For every incident record: incident ID/severity, UTC/local time, environment, safe user/store IDs, exact traceId, exact version/SHA, route/status (no body), health results, migration identifier, backup freshness, actions/operators/timestamps, result, and escalation. Redact/minimize PII and never attach production configuration or secret-bearing logs.

Use the deployment and backup evidence templates for release/recovery events. Link rather than duplicate sensitive operational material.

## 8. Escalation triggers

Escalate immediately for suspected data loss/corruption, cross-Store access, credential/session exposure, repeated transaction failure, migration mismatch, missing/stale log chain, readiness failure with healthy process, unexplained ledger/audit inconsistency, or any contemplated DB restore/manual edit. Product Owner approval is required at the defined release/readiness gates; support cannot self-close PR-B blockers or declare M7.
