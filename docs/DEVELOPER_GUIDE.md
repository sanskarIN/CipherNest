# CipherNest Developer Guide

This guide describes how the current source is organized and how to extend it without accidentally weakening the local-first security boundaries. It complements `architecture/ARCHITECTURE.md`, `security/THREAT_MODEL.md`, `security/CRYPTOGRAPHIC_DESIGN.md`, `TEST_PLAN.md`, and `setup/BUILD.md`.

## 1. Repository layout

`CipherNest.slnx` contains five source projects and three test projects.

### Source projects

- `src/CipherNest.Shared` — product/version/storage constants and small primitives shared across layers.
- `src/CipherNest.Domain` — framework-independent vault/domain records and enums.
- `src/CipherNest.Application` — public use-case abstractions, policy/services that do not require MAUI/SQLite, validation, and application exceptions.
- `src/CipherNest.Infrastructure` — cryptography, SQLite, migrations, encrypted attachment storage, encrypted backups, CSV parsing/transfer, password/passphrase generation, local RFC-compatible TOTP generation, and local audit implementations.
- `src/CipherNest.App` — .NET MAUI composition, Views/ViewModels, navigation, lifecycle, platform biometric/clipboard/screenshot/secure-storage/file-picker/share surfaces, localization, accessibility state, storage maintenance, About/legal UI, and privacy-safe diagnostics.

### Tests

- `tests/CipherNest.UnitTests` — deterministic policy/cryptographic/service tests.
- `tests/CipherNest.IntegrationTests` — real persistence/vault/backup/import/attachment/migration integration tests.
- `tests/CipherNest.UiTests` — source/UI-structure regression tests that can run without booting a MAUI target.

## 2. Build quality defaults

`Directory.Build.props` currently enables the shared solution policy:

- latest shared C# language policy;
- nullable reference analysis;
- implicit usings;
- warnings as errors;
- latest analysis level;
- code-style enforcement during build;
- deterministic managed compilation;
- CI build metadata when `CI=true`.

The MAUI App project has one deliberate project-local language override:

```xml
<LangVersion>preview</LangVersion>
```

That override exists because the current CommunityToolkit MVVM WinRT/AOT-safe partial `[ObservableProperty]` syntax used by CipherNest requires the preview language feature in the verified toolchain. It must remain scoped to `CipherNest.App` unless another project has a separately justified requirement. `ViewModelAotSourceTests` protects this rule.

Do not “fix” a build by globally disabling warnings-as-errors, nullable analysis, analyzers, deterministic builds, CommunityToolkit WinRT/AOT diagnostics, or security-sensitive tests. Resolve the underlying issue or document an explicit narrowly scoped reason.

## 3. Dependency direction

The intended direction is:

```text
Shared      Domain
   \         /
    Application
        ^
        |
 Infrastructure
        ^
        |
      App
```

The exact project references may include Shared where constants are required, but application behavior should remain dependency-inverted around Application abstractions.

### Rules

1. Domain records must not depend on MAUI, SQLite, platform APIs, or Infrastructure.
2. Application abstractions must not expose SQLite connections, MAUI controls, raw DEK arrays, or platform objects.
3. Infrastructure implements Application abstractions and owns encrypted persistence/format logic.
4. App owns platform interaction and dependency injection.
5. Views should not directly open databases, derive keys, parse encrypted containers, or construct crypto implementations.
6. New platform-specific capability must expose an honest unsupported/fallback state where the capability is unavailable.

## 4. Composition root

`src/CipherNest.App/MauiProgram.cs` is the runtime composition root.

Current singleton service registrations include:

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

1. define the stable abstraction in Application when it represents a use-case/cross-layer dependency;
2. implement it in Infrastructure or App depending on whether it is platform independent or platform facing;
3. register it only in the composition root;
4. add focused unit/integration/source coverage;
5. update architecture/API docs when the public boundary changes.

## 5. Navigation

The current Shell top-level routes are:

- `startup`
- `onboarding`
- `unlock`
- `vault`
- `generator`
- `audit`
- `trash`
- `settings`
- `security-info`
- `transfer`
- `about`
- `developer`

Additional registered routes:

- `ItemEditorPage`
- `GeneratorDefaultsPage`

`AppShell` disables the flyout and navigation bar; ViewModels/Views provide the current explicit navigation actions.

New routes must preserve lock-state expectations. A route that displays decrypted data must not remain a hidden bypass after the vault has locked.

## 6. Vault-service boundary

`IVaultService` is the main application-facing vault contract. It currently covers:

- vault existence;
- create/unlock/master re-authentication;
- secondary unlock configuration;
- master-passphrase change;
- full local-vault deletion;
- lock-state event/state;
- item read/write/search/trash operations;
- recent-access update;
- attachment add/remove/export.

