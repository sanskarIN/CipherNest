# CipherNest Test Suite Reference

This file is the canonical file-by-file map of the automated test projects in `tests/`. It documents what each test source file protects and how the three suites divide responsibility.

Inventory baseline: `7d046ab5c6dc15eecf06599ed68317aa88d8967` on 2026-08-19. A later test file is not considered fully documented until it is added here.

> A test name describes intended regression coverage; it does not itself prove that a later commit passed. Exact-head pass claims require observable execution evidence for that immutable SHA.

# 1. Shared test configuration

- `tests/Directory.Build.props` — shared analyzer/build/test properties applied below the `tests/` tree.

# 2. Unit tests

Project: `tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj` — deterministic policy, parser, crypto, generator, validation, settings, attachment, audit, and compatibility tests that do not require a live MAUI target.

- `tests/CipherNest.UnitTests/AppPreferencesPolicyTests.cs` — preference normalization, defaults, and allowed bounds.
- `tests/CipherNest.UnitTests/AttachmentFormatPolicyTests.cs` — encrypted attachment format/chunk/framing policy.
- `tests/CipherNest.UnitTests/AttachmentImportPolicyTests.cs` — import count/size/name/metadata limits.
- `tests/CipherNest.UnitTests/AttachmentMetadataAdversarialTests.cs` — malformed Unicode/control/format and hostile attachment metadata rejection.
- `tests/CipherNest.UnitTests/AttachmentStorageNamePolicyTests.cs` — canonical GUID-based `.cna` storage-name generation/validation.
- `tests/CipherNest.UnitTests/BackupArchivePolicyTests.cs` — backup ZIP path/count/aggregate safety rules.
- `tests/CipherNest.UnitTests/BackupFormatChunkPolicyTests.cs` — encrypted backup chunk-size/framing policy.
- `tests/CipherNest.UnitTests/BackupFormatPolicyTests.cs` — backup container version/framing/bounds rules.
- `tests/CipherNest.UnitTests/BackupHeaderJsonPolicyTests.cs` — strict backup-header JSON shape/type/version/bounds behavior.
- `tests/CipherNest.UnitTests/BackupPathPolicyTests.cs` — canonical archive path normalization and unsafe-path rejection.
- `tests/CipherNest.UnitTests/BackupStagingPolicyTests.cs` — safe staging/publication naming and collision handling.
- `tests/CipherNest.UnitTests/ClipboardSafetyPolicyTests.cs` — clipboard timeout normalization and policy behavior.
- `tests/CipherNest.UnitTests/CryptoKnownAnswerTests.cs` — cryptographic known-answer/compatibility vectors.
- `tests/CipherNest.UnitTests/CryptoMalformedEnvelopeTests.cs` — malformed authenticated-envelope rejection paths.
- `tests/CipherNest.UnitTests/CryptoPassphraseBoundsTests.cs` — passphrase length/resource ceilings before expensive crypto work.
- `tests/CipherNest.UnitTests/CryptoServiceTests.cs` — encryption/decryption, wrapping/authentication, tamper failure, and core crypto behavior.
- `tests/CipherNest.UnitTests/EncryptedAttachmentStoreBoundsTests.cs` — attachment-store hard size/resource bounds.
- `tests/CipherNest.UnitTests/JsonSettingsAdversarialTests.cs` — malformed/duplicate/hostile non-secret settings JSON behavior.
- `tests/CipherNest.UnitTests/JsonSettingsStoreBoundsTests.cs` — actual-read size sentinel/depth/resource ceilings for settings.
- `tests/CipherNest.UnitTests/JsonSettingsStoreTests.cs` — settings save/load/default/fallback/cancellation behavior.
- `tests/CipherNest.UnitTests/KdfResourceBoundsTests.cs` — Argon2/KDF parameter safety ceilings and rejection.
- `tests/CipherNest.UnitTests/PassphraseGeneratorTests.cs` — local-word-list passphrase generation options and constraints.
- `tests/CipherNest.UnitTests/PasswordGeneratorTests.cs` — password generation character sets, length, and randomness-facing invariants.
- `tests/CipherNest.UnitTests/SafeNoteMarkupServiceTests.cs` — bounded Markdown-like safe-note rendering and HTML neutralization.
- `tests/CipherNest.UnitTests/SecurityAuditServiceTests.cs` — weak/reused/duplicate/missing-title/review-due audit findings.
- `tests/CipherNest.UnitTests/SessionLockPolicyTests.cs` — inactivity/background/session lock decision rules.
- `tests/CipherNest.UnitTests/TotpAuditTests.cs` — TOTP item treatment in local security-audit logic.
- `tests/CipherNest.UnitTests/TotpBase32AdversarialTests.cs` — malformed/hostile Base32 seed handling.
- `tests/CipherNest.UnitTests/TotpServiceTests.cs` — RFC-compatible code generation, algorithms, digits, periods, and timing.
- `tests/CipherNest.UnitTests/TotpUriCodecTests.cs` — bounded `otpauth://totp/...` parse/format, duplicate-key, issuer, encoding, HOTP/counter rejection, and limits.
- `tests/CipherNest.UnitTests/TotpValidationTests.cs` — TOTP seed/settings/domain validation.
- `tests/CipherNest.UnitTests/TrashRetentionPolicyTests.cs` — retention normalization and expiry calculation.
- `tests/CipherNest.UnitTests/UnlockBackoffPolicyTests.cs` — repeated-failure delay bounds without destructive wipe behavior.
- `tests/CipherNest.UnitTests/VaultHeaderJsonPolicyTests.cs` — strict versioned vault-header JSON parser/serializer behavior.
- `tests/CipherNest.UnitTests/VaultItemTypeCompatibilityTests.cs` — persisted item-type numeric compatibility.
- `tests/CipherNest.UnitTests/VaultItemValidatorTests.cs` — decrypted vault-item semantic and aggregate resource validation.

