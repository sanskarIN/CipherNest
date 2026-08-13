# CipherNest Verification Gates

This document records the source and build gates that are configured in the repository. A configured gate is not the same as a passing gate: release evidence must come from the exact candidate commit and the target environment that executed it.

The first complete hosted cross-platform evidence captured for the current hardening line is recorded in `HOSTED_CI_EVIDENCE_2026_08_13.md`. Candidate `2327abba1646082a4d94a689d452b1116701cc0b` passed the configured core, Windows default/funding-disabled, Android, iOS simulator, Mac Catalyst, and CodeQL gates. Later candidates must rerun the gates rather than inheriting that result automatically.

## Core gate

The `test-core` job on Ubuntu restores, builds, and runs:

- `CipherNest.UnitTests`
- `CipherNest.IntegrationTests`
- `CipherNest.UiTests`

It also runs `dotnet format --verify-no-changes` against the Domain, Application, Infrastructure, Shared, and test projects. Repository build properties keep nullable analysis, analyzers, deterministic compilation, and warnings-as-errors enabled.

`CipherNest.UiTests` includes `DocumentationCoverageSourceTests` and `ViewModelAotSourceTests`. Documentation coverage guards required canonical documentation files, root/documentation-hub entry points, and explicit independent-audit disclaimers. The WinRT/AOT source guard rejects field-based CommunityToolkit observable-property generation in the MAUI ViewModels and requires the app-level preview language setting used by the partial-property implementation.

Semantic documentation accuracy still requires review against the exact source candidate; see `DOCUMENTATION_SUITE_2026_08_12.md`.

Local equivalents:

- PowerShell: `scripts/verify-core.ps1`
- POSIX shell: `scripts/verify-core.sh`

## Windows MAUI compile gate

The Windows job installs the MAUI workload, selects only `net10.0-windows10.0.19041.0` through the app-specific `CipherNestTargetFrameworks` property, uses `win-x64`, and compiles the Release app without producing a signed store package. It compiles both:

1. the normal default build; and
2. the store-policy variant with `CipherNestEnableFundingLink=false`.

The Windows build intentionally leaves CommunityToolkit WinRT/AOT diagnostics enabled. A prior hosted build surfaced `MVVMTK0045`; CipherNest fixed the affected ViewModels by migrating to partial observable properties instead of suppressing the analyzer.

Local equivalent: `scripts/verify-windows.ps1`.

## Android MAUI compile gate

The Android job installs `maui-android`, selects only `net10.0-android`, uses `android-arm64`, and compiles a Release app. The explicit app target/RID prevents the Linux host runtime identifier or unrelated Apple target frameworks from entering the Android restore graph.

This catches target-specific API/binding/compiler issues but does not prove behavior on a device.

Local equivalent: `scripts/verify-android.sh`.

## Apple MAUI compile gate

The hosted Apple gate uses an explicitly compatible toolchain instead of relying on whichever default workload manifest happens to be selected by a future runner image:

- runner label: `macos-26`;
- .NET SDK: `10.0.302`;
- Xcode: `26.5`;
- .NET workload set: `10.0.300.3`;
- iOS RID: `iossimulator-arm64`;
- Mac Catalyst RID: `maccatalyst-arm64`.

The job selects only the requested app target framework for each build:

- `net10.0-ios`
- `net10.0-maccatalyst`

The workload-set pin is deliberate. Earlier hosted evidence showed that allowing the default Apple workload resolution could install an older .NET iOS SDK pack that rejected Xcode 26.5. Compatibility validation remains enabled; the fix aligns the SDK/workload/Xcode versions instead of suppressing the check.

This is a source/toolchain compile gate, not a substitute for provisioning, signing, simulator interaction, physical-device smoke tests, Face ID/Touch ID behavior, notarization, or App Store validation.

Local equivalent: `scripts/verify-apple.sh`. Local Apple verification must use a mutually compatible .NET SDK/workload/Xcode combination; the hosted versions above describe the recorded CI pairing rather than a promise that every developer machine must remain on those exact versions forever.

