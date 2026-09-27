# PR-B Windows 11 rollback and failure-safety evidence — 2026-09-27

`NOT EXERCISED`.

The reviewed artifact for `00e11d4c1a33be90cb04648c9fecb78007f9f30b` was built and verified, but dedicated IIS site/app-pool creation was blocked by automatic command approval (`blocked by policy`). No release was deployed through `Install-SimpleStoreRelease.ps1`, so there was no previous IIS release path or schema-compatible candidate pair on this machine.

`Switch-SimpleStoreRelease.ps1 -SchemaCompatibilityReviewed` was not invoked. Pre-migration auto-resume and post-migration-attempt no-restart were not injected on this machine. No EF `Down()` or database modification was used to manufacture rollback evidence. The repository's earlier script-level injected failure tests remain separate from the requested Windows/IIS exercise.

Before claiming rollback or failure safety for PR-B, deploy two reviewed schema-compatible candidates to the isolated IIS topology, review schema compatibility, execute the switch, verify IIS path/version/live/ready/authenticated read, and perform safe controlled installer failure injections. Another human operator runbook exercise remains pending. PR-BLOCKER-04/05 remain open.
