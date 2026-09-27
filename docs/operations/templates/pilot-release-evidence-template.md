# Pilot Release Candidate Evidence — copy per candidate

Record UTC timestamps and links to retained, access-controlled evidence. Do not include passwords, connection strings, cookies, tokens, customer/payment payloads or sensitive logs. For each required row use `PASS`, `FAIL`, or `PENDING`; `N/A` requires an approved contract citation and reviewer sign-off. **Any required row other than PASS means candidate release gate FAIL / NOT READY.**

| Candidate identity | Value |
|---|---|
| Environment / safe release ID | `<value>` |
| Exact 40-character commit SHA / application version | `<sha>` / `<version>` |
| Artifact filename / manifest SHA+version / ZIP SHA-256 / independent verification | `<values and evidence>` |
| Operator / reviewer / UTC start and end | `<values>` |

| Required check | Result | Exact command, count, URL or evidence reference; UTC time |
|---|---|---|
| GitHub CI URL/run ID and backend/frontend result | PENDING | `<...>` |
| .NET restore, Release build warnings/errors, tests/counts | PENDING | `<...>` |
| pnpm frozen install, frontend build, tests/counts | PENDING | `<...>` |
| Migration before/after, approved bundle; clean/current upgrade and data preservation where relevant | PENDING | `<...>` |
| Critical real E2E: PR-A, Slice 6, Sale/reprint, Return/Void, EOD, Today/C14; command/environment/DB/results | PENDING | `<...>` |
| Backup freshness/chain and restore readiness | PENDING | `<...>` |
| Exact artifact Windows/IIS installation and explicit migration | PENDING | `<...>` |
| Authenticated `/api/system/version` SHA/version/startup log match | PENDING | `<...>` |
| `/health/live` and `/health/ready` | PENDING | `<...>` |
| Authenticated session/Store/Product read smoke and trace/log | PENDING | `<...>` |
| 80 mm physical printer certification reference and print sanity when changed | PENDING | `<...>` |
| Reviewed schema-compatible rollback or recovery path/rehearsal | PENDING | `<...>` |

| D-094 PR-B evidence still required before final M7 | Status and reference |
|---|---|
| Separate failure-domain backup and protected off-host/external copy | PENDING — D-094 |
| Backup failure/freshness alert delivery and pilot retention | PENDING — D-094 |
| Backup-service SQL privilege review, including `CREATE DATABASE` for `RESTORE VERIFYONLY` | PENDING — D-094 |
| Another human operator exercising deployment/backup/support runbooks | PENDING — D-094 |
| Materially different pilot Windows Server/certificate/network/deployment evidence | PENDING — D-094 |

- Unresolved gaps/incident IDs: `<...>`
- Operator result and UTC time: `FAIL / NOT READY — <...>`
- Reviewer result and UTC time: `<...>`
- Product Owner PR-C decision reference (if later approved): `<...>`
- Final separate M7 decision reference (if later approved): `<...>`
