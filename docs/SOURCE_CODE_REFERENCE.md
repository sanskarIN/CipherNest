# CipherNest Source Code Reference

This file is the canonical **production-source and application-asset file reference** for CipherNest. It is intentionally path-oriented so a maintainer can map a repository file to its responsibility, security boundary, and deeper documentation without reverse-engineering the solution first.

Baseline used for this inventory: Transfer localization implementation head `0eec0f1e60de5ecf4576820935b8684ead42574b` on 2026-08-20. Later files must be added here in the same change that adds them.

> This is a source map, not an audit certificate. CipherNest has not completed an independent professional security audit. Current executable source and focused tests remain authoritative when prose and implementation disagree.

## Layer model

`CipherNest.Shared` and `CipherNest.Domain` are dependency-light foundations. `CipherNest.Application` owns contracts, pure policies, validators, and application-level models. `CipherNest.Infrastructure` implements cryptography, persistence, transfer, backup, attachment, generation, audit, and TOTP services. `CipherNest.App` composes those services into the .NET MAUI user interface and platform integrations.

The intended dependency direction is documented in `architecture/ARCHITECTURE.md` and `architecture/DEPENDENCY_MAP.md`.

# 1. CipherNest.App

## Application composition

- `src/CipherNest.App/CipherNest.App.csproj` — MAUI application project; target frameworks, application identity, resources, package references, and build-time funding-link behavior are rooted here.
- `src/CipherNest.App/App.xaml` — application-level XAML resources and startup resource composition.
- `src/CipherNest.App/App.xaml.cs` — application lifecycle coordination, startup wiring, session/background behavior, theme/localization preference application, and fail-closed lifecycle handling.
- `src/CipherNest.App/AppShell.xaml` — Shell navigation container and route-facing visual structure.
- `src/CipherNest.App/AppShell.xaml.cs` — Shell code-behind kept intentionally small.
- `src/CipherNest.App/MauiProgram.cs` — dependency-injection composition root registering Application abstractions, Infrastructure implementations, app services, ViewModels, pages, localization, diagnostics, and platform-facing services.

## Converters and localization

- `src/CipherNest.App/Converters/InverseBoolConverter.cs` — reverses Boolean values for XAML binding scenarios.
- `src/CipherNest.App/Converters/StringNotEmptyConverter.cs` — maps string presence to Boolean visibility/enabled-state style bindings.
- `src/CipherNest.App/Localization/TranslateExtension.cs` — reusable XAML markup extension that resolves reviewed localized strings through the existing `ILocalizationService`; it must not create a parallel translation store.

## App services

