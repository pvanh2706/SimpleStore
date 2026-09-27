# PR-B Windows 11 IIS rollback and failure safety — 2026-09-27

## Outcome

`PASS — LOCAL SCHEMA-COMPATIBLE IIS ROLLBACK AND CONTROLLED PRE/POST-MIGRATION FAILURE SAFETY`.

This is an isolated Windows 11 test against `SimpleStorePilotIisTest`, not pilot production or a Product Owner approval. PR-BLOCKER-04/05 remain **OPEN** pending review and remaining operational evidence. The related [IIS deployment evidence](pr-b-win11-iis-deployment-2026-09-27.md) records HTTPS, smoke, logging and restart.

## Reviewed releases and compatibility

| Release | Commit | Artifact SHA-256 | Role |
|---|---|---|---|
| A, `0.1.0-00e11d4c1a33` | `00e11d4c1a33be90cb04648c9fecb78007f9f30b` | `21837728b1c94e0504a6cd1882663821c6e26e0be8d6319317044ae46231004e` | Previous/rollback target; installed through the real installer phases, then explicitly recovered from an IIS start race. |
| B, `0.1.0-c1293f0823a0` | `c1293f0823a0777a8f70fcfa833fe1eaecff3441` | `f409d4c6c541ab3915375729a2018e187abc7191f83589ec8356e9f02005a60c` | Clean successful installer deployment and restart. |
| Fault-test, `0.1.0-faulttest-a13ba29c7df8` | `a13ba29c7df83b4471987c6a755d022fb3c1c5f9` | `8827305ce46b35954bb637a49786c34adb88a9134b51a726d9f8e7c5b6cc8d2f` | Separate test candidate for controlled failures only. |

The A→B and A→fault-test Git diffs contained **zero backend/frontend file changes**. The isolated database's latest migration was `20260926023259_ImplementPilotReadinessPrA` before and after these operations. No migration was added or applied to manufacture evidence. Schema compatibility was explicitly checked before both release switches. No EF `Down()` was invoked.

## Compatible rollback B → A

1. B was installed through `Install-SimpleStoreRelease.ps1`; its installer evidence recorded checksum/manifest, explicit successful migration invocation, path switch and app-pool start. The bundle reported no pending migration. Authenticated B smoke matched B's SHA.
2. At `2026-09-27T07:12:48–07:12:51Z`, `Switch-SimpleStoreRelease.ps1 -SchemaCompatibilityReviewed -Confirm:$false` stopped/waited for the dedicated pool, selected `D:\SimpleStorePilotTest\releases\0.1.0-00e11d4c1a33\app`, then started/waited for it. The recorded previous path was B's `app` path; final pool state was `Started`.
3. After the switch, `/health/live` and `/health/ready` returned `200 Healthy`. `Invoke-DeploymentSmoke.ps1` passed Owner login, authenticated version/session, Store and product read. Version was `0.1.0`, SHA was A's `00e11d4c1a33be90cb04648c9fecb78007f9f30b`, environment `Production`. A startup and HTTP records appeared in the external JSON log, while B's prior records remained.

## Controlled failure before migration

A remained active. A local wrapper outside Git shadowed only `Stop-WebAppPool` for this invocation: it called the real IIS cmdlet, waited until the pool was `Stopped`, then threw a controlled exception **before** the production installer's migration invocation. The production installer was not weakened or edited for fault injection.

The fault-test candidate's `IisReleaseInstallFailed` record at `2026-09-27T07:15:13Z` showed `failedPhase=StopAppPool`, `migrationAttempted=false`, `previousPathStillConfigured=true`, `existingApplicationAutoRestarted=true`, and `recoveryAction=ExistingApplicationRestartedAfterProvenPreMigrationFailure`. IIS still targeted the exact A `app` path and the pool ended `Started`. A subsequent authenticated A smoke passed. This demonstrates automatic previous-app resume only when both schema untouched and exact previous IIS path were proven.

## Controlled failure after migration invocation

The same reviewed fault-test ZIP was retried. A different local wrapper shadowed only `Start-WebAppPool` and threw after the real installer had stopped the pool, invoked the migration bundle, moved the candidate release and switched IIS to its path. The bundle reported the database already up to date. The wrapper did not modify the production installer or the database.

The `IisReleaseInstallFailed` record at `2026-09-27T07:16:00Z` showed `failedPhase=StartCandidateApplication`, `migrationAttempted=true`, `migrationSucceeded=true`, `previousPathStillConfigured=false`, `existingApplicationAutoRestarted=false`, `appPoolState=Stopped`, `recoveryAction=ApplicationPoolLeftStoppedForManualReviewedRecovery`, and `recoveryStatus=ManualReviewedRecoveryRequired`. Independent IIS inspection confirmed the fault-test path and stopped pool. The prior A application was **not** automatically restarted.

An explicit reviewed recovery then confirmed the failure record, stopped pool, exact fault-test path, zero backend/frontend diff and unchanged latest migration. `Switch-SimpleStoreRelease.ps1 -SchemaCompatibilityReviewed -Confirm:$false` switched from the stopped fault-test candidate to A at `2026-09-27T07:16:40Z`. Final path was A, pool `Started`, and no EF Down ran. Live, ready, `/health` and a fresh authenticated A smoke passed; the external log gained a new A startup event. The final IIS state serves A.

## Retained evidence and limits

Non-secret installer, smoke, restart, rollback, fault and recovery JSON files remain under Administrator-restricted `D:\SimpleStorePilotTest\deployment-evidence`. The three ZIPs, checksums and wrapper scripts remain outside Git. The test Owner password is in a separate restricted local file and is not included in evidence or the repository.

This exercise proves local IIS behavior with a schema-compatible pair. Production Windows Server, genuine migration-forward compatibility decisions, separate-failure-domain backup storage, elapsed retention/alert routing, another human operator and Product Owner review remain outstanding. PR-B is not approved; PR-C is not started; M7 is not achieved.
