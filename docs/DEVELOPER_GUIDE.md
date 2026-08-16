# CipherNest Developer Guide

This guide explains how the current CipherNest source is organized and how to extend it without bypassing its local-first security boundaries. Read it together with [`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md), [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md), [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md), [`TEST_PLAN.md`](TEST_PLAN.md), and [`setup/BUILD.md`](setup/BUILD.md).

For a faster orientation, also use [`QUICK_START.md`](QUICK_START.md), [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md), [`UI_REFERENCE.md`](UI_REFERENCE.md), and [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md).

> CipherNest has **not** completed an independent professional security audit. Developer documentation must not turn passing tests or familiar primitives into unsupported absolute security claims.

## 1. Repository layout

`CipherNest.slnx` contains five source projects and three test projects.

### Source projects

- `src/CipherNest.Shared` — product/version/storage constants and small cross-layer primitives.
- `src/CipherNest.Domain` — framework-independent domain records and enums.
- `src/CipherNest.Application` — use-case abstractions, policies, validation, DTOs, safe-note contracts, session/security policy, and application exceptions; no MAUI/SQLite dependency.
- `src/CipherNest.Infrastructure` — Argon2id/AES-GCM cryptography, SQLite, migrations, encrypted attachments, encrypted backup/restore, CSV parsing/transfer, password/passphrase generation, RFC-compatible TOTP generation, and local audit implementations.
- `src/CipherNest.App` — .NET MAUI composition, Views/ViewModels, routes, lifecycle, biometrics, secure storage, clipboard, screenshot controls, file picker/share, localization/accessibility state, storage maintenance, About/legal/BMC UI, and privacy-safe diagnostics.

### Test projects

- `tests/CipherNest.UnitTests` — deterministic policy, validation, parser, generator, TOTP, and cryptographic tests.
- `tests/CipherNest.IntegrationTests` — real SQLite/vault/backup/attachment/import/migration/session integration tests.
- `tests/CipherNest.UiTests` — source/UI/documentation/workflow regression tests that do not require booting a MAUI target.

## 2. Dependency direction

Intended direction:

```text
Shared       Domain
   \         /
    Application
        ^
        |
 Infrastructure
        ^
        |
      App
```

Rules:

1. Domain must not depend on MAUI, SQLite, platform APIs, or Infrastructure.
2. Application abstractions must not expose raw SQLite connections, MAUI controls, platform-native objects, or raw DEK arrays.
3. Infrastructure implements Application abstractions and owns encrypted persistence/format logic.
4. App owns platform integration and dependency injection.
5. Views must not derive keys, parse encrypted containers, or open the database directly.
6. Unsupported platform capabilities must have an honest fallback/unsupported state.

## 3. Build-quality defaults

`Directory.Build.props` currently enables:

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

This is currently required by the verified CommunityToolkit MVVM partial observable-property syntax used by MAUI ViewModels for the Windows/WinRT/AOT-safe source shape. `ViewModelAotSourceTests` guards against reintroducing field-based `[ObservableProperty]` declarations that trigger `MVVMTK0045` on the Windows target.

Do not “fix” build failures by globally disabling warnings-as-errors, nullable analysis, analyzers, deterministic compilation, CommunityToolkit diagnostics, or security-sensitive tests.

## 4. Runtime composition root

`src/CipherNest.App/MauiProgram.cs` is the composition root.

Current singleton registrations include:

- `IClock -> SystemClock`
- `ICryptoService -> CryptoService`
- `IVaultStore -> SqliteVaultStore`
- `IVaultService -> VaultService`
- `IPasswordGenerator -> PasswordGenerator`
- `ITotpService -> TotpService`
- `ISecurityAuditService -> SecurityAuditService`
- `ISafeNoteMarkupService -> SafeNoteMarkupService`
- `ISettingsStore -> JsonSettingsStore`
- `IBackupService -> EncryptedBackupService`
- `IPlaintextTransferService -> CsvTransferService`
- `IClipboardSecurityService -> ClipboardSecurityService`
- `IScreenshotProtectionService -> ScreenshotProtectionService`
- `IBiometricUnlockService -> BiometricUnlockService`
- `IStorageMaintenanceService -> StorageMaintenanceService`
- `IPrivacySafeExceptionReporter -> PrivacySafeExceptionReporter`
- `ILocalizationService -> LocalizationService`
- `UnlockRateLimiter`
- `SessionSecurityState`
- `SessionLockPolicy`

Views and ViewModels are registered transiently.

When adding a service:

1. define a stable Application abstraction when the dependency crosses layers;
2. implement it in Infrastructure or App according to platform ownership;
3. register it only in the composition root;
4. add focused unit/integration/source tests;
5. update architecture/API/docs when the public boundary changes.

## 5. Navigation

Top-level Shell routes:

```text
startup
onboarding
unlock
vault
generator
audit
trash
settings
security-info
transfer
about
developer
```

Additional routes:

```text
ItemEditorPage
GeneratorDefaultsPage
```

`AppShell` disables the flyout and default navigation bar. A route that can display decrypted data must preserve lock-state expectations and must not remain a hidden decrypted-data bypass after vault lock.

See [`UI_REFERENCE.md`](UI_REFERENCE.md).

## 6. Main application contract: `IVaultService`

`IVaultService` is the main application-facing vault boundary. It covers:

- vault existence;
- create/unlock/lock;
- current-master re-authentication;
- secondary unlock configuration;
- master-passphrase change;
- full local-vault deletion;
- lock state/event;
- item read/write/search/trash/recent-access operations;
- attachment add/remove/export.

Do not bypass `VaultService` from new UI features to gain direct persistence access. `VaultService` applies service-level validation, resource checks, decrypted-record validation, session/key-lease behavior, maintenance, mutation serialization, and authorization sequencing.

See [`API_REFERENCE.md`](API_REFERENCE.md).

## 7. Session and vault-key rules

A random 256-bit DEK protects vault records/attachments. Master, recovery, and optional secondary credentials wrap that key independently.

Current invariants:

- shared session key state is synchronized;
- key-using operations receive private 32-byte `VaultKeyLease` copies;
- a lease links caller cancellation with current unlock-session cancellation;
- lease buffers zero on disposal where practical;
- lock removes/zeroes shared key state and cancels the current session token;
- create/master-recovery unlock/secondary unlock/lock/full-vault deletion use a serialized transition gate;
- full-vault deletion carries live-session authorization while waiting for that gate;
- attachment mutations use a separate cancellable serialization path so long file work does not block security lock.

Any change to these rules requires focused concurrency/integration coverage and review of `architecture/SESSION_AND_CONCURRENCY.md`, `security/SESSION_SECURITY.md`, threat model, crypto design, test plan, and release checklist.

## 8. Cryptography rules

Current primitives:

- Argon2id for passphrase-based key derivation/wrapping;
- AES-256-GCM for authenticated encryption.

Never:

- invent a custom cipher/MAC/password hash/PRNG;
- intentionally reuse GCM nonces;
- remove associated-data identity/context binding;
- accept unbounded unauthenticated KDF metadata before expensive work;
- silently change framing under an existing format version;
- store the master passphrase for convenience unlock;
- claim independent audit/security properties that do not exist.

A crypto/format change requires explicit compatibility/version design, known-answer/round-trip/tamper/wrong-key tests, backup/recovery implications, migration/release documentation, and threat-model review.

## 9. Vault header rules

Current source accepts exact supported vault-header structures only:

- historical v1 remains readable with its historical schema;
- v2 is the current write format;
- future/unknown/hybrid/duplicate/unknown/case-variant/wrong-kind metadata is rejected;
- header UTF-8 is capped at 64 KiB;
- JSON depth is capped at 16;
- strict schema validation occurs before typed deserialization/wrapped-key unwrap;
- replacement database candidates pass the same policy before active DB/WAL/SHM mutation.

Do not add a parallel permissive parser.

See `formats/VAULT_HEADER.md`.

## 10. Persistence and migrations

SQLite stores encrypted record envelopes and small required structural metadata.

Migration rules:

1. released migration versions are append-only compatibility history;
2. future schema versions are rejected;
3. version number alone is insufficient—required table/column shape is validated;
4. migration history is bounded/validated rather than trusted blindly;
5. rollback failure must not mask the primary migration failure;
6. replacement databases are validated before active file mutation;
7. snapshot/backup destinations must not clobber the active SQLite file set.

Current schema version is `1`. See `architecture/DATABASE.md`.

## 11. Vault item validation

Preserve `VaultItemValidator` as the shared item validation boundary.

Key limits include:

- non-empty item GUID;
- defined `VaultItemType`;
- title required/max 256;
- username max 2,048;
- general secret max 100,000;
- URL max 4,096;
- secure note max 200,000 chars / 5,000 lines;
- collection max 128;
- max 100 tags, each max 128;
- max 100 custom fields;
- max 25 attachments/item;
- max 2,000,000 aggregate item text/metadata;
- attachment metadata/storage-name/ID/uniqueness validation.

When adding a field, update aggregate resource accounting, serialization/compatibility assumptions, tests, API docs, format docs, and limits.

## 12. Persisted item-type compatibility

Current numeric values are compatibility-sensitive:

```text
Login = 0
SecureNote = 1
Identity = 2
PaymentCardReference = 3
WifiCredential = 4
SoftwareLicense = 5
ServerSshReference = 6
Document = 7
Custom = 8
OneTimePassword = 9
```

Do not insert/reorder existing persisted values without an explicit compatibility migration/version boundary.

## 13. TOTP extension rules

TOTP seed storage and local code generation are **implemented** current features, not deferred work.

`ITotpService` is an Application abstraction with a platform-independent Infrastructure implementation.

Current supported behavior:

- encrypted Base32 seed in `VaultItem.Secret` for TOTP items;
- SHA-1/SHA-256/SHA-512;
- 6/8 digits;
- 15–120-second period;
- explicit refresh/copy;
- generated codes are not persisted;
- RFC 6238 known-answer tests;
- bounded parser/HMAC inputs.

Do not add QR parsing/rendering, `otpauth://` URI parsing/export, automatic background refresh timers, autofill, or provider enrollment as incidental changes. Those **remain deferred** and require bounded parsing, lifecycle/accessibility/privacy/security review, source/device tests, threat-model updates, and interoperability docs.

TOTP algorithm numeric values are also persisted compatibility. Keep TOTP/`VaultItemType` compatibility tests green.

## 14. Attachments

Attachment rules:

- validate metadata before encryption;
- use bounded streaming instead of whole-file plaintext materialization;
- zero owned plaintext chunk buffers where practical;
- authenticate item/attachment/chunk identity through associated data;
- use collision-resistant `CreateNew` staging;
- refuse final overwrite on collision;
- enforce per-item/global attachment budgets;
- keep opaque storage names canonical/path-free/ID-bound;
- keep text preview/export deliberately bounded and documented.

Metadata validation must reuse the canonical `AttachmentImportPolicy`; do not create a second divergent display/media validator. Opaque encrypted storage names must pass `AttachmentStorageNamePolicy` before `Path.Combine`/file access.

Any new preview type is a plaintext rendering attack surface and requires dedicated review.

## 15. Encrypted backup/restore

Development rules:

- validate unauthenticated header schema/resource metadata before Argon2;
- keep framing/chunk loops bounded;
- enforce archive count/aggregate/path limits symmetrically on export/restore;
- reject duplicate normalized paths;
- validate attachment container-size envelopes;
- require actual extracted bytes to exactly match declared uncompressed entry lengths;
- validate staged SQLite before active replacement;
- once active mutation begins, required rollback/recovery must not be cancelled by the original cancelled request;
- preserve the primary failure when cleanup/rollback also fails;
- clear local biometric pairing after successful restore.

See `formats/ENCRYPTED_BACKUP.md` and `operations/BACKUP_RECOVERY_RUNBOOK.md`.

## 16. CSV transfer

CSV is plaintext interoperability.

Rules:

- import requires explicit mapping;
- bounds must apply to all field/row termination paths including final fields at newline/EOF;
- header metadata has a stricter 256-character, uniqueness, Control/Format safety policy;
- user-facing errors/warnings must not echo secret-bearing raw rows;
- mapped Tags must enforce the canonical 100-tag/128-character item policy before item construction;
- export requires current-master re-authentication plus the exact UI acknowledgement phrase;
- attachments are not silently included;
- temporary plaintext staging is cleaned best-effort without leaking sensitive paths.

## 17. Error handling and diagnostics

Sensitive UI must not display raw exception messages from filesystem/database/crypto/secure-storage/picker/share/platform calls because those strings can reveal paths/context.

Use `IPrivacySafeExceptionReporter` with a stable operation ID and fixed user-facing messages.

Do not log:

- master/backup passphrases;
- recovery material;
- secondary secrets;
- DEKs/KEKs;
- decrypted items/notes/attachments;
- raw secret-bearing CSV rows;
- raw exception messages/stacks through the privacy-safe path;
- identifying filesystem paths;
- clipboard plaintext;
- TOTP seeds/codes.

See `privacy/DIAGNOSTICS.md`.

## 18. Platform-boundary rules

File picker, share sheet, launcher, secure storage, biometrics, clipboard, screenshot controls, lifecycle callbacks, and platform directories are failure-prone boundaries.

- keep them inside protected async flows;
- display fixed safe messages;
- clear bound credentials before long platform operations where practical;
- delete plaintext staging in `finally`/best-effort paths;
- report unsupported behavior honestly;
- use source tests where runtime automation is impossible;
- still execute simulator/physical-device validation before release claims.

## 19. UI/ViewModel conventions

- Keep decrypted state out of global/static UI state.
- Clear sensitive ViewModel fields when sensitive pages disappear.
- Do not reveal secret custom-field values merely to populate quick-action lists.
- Preserve per-item re-authentication behavior.
- Keep layouts responsive on narrow/resizable windows.
- Preserve semantic metadata and important state announcements where supported.
- Use partial `[ObservableProperty]` properties rather than field-based generation in MAUI ViewModels.
- Keep the App project's narrowly scoped preview-language requirement while needed by that syntax.
- Do not suppress `MVVMTK0045` as a shortcut.
- Do not weaken security warnings for localization or visual brevity.
- Guard BMC/funding UI with `BuildFeatureFlags.IsFundingLinkEnabled` so `CipherNestEnableFundingLink=false` remains a valid store build.

## 20. Settings

`AppPreferences` is non-secret configuration. `AppPreferencesPolicy` is the normalization boundary.

When adding a preference:

1. add a safe default;
2. define normalization/bounds;
3. persist through `ISettingsStore`/`JsonSettingsStore`;
4. update Settings UI/ViewModel;
5. add round-trip/corruption/out-of-range tests;
6. update `CONFIGURATION_REFERENCE.md`, `USER_GUIDE.md`, and `LIMITS_AND_DEFAULTS.md`.

Current settings JSON itself is capped at 64 KiB, bounded by a 64 KiB + 1 actual-read sentinel, and capped at depth 16.

## 21. Localization

The current preference model supports:

```text
System
English
Hindi
```

Neutral English is the fallback. A reviewed `hi-IN` resource-backed catalog is **implemented** for migrated strings. Some remaining UI literals may still appear in English, so a fully translated application is not claimed.

Security warnings must preserve meaning across translations. See `architecture/LOCALIZATION.md`.

## 22. Accessibility

New UI must preserve:

- semantic names/descriptions where needed;
- readable dynamic typography;
- keyboard/focus behavior on desktop;
- narrow/resizable responsiveness;
- adequate touch targets;
- System/Light/Dark readability;
- reduced-motion expectations.

Source support does not replace TalkBack/VoiceOver/Narrator/keyboard/focus/large-text/contrast testing on release targets.

## 23. Tests: choose the right layer

### Unit tests

Use for pure policies, validators, cryptographic vectors, bounded parsers, generator/TOTP behavior, and deterministic services.

### Integration tests

Use when behavior depends on real SQLite, encrypted record round trips, backup/restore, attachment streaming, migrations, session cancellation, or infrastructure interactions.

### UI/source tests

Use for source/documentation/workflow invariants that do not require a device, such as:

- routes;
- semantic/source structure;
- redacted error handling;
- forbidden legacy patterns;
- BMC/funding build guards;
- CI/script presence;
- documentation presence/links/disclaimers;
- WinRT/AOT-safe ViewModel patterns.

### Device/manual tests

Required for biometrics, screenshot protection, clipboard history/API behavior, secure storage, lifecycle callbacks, picker/share behavior, assistive technologies, signing/packaging, and store behavior.

## 24. Local and hosted verification

Canonical scripts:

```text
scripts/verify-core.ps1
scripts/verify-core.sh
scripts/verify-windows.ps1
scripts/verify-android.sh
scripts/verify-apple.sh
```

The immutable implementation baseline immediately before the 2026-08-16 complete-documentation expansion is:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

Observed exact-candidate evidence:

- 346 UnitTests passed;
- 98 IntegrationTests passed;
- 111 UI/source tests passed;
- **555 total passed, 0 failed, 0 skipped**;
- core analyzer builds passed without build warnings/errors;
- core formatting passed;
- Windows default and funding-disabled Release builds passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI application builds.

Run IDs:

```text
CipherNest CI: 31937127961
CodeQL:       31937127900
```

The recorded Apple hosted pairing remains `macos-26`, .NET SDK `10.0.302`, Xcode `26.5`, workload set `10.0.300.3`, iOS RID `iossimulator-arm64`, and Mac Catalyst RID `maccatalyst-arm64`.

Do not report a later SHA as verified until its own configured gates finish successfully. Historical 240-test and 554-test evidence files remain valid historical records for their original SHAs.

## 25. Source-control practice

Prefer small logical commits, especially for security-sensitive changes:

1. policy/contract;
2. implementation;
3. tests;
4. documentation/release gate;
5. progress ledger.

Never commit real vault files, decrypted backups, secret-bearing screenshots, passphrases, recovery material, signing keys, certificates, private keys, or store credentials.

Recent repository workflow metadata records the active commit identity as `Sanskar <sanskarin@outlook.in>`.

## 26. Pull-request/review checklist

Before merging, confirm:

- dependency direction remains intact;
- tests exist at the correct layer;
- malformed/untrusted input is bounded before expensive work;
- cancellation cannot corrupt committed state;
- rollback/recovery remains uncancellable after destructive commit points where required;
- cleanup cannot mask the primary failure;
- raw secret/path exception surfaces were not introduced;
- no accidental plaintext persistent index/cache was introduced;
- versioned format/schema compatibility is explicit;
- ViewModel observable properties remain WinRT/AOT-safe;
- build/toolchain changes are documented and executed on the exact candidate;
- security/privacy docs reflect attack-surface changes;
- user docs reflect behavior changes;
- release/test gates include the new behavior;
- BMC/store behavior obeys the funding switch and current distribution policy;
- deferred features are not accidentally advertised as complete.

## 27. Areas requiring dedicated design before implementation

Do not bolt these onto the current local-only architecture without separate security/privacy/protocol/platform design:

- account/cloud synchronization;
- collaboration/shared vaults;
- browser/app autofill;
- Windows Hello convenience unlock;
- TOTP QR scanning/rendering and bounded `otpauth://` import/export;
- TOTP provider/autofill enrollment;
- rich binary/PDF preview and scanning;
- pronounceable-password generation;
- destructive wipe after failed attempts;
- complete migration/review of remaining UI literals and additional full localization catalogs.

Local TOTP seed storage/generation itself and the reviewed Hindi resource-backed catalog are already implemented current features and must not be listed as deferred.

See `NEXT_STEPS.md` and `FEATURE_MATRIX.md`.

## 28. Related documentation

- [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md)
- [`QUICK_START.md`](QUICK_START.md)
- [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md)
- [`UI_REFERENCE.md`](UI_REFERENCE.md)
- [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md)
- [`API_REFERENCE.md`](API_REFERENCE.md)
- [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md)
- [`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md)
- [`architecture/DATABASE.md`](architecture/DATABASE.md)
- [`architecture/SESSION_AND_CONCURRENCY.md`](architecture/SESSION_AND_CONCURRENCY.md)
- [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md)
- [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md)
- [`TESTING_GUIDE.md`](TESTING_GUIDE.md)
- [`verification/CI_GATES.md`](verification/CI_GATES.md)
- [`verification/COMPLETE_DOCUMENTATION_2026_08_16.md`](verification/COMPLETE_DOCUMENTATION_2026_08_16.md)