- `src/CipherNest.App/Services/AccessibilityPreferenceApplicator.cs` — applies persisted accessibility-oriented interface preferences such as larger-interface behavior.
- `src/CipherNest.App/Services/AttachmentTypePolicy.cs` — presentation/platform-side attachment type and preview suitability policy.
- `src/CipherNest.App/Services/BiometricUnlockService.cs` — platform convenience-unlock integration and secure-storage interaction; it is not a replacement for the master/recovery key hierarchy.
- `src/CipherNest.App/Services/BuildFeatureFlags.cs` — exposes compile/build-time feature switches, including funding-surface availability.
- `src/CipherNest.App/Services/ClipboardSecurityService.cs` — secret copy plus best-effort timed/conditional cleanup; OS clipboard history/synchronization remains outside CipherNest's guarantee boundary.
- `src/CipherNest.App/Services/IBiometricUnlockService.cs` — app-layer biometric convenience-unlock abstraction.
- `src/CipherNest.App/Services/IClipboardSecurityService.cs` — app-layer secure clipboard abstraction.
- `src/CipherNest.App/Services/ILocalizationService.cs` — resource-backed localization contract.
- `src/CipherNest.App/Services/IPrivacySafeExceptionReporter.cs` — diagnostic reporting contract that avoids raw sensitive exception content.
- `src/CipherNest.App/Services/IScreenshotProtectionService.cs` — platform screenshot/task-preview mitigation abstraction.
- `src/CipherNest.App/Services/IStorageMaintenanceService.cs` — local cache/storage inspection and safe-maintenance abstraction.
- `src/CipherNest.App/Services/LocalizationService.cs` — neutral English / reviewed Hindi culture application plus ordered primary/feature resource-catalog resolution, currently including Trash and Transfer feature catalogs; missing keys fail visibly by returning the key name.
- `src/CipherNest.App/Services/PrivacySafeExceptionReporter.cs` — redacted/fixed diagnostic reporting implementation; raw paths, vault data, credentials, and exception details must not become user-facing telemetry.
- `src/CipherNest.App/Services/ScreenshotProtectionService.cs` — target-specific screenshot/privacy-control implementation with honest unsupported-target behavior.
- `src/CipherNest.App/Services/ServiceProviderHelper.cs` — controlled access helper for application service resolution where XAML/application integration requires it.
- `src/CipherNest.App/Services/SessionSecurityState.cs` — UI-facing state for master-auth/secondary-auth/security-session decisions.
- `src/CipherNest.App/Services/StorageMaintenanceService.cs` — local cache/storage enumeration and cleanup implementation; it must not intentionally remove the encrypted vault database, encrypted attachment store, or app-data backups.
- `src/CipherNest.App/Services/UnlockRateLimiter.cs` — bounded interactive unlock backoff state used to slow repeated failures without destructive wipe behavior.

## ViewModels

- `src/CipherNest.App/ViewModels/AuditViewModel.cs` — drives the local decrypted-content security audit while unlocked and contains failure reporting.
- `src/CipherNest.App/ViewModels/DeveloperViewModel.cs` — developer-information surface without turning debug information into a secret-exposure path.
- `src/CipherNest.App/ViewModels/GeneratorDefaultsViewModel.cs` — edits and persists non-secret password/passphrase generator defaults.
- `src/CipherNest.App/ViewModels/GeneratorViewModel.cs` — password/passphrase generation workflow, strength guidance, clipboard interaction, and sensitive generated-value lifetime.
- `src/CipherNest.App/ViewModels/ItemEditorViewModel.cs` — primary item-editor orchestration, loading, validation, save/delete state, fields, tags, attachments, and per-item security behavior.
- `src/CipherNest.App/ViewModels/ItemEditorViewModel.Clipboard.cs` — username/secret/custom-secret copy paths and clipboard-failure containment.
- `src/CipherNest.App/ViewModels/ItemEditorViewModel.Preview.cs` — bounded attachment/text preview and plaintext-export/share interaction.
- `src/CipherNest.App/ViewModels/ItemEditorViewModel.Totp.cs` — TOTP generation, refresh, bounded setup-URI import/copy, localized status text, transient-code handling, and seed-bearing field cleanup.
- `src/CipherNest.App/ViewModels/OnboardingViewModel.cs` — first-vault creation, master-passphrase/recovery setup, navigation, and initialization state.
- `src/CipherNest.App/ViewModels/OnboardingViewModel.Security.cs` — security-sensitive onboarding helpers, passphrase/recovery handling, cleanup, and localized security status behavior.
- `src/CipherNest.App/ViewModels/SettingsViewModel.cs` — settings orchestration and persisted non-secret preference state.
- `src/CipherNest.App/ViewModels/SettingsViewModel.Accessibility.cs` — accessibility/theme/language-related settings behavior.
- `src/CipherNest.App/ViewModels/SettingsViewModel.Localization.cs` — localized settings/status helpers and culture-sensitive presentation.
- `src/CipherNest.App/ViewModels/SettingsViewModel.Navigation.cs` — settings navigation to transfer, security, legal, generator-default, and related screens.
- `src/CipherNest.App/ViewModels/SettingsViewModel.Security.cs` — master-passphrase rotation, biometric enable/disable, backup/restore, destructive vault deletion, security confirmations, and related authorization-sensitive operations.
- `src/CipherNest.App/ViewModels/TransferViewModel.cs` — generic CSV import and guarded plaintext CSV export orchestration, reviewed localized fixed/runtime safety text, exact `EXPORT PLAINTEXT` acknowledgement handling, current-master confirmation, privacy-safe result publication, temporary-share cleanup, and localized cache-removal status.
- `src/CipherNest.App/ViewModels/TransferViewModel.Security.cs` — current-master/plaintext-export authorization, confirmation, and sensitive transfer state handling.
- `src/CipherNest.App/ViewModels/TrashViewModel.cs` — trash listing, restore, retention cleanup, permanent deletion, empty-trash authorization, reviewed localized destructive-action text, and success-state publication without immediately overwriting the completed empty-trash message.
- `src/CipherNest.App/ViewModels/UnlockViewModel.cs` — master/recovery/secondary unlock orchestration and startup unlock state.
- `src/CipherNest.App/ViewModels/UnlockViewModel.Security.cs` — unlock security decisions, localized failures, rate limiting, capability checks, and sensitive credential cleanup.
- `src/CipherNest.App/ViewModels/VaultViewModel.cs` — unlocked vault list/search/filter/sort/load-more/recent-access behavior and navigation to item workflows.

