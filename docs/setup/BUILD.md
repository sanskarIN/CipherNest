# CipherNest Build and Run Guide

CipherNest targets .NET 10 MAUI for Android, iOS, Mac Catalyst, and Windows. Core source/test projects can be built on a supported .NET 10 host; MAUI platform builds require the corresponding workload/toolchain.

For architecture and configuration context, see [`../DEVELOPER_GUIDE.md`](../DEVELOPER_GUIDE.md), [`../CONFIGURATION_REFERENCE.md`](../CONFIGURATION_REFERENCE.md), and [`../architecture/DEPENDENCY_MAP.md`](../architecture/DEPENDENCY_MAP.md).

## 1. Prerequisites

- .NET SDK family selected by `global.json` (`10.0.100` baseline with `latestFeature` roll-forward).
- .NET MAUI workload for platform builds.
- Android SDK/JDK for Android.
- Windows SDK/tooling on Windows for the Windows target.
- A supported Mac/Xcode environment for iOS and Mac Catalyst.

Inspect the active environment:

```bash
dotnet --info
dotnet workload list
```

Record the actual SDK/workload/platform-toolchain versions when creating release evidence.

## 2. Target frameworks

Default MAUI target set:

```text
net10.0-android
net10.0-ios
net10.0-maccatalyst
net10.0-windows10.0.19041.0
```

Minimum declared platform versions:

| Platform | Minimum |
|---|---:|
| Android | API 26 |
| iOS | 15.0 |
| Mac Catalyst | 15.0 |
| Windows | 10.0.19041.0 |

The Android optional biometric convenience implementation uses the API-28 `BiometricPrompt` baseline and must fail/fallback safely where that capability is unavailable.

## 3. Restore

For ordinary core/solution work:

```bash
dotnet restore CipherNest.slnx
```

Package versions are centrally managed in `Directory.Packages.props`.

For a platform-only MAUI build, prefer the committed verification script or explicitly set `CipherNestTargetFrameworks` so unsupported unrelated target graphs do not enter restore/build on the current host.

## 4. Core verification

PowerShell:

```powershell
./scripts/verify-core.ps1
```

POSIX:

```bash
sh scripts/verify-core.sh
```

The core gate restores/builds/runs:

- `CipherNest.UnitTests`
- `CipherNest.IntegrationTests`
- `CipherNest.UiTests`

and verifies formatting for the non-MAUI source/test projects.

See [`../TESTING_GUIDE.md`](../TESTING_GUIDE.md) and [`../verification/CI_GATES.md`](../verification/CI_GATES.md).

## 5. Windows

Canonical script:

```powershell
./scripts/verify-windows.ps1
```

Direct build shape matching CI intent:

```powershell
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release `
  -p:CipherNestTargetFrameworks=net10.0-windows10.0.19041.0 `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:WindowsPackageType=None
```

Windows builds intentionally keep CommunityToolkit WinRT/AOT diagnostics active. MAUI ViewModels use partial `[ObservableProperty]` properties rather than the field-based generation pattern that triggers `MVVMTK0045` in the verified Windows toolchain.

### Funding-disabled Windows variant

CI also compiles the app with the in-app BMC/funding CTA disabled:

```powershell
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release `
  -p:CipherNestTargetFrameworks=net10.0-windows10.0.19041.0 `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:WindowsPackageType=None `
  -p:CipherNestEnableFundingLink=false
```

## 6. Android

Canonical script:

```bash
sh scripts/verify-android.sh
```

Direct target:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-android \
  -f net10.0-android \
  -r android-arm64
```

Hosted compilation catches binding/compiler/toolchain issues but does not prove physical-device biometric enrollment/denial/cancellation/lockout, secure storage, screenshot, clipboard, lifecycle, share-sheet, accessibility, or store behavior.

## 7. iOS and Mac Catalyst

Canonical script on a compatible Mac/Xcode environment:

```bash
sh scripts/verify-apple.sh
```

Direct target shapes:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-ios \
  -f net10.0-ios \
  -r iossimulator-arm64
```

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-maccatalyst \
  -f net10.0-maccatalyst \
  -r maccatalyst-arm64
```

### Recorded hosted Apple pairing

The compatible hosted verification line uses:

- runner: `macos-26`;
- .NET SDK: `10.0.302`;
- Xcode: `26.5`;
- workload set: `10.0.300.3`;
- iOS simulator RID: `iossimulator-arm64`;
- Mac Catalyst RID: `maccatalyst-arm64`.

The workload set is explicitly controlled because an earlier default workload resolution installed an Apple SDK pack incompatible with the selected Xcode. Do not disable Xcode compatibility validation to hide a mismatch; align the SDK/workload/Xcode combination.

Local developers do not need to remain permanently on those exact versions, but the selected .NET Apple workloads and Xcode must be mutually compatible. Record exact release-toolchain versions.

