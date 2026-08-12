# Architecture

CipherNest uses a dependency-inverted multi-project .NET MAUI structure:

- `CipherNest.Domain` — vault entities, item types, application preference models, security findings, generator options, attachment references, and other framework-independent domain state.
- `CipherNest.Application` — use-case/service abstractions, safe-note model/parser contract, validation, DTOs, and exceptions. It has no MAUI/SQLite dependency.
- `CipherNest.Infrastructure` — Argon2id/AES-GCM cryptography, encrypted attachment storage, SQLite persistence/migrations, encrypted backup/restore, CSV transfer parsing, password/passphrase generation, clocks, and local audit implementations.
- `CipherNest.Shared` — cryptographic/database/app version constants and small shared primitives.
- `CipherNest.App` — MAUI views/ViewModels, dependency injection composition, application lifecycle, biometric/clipboard/screenshot/secure-storage surfaces, localization, accessibility preferences, storage/cache controls, redacted exception reporting, and explicit OS share/file-picker integration.
- `tests/CipherNest.UnitTests` — deterministic/security-sensitive pure service and cryptographic tests.
- `tests/CipherNest.IntegrationTests` — SQLite/vault/backup/import/attachment/migration integration tests.
- `tests/CipherNest.UiTests` — repository/UI-structure checks that can run without booting a MAUI device, complementing manual/emulator/physical-device smoke tests.

## Dependency direction

The UI never receives a database connection, encryption key, KDF implementation, or raw encrypted-record store directly. It interacts with application abstractions such as `IVaultService`, `IPasswordGenerator`, `ISecurityAuditService`, `IBackupService`, `IPlaintextTransferService`, `ISettingsStore`, and safe-note contracts. Infrastructure implements those abstractions.

Platform-specific capabilities remain inside the MAUI/application boundary. Unsupported capabilities use an explicit safe fallback rather than a fake implementation—for example, Windows currently falls back to master-passphrase unlock instead of claiming biometric unlock support.

## Data boundary

At rest, `VaultItems` contains authenticated encrypted item envelopes. Searchable item fields, recent-use timestamps, review dates, tags, collections, custom fields, and attachment metadata remain inside the encrypted item payload. Attachments are separately chunk-encrypted files whose opaque filenames reveal no user title.

Search/filter/audit therefore operates over decrypted authenticated objects only while the vault is unlocked. CipherNest intentionally avoids a plaintext SQLite FTS index.

## Key boundary

The random vault DEK is the key that protects records/attachments. Master, recovery, and optional biometric-secondary paths wrap the DEK rather than duplicating/re-encrypting every record for each credential. Lock zeroes owned DEK buffers where practical and drops the active key reference.

Key-using operations receive private `VaultKeyLease` copies linked to the current unlock session and caller cancellation. Master/recovery unlock, secondary unlock, public lock, creation, and full-vault deletion coordinate through a serialized transition gate; destructive full-vault deletion binds authorization to a live session while waiting for that gate.

Managed strings and garbage-collected copies cannot be deterministically erased; the design documents that limitation rather than claiming otherwise.

## UI and lifecycle boundary

MAUI ViewModels expose commands/state; Views contain presentation and semantic metadata. Application lifecycle events enforce background/timeout locks and fail closed when preference handling fails. Screenshot protection, biometric prompts, clipboard clearing, secure storage, file picking, and OS sharing are treated as platform surfaces with documented limitations.

`AccessibilityPreferenceApplicator` owns dynamic interface-size/reduced-motion resource state. `LocalizationService` owns persisted UI-culture preference/resource lookup. Neither changes encrypted vault formats.

## Diagnostics boundary

`PrivacySafeExceptionReporter` is the centralized unhandled-error path. It logs a sanitized operation identifier, exception type, HResult, severity, and fixed wording only. It intentionally does not send exception messages/stacks or decrypted user values to the logger.

## Versioning

Cryptographic envelope version and database schema version are explicit independent constants. Database changes pass through ordered transactional migrations. A future unsupported schema is rejected instead of guessed. Cryptographic format changes require focused compatibility/security review and test vectors before release.

Vault-header document version, encrypted attachment magic/framing, and encrypted backup format version are also independent compatibility surfaces and must not be changed incompatibly under an existing version identifier.

## Detailed architecture references

- `DEPENDENCY_MAP.md` — project/package/DI/platform-target ownership.
- `DATA_FLOW.md` — end-to-end sensitive data paths.
- `SESSION_AND_CONCURRENCY.md` — key leases, transition/mutation gates, cancellation, destructive commit points, and recovery ordering.
- `DATABASE.md` — SQLite schema/migration/replacement/snapshot/recovery details.
- `LOCALIZATION.md` — resource/culture architecture.
- `../API_REFERENCE.md` — Application contracts and Domain models.
- `../LIMITS_AND_DEFAULTS.md` — current resource/default/version values.
- `../formats/VAULT_RECORDS.md` — encrypted item records.
- `../formats/ATTACHMENTS.md` — `.cna` encrypted attachment framing.
- `../formats/ENCRYPTED_BACKUP.md` — `.cnbak` backup framing/restore.
- `../formats/CSV_TRANSFER.md` — plaintext interoperability.
- `../security/THREAT_MODEL.md`, `../security/CRYPTOGRAPHIC_DESIGN.md`, `../security/SESSION_SECURITY.md`, and `../security/DATA_LIFECYCLE.md` — security/privacy interpretation of these boundaries.