## CodeQL

CodeQL uses `github/codeql-action` v4 and analyzes C# source after building the analyzable core path and the Android MAUI application target. This broadens analysis beyond the non-MAUI libraries. CodeQL results must still be reviewed for the exact release candidate.

Candidate `2327abba1646082a4d94a689d452b1116701cc0b` completed the configured CodeQL v4 run successfully; details are in `HOSTED_CI_EVIDENCE_2026_08_13.md`.

## Dependency review

Pull requests run GitHub dependency review with `fail-on-severity: high`. Release review must also inspect the exact restored direct/transitive package graph, license obligations, and any accepted vulnerability exception.

A prior hosted restore surfaced a high-severity `NU1903` finding in the older SQLitePCLRaw native dependency. The repository now pins `Microsoft.Data.Sqlite` 10.0.10 and `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12. Successful later hosted restore/build evidence confirms the earlier blocker is no longer present in that candidate, but pull-request dependency review and release-time advisory review remain separate gates.

## Documentation verification

Committed verification references complement the executable core gate:

- `SECURITY_HARDENING_2026_08_11.md` — framing/resource/session/platform source hardening gates.
- `DOCUMENTATION_SUITE_2026_08_12.md` — complete documentation-suite presence, link, disclaimer, semantic source-to-document, synthetic-data, and historical-preservation gates.
- `SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md` — BMC support presentation plus runtime record/session/backup hardening gates.
- `HOSTED_CI_EVIDENCE_2026_08_13.md` — exact observed hosted test/format/platform/CodeQL evidence and the remaining external limitations.

For a release candidate:

1. `CipherNest.UiTests`, including documentation and WinRT/AOT source regressions, must execute successfully.
2. Reviewers must manually compare any changed public interfaces, models, format framing, limits/defaults, session/destructive authorization, platform support, recovery/deletion semantics, dependencies, CI toolchain pins, and deferred features against the canonical documentation.
3. Root `README.md`, `docs/README.md`, security/privacy/support/contribution entry points, changelog, project status, release checklist, and affected format/security docs must all remain synchronized.
4. Historical hosted evidence must identify the exact commit/run and must not be silently reused as evidence for a later untested candidate.

A documentation source test proves required files/strings are present; it does not prove the prose accurately describes runtime behavior unless reviewers perform the source-to-document comparison.

## Workflow resource controls

Primary CI, CodeQL, and dependency review use concurrency groups that cancel superseded runs and explicit job timeouts. This prevents stale commits from consuming unlimited hosted-runner time and makes stuck workload/install failures visible.

## What these gates do not prove

The configured workflows do not replace:

- Android/iOS/Mac Catalyst/Windows physical-device or interactive simulator smoke tests;
- biometric enrollment, denial, cancellation, lockout, and secure-storage lifecycle testing;
- screenshot/app-switcher privacy behavior;
- real clipboard history and clearing behavior;
- sleep/background/resume lifecycle validation;
- accessibility testing with TalkBack, VoiceOver, Narrator, keyboard-only navigation, and OS large text;
- file picker/share-provider plaintext-retention behavior;
- semantic review that documentation exactly matches the current candidate beyond the automated presence/link/disclaimer checks;
- signing, notarization, store package validation, or store policy review;
- pull-request dependency review for a candidate that has not gone through the PR gate;
- an independent cryptographic/security audit.

## Release evidence

For every candidate, record:

- immutable commit/tag;
- .NET SDK version;
- installed workload/workload-set versions;
- runner/host OS, platform SDK versions, selected Xcode, and target RID where applicable;
- exact CI run and CodeQL run identifiers/conclusions;
- dependency-review and advisory-review results;
- documentation-completeness test result and semantic documentation-review record;
- selected `CipherNestEnableFundingLink` value for each distributed package;
- device/simulator matrix and smoke-test results;
- signing/store pipeline identity outside the repository;
- known accepted exceptions with owner and expiry.

Do not mark a release gate complete from source inspection alone.
