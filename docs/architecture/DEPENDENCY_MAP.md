# CipherNest Dependency Map

## Solution projects

`CipherNest.slnx` includes:

```text
src/
  CipherNest.Shared
  CipherNest.Domain
  CipherNest.Application
  CipherNest.Infrastructure
  CipherNest.App

tests/
  CipherNest.UnitTests
  CipherNest.IntegrationTests
  CipherNest.UiTests
```

## Responsibility map

| Project | Owns | Must not own |
|---|---|---|
| Shared | product/version/storage constants, small cross-layer primitives | MAUI UI, SQLite connections, user workflows |
| Domain | framework-independent vault/preference/audit/generator/attachment models | platform APIs, persistence implementations, cryptographic implementations |
| Application | service/use-case abstractions, policies, validation, application models/exceptions | MAUI controls, SQLite implementation details, platform secure storage |
| Infrastructure | crypto, SQLite/migrations, backup, attachment storage, CSV parser/transfer, generator/audit implementations | user-facing MAUI navigation/platform presentation |
| App | DI composition, Views/ViewModels, lifecycle, platform biometric/clipboard/screenshot/secure-storage/picker/share, accessibility/localization, About/support UI | alternate direct database/crypto flows that bypass Application contracts |

## High-level dependency graph

```text
CipherNest.Shared  <-------------------------------+
      ^                                               |
      |                                               |
CipherNest.Domain <----------------------+            |
      ^                                  |            |
      |                                  |            |
CipherNest.Application                   |            |
      ^                                  |            |
      |                                  |            |
CipherNest.Infrastructure ---------------+------------+
      ^
      |
CipherNest.App
```

The App references all source layers for composition. Infrastructure references the contracts/models/constants it implements/uses. The design goal is not to make every project reference mathematically minimal; it is to keep policy/use-case boundaries free of platform/persistence implementation dependencies.

## Runtime composition

`MauiProgram.CreateMauiApp` registers the production graph.

### Core/infrastructure singletons

```text
IClock                    -> SystemClock
ICryptoService            -> CryptoService
IVaultStore               -> SqliteVaultStore
IVaultService             -> VaultService
IPasswordGenerator        -> PasswordGenerator
ISecurityAuditService     -> SecurityAuditService
ISafeNoteMarkupService    -> SafeNoteMarkupService
ISettingsStore            -> JsonSettingsStore
IBackupService            -> EncryptedBackupService
IPlaintextTransferService -> CsvTransferService
```

### App/platform singletons

```text
IClipboardSecurityService    -> ClipboardSecurityService
IScreenshotProtectionService -> ScreenshotProtectionService
IBiometricUnlockService      -> BiometricUnlockService
IStorageMaintenanceService   -> StorageMaintenanceService
IPrivacySafeExceptionReporter-> PrivacySafeExceptionReporter
ILocalizationService         -> LocalizationService
UnlockRateLimiter
SessionSecurityState
SessionLockPolicy
```

ViewModels/Pages are registered transiently.

## Platform targets

The MAUI app currently targets:

```text
net10.0-android
net10.0-ios
net10.0-maccatalyst
net10.0-windows10.0.19041.0
```

Declared minimum supported platform versions in the current project file:

| Target | Declared minimum |
|---|---|
| Android | API 26 |
| iOS | 15.0 |
| Mac Catalyst | 15.0 |
| Windows | 10.0.19041.0 |

The biometric implementation has a narrower behavior boundary than the overall Android app target: native Android convenience unlock uses the API-28 `BiometricPrompt` path and must provide fallback on unsupported older platform versions.

## Central NuGet versions

`Directory.Packages.props` currently pins:

| Package | Version | Primary role |
|---|---:|---|
| CommunityToolkit.Mvvm | 8.4.0 | observable ViewModels / relay commands |
| Konscious.Security.Cryptography.Argon2 | 1.3.1 | Argon2id KDF implementation |
| Microsoft.Data.Sqlite | 10.0.0 | SQLite persistence/migrations/backup snapshot plumbing |
| Microsoft.Extensions.Logging.Debug | 10.0.0 | DEBUG logging integration |
| Microsoft.Maui.Controls | 10.0.0 | cross-platform MAUI UI/runtime |
| Microsoft.NET.Test.Sdk | 18.0.0 | test host |
| xunit | 2.9.3 | test framework |
| xunit.runner.visualstudio | 3.1.4 | test discovery/runner integration |

Dependency versions are centralized; exact direct/transitive restored package metadata and license/vulnerability state must still be reviewed for each release candidate.

## Build policy shared across projects

`Directory.Build.props` currently applies:

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

## App identifiers and build feature flags

Current MAUI metadata:

```text
ApplicationTitle = CipherNest
ApplicationId = in.sanskar.ciphernest
ApplicationDisplayVersion = 0.1.0
ApplicationVersion = 1
```

`CipherNestEnableFundingLink` defaults to `true`. Explicit `false` defines `CIPHERNEST_DISABLE_FUNDING_LINK` for the App project and hides the in-app funding CTA through `BuildFeatureFlags`.

## Persistence ownership

```text
MauiProgram
  -> SqliteVaultStore(app-data/ciphernest.db)
  -> JsonSettingsStore(app-data/settings.json)
  -> EncryptedAttachmentStore(app-data/attachments/*) via infrastructure services
  -> encrypted backups under app data / user-selected share paths
```

Exact platform filesystem locations come from MAUI `FileSystem.Current` APIs and vary by OS/install sandbox.

## Security-sensitive dependency boundaries

### Crypto

Only Infrastructure should implement Argon2/AES-GCM framing. App should not create ad-hoc encryption logic.

### Database

App should not obtain raw SQLite connections. Use `IVaultService`; backup internals use `IVaultStore` through Infrastructure.

### Platform secrets

Biometric secondary secrets use App/platform secure-storage APIs. Infrastructure must not assume a specific platform secure-store implementation.

### Diagnostics

Sensitive App/platform failure surfaces use `IPrivacySafeExceptionReporter`; do not add direct third-party telemetry dependencies without a separate privacy/threat review.

### Network

The current project has no required CipherNest backend/client network dependency for vault functionality. Introducing networking for synchronization/accounts would materially change this architecture and requires separate protocol/privacy/threat design.

## Test dependency intent

### UnitTests

Depend on the smallest source layers necessary for deterministic policy/crypto/service tests.

### IntegrationTests

Exercise real Infrastructure behavior such as SQLite, backup/restore, encryption, attachments, parser, migrations, and session cancellation where practical.

### UiTests

Inspect App/repository source structure without launching MAUI. These tests intentionally complement—not replace—emulator/physical-device behavior tests.

## Dependency change checklist

Before adding/upgrading a dependency:

1. Confirm the feature cannot be implemented safely with existing platform/BCL capability.
2. Review package ownership/reputation/maintenance and exact license.
3. Review transitive dependencies.
4. Review vulnerability/dependency-review/CodeQL output.
5. Confirm the dependency belongs in the correct project/layer.
6. Avoid dependencies that require sending vault data to a remote service unless the feature has an approved network/privacy architecture.
7. Add/update tests.
8. Update `THIRD_PARTY_NOTICES.md` when required.
9. Update this map and release provenance for release-impacting changes.

## TOTP dependency path

```text
ItemEditorViewModel
      |
      v
ITotpService (Application)
      |
      v
TotpService (Infrastructure)
      |
      +--> TotpPolicy (Application validation)
      +--> TotpAlgorithm / VaultItem (Domain)
      +--> System.Security.Cryptography HMAC implementations
```

The MAUI composition root registers `ITotpService -> TotpService`. TOTP remains platform-independent; clipboard behavior stays behind the existing App clipboard-security service. No provider network SDK or additional TOTP package is required by the current implementation.