## Views

Each `.xaml` file defines the visual/semantic surface; its `.xaml.cs` file owns narrow lifecycle/event glue. Security/business logic belongs in services/ViewModels rather than being duplicated in code-behind.

- `src/CipherNest.App/Views/AboutPage.xaml` — product identity, creator/support, legal/privacy/security entry points, and funding surface where enabled.
- `src/CipherNest.App/Views/AboutPage.xaml.cs` — About lifecycle/event glue.
- `src/CipherNest.App/Views/AuditPage.xaml` — local audit results and audit controls.
- `src/CipherNest.App/Views/AuditPage.xaml.cs` — Audit page glue.
- `src/CipherNest.App/Views/DeveloperPage.xaml` — developer/build information UI.
- `src/CipherNest.App/Views/DeveloperPage.xaml.cs` — Developer page glue.
- `src/CipherNest.App/Views/GeneratorDefaultsPage.xaml` — generator-default settings UI.
- `src/CipherNest.App/Views/GeneratorDefaultsPage.xaml.cs` — defaults page glue.
- `src/CipherNest.App/Views/GeneratorPage.xaml` — password/passphrase generator UI.
- `src/CipherNest.App/Views/GeneratorPage.xaml.cs` — generator lifecycle cleanup/glue.
- `src/CipherNest.App/Views/ItemEditorPage.xaml` — item editing, attachments, secret-copy actions, TOTP controls, setup-URI workflow, validation/status surfaces, and semantic metadata.
- `src/CipherNest.App/Views/ItemEditorPage.xaml.cs` — item-editor lifecycle cleanup and page glue.
- `src/CipherNest.App/Views/OnboardingPage.xaml` — vault creation, master/recovery explanations, warnings, and onboarding controls.
- `src/CipherNest.App/Views/OnboardingPage.xaml.cs` — onboarding lifecycle/glue.
- `src/CipherNest.App/Views/SecurityInfoPage.xaml` — security/privacy limitations and user education.
- `src/CipherNest.App/Views/SecurityInfoPage.xaml.cs` — security-info page glue.
- `src/CipherNest.App/Views/SettingsPage.xaml` — appearance, language, accessibility, privacy timers, reminders, storage, backup/security, optional funding, and About/legal navigation.
- `src/CipherNest.App/Views/SettingsPage.xaml.cs` — settings lifecycle/glue.
- `src/CipherNest.App/Views/StartupPage.xaml` — startup routing/initialization surface.
- `src/CipherNest.App/Views/StartupPage.xaml.cs` — startup navigation glue.
- `src/CipherNest.App/Views/TransferPage.xaml` — generic CSV import mapping plus guarded plaintext-export UI with reviewed resource-backed fixed text, translated semantic descriptions, current-master input, exact acknowledgement phrase guidance, and explicit sensitive-data warnings.
- `src/CipherNest.App/Views/TransferPage.xaml.cs` — transfer lifecycle/glue.
- `src/CipherNest.App/Views/TrashPage.xaml` — trash/restore/permanent-delete UI with reviewed resource-backed fixed text and semantic labels for the destructive-action surface.
- `src/CipherNest.App/Views/TrashPage.xaml.cs` — Trash page glue.
- `src/CipherNest.App/Views/UnlockPage.xaml` — master/recovery/biometric convenience unlock UI and security warnings.
- `src/CipherNest.App/Views/UnlockPage.xaml.cs` — unlock lifecycle cleanup/glue.
- `src/CipherNest.App/Views/VaultPage.xaml` — main vault list/search/filter/sort/load-more UI.
- `src/CipherNest.App/Views/VaultPage.xaml.cs` — vault page lifecycle/navigation glue.