Do not bypass `VaultService` in new UI features to obtain direct persistence access. `VaultService` applies service-level resource checks, item validation, decrypted record validation, session/key-lease behavior, trash maintenance, mutation serialization, and authorization sequencing that an alternate UI path could otherwise skip.

## 7. Session and key rules

The random 256-bit DEK protects vault items/attachments. Master, recovery, and optional secondary credentials wrap that DEK.

Important current invariants:

- shared session key state is synchronized;
- key-using operations receive private `VaultKeyLease` copies;
- a lease links caller cancellation with the active unlock-session token;
- lease buffers zero on disposal;
- locking removes/zeroes shared key state and cancels the session token;
- creation/master/recovery unlock/secondary unlock/lock/full-vault deletion share a serialized transition gate;
- destructive full-vault deletion holds live session authorization while waiting for that gate;
- attachment mutations use their own cancellable serialization path instead of blocking security lock behind long file work.

Any change to these rules requires focused concurrency tests and review of `architecture/SESSION_AND_CONCURRENCY.md`, `security/SESSION_SECURITY.md`, the threat model, cryptographic design, test plan, and release checklist.

## 8. Cryptography rules

The current implemented primitives are Argon2id for passphrase KDF and AES-256-GCM for authenticated encryption.

Never:

- add a custom cipher, MAC, password hash, or PRNG;
- reuse GCM nonces intentionally;
- remove associated-data binding for item/chunk identity/context;
- accept unbounded KDF metadata from unauthenticated containers;
- silently change cryptographic framing under an existing version;
- store the master passphrase for convenience unlock;
- claim security properties that have not been independently verified.

A cryptographic format change requires an explicit version/compatibility plan, known-answer/round-trip/tamper/wrong-key tests, backup/recovery implications, migration/release notes, and security-document updates.

## 9. Persistence and migrations

SQLite persists encrypted vault records and small structural metadata. Current database schema version is defined by `AppConstants.DatabaseSchemaVersion`.

Migration rules:

1. Released migration versions are append-only compatibility history.
2. Future schema versions are rejected rather than guessed.
3. Reaching a version is not sufficient; required table/column shape is validated.
4. Migration history itself is validated rather than trusted blindly.
5. Rollback failure must not mask the primary migration failure.
6. Database replacement candidates are validated before active DB/WAL/SHM mutation.
7. Snapshot/backup destinations must not clobber the active SQLite file set.

See `architecture/DATABASE.md`.

## 10. Vault item validation

All item-save paths must preserve `VaultItemValidator` as the shared validation boundary.

Current important limits include:

- non-empty `Guid` item ID;
- defined `VaultItemType`;
- title required/max 256;
- username max 2,048;
- secret max 100,000;
- TOTP item seeds additionally use bounded Base32 validation (4,096 formatted / 1,024 normalized characters, minimum 16) with SHA-1/SHA-256/SHA-512, 6/8 digits, and 15–120-second periods;
- URL max 4,096;
- secure-note max 200,000 chars/5,000 lines;
- collection max 128;
- max 100 tags/custom fields;
- max 25 attachments/item;
- max 2,000,000 aggregate item text/metadata;
- canonical attachment ID/storage-name binding;
- per-item attachment-ID/storage-name uniqueness.

When adding a new field, update aggregate resource accounting and tests. Do not add a field to `VaultItem` that escapes encrypted-at-rest storage without a documented privacy/security reason.


### TOTP extension rules

TOTP generation is an Application abstraction (`ITotpService`) with a platform-independent Infrastructure implementation (`TotpService`). Keep provider/network/UI concerns out of that implementation. RFC 6238 known-answer vectors are release-blocking compatibility tests.

The current encrypted JSON writes enums numerically. `VaultItemType.Custom = 8` is existing persisted compatibility and `OneTimePassword = 9` was intentionally appended rather than inserted. `VaultItemTypeCompatibilityTests` must stay green whenever item-type enums change. TOTP algorithm numeric values are also pinned.

Do not add QR parsing, `otpauth://` URI parsing, automatic refresh timers, autofill, or provider enrollment as incidental changes: each needs bounded parsing, lifecycle/accessibility review, source/device tests, threat-model updates, and honest interoperability documentation.

## 11. Attachments

Attachment implementation rules:

- validate import metadata before encryption;
- use bounded streaming rather than whole-file plaintext materialization;
- zero owned plaintext chunk buffers where practical;
- authenticate chunk context using item/attachment/chunk identity;
- use collision-resistant `CreateNew` staging;
- never overwrite an existing final encrypted container on collision;
- keep opaque storage names canonical and path-free;
- maintain per-item/global attachment budgets;
- keep plaintext preview/export explicitly bounded and documented.

