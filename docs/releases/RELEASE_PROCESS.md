# CipherNest Release Process

This process turns a source commit into an evidence-backed release candidate. It deliberately separates source completion from build/test/device/store/signing evidence.

## 1. Release principles

A CipherNest release must be tied to an immutable source commit/tag and must not make claims unsupported by executed evidence.

Never release by assuming that configured CI, committed tests, or documentation imply successful execution.

## 2. Source freeze prerequisites

Before selecting a candidate:

- intended source changes are committed on the release branch/main according to project policy;
- `README.md`, `docs/README.md`, user/developer/security/format docs reflect current behavior;
- `CHANGELOG.md` Unreleased section reflects the candidate;
- `PROJECT_STATUS.md` separates completed source from external gates/deferred scope;
- `TEST_PLAN.md` and `RELEASE_CHECKLIST.md` include new security/resource/platform behavior;
- no known plaintext secrets/signing credentials/store tokens/private keys are present;
- independent-audit status is stated accurately.

## 3. Select the candidate commit

Record:

```text
repository URL
candidate commit SHA
desired version/build numbers
release branch/tag plan
UTC build date/time
```

Do not move the candidate identity silently after evidence collection. A code change requires a new candidate SHA and re-running applicable gates.

## 4. Capture build environment

For every environment used, record without secrets:

```bash
dotnet --info
dotnet workload list
dotnet nuget list source
```

Also record:

- operating-system build;
- Android SDK/JDK versions;
- Xcode version for iOS/Mac Catalyst;
- Windows SDK/tooling where relevant;
- package feed/provenance information;
- exact package restore graph if available.

See `REPRODUCIBLE_BUILDS.md`.

## 5. Core verification

From a clean checkout of the exact candidate run one of:

```powershell
./scripts/verify-core.ps1
```

or:

```bash
sh scripts/verify-core.sh
```

The candidate cannot progress if required core build/tests/format/analyzers fail.

## 6. Platform compile verification

### Windows

```powershell
./scripts/verify-windows.ps1
```

This includes default/funding-disabled build intent.

### Android

```bash
sh scripts/verify-android.sh
```

### Apple host

```bash
sh scripts/verify-apple.sh
```

Record exact success/failure output for the candidate.

## 7. GitHub-hosted checks

Review the exact candidate's configured checks:

- main core/test/format workflow;
- Windows default/funding-disabled compile;
- Android compile;
- iOS/Mac Catalyst compile;
- CodeQL including application path;
- dependency review;
- vulnerability/security scanning available to the repository;
- secret scanning where available.

A required workflow with no run/no result is not equivalent to passing.

## 8. Dependency/license review

For the exact restored graph:

- check direct/transitive versions;
- review vulnerability findings;
- verify dependency licenses/notices;
- reconcile `THIRD_PARTY_NOTICES.md` with exact package metadata;
- document any accepted exception, owner, reason, severity, and expiry/review date.

Unresolved high-severity security/dependency findings are release-blocking unless explicitly reviewed/owned under project policy.

## 9. Database/migration compatibility

Create disposable test vaults/databases representing supported prior states and verify:

- migration to current schema;
- migration idempotence;
- future-schema rejection;
- malformed/forged migration history rejection;
- current required schema shape;
- item/header/resource bounds;
- DB/WAL/SHM replacement/recovery behavior.

Do not test migration using irreplaceable real user data.

## 10. Backup/restore compatibility matrix

Using synthetic/disposable vaults test:

- no attachments;
- many attachments;
- representative large attachment(s);
- wrong backup passphrase;
- corrupted/truncated backup;
- invalid header/KDF/chunk metadata;
- duplicate/unexpected archive paths;
- invalid staged database;
- cancellation during replacement;
- active-vault preservation/recovery after failure;
- restore across supported platform combinations where format compatibility is expected.

After successful restore verify local biometric pairing is disabled and the restored master/recovery paths behave as documented.

See `../operations/BACKUP_RECOVERY_RUNBOOK.md`.

## 11. Device security validation

### All relevant targets

Test:

- startup/onboarding/unlock;
- manual lock;
- background lock;
- inactivity lock;
- sleep/wake/resume;
- clipboard copy/delayed clear/newer-content preservation;
- screenshot/privacy behavior and honest fallback;
- file picker/share/export cleanup;
- protected-item re-authentication;
- trash/destructive confirmation;
- backup/restore;
- secure-note/attachment preview;
- storage/cache maintenance.

### Biometrics

Android API-28+ and Apple targets require enrollment/no-enrollment/cancel/failure/lockout/device-change/secure-storage scenarios appropriate to the platform.

Windows must continue to show master-passphrase fallback unless Windows Hello is separately implemented/reviewed.

## 12. Accessibility validation

Run the matrix in `../ACCESSIBILITY.md`:

- TalkBack/VoiceOver/Narrator;
- keyboard-only desktop use;
- large text + Larger Interface;
- Reduced Motion;
- Light/Dark/System;
- narrow/wide layouts;
- critical security/destructive/plaintext warnings.

