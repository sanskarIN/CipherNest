# CipherNest Verification Gates

This document records the executable/source/build gates configured in the repository and the evidence rules for using them. A configured gate is not the same as a passing gate: release evidence must come from the exact immutable candidate and environment that executed it.

## 1. Current immutable pre-documentation implementation baseline

The complete-documentation expansion is grounded in exact source commit:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

Observed exact-candidate hosted evidence:

- CipherNest CI run `31937127961`: **success**;
- CodeQL run `31937127900`: **success**;
- UnitTests: **346 passed**;
- IntegrationTests: **98 passed**;
- UI/source tests: **111 passed**;
- total: **555 passed, 0 failed, 0 skipped**;
- analyzer-enabled core test builds: zero build warnings/errors;
- configured core formatting: passed;
- Windows default Release: passed;
- Windows `CipherNestEnableFundingLink=false` Release: passed;
- Android Release: passed;
- iOS simulator Release: passed;
- Mac Catalyst Release: passed;
- CodeQL v4 analysis: passed after analyzable core and MAUI application builds.

This is exact evidence for `8566980f...` only. Documentation or source commits after that SHA require a new exact-head run before they can be described as verified release candidates.

## 2. Historical evidence preservation

Earlier records remain intentionally preserved:

- `HOSTED_CI_EVIDENCE_2026_08_13.md` records the first complete hosted cross-platform line for its historical 240-test exact candidate.
- `VERIFIED_MAIN_BASELINE_2026_08_15.md` records the later historical 554-test exact candidate.
- `REPOSITORY_AUDIT_2026_08_16.md` records the bug/error/feature/BMC audit pass.
- `COMPLETE_DOCUMENTATION_2026_08_16.md` defines the current complete-documentation source-to-doc and post-documentation verification gate.

Do not rewrite historical files to pretend an older run describes a newer commit.

# 3. Core gate

The `test-core` job on Ubuntu restores, builds, and runs:

- `CipherNest.UnitTests`
- `CipherNest.IntegrationTests`
- `CipherNest.UiTests`

It also executes `dotnet format --verify-no-changes` against Domain, Application, Infrastructure, Shared, and test projects.

Shared build policy keeps enabled:

- nullable analysis;
- warnings as errors;
- analyzers/latest analysis level;
- code-style enforcement;
- deterministic compilation.

Local equivalents:

```text
scripts/verify-core.ps1
scripts/verify-core.sh
```

## What core tests include

Current suites cover security/resource policies such as:

- cryptographic known-answer/tamper/wrong-key behavior;
- vault-header parsing/version compatibility;
- TOTP RFC compatibility and hostile Base32 input;
- settings JSON bounds/fallback;
- CSV parser/header bounds;
- attachment metadata/storage-name policies;
- vault validation/resource budgets;
- migration/database replacement;
- encrypted backup/restore/corruption/resource behavior;
- session transition/cancellation behavior;
- attachment streaming/cancellation;
- generator/audit/note policies;
- lifecycle/privacy/source invariants;
- documentation presence/links/disclaimers;
- WinRT/AOT-safe MAUI ViewModel source patterns;
- BMC/funding source surfaces.

`UiTests` source guards do not prove runtime device behavior.

# 4. Documentation source gate

`DocumentationCoverageSourceTests` guards canonical documentation files and selected cross-links/security wording.

The complete documentation expansion additionally requires:

- `docs/QUICK_START.md`;
- `docs/FEATURE_MATRIX.md`;
- `docs/UI_REFERENCE.md`;
- `docs/CONFIGURATION_REFERENCE.md`;
- `docs/COMPLETE_PROJECT_DOCUMENTATION.md`;
- `docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md`.

Automated presence/wording checks complement manual semantic source-to-document review. A source test cannot prove every sentence of a long document matches runtime behavior.

See `COMPLETE_DOCUMENTATION_2026_08_16.md`.

# 5. Windows MAUI compile gate

The Windows job:

1. installs the MAUI workload;
2. selects only `net10.0-windows10.0.19041.0` using `CipherNestTargetFrameworks`;
3. uses `win-x64`;
4. compiles the Release app without store signing;
5. compiles both the normal build and the funding-disabled build.

Funding-disabled variant:

```text
CipherNestEnableFundingLink=false
```

The Windows target intentionally leaves CommunityToolkit WinRT/AOT diagnostics enabled. The repository migrated affected ViewModels to partial observable properties instead of suppressing `MVVMTK0045`.

Local equivalent:

```text
scripts/verify-windows.ps1
```

# 6. Android MAUI compile gate

The Android job:

- installs `maui-android`;
- selects only `net10.0-android`;
- uses `android-arm64`;
- compiles a Release application.

The explicit target/RID prevents unrelated target graphs from entering restore/build on the Linux host.

Local equivalent:

```text
scripts/verify-android.sh
```

This catches compiler/binding/toolchain issues but does not prove physical-device behavior.

# 7. Apple MAUI compile gate

The hosted Apple line uses an explicitly compatible toolchain:

```text
runner: macos-26
.NET SDK: 10.0.302
Xcode: 26.5
.NET workload set: 10.0.300.3
iOS RID: iossimulator-arm64
Mac Catalyst RID: maccatalyst-arm64
```

