# Build and Run

CipherNest targets .NET 10 MAUI for Android, iOS, Mac Catalyst, and Windows. Core source/test projects can be built on any supported .NET 10 host; MAUI platform builds require the corresponding workload/toolchain.

For project architecture/dependency ownership before changing build inputs, see `../DEVELOPER_GUIDE.md` and `../architecture/DEPENDENCY_MAP.md`. The complete documentation index is `../README.md`.

## Prerequisites

- .NET SDK family pinned by `global.json` (10.0.100 with `latestFeature` roll-forward).
- .NET MAUI workload for platform builds.
- Android SDK/JDK for Android.
- Windows SDK/tooling on Windows for Windows target.
- A supported Mac/Xcode environment for iOS and Mac Catalyst.

Inspect the environment with:

```bash
dotnet --info
dotnet workload list
```

## Restore

For core projects, ordinary solution/project restore is appropriate:

```bash
dotnet restore CipherNest.slnx
```

For a platform-only MAUI build, prefer the committed verification script or pass `CipherNestTargetFrameworks` so the app project evaluates only the requested target framework. This prevents unrelated platform workloads from entering the restore graph on a host that does not support them.

Package versions are centrally managed in `Directory.Packages.props`.

## Core build/test

Prefer the committed verification scripts because they encode the repository's intended core gate:

PowerShell:

```powershell
./scripts/verify-core.ps1
```

POSIX:

```bash
sh scripts/verify-core.sh
```

These cover the non-MAUI core/test projects and formatting verification. See `../TESTING_GUIDE.md` and `../verification/CI_GATES.md` for test/evidence semantics.

## Windows

On Windows with the MAUI workload/tooling:

```powershell
./scripts/verify-windows.ps1
```

Direct build target matching CI intent:

```powershell
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release `
  -p:CipherNestTargetFrameworks=net10.0-windows10.0.19041.0 `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:WindowsPackageType=None
```

Current declared Windows minimum is `10.0.19041.0`.

The Windows build intentionally keeps CommunityToolkit WinRT/AOT diagnostics enabled. CipherNest MAUI ViewModels use partial `[ObservableProperty]` properties rather than field-based generation so the generated surface is compatible with the Windows/WinRT analyzer requirements.

## Android

With Android MAUI workload/SDK/JDK:

```bash
sh scripts/verify-android.sh
```

Direct target matching CI intent:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-android \
  -f net10.0-android \
  -r android-arm64
```

The app project declares Android minimum API 26. The optional native biometric convenience-unlock implementation uses the API-28 `BiometricPrompt` path and must fall back safely where that capability is unavailable.

## iOS and Mac Catalyst

On a supported Mac/Xcode environment:

```bash
sh scripts/verify-apple.sh
```

Direct targets use an app-only target-framework selection and an appropriate target RID. Example shapes:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-ios \
  -f net10.0-ios \
  -r iossimulator-arm64

dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-maccatalyst \
  -f net10.0-maccatalyst \
  -r maccatalyst-arm64
```

The current project declares minimum version 15.0 for iOS and Mac Catalyst.

### Hosted Apple toolchain baseline recorded on 2026-08-13

The successful hosted Apple CI candidate documented in `../verification/HOSTED_CI_EVIDENCE_2026_08_13.md` used:

- GitHub runner label `macos-26`;
- .NET SDK `10.0.302`;
- Xcode `26.5`;
- .NET workload set `10.0.300.3`;
- iOS simulator RID `iossimulator-arm64`;
- Mac Catalyst RID `maccatalyst-arm64`.

The workload set is explicitly pinned in hosted CI because default workload resolution previously installed an Apple SDK pack incompatible with the selected Xcode. Do not disable Xcode compatibility validation to work around a mismatch; align the .NET SDK/workload/Xcode combination instead.

Local developers do not need to remain forever on those exact versions, but the local .NET Apple workloads and Xcode must be mutually compatible. Record the exact toolchain in release evidence.

## Funding-link build switch

The in-app optional Buy Me a Coffee surface is enabled unless the property is explicitly set to `false`.

Example:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

This defines the app's disable symbol and hides the in-app funding CTA. It does not remove repository `.github/FUNDING.yml` metadata. Verify current policy for the exact store/distribution/region before choosing the release value, and record it in release provenance.

## Build quality policy

`Directory.Build.props` applies the shared project policy:

- latest stable project language policy from the shared props;
- nullable reference analysis;
- implicit usings;
- warnings as errors;
- latest analysis level;
- code-style enforcement in build;
- deterministic managed compilation;
- `ContinuousIntegrationBuild=true` when `CI=true`.

The MAUI **App project** deliberately overrides the language version to:

```xml
<LangVersion>preview</LangVersion>
```

This is narrowly scoped to the app because the current CommunityToolkit WinRT/AOT-safe partial observable-property syntax requires the preview C# feature in this toolchain. Do not move that override into every project unless another layer genuinely needs the preview language surface.

Resolve new warnings/errors instead of globally disabling these gates.

## CI

Main CI is configured for:

- Ubuntu core restore/build/test/format;
- Windows default Release compilation;
- Windows funding-disabled Release compilation;
- Android Release compilation;
- iOS and Mac Catalyst Release compilation on macOS;
- timeouts and superseded-run cancellation.

CodeQL v4 analyzes the MAUI Android application path plus analyzable core code. Dependency review is configured separately for pull requests.

Hosted candidate `2327abba1646082a4d94a689d452b1116701cc0b` completed all configured core/platform compile gates and CodeQL successfully. Exact run evidence and test counts are in `../verification/HOSTED_CI_EVIDENCE_2026_08_13.md`. A later candidate must rerun the gates; historical evidence is not inherited automatically.

## Debug versus Release

The App registers normal debug logging only under `DEBUG`. Developers must still avoid writing sensitive vault values, credentials, paths, or plaintext to debug logs.

Security/resource behavior must not depend on Debug-only checks; release builds require the same validation/authentication boundaries.

## Packaging/signing

Compilation does not produce a distribution-ready claim by itself. Signing/provisioning/store packaging must happen in protected environments with credentials outside Git.

See:

- `../releases/PACKAGING.md`
- `../releases/REPRODUCIBLE_BUILDS.md`
- `../releases/STORE_LISTING_GUIDE.md`
- `../releases/RELEASE_PROCESS.md`
- `../RELEASE_CHECKLIST.md`

## Troubleshooting

See `../TROUBLESHOOTING.md`. When reporting build problems, provide SDK/workload/platform details and redacted error output; never include signing secrets, real vault data, passphrases, recovery material, or store tokens.
