# Hosted CI Evidence — 2026-08-13

This document records hosted GitHub Actions evidence that was actually observed for CipherNest rather than inferring success from workflow source.

## Exact candidate

- Repository: `sanskarIN/CipherNest`
- Branch: `main`
- Candidate commit: `2327abba1646082a4d94a689d452b1116701cc0b`
- Candidate commit subject: `ci(apple): pin Xcode 26.5 compatible workload set`
- Main CI run: `31697433940`
- CodeQL run: `31697433730`

The candidate commit includes the Windows WinRT/AOT observable-property migration, application-only C# preview language setting required for CommunityToolkit partial observable properties, SQLite dependency remediation, Android/Windows/Apple target isolation, Apple runner/toolchain alignment, BMC support highlighting, runtime integration hardening, and the preceding source/format fixes.

## Main CI result

The complete `CipherNest CI` run for the exact candidate completed successfully.

### Core

The `test-core` job completed successfully on Ubuntu 24.04.

Observed analyzer builds:

- `CipherNest.UnitTests`: succeeded with 0 warnings / 0 errors.
- `CipherNest.IntegrationTests`: succeeded with 0 warnings / 0 errors.
- `CipherNest.UiTests`: succeeded with 0 warnings / 0 errors.

Observed runtime test results:

- Unit tests: **106 passed, 0 failed, 0 skipped**.
- Integration tests: **60 passed, 0 failed, 0 skipped**.
- UI/source regression tests: **74 passed, 0 failed, 0 skipped**.
- Total: **240 passed, 0 failed, 0 skipped**.

The configured `dotnet format --verify-no-changes` checks for Shared, Domain, Application, Infrastructure, UnitTests, IntegrationTests, and UiTests also completed successfully.

### Windows

The `build-windows` job completed successfully.

Observed successful builds:

- Windows Release app build with analyzers.
- Windows Release app build with `CipherNestEnableFundingLink=false`.

This is compile/build evidence, not signed-package, store-install, Windows biometric, clipboard-history, screenshot, lifecycle, or physical-device evidence.

### Android

The `build-android` job completed successfully.

Observed successful build:

- Android Release app build using `net10.0-android` and `android-arm64`.

This is compile/build evidence, not emulator/physical-device biometric, clipboard, screenshot, lifecycle, secure-storage, share-sheet, or store-package evidence.

### Apple

The `build-apple` job completed successfully on the supported GitHub-hosted `macos-26` runner.

The verified Apple toolchain pairing was:

- .NET SDK: `10.0.302`
- Xcode: `26.5`
- .NET workload set: `10.0.300.3`
- iOS/Mac Catalyst SDK family supplied by that workload set: Xcode 26.5-compatible .NET 10 Apple workloads.

Observed successful builds:

- iOS simulator Release app build using `iossimulator-arm64`.
- Mac Catalyst Release app build using `maccatalyst-arm64`.

This is compile/build evidence, not Face ID/Touch ID enrollment, SecureStorage lifecycle, real simulator interaction, notarization, signing, App Store submission, or physical-device evidence.

## CodeQL result

The CodeQL v4 run for the exact candidate completed successfully.

Observed successful stages:

- CodeQL initialization.
- analyzable core build.
- .NET MAUI Android workload installation.
- analyzable MAUI Android application build.
- CodeQL analysis.

A successful CodeQL workflow is an automated static-analysis signal. It is not an independent professional security audit and must not be described as one.

## Dependency remediation represented by this candidate

A prior hosted restore surfaced `NU1903` for vulnerable `SQLitePCLRaw.lib.e_sqlite3` 2.1.11. The repository was remediated by:

- updating `Microsoft.Data.Sqlite` to `10.0.10`;
- explicitly pinning `SQLitePCLRaw.bundle_e_sqlite3` to `2.1.12` in central package management and referencing the bundle from Infrastructure.

The exact candidate's hosted restores/builds succeeded without the earlier `NU1903` blocker. Pull-request dependency review remains a separate configured gate and was not substituted by this push-run evidence.

## Windows WinRT/AOT remediation represented by this candidate

A hosted Windows build surfaced CommunityToolkit `MVVMTK0045` for field-based `[ObservableProperty]` generation on WinRT/WinUI. CipherNest migrated the affected MAUI ViewModels to partial observable properties instead of suppressing the analyzer.

The MAUI app project explicitly uses:

```xml
<LangVersion>preview</LangVersion>
```

because the CommunityToolkit partial-property pattern used for the Windows WinRT/AOT-safe generated surface requires the preview language feature in this toolchain. `ViewModelAotSourceTests` prevents reintroduction of field-based observable properties and requires the app-level preview setting.

## Apple CI remediation represented by this candidate

The Apple gate was corrected in three distinct steps based on hosted-run evidence:

1. the workflow stopped using `macos-26-arm64` as a YAML runner label and now uses the supported `macos-26` label;
2. the job selects Xcode 26.5 explicitly;
3. the job installs the Xcode 26.5-compatible .NET workload set `10.0.300.3` instead of allowing the default workload manifest to install a mismatched Apple SDK pack.

No Xcode compatibility validation was disabled.

## Evidence that remains external

This hosted run does not establish:

- real-device biometric behavior or enrollment changes;
- secure-storage loss/recovery behavior on physical devices;
- screenshot blocking on every supported OS/version;
- clipboard/history behavior on every target;
- background/suspend/resume behavior on physical devices;
- share-sheet plaintext-cache behavior outside CipherNest's own temporary-file handling;
- accessibility certification or complete TalkBack/VoiceOver/Narrator/keyboard validation;
- signed/notarized/store package correctness;
- store-policy acceptance of the optional funding CTA;
- independent professional cryptographic/security audit;
- forensic or physical secure deletion guarantees.

The application must continue to use the existing honest limitations in the threat model, privacy documentation, release checklist, and store guidance.

## Release use

For a release candidate derived from a later commit, this evidence is historical only. The required build/test/security gates must be rerun on the exact release candidate, especially when source, project files, package versions, workflows, resource limits, cryptographic formats, migrations, platform bindings, or release configuration change.
