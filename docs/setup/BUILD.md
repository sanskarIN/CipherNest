# Build and Run

## Prerequisites

Install a current supported .NET 10 SDK and the .NET MAUI workload. Platform targets additionally require the platform SDK/toolchain: Android SDK/JDK for Android, Windows App SDK tooling on Windows, and Xcode on a supported Mac for iOS/Mac Catalyst.

```bash
dotnet --info
dotnet workload restore
dotnet restore CipherNest.slnx
```

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

## Windows

From Windows with the MAUI workload installed:

```powershell
dotnet restore src/CipherNest.App/CipherNest.App.csproj -p:TargetFramework=net10.0-windows10.0.19041.0
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-windows10.0.19041.0 --no-restore -p:WindowsPackageType=None
```

Store/MSIX signing is a separate release step and requires signing material outside the repository.

## Android

From a host with the supported Android toolchain:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Debug -f net10.0-android
```

Use an emulator or physical device for biometric, screenshot, clipboard, lifecycle, file-picker, sharing, and accessibility verification. Source compilation alone is not proof that those behaviors work on a specific Android device/OS build.

## iOS and Mac Catalyst

Use a supported Mac/Xcode environment:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Debug -f net10.0-ios
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Debug -f net10.0-maccatalyst
```

Provisioning/signing identities, App Store credentials, and certificates must be supplied through protected local/CI mechanisms and never committed.

## Optional project-support CTA build switch

The repository's optional support URL is `https://buymeacoffee.com/sanskarIN`. Normal builds expose the voluntary support surface in About. If a target store/distribution policy does not permit that in-app external funding CTA, compile the app with it disabled rather than editing source:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

`CipherNestEnableFundingLink` defaults to `true`. A non-`true` value defines `CIPHERNEST_DISABLE_FUNDING_LINK` for the MAUI app and `BuildFeatureFlags.IsFundingLinkEnabled` hides the in-app funding frame and metadata label. This switch does not alter repository README/Support/Funding metadata. Verify the current policy for the exact store, region, distribution method, and app category before choosing the release value.

## Full solution

`dotnet build CipherNest.slnx` evaluates all included projects and may require every target workload/toolchain represented by the MAUI app. Prefer the target-specific app commands above on a host that cannot build every platform.

## Formatting and analysis

Before a release candidate:

```bash
dotnet format CipherNest.slnx --verify-no-changes
```

Repository build properties enable nullable analysis, current analyzers, deterministic builds, and warnings-as-errors. See `docs/TEST_PLAN.md`, `docs/RELEASE_CHECKLIST.md`, and `docs/NEXT_STEPS.md` for the complete release gate and ordered follow-up plan.
