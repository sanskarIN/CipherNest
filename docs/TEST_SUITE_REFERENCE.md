# CipherNest Test Suite Reference

This is the canonical file-by-file map of the automated test projects under `tests/`. It records what each test source protects and how unit, integration, and UI/source-contract coverage divide responsibility.

Inventory baseline: Trash localization regression head `142ce6125a8f893701857680a99d01b78f860feb` on 2026-08-20. New test files must be added here in the same change series.

> A test file documents intended regression coverage. Its presence is not evidence that a later commit passed. Exact-head pass claims require observable execution for that immutable SHA.

# 1. Shared test configuration

- `tests/Directory.Build.props` — common analyzer/compiler/test build properties for the test tree.

# 2. Unit tests

Project: `tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj` — deterministic policy, parser, validation, cryptography, generator, settings, attachment, audit, and compatibility tests without a live MAUI target.

- `tests/CipherNest.UnitTests/AppPreferencesPolicyTests.cs` — preference defaults, normalization, and allowed bounds.
- `tests/CipherNest.UnitTests/AttachmentFormatPolicyTests.cs` — encrypted attachment framing/chunk policy.
- `tests/CipherNest.UnitTests/AttachmentImportPolicyTests.cs` — attachment import count/size/name/metadata limits.
- `tests/CipherNest.UnitTests/AttachmentMetadataAdversarialTests.cs` — malformed Unicode/control/format metadata rejection.
- `tests/CipherNest.UnitTests/AttachmentStorageNamePolicyTests.cs` — canonical GUID-based `.cna` storage names.
- `tests/CipherNest.UnitTests/BackupArchivePolicyTests.cs` — archive path/count/aggregate safety rules.
- `tests/CipherNest.UnitTests/BackupFormatChunkPolicyTests.cs` — backup chunk-size/framing rules.
- `tests/CipherNest.UnitTests/BackupFormatPolicyTests.cs` — backup container version/framing/bounds.
- `tests/CipherNest.UnitTests/BackupHeaderJsonPolicyTests.cs` — strict backup-header JSON shape/type/version/bounds behavior.
- `tests/CipherNest.UnitTests/BackupPathPolicyTests.cs` — archive path normalization and unsafe-path rejection.
- `tests/CipherNest.UnitTests/BackupStagingPolicyTests.cs` — collision-safe staging/publication naming.
- `tests/CipherNest.UnitTests/ClipboardSafetyPolicyTests.cs` — clipboard timeout normalization/policy.
- `tests/CipherNest.UnitTests/CryptoKnownAnswerTests.cs` — cryptographic known-answer/compatibility vectors.
- `tests/CipherNest.UnitTests/CryptoMalformedEnvelopeTests.cs` — malformed authenticated-envelope rejection.
- `tests/CipherNest.UnitTests/CryptoPassphraseBoundsTests.cs` — passphrase/resource ceilings before expensive crypto work.
- `tests/CipherNest.UnitTests/CryptoServiceTests.cs` — encryption/decryption, wrapping/authentication, tamper failure, and core crypto behavior.
- `tests/CipherNest.UnitTests/EncryptedAttachmentStoreBoundsTests.cs` — attachment-store hard size/resource ceilings.
- `tests/CipherNest.UnitTests/JsonSettingsAdversarialTests.cs` — malformed/duplicate/hostile settings JSON behavior.
- `tests/CipherNest.UnitTests/JsonSettingsStoreBoundsTests.cs` — settings actual-read size/depth ceilings.
- `tests/CipherNest.UnitTests/JsonSettingsStoreTests.cs` — settings save/load/default/fallback/cancellation behavior.
- `tests/CipherNest.UnitTests/KdfResourceBoundsTests.cs` — Argon2/KDF parameter safety ceilings.
- `tests/CipherNest.UnitTests/PassphraseGeneratorTests.cs` — local-word-list passphrase generation options/constraints.
- `tests/CipherNest.UnitTests/PasswordGeneratorTests.cs` — password character sets, lengths, and generator invariants.
- `tests/CipherNest.UnitTests/SafeNoteMarkupServiceTests.cs` — bounded safe-note markup and HTML neutralization.
- `tests/CipherNest.UnitTests/SecurityAuditServiceTests.cs` — weak/reused/duplicate/missing-title/review-due findings.
- `tests/CipherNest.UnitTests/SessionLockPolicyTests.cs` — inactivity/background/session lock decisions.
- `tests/CipherNest.UnitTests/TotpAuditTests.cs` — TOTP item treatment in local audit logic.
- `tests/CipherNest.UnitTests/TotpBase32AdversarialTests.cs` — malformed/hostile Base32 handling.
- `tests/CipherNest.UnitTests/TotpServiceTests.cs` — RFC-compatible TOTP generation, algorithms, digits, periods, timing.
- `tests/CipherNest.UnitTests/TotpUriCodecTests.cs` — bounded `otpauth://totp/...` parse/format, duplicate-key/issuer/encoding/HOTP/counter/bounds rules.
- `tests/CipherNest.UnitTests/TotpValidationTests.cs` — TOTP seed/settings/domain validation.
- `tests/CipherNest.UnitTests/TrashRetentionPolicyTests.cs` — trash retention normalization/expiry.
- `tests/CipherNest.UnitTests/UnlockBackoffPolicyTests.cs` — repeated-failure delay bounds without destructive wipe.
- `tests/CipherNest.UnitTests/VaultHeaderJsonPolicyTests.cs` — strict versioned vault-header JSON behavior.
- `tests/CipherNest.UnitTests/VaultItemTypeCompatibilityTests.cs` — persisted item-type numeric compatibility.
- `tests/CipherNest.UnitTests/VaultItemValidatorTests.cs` — decrypted item semantic/aggregate resource validation.

