# Build and Run

## Prerequisites

Install a current supported .NET 10 SDK and the .NET MAUI workload. Platform targets additionally require the platform SDK/toolchain: Android SDK/JDK for Android, Windows App SDK tooling on Windows, and Xcode on a supported Mac for iOS/Mac Catalyst.

```bash
dotnet --info
dotnet workload restore
dotnet restore CipherNest.slnx
```

## Reproducible verification entry points

Use the committed scripts before hand-assembling local commands. They mirror the repository's current verification intent and fail on the first unsuccessful restore/build/test/format step.

- PowerShell core verification: `scripts/verify-core.ps1`
- POSIX core verification: `scripts/verify-core.sh`
- Windows MAUI verification: `scripts/verify-windows.ps1`
- Android MAUI verification: `scripts/verify-android.sh`
- iOS + Mac Catalyst verification on macOS: `scripts/verify-apple.sh`

The Windows script compiles both the normal app and the `CipherNestEnableFundingLink=false` variant. The platform scripts are compile gates; device behavior still requires the release test matrix. See `docs/verification/CI_GATES.md`.

## Host-independent core tests

The non-MAUI test projects can be restored/built/run on a normal .NET 10 host:

```bash
dotnet build tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj -c Release
dotnet build tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj -c Release
dotnet build tests/CipherNest.UiTests/CipherNest.UiTests.csproj -c Release

dotnet test tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj -c Release --no-build
dotnet test tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj -c Release --no-build
dotnet test tests/CipherNest.UiTests/CipherNest.UiTests.csproj -c Release --no-build
```

The core CI gate also runs `dotnet format --verify-no-changes` for Domain, Application, Infrastructure, Shared, UnitTests, IntegrationTests, and UiTests.

## Windows

From Windows with the MAUI workload installed:

```powershell
dotnet restore src/CipherNest.App/CipherNest.App.csproj -p:TargetFramework=net10.0-windows10.0.19041.0
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-windows10.0.19041.0 --no-restore -p:WindowsPackageType=None
```

Or run:

```powershell
./scripts/verify-windows.ps1
```

Store/MSIX signing is a separate release step and requires signing material outside the repository.

## Android

From a host with the supported Android toolchain:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-android
```

Or run:

```bash
sh scripts/verify-android.sh
```

Use an emulator or physical device for biometric, screenshot, clipboard, lifecycle, file-picker, sharing, and accessibility verification. Source compilation alone is not proof that those behaviors work on a specific Android device/OS build.

## iOS and Mac Catalyst

Use a supported Mac/Xcode environment:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-ios
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-maccatalyst
```

Or run:

```bash
sh scripts/verify-apple.sh
```

Provisioning/signing identities, App Store credentials, and certificates must be supplied through protected local/CI mechanisms and never committed.

## Optional project-support CTA build switch

The repository's optional support URL is `https://buymeacoffee.com/sanskarIN`. Normal builds expose the voluntary support surface in About. If a target store/distribution policy does not permit that in-app external funding CTA, compile the app with it disabled rather than editing source:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

`CipherNestEnableFundingLink` defaults to `true`. Setting it explicitly to `false` defines `CIPHERNEST_DISABLE_FUNDING_LINK` for the MAUI app and `BuildFeatureFlags.IsFundingLinkEnabled` hides the in-app funding frame and metadata label. Other values leave the default UI behavior unchanged. This switch does not alter repository README/Support/Funding metadata. Verify the current policy for the exact store, region, distribution method, and app category before choosing the release value.

## GitHub CI coverage

The main workflow is configured to run:

- core unit/integration/UI-structure tests and formatting on Ubuntu;
- default and funding-disabled Windows Release compilation;
- Android Release compilation;
- iOS and Mac Catalyst Release compilation on macOS.

CodeQL builds both analyzable core code and the Android MAUI application target. Dependency review fails pull requests when introduced dependencies meet the configured high-severity threshold. Main CI, CodeQL, and dependency review use concurrency cancellation and explicit job timeouts.

A configured workflow is not evidence that a particular commit passed. Review the checks for the exact release candidate.

## Full solution

`dotnet build CipherNest.slnx` evaluates all included projects and may require every target workload/toolchain represented by the MAUI app. Prefer the target-specific app commands above on a host that cannot build every platform.

## Formatting and analysis

Before a release candidate, use the core verification script or run formatting explicitly. Running `dotnet format` across the full solution can require workloads for every target, so on a host without all MAUI workloads prefer the project-scoped formatting commands encoded by `scripts/verify-core.*`.

Repository build properties enable nullable analysis, current analyzers, deterministic builds, and warnings-as-errors. See `docs/TEST_PLAN.md`, `docs/RELEASE_CHECKLIST.md`, `docs/NEXT_STEPS.md`, and `docs/verification/CI_GATES.md` for the complete release gate and ordered follow-up plan.