## Platform entry points and manifests

- `src/CipherNest.App/Platforms/Android/AndroidManifest.xml` — Android manifest and application declarations.
- `src/CipherNest.App/Platforms/Android/MainActivity.cs` — Android MAUI activity, lifecycle hooks, and target-specific window/privacy integration points.
- `src/CipherNest.App/Platforms/Android/MainApplication.cs` — Android application bootstrap.
- `src/CipherNest.App/Platforms/iOS/AppDelegate.cs` — iOS MAUI app delegate.
- `src/CipherNest.App/Platforms/iOS/Info.plist` — iOS bundle/platform metadata and declarations.
- `src/CipherNest.App/Platforms/iOS/Program.cs` — iOS process entry point.
- `src/CipherNest.App/Platforms/MacCatalyst/AppDelegate.cs` — Mac Catalyst MAUI app delegate.
- `src/CipherNest.App/Platforms/MacCatalyst/Info.plist` — Mac Catalyst bundle/platform metadata.
- `src/CipherNest.App/Platforms/MacCatalyst/Program.cs` — Mac Catalyst process entry point.
- `src/CipherNest.App/Platforms/Windows/App.xaml` — WinUI host resources.
- `src/CipherNest.App/Platforms/Windows/App.xaml.cs` — WinUI host bootstrap.
- `src/CipherNest.App/Platforms/Windows/Package.appxmanifest` — Windows package identity, assets, and target declarations.

## Resources and assets

- `src/CipherNest.App/Resources/AppIcon/appicon.svg` — base application icon vector.
- `src/CipherNest.App/Resources/AppIcon/appiconfg.svg` — icon foreground layer.
- `src/CipherNest.App/Resources/AppIcon/appicon-mono.svg` — monochrome/adaptive icon asset.
- `src/CipherNest.App/Resources/Images/ciphernest_logo.svg` — primary project/application logo.
- `src/CipherNest.App/Resources/Images/ciphernest_logo_dark.svg` — dark-surface logo variant.
- `src/CipherNest.App/Resources/Images/bmc_support.svg` — Buy Me a Coffee support artwork; funding remains optional and feature-neutral.
- `src/CipherNest.App/Resources/Localization/AppStrings.resx` — neutral English primary localization catalog for shared and previously migrated UI surfaces.
- `src/CipherNest.App/Resources/Localization/AppStrings.hi-IN.resx` — reviewed Hindi primary catalog; primary-catalog key parity is regression-tested, but complete translation of every application literal is not claimed.
- `src/CipherNest.App/Resources/Localization/TrashStrings.resx` — neutral English feature catalog for Trash listing/permanent-deletion fixed text, confirmations, status formats, and safety wording.
- `src/CipherNest.App/Resources/Localization/TrashStrings.hi-IN.resx` — reviewed Hindi Trash feature catalog with exact key parity, distinct-value, placeholder, and security-meaning regression coverage.
- `src/CipherNest.App/Resources/Localization/TransferStrings.resx` — neutral English feature catalog for generic CSV mapping and the guarded plaintext import/export boundary, including confirmation/status formats and accessibility descriptions.
- `src/CipherNest.App/Resources/Localization/TransferStrings.hi-IN.resx` — reviewed Hindi Transfer feature catalog with exact key parity, exact `EXPORT PLAINTEXT` token preservation, dynamic-placeholder coverage, and plaintext/security-limit wording.
- `src/CipherNest.App/Resources/Raw/wordlist_notice.txt` — notice accompanying the bundled local passphrase word-list resource path.
- `src/CipherNest.App/Resources/Splash/splash.svg` — application splash vector.
- `src/CipherNest.App/Resources/Strings/AppResources.resx` — MAUI/general resource string container retained by the application resource structure.
- `src/CipherNest.App/Resources/Styles/Colors.xaml` — centralized application color resources.
- `src/CipherNest.App/Resources/Styles/Styles.xaml` — shared control/page typography/layout styles and accessibility-oriented style resources.