# 3. Integration tests

Project: `tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj` — real Application/Infrastructure interaction around SQLite, sessions, records, attachments, backups, recovery/replacement, CSV, and TOTP.

- `tests/CipherNest.IntegrationTests/AttachmentStreamingIntegrationTests.cs` — multi-chunk streaming attachment encrypt/decrypt.
- `tests/CipherNest.IntegrationTests/AttachmentTamperIntegrationTests.cs` — authenticated attachment tamper detection.
- `tests/CipherNest.IntegrationTests/BackupContainerBoundsIntegrationTests.cs` — backup container/archive ceilings end-to-end.
- `tests/CipherNest.IntegrationTests/BackupCorruptionIntegrationTests.cs` — corrupt/truncated/wrong-auth backup failures.
- `tests/CipherNest.IntegrationTests/BackupHeaderAdversarialIntegrationTests.cs` — hostile backup headers through real restore.
- `tests/CipherNest.IntegrationTests/BackupHeaderValidationIntegrationTests.cs` — header validation before unsafe/expensive restore work.
- `tests/CipherNest.IntegrationTests/BackupRollbackCancellationIntegrationTests.cs` — cancellation around active replacement and uncancelled rollback.
- `tests/CipherNest.IntegrationTests/CsvColumnLimitIntegrationTests.cs` — excessive-column rejection at row/EOF boundaries.
- `tests/CipherNest.IntegrationTests/CsvParserRobustnessTests.cs` — quoting/newlines/Unicode/malformed CSV robustness.
- `tests/CipherNest.IntegrationTests/CsvTransferTests.cs` — explicit mapping import and guarded plaintext export.
- `tests/CipherNest.IntegrationTests/DatabaseMigrationTests.cs` — schema creation/migration compatibility.
- `tests/CipherNest.IntegrationTests/DatabaseReplacementRecoveryIntegrationTests.cs` — staged replacement, failure recovery, active-vault preservation.
- `tests/CipherNest.IntegrationTests/DecryptedRecordValidationIntegrationTests.cs` — invalid decrypted record rejection.
- `tests/CipherNest.IntegrationTests/MasterPassphraseRotationIntegrationTests.cs` — master wrapper rotation/old-master invalidation.
- `tests/CipherNest.IntegrationTests/RecentAccessIntegrationTests.cs` — encrypted recent-access timestamp persistence.
- `tests/CipherNest.IntegrationTests/ReplacementVaultHeaderValidationIntegrationTests.cs` — candidate header validation before replacement.
- `tests/CipherNest.IntegrationTests/SecondaryUnlockIntegrationTests.cs` — secondary wrapper/convenience-unlock semantics.
- `tests/CipherNest.IntegrationTests/TotpVaultIntegrationTests.cs` — TOTP persistence/reopen/generation/settings integration.
- `tests/CipherNest.IntegrationTests/VaultHeaderAdversarialIntegrationTests.cs` — hostile header variants through real operations.
- `tests/CipherNest.IntegrationTests/VaultHeaderCompatibilityIntegrationTests.cs` — supported header version/schema compatibility.
- `tests/CipherNest.IntegrationTests/VaultHeaderStrictValidationIntegrationTests.cs` — duplicate/unknown/wrong-type/non-canonical header rejection.
- `tests/CipherNest.IntegrationTests/VaultIntegrationTests.cs` — primary create/unlock/save/read/search/lock lifecycle.
- `tests/CipherNest.IntegrationTests/VaultLockCancellationIntegrationTests.cs` — lock-linked cancellation of work/key leases.
- `tests/CipherNest.IntegrationTests/VaultSearchBoundsIntegrationTests.cs` — bounded search behavior with real persistence.
- `tests/CipherNest.IntegrationTests/VaultSecurityLifecycleTests.cs` — recovery/master/destructive security lifecycle invariants.
- `tests/CipherNest.IntegrationTests/VaultSessionTransitionIntegrationTests.cs` — serialized create/unlock/lock/delete transitions.
- `tests/CipherNest.IntegrationTests/VaultStorageBoundsIntegrationTests.cs` — database count/aggregate storage ceilings.
- `tests/CipherNest.IntegrationTests/VaultStorePathIntegrationTests.cs` — database/WAL/SHM/recovery storage-path behavior.