# 3. Integration tests

Project: `tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj` — real Infrastructure/Application interaction around SQLite, vault sessions, encrypted records, attachments, backups, replacement/recovery, CSV, and TOTP.

- `tests/CipherNest.IntegrationTests/AttachmentStreamingIntegrationTests.cs` — multi-chunk streaming encrypt/decrypt behavior.
- `tests/CipherNest.IntegrationTests/AttachmentTamperIntegrationTests.cs` — authenticated attachment tamper detection.
- `tests/CipherNest.IntegrationTests/BackupContainerBoundsIntegrationTests.cs` — backup container/archive hard ceilings in end-to-end processing.
- `tests/CipherNest.IntegrationTests/BackupCorruptionIntegrationTests.cs` — truncated/corrupt/wrong-auth backup failure behavior.
- `tests/CipherNest.IntegrationTests/BackupHeaderAdversarialIntegrationTests.cs` — hostile backup-header variants through the real restore path.
- `tests/CipherNest.IntegrationTests/BackupHeaderValidationIntegrationTests.cs` — backup-header validation before unsafe/expensive restore work.
- `tests/CipherNest.IntegrationTests/BackupRollbackCancellationIntegrationTests.cs` — cancellation around active replacement and uncancelled rollback safety.
- `tests/CipherNest.IntegrationTests/CsvColumnLimitIntegrationTests.cs` — excessive-column rejection including end-of-row/end-of-file edges.
- `tests/CipherNest.IntegrationTests/CsvParserRobustnessTests.cs` — quoting, newlines, Unicode, malformed CSV, and parser robustness.
- `tests/CipherNest.IntegrationTests/CsvTransferTests.cs` — explicit mapping import and guarded plaintext export behavior.
- `tests/CipherNest.IntegrationTests/DatabaseMigrationTests.cs` — schema creation/migration compatibility and required database shape.
- `tests/CipherNest.IntegrationTests/DatabaseReplacementRecoveryIntegrationTests.cs` — staged replacement, failure recovery, and active-vault preservation.
- `tests/CipherNest.IntegrationTests/DecryptedRecordValidationIntegrationTests.cs` — rejection of decrypted records that violate semantic/resource rules.
- `tests/CipherNest.IntegrationTests/MasterPassphraseRotationIntegrationTests.cs` — master-wrapper rotation and old-master invalidation.
- `tests/CipherNest.IntegrationTests/RecentAccessIntegrationTests.cs` — encrypted recent-access timestamp update/persistence behavior.
- `tests/CipherNest.IntegrationTests/ReplacementVaultHeaderValidationIntegrationTests.cs` — candidate vault-header validation before database replacement.
- `tests/CipherNest.IntegrationTests/SecondaryUnlockIntegrationTests.cs` — independent secondary wrapper/convenience-unlock service semantics.
- `tests/CipherNest.IntegrationTests/TotpVaultIntegrationTests.cs` — TOTP item persistence, reopening, code generation/settings integration.
- `tests/CipherNest.IntegrationTests/VaultHeaderAdversarialIntegrationTests.cs` — hostile header cases through real vault operations.
- `tests/CipherNest.IntegrationTests/VaultHeaderCompatibilityIntegrationTests.cs` — supported header-version/schema compatibility behavior.
- `tests/CipherNest.IntegrationTests/VaultHeaderStrictValidationIntegrationTests.cs` — duplicate/unknown/wrong-type/non-canonical strict header rejection.
- `tests/CipherNest.IntegrationTests/VaultIntegrationTests.cs` — primary create/unlock/save/read/search/lock vault lifecycle integration.
- `tests/CipherNest.IntegrationTests/VaultLockCancellationIntegrationTests.cs` — lock-linked cancellation of work/key leases.
- `tests/CipherNest.IntegrationTests/VaultSearchBoundsIntegrationTests.cs` — bounded search query/record behavior under real persistence.
- `tests/CipherNest.IntegrationTests/VaultSecurityLifecycleTests.cs` — recovery/master authorization and destructive/security lifecycle invariants.
- `tests/CipherNest.IntegrationTests/VaultSessionTransitionIntegrationTests.cs` — serialized create/unlock/lock/delete transition behavior.
- `tests/CipherNest.IntegrationTests/VaultStorageBoundsIntegrationTests.cs` — database item/count/aggregate size ceilings.
- `tests/CipherNest.IntegrationTests/VaultStorePathIntegrationTests.cs` — vault database/WAL/SHM/recovery path and storage-location behavior.