# 2. CipherNest.Application

## Project

- `src/CipherNest.Application/CipherNest.Application.csproj` — application-layer project and dependency declarations.

## Abstractions

- `src/CipherNest.Application/Abstractions/IBackupService.cs` — encrypted backup create/restore contract.
- `src/CipherNest.Application/Abstractions/IClock.cs` — injectable time source for deterministic policies/TOTP/testing.
- `src/CipherNest.Application/Abstractions/ICryptoService.cs` — authenticated encryption, KDF/wrapping, and cryptographic helper contract.
- `src/CipherNest.Application/Abstractions/IPasswordGenerator.cs` — password/passphrase generation contract.
- `src/CipherNest.Application/Abstractions/IPlaintextTransferService.cs` — explicit plaintext CSV interoperability contract.
- `src/CipherNest.Application/Abstractions/ISafeNoteMarkupService.cs` — bounded safe-note preview/rendering contract.
- `src/CipherNest.Application/Abstractions/ISecurityAuditService.cs` — local decrypted-vault audit contract.
- `src/CipherNest.Application/Abstractions/ISettingsStore.cs` — persisted non-secret settings contract.
- `src/CipherNest.Application/Abstractions/ITotpService.cs` — TOTP code generation/normalization contract.
- `src/CipherNest.Application/Abstractions/ITotpUriCodec.cs` — bounded TOTP-only `otpauth://totp/...` parse/format contract.
- `src/CipherNest.Application/Abstractions/IVaultService.cs` — high-level vault lifecycle, CRUD, authorization, session, search, trash, and key-lease contract.
- `src/CipherNest.Application/Abstractions/IVaultStore.cs` — encrypted persistence/storage contract below `VaultService`.

## Exceptions

- `src/CipherNest.Application/Exceptions/VaultAuthenticationException.cs` — explicit authentication failure boundary.
- `src/CipherNest.Application/Exceptions/VaultLockedException.cs` — operation attempted without a valid unlocked session/key lease.

## Models

- `src/CipherNest.Application/Models/PasswordStrengthResult.cs` — generator/secret-strength guidance result.
- `src/CipherNest.Application/Models/SafeNotePreview.cs` — safe-note preview model.
- `src/CipherNest.Application/Models/TotpCodeResult.cs` — generated TOTP code plus timing metadata.
- `src/CipherNest.Application/Models/TotpUriProfile.cs` — validated bounded TOTP setup-URI profile used by parser/formatter workflows.

## Pure policies/services

- `src/CipherNest.Application/Services/AppPreferencesPolicy.cs` — normalization/default bounds for persisted non-secret preferences.
- `src/CipherNest.Application/Services/ClipboardSafetyPolicy.cs` — clipboard delay/bounds policy independent of platform APIs.
- `src/CipherNest.Application/Services/SafeNoteMarkupService.cs` — bounded Markdown-like safe-note transformation with HTML neutralization.
- `src/CipherNest.Application/Services/SessionLockPolicy.cs` — inactivity/background/session timing decisions.
- `src/CipherNest.Application/Services/TrashRetentionPolicy.cs` — trash expiry policy.
- `src/CipherNest.Application/Services/UnlockBackoffPolicy.cs` — bounded exponential-style retry delay calculation without destructive wipe.