It compiles:

```text
net10.0-ios
net10.0-maccatalyst
```

The workload-set pin is deliberate. An earlier default workload resolution installed an Apple SDK pack incompatible with the selected Xcode. Compatibility validation remains enabled; align the .NET SDK/workload/Xcode combination instead of suppressing the check.

Local equivalent:

```text
scripts/verify-apple.sh
```

This remains a compile/toolchain gate, not a substitute for provisioning, signing, simulator interaction, physical-device biometric testing, secure storage, notarization, or App Store validation.

# 8. CodeQL

CodeQL uses `github/codeql-action` v4 for C#.

The workflow builds:

- analyzable core code;
- the Android MAUI application path;

before analysis, so coverage is not limited to non-MAUI libraries.

The pre-documentation baseline `8566980f...` completed CodeQL run `31937127900` successfully.

CodeQL is a static-analysis gate, not proof of complete runtime security or an independent professional security audit.

# 9. Dependency review

Pull requests run GitHub dependency review with a high-severity failure threshold.

Release review must also inspect:

- exact direct/transitive restored packages;
- current advisories;
- license obligations;
- accepted exceptions with owner/expiry.

A prior hosted restore exposed a high-severity `NU1903` issue in an older SQLite native dependency. Current central pins include:

```text
Microsoft.Data.Sqlite 10.0.10
SQLitePCLRaw.bundle_e_sqlite3 2.1.12
```

Later successful hosted restore/build evidence no longer shows that original blocker, but dependency review remains a separate release gate.

# 10. BMC/funding build gate

Windows CI compiles both funding states so BMC support additions cannot silently break the store-policy-disabled variant.

In-app funding UI is guarded by the build feature flag associated with:

```text
CipherNestEnableFundingLink
```

Default: enabled.

A store/distribution build may set it to false. `.github/FUNDING.yml` is repository metadata and remains separate.

The exact target store/region policy still must be checked before packaging.

# 11. Documentation verification records

Key records include:

- `SECURITY_HARDENING_2026_08_11.md`
- `DOCUMENTATION_SUITE_2026_08_12.md`
- `SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md`
- `HOSTED_CI_EVIDENCE_2026_08_13.md`
- `DOCUMENTATION_CONSOLIDATION_2026_08_14.md`
- `TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md`
- `CSV_IMPORT_HARDENING_2026_08_15.md`
- `SETTINGS_JSON_HARDENING_2026_08_15.md`
- `BACKUP_HEADER_HARDENING_2026_08_15.md`
- `VAULT_HEADER_HARDENING_2026_08_15.md`
- `ATTACHMENT_METADATA_HARDENING_2026_08_15.md`
- `FINAL_REPOSITORY_HARDENING_2026_08_15.md`
- `VERIFIED_MAIN_BASELINE_2026_08_15.md`
- `REPOSITORY_AUDIT_2026_08_16.md`
- `COMPLETE_DOCUMENTATION_2026_08_16.md`

For a release candidate:

1. documentation/source tests must pass;
2. reviewers must compare changed public contracts/models/formats/limits/session/platform/recovery/deferred claims against current source;
3. README, docs hub, security/privacy/support/contribution, changelog, status, release checklist, and affected specialist docs must remain synchronized;
4. historical evidence must retain its exact original commit/run context.

# 12. Workflow resource controls

Primary CI, CodeQL, and dependency review use bounded job timeouts and concurrency groups that cancel superseded runs. This prevents stale commits from consuming unlimited hosted-runner time and exposes stuck workload/setup failures.

# 13. What configured gates do not prove

Hosted/source automation does not replace:

- Android physical-device biometric enrollment/denial/cancellation/lockout/secure-storage testing;
- iOS/Mac Catalyst Face ID/Touch ID/secure-storage runtime validation;
- Windows/iOS/macOS/Android real clipboard history/cleanup behavior;
- screenshot/app-switcher privacy behavior;
- sleep/background/resume lifecycle timing;
- picker/share-provider plaintext-retention behavior;
- TalkBack/VoiceOver/Narrator/keyboard/focus/large-text accessibility testing;
- representative responsive layout/contrast/touch-target review;
- session/attachment/restore/filesystem stress/interleaving beyond automated cases;
- signing/provisioning/notarization/package validation;
- store privacy/policy/submission review;
- pull-request dependency review for a commit that bypassed the PR gate;
- exact release package graph/license/advisory review;
- independent professional cryptographic/security audit.

# 14. Release evidence checklist

For every immutable release candidate record:

- commit/tag;
- product/package version;
- .NET SDK version;
- installed workload/workload-set versions;
- runner/host OS;
- platform SDK/Xcode versions;
- target framework/RID;
- exact CI run/result;
- exact CodeQL run/result;
- dependency/advisory review result;
- exact test counts;
- documentation-source-test result;
- semantic documentation review record;
- `CipherNestEnableFundingLink` value per distributed package;
- device/simulator validation matrix;
- accessibility/localization/responsive results;
- backup/restore/recovery compatibility results;
- signing/store pipeline identity outside the repository;
- accepted exceptions with owner/expiry.

Do not mark a release gate complete from source inspection alone.