# 4. UI/source-contract tests

Project: `tests/CipherNest.UiTests/CipherNest.UiTests.csproj` — source-contract, XAML, documentation, lifecycle, privacy, localization, cancellation, and architecture guards that can run without launching a MAUI device target. These complement rather than replace physical-device accessibility/security testing.

- `tests/CipherNest.UiTests/AboutSecurityLocalizationSourceTests.cs` — About/Security privacy wording is resource-backed and reviewed across catalogs.
- `tests/CipherNest.UiTests/AttachmentFramingSourceTests.cs` — attachment framing constants/ordering remain aligned with documented format.
- `tests/CipherNest.UiTests/AttachmentMetadataSafetySourceTests.cs` — source keeps strict attachment metadata validation at relevant boundaries.
- `tests/CipherNest.UiTests/AttachmentMutationSourceTests.cs` — attachment mutations keep authorization/session/save ordering.
- `tests/CipherNest.UiTests/AttachmentStagingSourceTests.cs` — plaintext/encrypted staging stays collision-safe and bounded.
- `tests/CipherNest.UiTests/AttachmentStoreSecuritySourceTests.cs` — encrypted attachment-store security invariants remain wired.
- `tests/CipherNest.UiTests/AuditFailureContainmentSourceTests.cs` — audit failures are contained and do not leak unsafe raw detail.
- `tests/CipherNest.UiTests/AuthenticationLocalizationCatalogSourceTests.cs` — English/Hindi authentication resource key/value coverage.
- `tests/CipherNest.UiTests/AuthenticationLocalizationRoadmapSourceTests.cs` — authentication migration scope/non-claims stay synchronized with localization roadmap docs.
- `tests/CipherNest.UiTests/BackupArchiveSourceTests.cs` — backup archive count/path/size defenses stay present in source.
- `tests/CipherNest.UiTests/BackupChunkFramingSourceTests.cs` — backup chunk framing/authentication source contract.
- `tests/CipherNest.UiTests/BackupExportPublicationCancellationSourceTests.cs` — cancellation cannot publish a partial/unsafe backup as successful output.
- `tests/CipherNest.UiTests/BackupFormatSourceTests.cs` — backup version/header/framing constants stay aligned with documented format.
- `tests/CipherNest.UiTests/BackupRestoreHardeningSourceTests.cs` — restore staging, validation, replacement, rollback, and cleanup defenses remain composed.
- `tests/CipherNest.UiTests/BiometricEnableRollbackSourceTests.cs` — failed biometric setup does not leave a falsely enabled/persisted convenience path.
- `tests/CipherNest.UiTests/BmcSupportSourceTests.cs` — funding link/asset/build flag and equal-treatment wording remain intentional.
- `tests/CipherNest.UiTests/ClipboardSecuritySourceTests.cs` — secret copy uses fingerprint/timed conditional cleanup rather than retaining plaintext for delayed comparison.
- `tests/CipherNest.UiTests/CsvRowSafetySourceTests.cs` — CSV row/resource ceilings remain enforced while parsing.
- `tests/CipherNest.UiTests/CsvSafetySourceTests.cs` — CSV plaintext warnings/auth/mapping and parser safety wiring.
- `tests/CipherNest.UiTests/DatabaseDeletionSourceTests.cs` — database/WAL/SHM/recovery deletion/cleanup paths stay represented.
- `tests/CipherNest.UiTests/DatabaseRecoverySourceTests.cs` — stale recovery/replacement recovery ordering remains source-protected.
- `tests/CipherNest.UiTests/DatabaseReplacementSourceTests.cs` — candidate validation occurs before active database mutation.
- `tests/CipherNest.UiTests/DecryptedRecordValidationSourceTests.cs` — decrypted item validation remains present before publication/use.
- `tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs` — canonical documentation entry points, security disclaimers, feature wording, and documentation presence guards.
- `tests/CipherNest.UiTests/GeneratorFailureContainmentSourceTests.cs` — generator errors use privacy-safe status handling.
- `tests/CipherNest.UiTests/GeneratorMemorySourceTests.cs` — generated sensitive values are cleared where the ViewModel/page owns them.
- `tests/CipherNest.UiTests/ItemEditorClipboardFailureSourceTests.cs` — clipboard failures are contained and safely surfaced.
- `tests/CipherNest.UiTests/ItemEditorSaveFailureContainmentSourceTests.cs` — save failures do not publish false success or raw sensitive exceptions.
- `tests/CipherNest.UiTests/LifecycleFailClosedSourceTests.cs` — app lifecycle security handling attempts fail-closed behavior when secondary cleanup fails.
- `tests/CipherNest.UiTests/LifecycleTransitionSerializationSourceTests.cs` — lifecycle security transitions stay serialized with vault session transitions.
- `tests/CipherNest.UiTests/LocalizationRoadmapSourceTests.cs` — localization documentation and implemented/deferred wording stay honest.
- `tests/CipherNest.UiTests/LocalizationSourceTests.cs` — neutral/Hindi resource catalogs, key parity, preference wiring, and fallback contract.
- `tests/CipherNest.UiTests/MasterAuthorizationSessionSourceTests.cs` — master-only sensitive actions remain bound to a live authorized session.
- `tests/CipherNest.UiTests/MauiApiSourceTests.cs` — MAUI/platform API usage stays compatible with intended target/source contracts.
- `tests/CipherNest.UiTests/OnboardingFailureContainmentSourceTests.cs` — onboarding failures are contained and sensitive state is not carelessly retained.
- `tests/CipherNest.UiTests/OnboardingLocalizationSourceTests.cs` — onboarding/recovery security UI uses reviewed resource-backed text.
- `tests/CipherNest.UiTests/OnboardingPassphraseBoundsSourceTests.cs` — onboarding applies passphrase limits consistently before expensive/security-sensitive work.
- `tests/CipherNest.UiTests/RepositoryUiStructureTests.cs` — expected pages, ViewModels, routes/resources, and UI project structure remain present.
- `tests/CipherNest.UiTests/RestoreCompletionStateSourceTests.cs` — restore success/failure state is published only after required completion/reset work.
- `tests/CipherNest.UiTests/SensitiveCredentialLifetimeSourceTests.cs` — credential/recovery/master inputs are cleared at owned lifecycle boundaries.
- `tests/CipherNest.UiTests/SensitiveErrorSurfaceSourceTests.cs` — user-facing sensitive failures avoid raw exception/path/vault detail.
- `tests/CipherNest.UiTests/SettingsJsonSafetySourceTests.cs` — Settings UI/store handling preserves bounded non-secret JSON fallback semantics.
- `tests/CipherNest.UiTests/SettingsPublicationCancellationSourceTests.cs` — cancelled settings operations do not publish misleading success state.
- `tests/CipherNest.UiTests/SettingsSecurityLocalizationSourceTests.cs` — security-decision Settings surface uses reviewed localized resources.
- `tests/CipherNest.UiTests/SettingsSecurityOperationLocalizationSourceTests.cs` — sensitive Settings operation success/failure text is resource-backed.
- `tests/CipherNest.UiTests/SettingsSurfaceLocalizationSourceTests.cs` — remaining fixed Settings surface is resource-backed in neutral/Hindi catalogs.
- `tests/CipherNest.UiTests/StartupPreferenceFallbackSourceTests.cs` — startup preference failures fall back safely instead of preventing secure startup.
- `tests/CipherNest.UiTests/StorageMaintenanceSourceTests.cs` — cache cleanup cannot intentionally target protected vault/attachment/backup data.
- `tests/CipherNest.UiTests/TotpDocumentationSourceTests.cs` — public/security/release docs describe current TOTP text-URI boundary accurately.
- `tests/CipherNest.UiTests/TotpLocalizationCatalogSourceTests.cs` — TOTP neutral/Hindi resource keys, reviewed values, format placeholders, and protocol skeleton parity.
- `tests/CipherNest.UiTests/TotpLocalizationUiSourceTests.cs` — TOTP XAML uses localized fixed/dynamic text and security warnings.
- `tests/CipherNest.UiTests/TotpLocalizedStatusSourceTests.cs` — TOTP dynamic/status/error messages resolve through localization resources.
- `tests/CipherNest.UiTests/TotpSafetySourceTests.cs` — TOTP seed/code handling, clipboard and persistence source invariants.
- `tests/CipherNest.UiTests/TotpUiSourceTests.cs` — TOTP item-editor controls/bindings/workflow structure.
- `tests/CipherNest.UiTests/TransferCsvFailureStateSourceTests.cs` — CSV transfer failure/cancellation cannot leave misleading successful UI state.
- `tests/CipherNest.UiTests/TranslationExtensionSourceTests.cs` — reusable XAML translation extension resolves the registered localization service and fails on missing key metadata.
- `tests/CipherNest.UiTests/UnlockCapabilityFailureContainmentSourceTests.cs` — biometric/capability probing failures fall back safely to master authentication.
- `tests/CipherNest.UiTests/UnlockLocalizationSourceTests.cs` — unlock/recovery/biometric security text uses reviewed resources.
- `tests/CipherNest.UiTests/VaultDeletionOrderingSourceTests.cs` — encrypted record deletion precedes best-effort attachment cleanup and authorization ordering remains explicit.
- `tests/CipherNest.UiTests/VaultHeaderSafetySourceTests.cs` — strict vault-header parser/resource/version safeguards remain wired before unwrap/replacement.
- `tests/CipherNest.UiTests/VaultKeyLeaseSourceTests.cs` — callers receive private disposable/cancellable key leases rather than unsafe shared key references.
- `tests/CipherNest.UiTests/VaultSearchFailureContainmentSourceTests.cs` — search failures/cancellation stay contained without stale unsafe publication.
- `tests/CipherNest.UiTests/VaultSessionTransitionSourceTests.cs` — source retains serialized security transition gate and session cancellation semantics.
- `tests/CipherNest.UiTests/VaultStorageBoundsSourceTests.cs` — hard storage ceilings remain referenced/enforced in persistence paths.
- `tests/CipherNest.UiTests/ViewModelAotSourceTests.cs` — generated ViewModel/property patterns remain compatible with intended trimming/AOT/source-generation constraints.
- `tests/CipherNest.UiTests/XamlStructureTests.cs` — XAML pages retain expected roots, bindings, semantic structure, and core UI declarations.

# 5. What these suites do not replace

Automated tests do not replace representative physical-device evidence for:

- Android/iOS/Mac Catalyst biometrics and secure storage;
- background/suspend/resume lifecycle behavior;
- OS clipboard history and cross-device synchronization;
- screenshot/task-preview behavior;
- TalkBack, VoiceOver, Narrator, keyboard-only navigation, large text, and translated layouts;
- file picker/share-sheet behavior;
- signing, notarization, packaging, store acceptance, or current store policy;
- independent professional cryptographic/security review;
- representative third-party authenticator TOTP interoperability.

See `TESTING_GUIDE.md`, `TEST_PLAN.md`, `verification/CI_GATES.md`, and `RELEASE_CHECKLIST.md`.

# 6. Test maintenance contract

For every new or renamed test source file:

1. add it here with the behavior it guards;
2. update `REPOSITORY_FILE_REFERENCE.md` if repository structure changes;
3. update `DocumentationCoverageSourceTests.cs` when a new canonical documentation artifact/verification contract should be mandatory;
4. update exact test counts only after an observable exact-SHA run;
5. keep historical test-count records immutable rather than rewriting them for newer heads.