Record tested devices/OS/assistive technology versions.

## 13. Localization validation

Current release is English-first. Verify:

- System/English preference;
- resource fallback;
- startup/resume application;
- long-string layout resilience.

Do not list Hindi/additional languages as complete unless a complete reviewed catalog exists.

## 14. Performance/resource validation

Use synthetic data to measure:

- unlock time;
- search/audit latency;
- 1k/5k/10k item vault behavior;
- 50-item UI rendering increments;
- attachment import/export throughput;
- backup/restore time;
- large valid CSV parsing;
- memory behavior near representative large valid records.

Safety ceilings are not performance targets. The app need not be optimized for routinely hitting the absolute 100k/256MiB/1GiB boundaries.

## 15. Funding CTA policy decision

Before packaging, check the **current** policy for the exact target store/region/distribution/app category.

If the in-app Buy Me a Coffee CTA cannot be shipped on a target, build that package with:

```text
-p:CipherNestEnableFundingLink=false
```

Record the value used in release provenance.

Do not alter source solely to remove the CTA for one package when the build switch is sufficient.

## 16. Store/privacy declarations

For each distribution target verify current requirements against actual behavior:

- application/package identifiers;
- version/build number;
- permissions/entitlements;
- Face ID/biometric usage descriptions where required;
- data/privacy declarations;
- local-file/share behavior;
- no unsupported cloud/account claim;
- screenshot/clipboard limitation wording;
- optional external support link policy.

Use current platform/store documentation during packaging; store requirements change over time.

## 17. Branding/store assets

Use repository-original branding guidance from:

- `../branding/ASSETS.md`;
- `STORE_LISTING_GUIDE.md`.

Verify generated assets on actual/official target surfaces for:

- clipping/safe zones;
- transparency/background rules;
- light/dark contrast;
- taskbar/app-list/adaptive behavior;
- splash presentation;
- creator credit placement.

Use only synthetic vault content in screenshots/marketing.

## 18. Signing and protected packaging

Signing material never belongs in Git.

Use protected local/CI secret storage for:

- Android keystore/password;
- Windows signing certificate/password;
- Apple signing/provisioning credentials;
- notarization credentials;
- store API tokens.

Follow `PACKAGING.md`.

## 19. Provenance record

For each final artifact record:

```text
source commit/tag
product version/build
build host/toolchain versions
resolved dependencies
funding CTA build flag
signing identity/certificate reference (not secret/private key)
artifact filename/target
artifact SHA-256 checksum
CI/test evidence references
known approved exceptions
```

Do not place secret values in provenance.

## 20. Final release checklist

Complete every applicable item in `../RELEASE_CHECKLIST.md`.

An item can be marked not applicable only with a written reason.

Examples that are not acceptable reasons:

- “source looks correct” instead of running a required target compile;
- “CI is configured” instead of reviewing a result;
- “tests exist” instead of executing them;
- “open source” instead of obtaining an independent audit for an audit claim.

## 21. Independent security review

Before stronger security marketing claims, obtain independent review of at least:

- cryptographic envelope/KDF/AAD/nonces;
- recovery/secondary-wrapper design;
- session/key-lease concurrency;
- attachment format;
- backup format/rollback;
- database migration/replacement;
- parser/resource boundaries;
- plaintext export/clipboard lifecycle;
- supply-chain/release pipeline.

Until that happens, keep the product status explicitly “not independently audited.”

## 22. Release notes

Release notes should include:

- user-visible changes;
- security/recovery/compatibility changes;
- migration/backup implications;
- known limitations;
- deferred features if relevant;
- exact audit status;
- links to support/security disclosure.

Do not include sensitive internal environment details or signing secrets.

## 23. Tag/release

Only after evidence is complete:

1. ensure working tree/source candidate is unchanged;
2. create the intended immutable version tag according to repository policy;
3. build/sign final artifacts from the exact tagged commit in protected environment;
4. publish artifacts/checksums where appropriate;
5. publish release notes;
6. verify store/listing metadata matches the shipped build;
7. retain evidence/provenance securely.

## 24. Post-release monitoring

After release:

- triage crash/bug/security reports without requesting user secrets;
- watch dependency/security advisories;
- verify support docs still match released behavior;
- keep `CHANGELOG.md`/`PROJECT_STATUS.md` current;
- start new Unreleased work from a clear version baseline;
- prepare a security-response process for urgent fixes.

See `../operations/SECURITY_RESPONSE.md`.

## 25. Emergency hotfix rule

An urgent security hotfix can shorten scheduling but not remove essential evidence.

At minimum:

- identify exact vulnerable/fixed commit range;
- add regression test where feasible;
- run affected core/platform build/test gates;
- review migration/format compatibility if touched;
- update security/changelog/release notes;
- repackage/sign from protected environment;
- avoid public exploit detail before users have a reasonable upgrade path when coordinated disclosure requires restraint.