## Validation

- `src/CipherNest.Application/Validation/AttachmentImportPolicy.cs` — attachment count/size/name/type metadata validation at import boundaries.
- `src/CipherNest.Application/Validation/SafeNoteLimits.cs` — shared note character/line ceilings.
- `src/CipherNest.Application/Validation/TotpPolicy.cs` — TOTP seed/algorithm/digits/period/setup-URI input bounds and normalization rules.
- `src/CipherNest.Application/Validation/VaultItemValidator.cs` — central decrypted-item semantic/resource validation before persistence/use.

# 3. CipherNest.Domain

- `src/CipherNest.Domain/CipherNest.Domain.csproj` — framework-independent domain project.
- `src/CipherNest.Domain/Models/AppLanguagePreference.cs` — System/English/Hindi language preference enum/model.
- `src/CipherNest.Domain/Models/AppPreferences.cs` — persisted non-secret application preferences.
- `src/CipherNest.Domain/Models/AttachmentReference.cs` — encrypted-record attachment reference metadata.
- `src/CipherNest.Domain/Models/CustomField.cs` — user-defined item field model, including secret/non-secret semantics.
- `src/CipherNest.Domain/Models/GeneratorOptions.cs` — password/passphrase generation options.
- `src/CipherNest.Domain/Models/SecurityAuditFinding.cs` — local audit finding representation.
- `src/CipherNest.Domain/Models/TotpAlgorithm.cs` — allowed TOTP HMAC algorithms.
- `src/CipherNest.Domain/Models/VaultItem.cs` — canonical decrypted vault-item aggregate before authenticated serialization/encryption.
- `src/CipherNest.Domain/Models/VaultItemType.cs` — persisted item-type enum; numeric compatibility is migration-sensitive and tested.

# 4. CipherNest.Infrastructure

## Project and crypto

- `src/CipherNest.Infrastructure/CipherNest.Infrastructure.csproj` — infrastructure implementation project and package dependencies.
- `src/CipherNest.Infrastructure/Crypto/CryptoService.cs` — Argon2id/AES-256-GCM cryptographic implementation, key generation/wrapping, authenticated envelopes, version/bounds handling, and fixed-time authentication-sensitive comparisons where applicable. See `security/CRYPTOGRAPHIC_DESIGN.md`.

## Persistence

- `src/CipherNest.Infrastructure/Persistence/DatabaseMigrator.cs` — schema creation/migrations, compatibility checks, staged replacement validation, integrity/recovery handling, and database lifecycle helpers.
- `src/CipherNest.Infrastructure/Persistence/SqliteVaultStore.cs` — SQLite implementation of encrypted vault persistence, snapshots, bounded reads, header/item storage, and database replacement operations.

## Infrastructure services