Any new preview type must be reviewed as a plaintext rendering attack surface.

## 12. Backup/restore

Encrypted backup is a separate authenticated format with its own backup passphrase and format version.

Development rules:

- validate unauthenticated header resource metadata before Argon2 work;
- keep chunk framing bounded;
- enforce archive count/aggregate/path limits symmetrically on export/restore;
- reject duplicate normalized archive paths;
- validate attachment container size envelopes;
- validate the staged SQLite candidate before active replacement;
- once active mutation begins, recovery/rollback must not be cancellable by the already-cancelled caller token;
- preserve the original failure if cleanup/rollback has a secondary failure;
- clear local biometric pairing after successful restore.

See `formats/ENCRYPTED_BACKUP.md` and `operations/BACKUP_RECOVERY_RUNBOOK.md`.

## 13. CSV transfer

CSV import/export is deliberately plaintext interoperability.

- Import requires explicit mapping.
- Parser bounds must apply to every field/row termination path, including final fields at newline/EOF.
- User-facing warnings must not embed raw invalid row/field content that could contain secrets.
- Export requires current-master re-authentication and the exact confirmation phrase used by the UI.
- Attachments are not silently included in CSV export.
- Temporary plaintext staging must be cleaned best-effort and cleanup failures must not leak filesystem paths.

## 14. Error handling and diagnostics

Sensitive UI must not display raw exception messages from filesystem, database, crypto, secure-storage, picker/share, or platform calls because exception text can expose paths/context.

Use `IPrivacySafeExceptionReporter` with a stable operation identifier and show fixed user-facing text.

Do not log:

- passphrases/recovery keys/secondary secrets;
- DEKs/KEKs/nonces paired with plaintext;
- decrypted items/notes/attachments;
- raw CSV rows containing secrets;
- full exception messages/stacks in the privacy-safe reporter;
- filesystem paths that may identify user content;
- clipboard plaintext;
- TOTP seeds or generated one-time codes.

See `privacy/DIAGNOSTICS.md`.

## 15. MAUI/platform calls

File picker, share sheet, launcher, secure storage, biometrics, clipboard, screenshot protection, lifecycle callbacks, and platform directories are failure-prone platform boundaries.

- keep them inside protected async flows;
- report fixed user-safe messages;
- clear bound credentials before long platform work when practical;
- delete plaintext staging in `finally`/best-effort cleanup paths;
- treat unsupported behavior honestly;
- cover source shape with tests where runtime automation is unavailable;
- still perform emulator/physical-device validation before release.

## 16. UI/ViewModel conventions

- Keep decrypted state out of static/global UI state.
- Clear sensitive ViewModel fields when sensitive pages disappear.
- Do not reveal secret custom-field values merely to build a quick-action list.
- Ensure protected items require re-authentication before revealing/changing protected content.
- Keep navigation responsive and accessible on narrow and desktop windows.
- Preserve supported semantic descriptions/state announcements for important state changes without reintroducing unsupported MAUI XAML properties.
- For CommunityToolkit observable state in MAUI ViewModels, use partial `[ObservableProperty]` properties rather than field-based generation. The Windows/WinRT build treats the field-based pattern as `MVVMTK0045` and release builds keep that analyzer active.
- Preserve the App project's narrowly scoped preview-language setting while that partial-property syntax requires it; do not suppress `MVVMTK0045` as a shortcut.
- Do not weaken security warnings for localization brevity.

## 17. Settings

`AppPreferences` is non-secret local configuration. `AppPreferencesPolicy` is the normalization boundary. Do not trust deserialized preference values directly when new settings are added.

When adding a preference:

1. add a safe default to `AppPreferences`;
2. define normalization/bounds in `AppPreferencesPolicy` where applicable;
3. persist via `ISettingsStore`/`JsonSettingsStore`;
4. update Settings UI/ViewModel;
5. add round-trip/corruption/out-of-range tests;
6. document the new preference in `USER_GUIDE.md` and `LIMITS_AND_DEFAULTS.md`.

## 18. Localization

Neutral English remains the fallback, with System/English/Hindi preferences for the reviewed resource-backed surface. Do not claim a fully translated interface until every remaining user/security/error literal in that surface has been migrated to resources and reviewed.

Security warnings must preserve meaning across translations. See `architecture/LOCALIZATION.md`.

## 19. Accessibility

New UI must preserve:

- semantic names/descriptions where needed;
- readable dynamic typography;
- keyboard/focus behavior on desktop;
- narrow-window responsiveness;
- adequate touch targets;
- light/dark/system readability;
- reduced-motion expectations.

See `ACCESSIBILITY.md` and the test/release checklists.