# 4. UI/source-contract tests

Project: `tests/CipherNest.UiTests/CipherNest.UiTests.csproj` — XAML/source/documentation/lifecycle/privacy/localization/cancellation/architecture guards runnable without booting a MAUI device target. They complement, not replace, target-device validation.

- `tests/CipherNest.UiTests/AboutSecurityLocalizationSourceTests.cs` — About/Security privacy wording across localization catalogs.
- `tests/CipherNest.UiTests/AttachmentFramingSourceTests.cs` — attachment framing constants/ordering alignment.
- `tests/CipherNest.UiTests/AttachmentMetadataSafetySourceTests.cs` — attachment metadata validation remains wired.
- `tests/CipherNest.UiTests/AttachmentMutationSourceTests.cs` — attachment mutation authorization/session/save ordering.
- `tests/CipherNest.UiTests/AttachmentStagingSourceTests.cs` — collision-safe bounded staging.
- `tests/CipherNest.UiTests/AttachmentStoreSecuritySourceTests.cs` — encrypted attachment-store security invariants.
- `tests/CipherNest.UiTests/AuditFailureContainmentSourceTests.cs` — audit failure containment/redacted errors.
- `tests/CipherNest.UiTests/AuthenticationLocalizationCatalogSourceTests.cs` — authentication English/Hindi key/value coverage.
- `tests/CipherNest.UiTests/AuthenticationLocalizationRoadmapSourceTests.cs` — authentication localization scope/non-claims.
- `tests/CipherNest.UiTests/BackupArchiveSourceTests.cs` — source archive count/path/size defenses.
- `tests/CipherNest.UiTests/BackupChunkFramingSourceTests.cs` — backup chunk framing/authentication contract.
- `tests/CipherNest.UiTests/BackupExportPublicationCancellationSourceTests.cs` — cancelled backup cannot publish false-success output.
- `tests/CipherNest.UiTests/BackupFormatSourceTests.cs` — backup version/header/framing source-document alignment.
- `tests/CipherNest.UiTests/BackupRestoreHardeningSourceTests.cs` — restore staging/validation/replacement/rollback/cleanup composition.
- `tests/CipherNest.UiTests/BiometricEnableRollbackSourceTests.cs` — failed biometric setup cannot leave false enabled state.
- `tests/CipherNest.UiTests/BmcSupportSourceTests.cs` — funding asset/link/build-flag/equal-treatment behavior.
- `tests/CipherNest.UiTests/ClipboardSecuritySourceTests.cs` — fingerprint-based timed conditional cleanup instead of retained delayed plaintext.
- `tests/CipherNest.UiTests/CsvRowSafetySourceTests.cs` — CSV row/resource ceilings while parsing.
- `tests/CipherNest.UiTests/CsvSafetySourceTests.cs` — plaintext warnings/auth/mapping/parser safety.
- `tests/CipherNest.UiTests/DatabaseDeletionSourceTests.cs` — database/WAL/SHM/recovery deletion paths.
- `tests/CipherNest.UiTests/DatabaseRecoverySourceTests.cs` — stale recovery/replacement ordering.
- `tests/CipherNest.UiTests/DatabaseReplacementSourceTests.cs` — candidate validation before active mutation.
- `tests/CipherNest.UiTests/DecryptedRecordValidationSourceTests.cs` — decrypted item validation before publication/use.
- `tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs` — canonical docs, public security disclaimers, feature wording, and required-document presence.
- `tests/CipherNest.UiTests/GeneratorFailureContainmentSourceTests.cs` — privacy-safe generator failure handling.
- `tests/CipherNest.UiTests/GeneratorMemorySourceTests.cs` — generated-sensitive-value cleanup at owned lifecycle boundaries.
- `tests/CipherNest.UiTests/ItemEditorClipboardFailureSourceTests.cs` — clipboard failure containment.
- `tests/CipherNest.UiTests/ItemEditorSaveFailureContainmentSourceTests.cs` — save failure cannot publish false success/raw sensitive errors.
- `tests/CipherNest.UiTests/LifecycleFailClosedSourceTests.cs` — fail-closed lifecycle behavior if secondary cleanup fails.
- `tests/CipherNest.UiTests/LifecycleTransitionSerializationSourceTests.cs` — lifecycle security transitions stay serialized.
- `tests/CipherNest.UiTests/LocalizationRoadmapSourceTests.cs` — localization implementation/deferred wording stays honest.
- `tests/CipherNest.UiTests/LocalizationSourceTests.cs` — neutral/Hindi primary-catalog parity, preference wiring, fallback contract.
- `tests/CipherNest.UiTests/MasterAuthorizationSessionSourceTests.cs` — master-only actions remain tied to live authorization.
- `tests/CipherNest.UiTests/MauiApiSourceTests.cs` — target/source MAUI API contract.
- `tests/CipherNest.UiTests/OnboardingFailureContainmentSourceTests.cs` — onboarding failure containment/sensitive-state cleanup.
- `tests/CipherNest.UiTests/OnboardingLocalizationSourceTests.cs` — onboarding/recovery security localization.
- `tests/CipherNest.UiTests/OnboardingPassphraseBoundsSourceTests.cs` — onboarding passphrase bounds before expensive work.
- `tests/CipherNest.UiTests/RepositoryDocumentationInventorySourceTests.cs` — canonical document/hub presence plus exhaustive `git ls-files` mapping of every tracked path to `REPOSITORY_FILE_REFERENCE.md`, `SOURCE_CODE_REFERENCE.md`, or `TEST_SUITE_REFERENCE.md`; representative layer/suite assertions remain as secondary ownership checks.
- `tests/CipherNest.UiTests/RepositoryUiStructureTests.cs` — expected pages/ViewModels/routes/resources/project structure.
- `tests/CipherNest.UiTests/RestoreCompletionStateSourceTests.cs` — restore completion state only after required completion/reset.
- `tests/CipherNest.UiTests/SensitiveCredentialLifetimeSourceTests.cs` — credential/recovery/master input cleanup.
- `tests/CipherNest.UiTests/SensitiveErrorSurfaceSourceTests.cs` — sensitive errors avoid raw exception/path/vault detail.
- `tests/CipherNest.UiTests/SettingsJsonSafetySourceTests.cs` — bounded settings JSON/fallback behavior.
- `tests/CipherNest.UiTests/SettingsPublicationCancellationSourceTests.cs` — cancelled settings operation cannot publish false success.
- `tests/CipherNest.UiTests/SettingsSecurityLocalizationSourceTests.cs` — security-decision Settings localization.
- `tests/CipherNest.UiTests/SettingsSecurityOperationLocalizationSourceTests.cs` — security operation status localization.
- `tests/CipherNest.UiTests/SettingsSurfaceLocalizationSourceTests.cs` — remaining fixed Settings neutral/Hindi localization.
- `tests/CipherNest.UiTests/StartupPreferenceFallbackSourceTests.cs` — startup preference failures use safe fallback.
- `tests/CipherNest.UiTests/StorageMaintenanceSourceTests.cs` — cache cleanup cannot intentionally target protected vault/attachment/backup data.
- `tests/CipherNest.UiTests/TotpDocumentationSourceTests.cs` — public/security/release TOTP text-URI boundary wording.
- `tests/CipherNest.UiTests/TotpLocalizationCatalogSourceTests.cs` — TOTP resource keys/values/placeholders/protocol skeleton parity.
- `tests/CipherNest.UiTests/TotpLocalizationUiSourceTests.cs` — TOTP XAML fixed/dynamic/security-warning localization.
- `tests/CipherNest.UiTests/TotpLocalizedStatusSourceTests.cs` — TOTP dynamic/status/error localization resources.
- `tests/CipherNest.UiTests/TotpSafetySourceTests.cs` — TOTP seed/code clipboard/persistence safety invariants.
- `tests/CipherNest.UiTests/TotpUiSourceTests.cs` — TOTP editor controls/bindings/workflow structure.
- `tests/CipherNest.UiTests/TransferCsvFailureStateSourceTests.cs` — CSV failure/cancellation cannot leave false success state.
- `tests/CipherNest.UiTests/TranslationExtensionSourceTests.cs` — registered localization-service resolution and missing-key behavior.
- `tests/CipherNest.UiTests/TrashLocalizationSourceTests.cs` — Trash fixed/runtime destructive-action localization, feature-catalog registration/parity, safety caveat preservation, and empty-trash success-state regression.
- `tests/CipherNest.UiTests/UnlockCapabilityFailureContainmentSourceTests.cs` — biometric/capability failures safely fall back to master auth.
- `tests/CipherNest.UiTests/UnlockLocalizationSourceTests.cs` — unlock/recovery/biometric security localization.
- `tests/CipherNest.UiTests/VaultDeletionOrderingSourceTests.cs` — record deletion, attachment cleanup, and authorization ordering.
- `tests/CipherNest.UiTests/VaultHeaderSafetySourceTests.cs` — strict header parser/resource/version safeguards.
- `tests/CipherNest.UiTests/VaultKeyLeaseSourceTests.cs` — private disposable/cancellable key leases rather than shared key references.
- `tests/CipherNest.UiTests/VaultSearchFailureContainmentSourceTests.cs` — search failure/cancellation containment.
- `tests/CipherNest.UiTests/VaultSessionTransitionSourceTests.cs` — serialized security transition/session cancellation source contract.
- `tests/CipherNest.UiTests/VaultStorageBoundsSourceTests.cs` — hard persistence/storage ceilings remain wired.
- `tests/CipherNest.UiTests/ViewModelAotSourceTests.cs` — ViewModel/source-generation trimming/AOT compatibility guards.
- `tests/CipherNest.UiTests/XamlStructureTests.cs` — expected XAML roots/bindings/semantic structure.

# 5. What automated suites do not replace

Automated tests do not replace representative evidence for Android/iOS/Mac Catalyst biometrics/secure storage; suspend/resume/background lifecycle; OS clipboard history/synchronization; screenshot/task-preview behavior; TalkBack/VoiceOver/Narrator/keyboard/large-text/translated layouts; file picker/share sheets; signing/notarization/store acceptance; representative third-party TOTP interoperability; or independent professional security review.

See `TESTING_GUIDE.md`, `TEST_PLAN.md`, `verification/CI_GATES.md`, and `RELEASE_CHECKLIST.md`.

# 6. Test maintenance contract

For every new/removed/renamed/materially repurposed test file:

1. update this reference in the same change series;
2. keep the tracked-file documentation gate passing so every tracked path remains represented by its canonical inventory;
3. update `REPOSITORY_FILE_REFERENCE.md` when repository structure/documentation semantics change;
4. update documentation source guards when a new canonical document/verification record should remain mandatory;
5. update exact test counts only after an observable exact-SHA run;
6. preserve historical pass-count records instead of rewriting them for newer heads.