## 8. Funding/BMC build switch

The optional in-app Buy Me a Coffee support surface is enabled unless explicitly disabled.

Disable example:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

The App project then defines `CIPHERNEST_DISABLE_FUNDING_LINK`, and UI guarded by `BuildFeatureFlags.IsFundingLinkEnabled` is hidden/removed from the app build.

This does not remove repository `.github/FUNDING.yml` metadata. Check current policy for the exact store, distribution method, and region before selecting the release value and record it in provenance.

## 9. Build-quality policy

`Directory.Build.props` applies:

```text
LangVersion = latest
Nullable = enable
ImplicitUsings = enable
TreatWarningsAsErrors = true
AnalysisLevel = latest
EnforceCodeStyleInBuild = true
Deterministic = true
ContinuousIntegrationBuild = true when CI=true
```

The MAUI App project deliberately overrides:

```xml
<LangVersion>preview</LangVersion>
```

This remains scoped to `CipherNest.App` because the current CommunityToolkit partial observable-property syntax used for the WinRT/AOT-safe ViewModel source shape requires the preview language feature in the verified toolchain.

Resolve new warnings/errors instead of globally disabling quality gates.

## 10. Debug versus Release

Normal debug logging is registered only under `DEBUG`. Developers must still avoid writing vault contents, passphrases, recovery material, TOTP seeds/codes, filesystem paths identifying user content, clipboard plaintext, or other secrets to debug logs.

Security/resource validation must not rely only on Debug-only checks. Release builds require the same authentication/validation boundaries.

## 11. Current immutable pre-documentation implementation baseline

The complete-documentation expansion is grounded in exact implementation commit:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

Observed exact-candidate hosted evidence:

- UnitTests: **346 passed**;
- IntegrationTests: **98 passed**;
- UI/source tests: **111 passed**;
- total: **555 passed, 0 failed, 0 skipped**;
- analyzer-enabled core test builds completed with zero build warnings/errors;
- configured core formatting passed;
- Windows default Release passed;
- Windows funding-disabled Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI application builds.

Exact run identifiers:

```text
CipherNest CI: 31937127961
CodeQL:       31937127900
```

Historical dated records for earlier 240-test and 554-test exact candidates remain in `docs/verification/` and are intentionally preserved as historical evidence.

Any commit after `8566980f...` is a new exact head and must execute its own configured gates before being called release-candidate verified.

## 12. CI configuration

Main CI is configured for:

- Ubuntu core restore/build/test/format;
- Windows default Release compilation;
- Windows funding-disabled Release compilation;
- Android Release compilation;
- iOS simulator Release compilation;
- Mac Catalyst Release compilation;
- bounded timeouts and superseded-run cancellation.

CodeQL v4 builds analyzable core plus the Android MAUI application path before C# analysis. Pull requests use dependency review separately.

See [`../verification/CI_GATES.md`](../verification/CI_GATES.md).

## 13. Packaging and signing

Compilation alone does not produce a distribution-ready claim. Signing/provisioning/notarization/store packaging must occur in protected environments with credentials outside Git.

Do not commit:

- signing certificates/private keys;
- provisioning secrets;
- store passwords/tokens/API keys;
- encrypted secret files whose decryption keys are also stored in the repository.

See:

- [`../releases/PACKAGING.md`](../releases/PACKAGING.md)
- [`../releases/REPRODUCIBLE_BUILDS.md`](../releases/REPRODUCIBLE_BUILDS.md)
- [`../releases/STORE_LISTING_GUIDE.md`](../releases/STORE_LISTING_GUIDE.md)
- [`../releases/RELEASE_PROCESS.md`](../releases/RELEASE_PROCESS.md)
- [`../RELEASE_CHECKLIST.md`](../RELEASE_CHECKLIST.md)

## 14. What a successful hosted build does not prove

A green hosted build does not replace:

- physical-device biometrics/secure-storage behavior;
- screenshots/task-preview privacy;
- real clipboard history/cleanup;
- background/sleep/resume lifecycle timing;
- OS picker/share behavior and plaintext retention;
- assistive-technology accessibility testing;
- responsive-layout review on representative device sizes;
- stress/interleaving/filesystem recovery validation;
- package signing/notarization;
- store submission/privacy/policy review;
- exact release dependency/license/advisory review;
- independent professional security review.

## 15. Troubleshooting

Use [`../TROUBLESHOOTING.md`](../TROUBLESHOOTING.md).

When reporting a build issue, include non-sensitive details such as:

- source commit;
- OS/platform version;
- `dotnet --info` output;
- workload list;
- selected target framework/RID;
- redacted error text.

Never include real vault data, passphrases, recovery material, signing secrets, store tokens, or secret-bearing diagnostics.