## 20. Tests: choosing the right layer

### Unit tests

Use for pure policies, validators, cryptographic vectors, deterministic parser/resource rules, and services that can be isolated cleanly.

### Integration tests

Use when the behavior depends on real SQLite, encrypted record round trips, backup/restore, attachment streaming, migrations, session cancellation, or interactions among infrastructure services.

### UI/source tests

Use to prevent structural regressions when a MAUI device is not required, for example:

- route presence;
- semantic metadata;
- source ordering/invariant checks;
- redacted error handling;
- forbidden legacy API patterns;
- CI/workflow/script presence;
- canonical documentation presence/link/audit wording;
- WinRT/AOT-safe CommunityToolkit ViewModel source patterns.

`DocumentationCoverageSourceTests` and `ViewModelAotSourceTests` are current examples. Source tests are regression signals, not proof of runtime platform behavior.

### Device/manual tests

Required for biometrics, screenshot protection, clipboard APIs/history behavior, secure storage, lifecycle callbacks, file picker/share sheet, accessibility readers, signing/packaging, and store behavior.

## 21. Local and hosted verification

Prefer committed scripts:

```text
scripts/verify-core.ps1
scripts/verify-core.sh
scripts/verify-windows.ps1
scripts/verify-android.sh
scripts/verify-apple.sh
```

Platform-specific MAUI builds use the App project's `CipherNestTargetFrameworks` property plus an appropriate target RID so a host does not evaluate unrelated MAUI target graphs.

The hosted baseline for candidate `2327abba1646082a4d94a689d452b1116701cc0b` is recorded in `verification/HOSTED_CI_EVIDENCE_2026_08_13.md`: 240 tests passed, formatting passed, both Windows variants passed, Android passed, iOS simulator passed, Mac Catalyst passed, and CodeQL v4 passed. The Apple hosted pairing was `macos-26`, .NET SDK `10.0.302`, Xcode `26.5`, and workload set `10.0.300.3`.

Do not report a pass unless the script/workflow actually ran successfully for the exact candidate commit. A later commit must rerun the relevant gates rather than inheriting historical evidence automatically.

## 22. Source-control and commit practice

Prefer small commits scoped to one logical change. A normal security-sensitive change sequence is:

1. policy/contract;
2. implementation;
3. unit/integration/source tests;
4. documentation/release gates;
5. progress ledger.

Do not commit secrets, local vault files, decrypted backups, signing material, real screenshots with credentials, or developer diagnostic exports containing private environment data.

Connector-created project commits in this work use:

```text
Signed-off-by: Sanskar <sanskarin@outlook.in>
```

The sign-off records the requested identity in the commit message; the connected GitHub API determines actual Git author/committer metadata.

## 23. Pull-request/review checklist

Before merging a change, confirm:

- dependency direction remains intact;
- tests exist at the right layers;
- malformed/untrusted input is bounded before expensive allocation/work;
- cancellation cannot corrupt committed state or cancel required rollback after a destructive commit point;
- cleanup cannot mask the primary failure;
- no raw secret/path exception surface was introduced;
- no plaintext persistent index/cache was introduced accidentally;
- versioned format/schema compatibility is explicit;
- MAUI ViewModel observable properties remain WinRT/AOT-safe and the Windows analyzer has not been suppressed;
- platform build selection/toolchain changes are represented in verification/build docs and exercised on the exact candidate;
- security/privacy docs are updated if the attack surface changed;
- user docs are updated if behavior changed;
- release checklist/test plan include the new gate;
- no deferred feature is accidentally advertised as complete.

## 24. Areas requiring dedicated design before implementation

Do not bolt the following onto the current local-only architecture without separate threat/privacy/protocol design:

- account/cloud synchronization;
- collaboration/shared vaults;
- autofill/browser integration;
- TOTP seed storage/generation;
- Windows Hello convenience unlock;
- rich binary/PDF preview/scanning;
- pronounceable-password generation;
- destructive wipe after failed attempts;
- complete additional localization catalogs.

See `NEXT_STEPS.md` for the ordered future-work process.

## Attachment metadata policy rule — 2026-08-15

Do not add a second attachment display/media metadata validator. Import normalization and decrypted/programmatic item validation must reuse `AttachmentImportPolicy` so rune-aware malformed-UTF-16 and Unicode Control/Format behavior cannot drift. Opaque encrypted storage names remain an Infrastructure filesystem boundary and must pass `AttachmentStorageNamePolicy` before `Path.Combine`/file access.

If the metadata acceptance contract changes, update the attachment/vault-record format docs, limits, deterministic hostile corpus, source-regression tests, threat model, test plan, changelog/status, and verification record in the same candidate.