- `src/CipherNest.Infrastructure/Services/AttachmentFormatPolicy.cs` — encrypted attachment framing constants and bounded format validation.
- `src/CipherNest.Infrastructure/Services/AttachmentStorageNamePolicy.cs` — canonical opaque GUID-N `.cna` storage-name validation/generation.
- `src/CipherNest.Infrastructure/Services/BackupArchivePolicy.cs` — bounded ZIP/archive entry/path/count/aggregate validation.
- `src/CipherNest.Infrastructure/Services/BackupFormatPolicy.cs` — `.cnbak` container framing/version/chunk policy.
- `src/CipherNest.Infrastructure/Services/BackupHeaderJsonPolicy.cs` — strict bounded backup-header JSON parser/serializer policy, including duplicate/unknown/type/bounds checks before expensive KDF work.
- `src/CipherNest.Infrastructure/Services/BackupPathPolicy.cs` — archive path normalization and rejection rules.
- `src/CipherNest.Infrastructure/Services/BackupStagingPolicy.cs` — collision-safe staging and publication path policy.
- `src/CipherNest.Infrastructure/Services/CsvTransferService.cs` — bounded generic CSV parsing/import/export with explicit mapping and guarded plaintext behavior.
- `src/CipherNest.Infrastructure/Services/EncryptedAttachmentStore.cs` — streaming chunked authenticated attachment encryption/decryption, collision-safe storage/staging, validation, and cleanup.
- `src/CipherNest.Infrastructure/Services/EncryptedBackupService.cs` — authenticated encrypted backup creation/restore, bounded archive processing, staged database replacement, rollback, and biometric-pairing reset integration boundaries.
- `src/CipherNest.Infrastructure/Services/JsonSettingsStore.cs` — bounded non-secret JSON settings persistence, normalization/fallback, and cancellation behavior.
- `src/CipherNest.Infrastructure/Services/PassphraseWordList.cs` — validates/loads the local passphrase word list used by the generator.
- `src/CipherNest.Infrastructure/Services/PasswordGenerator.cs` — cryptographically secure password/passphrase generation implementation.
- `src/CipherNest.Infrastructure/Services/SecurityAuditService.cs` — local decrypted-item audit implementation for weakness/reuse/duplicate/title/review checks.
- `src/CipherNest.Infrastructure/Services/SystemClock.cs` — production UTC time source.
- `src/CipherNest.Infrastructure/Services/TotpService.cs` — RFC-compatible local TOTP generation and Base32 processing with bounded settings.
- `src/CipherNest.Infrastructure/Services/TotpUriCodec.cs` — strict bounded TOTP-only setup-URI parser/formatter; rejects HOTP/counter, duplicates, malformed/oversized input, issuer disagreement, and unsupported settings.
- `src/CipherNest.Infrastructure/Services/VaultHeaderJsonPolicy.cs` — strict bounded/versioned vault-header JSON parser/serializer and compatibility validation before unwrap/replacement.
- `src/CipherNest.Infrastructure/Services/VaultKeyLease.cs` — private disposable vault-key copy tied to current session/caller cancellation; zeroes owned key bytes on disposal where practical.
- `src/CipherNest.Infrastructure/Services/VaultService.cs` — central security-sensitive vault lifecycle: create/unlock/recovery/secondary unlock/lock, serialized session transitions, key leases, authorization, CRUD/search/trash, passphrase rotation, full-vault deletion, and cancellation behavior.

# 5. CipherNest.Shared

- `src/CipherNest.Shared/CipherNest.Shared.csproj` — small shared project used across layers.
- `src/CipherNest.Shared/AppConstants.cs` — application/product/version/public contact/link constants and other small shared constants; public project metadata should remain centralized here instead of being duplicated across ViewModels.
- `src/CipherNest.Shared/VaultStorageLimits.cs` — shared hard resource ceilings for vault/database/item/attachment/search-related storage safety; changes require synchronized boundary tests and documentation.

# 6. Source-change synchronization checklist

When a production file is added, removed, renamed, or changes responsibility:

1. update this file;
2. update `REPOSITORY_FILE_REFERENCE.md` when the repository-level map changes;
3. update the relevant specialized architecture/security/format/UI/API document;
4. update `TEST_SUITE_REFERENCE.md` when coverage changes;
5. add or adjust focused automated tests;
6. update `CHANGELOG.md`, `PROJECT_STATUS.md`, and `what_changed.md` when release/current-state semantics change;
7. never copy historical CI evidence onto a newer source SHA.

For persisted/cryptographic/security-sensitive changes also review `THREAT_MODEL.md`, `CRYPTOGRAPHIC_DESIGN.md`, format references, `LIMITS_AND_DEFAULTS.md`, `TEST_PLAN.md`, and the release checklist.
