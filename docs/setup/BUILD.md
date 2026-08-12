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

```bash
dotnet restore CipherNest.slnx
```

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

Direct build target:

```powershell
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-windows10.0.19041.0
```

Current declared Windows minimum is `10.0.19041.0`.

## Android

With Android MAUI workload/SDK/JDK:

```bash
sh scripts/verify-android.sh
```

Direct target:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-android
```

The app project declares Android minimum API 26. The optional native biometric convenience-unlock implementation uses the API-28 `BiometricPrompt` path and must fall back safely where that capability is unavailable.

## iOS and Mac Catalyst

On a supported Mac/Xcode environment:

```bash
sh scripts/verify-apple.sh
```

Direct targets:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-ios
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-maccatalyst
```

The current project declares minimum version 15.0 for iOS and Mac Catalyst.

## Funding-link build switch

The in-app optional Buy Me a Coffee surface is enabled unless the property is explicitly set to `false`.

Example:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

This defines the app's disable symbol and hides the in-app funding CTA. It does not remove repository `.github/FUNDING.yml` metadata. Verify current policy for the exact store/distribution/region before choosing the release value, and record it in release provenance.

## Build quality policy

`Directory.Build.props` applies:

- latest C# language version;
- nullable reference analysis;
- implicit usings;
- warnings as errors;
- latest analysis level;
- code-style enforcement in build;
- deterministic managed compilation;
- `ContinuousIntegrationBuild=true` when `CI=true`.

Resolve new warnings/errors instead of globally disabling these gates.

## CI

Main CI is configured for:

- Ubuntu core restore/build/test/format;
- Windows default Release compilation;
- Windows funding-disabled Release compilation;
- Android Release compilation;
- iOS and Mac Catalyst Release compilation on macOS;
- timeouts and superseded-run cancellation.

CodeQL is configured to analyze the MAUI Android application path plus core/integration code. Dependency review is configured separately.

Configured workflow presence does **not** mean a candidate passed. Review the exact commit's run results according to `../verification/CI_GATES.md` and `../releases/RELEASE_PROCESS.md`.

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
