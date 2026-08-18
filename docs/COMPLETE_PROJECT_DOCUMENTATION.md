# CipherNest — Complete Project Documentation

This is the consolidated end-to-end reference for the current CipherNest repository. It is designed for users, contributors, maintainers, reviewers, security engineers, release engineers, and store/distribution owners who need one coherent description of the project while retaining links to the deeper canonical documents.

> **Documentation contract:** this file describes implemented source behavior and explicitly documented external/deferred work. It is not a marketing wish list. If a claim here conflicts with current source, focused tests, or a specialized security/format document, the current source and the specialized canonical document take precedence and this file must be corrected.

> **Security status:** CipherNest has **not** completed an independent professional security audit. It must not be described as unhackable, military-grade, 100% secure, capable of guaranteed managed-memory erasure, or capable of guaranteed physical-media sanitization.

---

## Table of contents

1. Project identity
2. Executive summary
3. Product goals
4. Explicit non-goals and deferred scope
5. Supported targets
6. Technology stack
7. Repository layout
8. Dependency architecture
9. Runtime composition and services
10. Navigation and UI surfaces
11. First launch and vault creation
12. Master passphrase and recovery model
13. Biometric convenience unlock
14. Cryptographic design
15. Vault-header format and compatibility
16. Vault item model and item types
17. Validation and resource ceilings
18. SQLite schema, migrations, and replacement
19. Session security and concurrency
20. Search, filters, sorting, and reminders
21. Local security audit
22. TOTP
23. Password/passphrase generator
24. Secure notes
25. Encrypted attachments
26. Encrypted backup and restore
27. CSV import and plaintext export
28. Clipboard and plaintext lifecycle
29. Trash, permanent deletion, and full-vault deletion
30. Settings and configuration
31. Accessibility
32. Localization
33. Privacy-safe diagnostics
34. Branding and Buy Me a Coffee support
35. Build prerequisites and commands
36. Package/dependency management
37. Automated tests
38. Hosted CI and CodeQL baseline
39. Security/threat-model summary
40. Data lifecycle
41. Format/version compatibility
42. Release and packaging
43. Store/distribution policy
44. Security-response and recovery operations
45. Support and troubleshooting
46. Contribution and code-review rules
47. Documentation governance
48. Known limitations and external validation gates
49. Future-version roadmap
50. Quick checklists
51. Canonical documentation map
52. Glossary

---

# 1. Project identity

| Field | Value |
|---|---|
| Product | **CipherNest** |
| Current source version | **0.1.0** |
| Primary language | C# |
| Runtime/framework | .NET 10 / .NET MAUI |
| Architecture | Local-first encrypted vault |
| License | GPL-3.0-or-later |
| Application ID | `in.sanskar.ciphernest` |
| Repository | `https://github.com/sanskarIN/CipherNest` |
| Creator | Sanskar |
| Creator profile | `https://www.github.com/sanskarIN` |
| Business contact | `sanskarin@outlook.in` |
| Support contact | `supportramsandesh@gmail.com` |
| Optional development support | `https://buymeacoffee.com/sanskarIN` |
| Creator watermark | `Made by the Sanskar` |

Public application/project metadata used by source is centralized through `CipherNest.Shared.AppConstants` so links/emails do not need to be duplicated throughout code.

---

# 2. Executive summary

CipherNest is a local-first password, credential, identity, secure-note, TOTP, custom-secret, and encrypted-document vault. Ordinary operation does not require a CipherNest account, email address, phone number, application server, or cloud synchronization service.

The current design emphasizes:

- local authenticated encryption;
- a random vault data-encryption key instead of direct per-record master-passphrase encryption;
- Argon2id passphrase-based key wrapping;
- AES-256-GCM authenticated encryption;
- independent master/recovery/secondary wrapper paths;
- strict bounded parsing and storage ceilings;
- SQLite integrity/schema/replacement checks;
- serialized security-session transitions;
- cancellable private key leases;
- safe local search/audit over decrypted objects only while unlocked;
- encrypted streaming attachments;
- authenticated encrypted backups;
- guarded plaintext interoperability;
- bounded local TOTP `otpauth://totp/...` text interoperability;
- explicit clipboard/temporary-plaintext limitations;
- privacy-safe diagnostics;
- honest platform/security limitations;
- multi-layer automated regression tests;
- cross-platform hosted compile gates;
- CodeQL analysis of core and the MAUI application path.

---

# 3. Product goals

CipherNest is designed to provide:

1. **A usable local encrypted vault** without requiring a remote account service.
2. **A small, reviewable security-sensitive core** around key wrapping, authenticated records, attachment containers, backup containers, session state, persistence boundaries, and bounded TOTP setup-URI parsing/formatting.
3. **Explicit recovery semantics** rather than pretending forgotten credentials can always be reset.
4. **Bounded hostile-input handling** so malformed database/header/CSV/settings/backup/attachment/TOTP-URI inputs cannot request unlimited work.
5. **Cross-platform MAUI reach** across Windows, Android, iOS, and Mac Catalyst.
6. **Clear plaintext boundaries** whenever the user explicitly copies, previews, exports, shares, or transfers decrypted/secret-bearing content.
7. **Accurate documentation** that separates source implementation, hosted evidence, platform validation, and future work.

---

# 4. Explicit non-goals and deferred scope

The current release does not claim completed support for:

- CipherNest-hosted cloud synchronization;
- user accounts;
- collaboration/shared vaults;
- server-side vault storage;
- multi-device conflict resolution;
- browser/application autofill;
- Windows Hello convenience unlock;
- TOTP QR scanning/rendering;
- camera-based TOTP enrollment;
- HOTP/counter interoperability;
- automatic TOTP provider enrollment;
- TOTP provider/autofill integration;
- rich binary/PDF rendering beyond the bounded safe text-preview formats;
- document scanning;
- pronounceable-password mode;
- destructive automatic wipe after failed attempts;
- complete translation of every remaining literal into Hindi;
- complete additional-language catalogs;
- guaranteed managed-memory erasure;
- guaranteed clipboard-history/synchronization erasure;
- guaranteed physical erasure from storage media;
- recovery when every valid local master/recovery path is lost.

Bounded **text-only** TOTP `otpauth://totp/...` import and canonical formatting/copy are implemented. That implementation must not be confused with QR/camera enrollment, HOTP, provider enrollment, universal third-party authenticator compatibility, or clipboard-history erasure.

Deferred features must not be represented in the UI, README, store listing, or release notes as complete until implementation, tests, security/privacy review, documentation, and target validation support that claim.

---

# 5. Supported targets

The MAUI project targets:

```text
net10.0-android
net10.0-ios
net10.0-maccatalyst
net10.0-windows10.0.19041.0
```

Minimum declared platform versions:

| Platform | Minimum in project | Current position |
|---|---:|---|
| Android | API 26 | Application target. Biometric convenience path uses API-28 `BiometricPrompt` baseline. |
| iOS | 15.0 | Application target with optional biometric convenience integration. |
| Mac Catalyst | 15.0 | Desktop application target with optional biometric convenience integration. |
| Windows | 10.0.19041.0 | Desktop target; current convenience unlock is master-passphrase fallback. |
| Linux | — | No shipping MAUI application target in the current solution. |

Hosted compilation does not replace physical-device/runtime validation of biometrics, secure storage, lifecycle, screenshots, clipboard, share sheets, accessibility, signing, packaging, store behavior, or representative third-party TOTP setup-URI interoperability.

---

# 6. Technology stack

## Application stack

- C# / .NET 10
- .NET MAUI Single Project
- CommunityToolkit.Mvvm
- SQLite through Microsoft.Data.Sqlite / SQLitePCLRaw
- Konscious.Security.Cryptography.Argon2
- xUnit-based automated tests
- GitHub Actions
- GitHub CodeQL

## Current centrally managed package versions

| Package | Version |
|---|---:|
| CommunityToolkit.Mvvm | `8.4.0` |
| Konscious.Security.Cryptography.Argon2 | `1.3.1` |
| Microsoft.Data.Sqlite | `10.0.10` |
| SQLitePCLRaw.bundle_e_sqlite3 | `2.1.12` |
| Microsoft.Extensions.Logging.Debug | `10.0.0` |
| Microsoft.Maui.Controls | `10.0.0` |
| Microsoft.NET.Test.Sdk | `18.0.0` |
| xunit | `2.9.3` |
| xunit.runner.visualstudio | `3.1.4` |

Package versions are centralized in `Directory.Packages.props`.

The TOTP setup-URI implementation uses the .NET `Uri`/text APIs and existing project abstractions; it does not add a QR, camera, HTTP/provider, or third-party TOTP URI dependency.

---

# 7. Repository layout

```text
CipherNest/
├─ src/
│  ├─ CipherNest.Shared/
│  ├─ CipherNest.Domain/
│  ├─ CipherNest.Application/
│  ├─ CipherNest.Infrastructure/
│  └─ CipherNest.App/
├─ tests/
│  ├─ CipherNest.UnitTests/
│  ├─ CipherNest.IntegrationTests/
│  └─ CipherNest.UiTests/
├─ docs/
│  ├─ architecture/
│  ├─ branding/
│  ├─ formats/
│  ├─ history/
│  ├─ operations/
│  ├─ privacy/
│  ├─ releases/
│  ├─ security/
│  ├─ setup/
│  └─ verification/
├─ scripts/
├─ .github/workflows/
├─ README.md
├─ SECURITY.md
├─ PRIVACY.md
├─ SUPPORT.md
├─ CONTRIBUTING.md
├─ PROJECT_STATUS.md
├─ CHANGELOG.md
└─ what_changed.md
```

## Source responsibilities

### `CipherNest.Shared`

Owns small cross-layer constants/primitives such as product metadata, format/database constants, and storage ceilings.

### `CipherNest.Domain`

Owns framework-independent domain records/enums:

- `VaultItem`;
- `VaultItemType`;
- `AttachmentReference`;
- `CustomField`;
- `AppPreferences`;
- TOTP/domain enums;
- generator options/results;
- security-audit finding models.

### `CipherNest.Application`

Owns stable use-case abstractions, policies, validators, application-facing DTOs, session/security policy, safe-note contracts, clocks, and exceptions. It does not own MAUI controls or SQLite connections.

The TOTP interoperability contract lives here through `ITotpUriCodec` and `TotpUriProfile` so the App does not implement a parallel parser.

### `CipherNest.Infrastructure`

Owns:

- Argon2id/AES-GCM implementation;
- SQLite store/migrations;
- encrypted record serialization;
- encrypted attachment storage;
- encrypted backup/restore;
- CSV parsing/transfer;
- local password/passphrase generation;
- local TOTP generation;
- bounded local TOTP setup-URI parsing/formatting;
- local audit implementation;
- other platform-independent concrete services.

### `CipherNest.App`

Owns:

- MAUI Views/ViewModels;
- dependency injection composition;
- Shell navigation;
- lifecycle handling;
- biometric/secure-storage integration;
- clipboard/screenshot services;
- file picker/share sheet;
- localization/accessibility state;
- storage maintenance;
- About/legal/support/BMC UI;
- privacy-safe application diagnostics.

---

# 8. Dependency architecture

Intended dependency direction:

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

Key rules:

1. Domain must not depend on MAUI, SQLite, or platform APIs.
2. Application abstractions must not expose raw database connections, UI controls, raw vault-key arrays, or platform-native objects.
3. Infrastructure implements application abstractions and owns encrypted persistence/format logic plus bounded TOTP setup-URI parsing/formatting.
4. App owns platform interaction and composition.
5. Views must not derive keys, open SQLite directly, parse encrypted containers, or implement an independent `otpauth://` parser.
6. New platform capabilities need honest unsupported/fallback states.

See `architecture/ARCHITECTURE.md`, `architecture/DEPENDENCY_MAP.md`, and `architecture/DATA_FLOW.md`.

---

# 9. Runtime composition and services

`src/CipherNest.App/MauiProgram.cs` is the runtime composition root.

Current singleton registrations include:

- `IClock -> SystemClock`
- `ICryptoService -> CryptoService`
- `IVaultStore -> SqliteVaultStore`
- `IVaultService -> VaultService`
- `IPasswordGenerator -> PasswordGenerator`
- `ITotpService -> TotpService`
- `ITotpUriCodec -> TotpUriCodec`
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

Views/ViewModels are registered transiently.

---

# 10. Navigation and UI surfaces

`AppShell` disables the flyout and default Shell navigation bar.

Top-level routes:

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

The complete page-by-page interaction reference is [`UI_REFERENCE.md`](UI_REFERENCE.md).

## Primary responsibilities

| Surface | Responsibility |
|---|---|
| Startup | Decide onboarding vs unlock. |
| Onboarding | Create local vault and optional recovery material. |
| Unlock | Master/recovery/optional biometric convenience unlock. |
| Vault | Search/filter/sort/list, navigation, lock, BMC entry. |
| Item Editor | Item CRUD, TOTP code generation/setup-URI text interoperability, secure note, custom fields, attachments. |
| Generator | Password/passphrase generation. |
| Generator Defaults | Persist generator defaults. |
| Audit | Local vault-content security findings. |
| Trash | Restore/permanent-delete/empty-trash workflows. |
| Settings | Security/privacy/backup/appearance/language/storage/support controls. |
| Security Info | User-facing threat/privacy/audit-status disclosure. |
| Transfer | CSV import/plaintext export. |
| About | Version/legal/repository/support/BMC/audit status. |
| Developer | Redacted developer diagnostics/information. |

The TOTP Item Editor panel includes manual code refresh/copy, a masked transient setup-URI import field, **Import URI**, and **Copy setup URI**. The setup-URI field is cleared after import attempts and when the editor clears owned sensitive state.

---

# 11. First launch and vault creation

On a fresh installation:

1. Startup checks for an existing local vault.
2. No vault routes to Onboarding.
3. User selects a strong master passphrase.
4. The onboarding strength rule must accept it.
5. CipherNest generates a random 256-bit DEK.
6. A master-passphrase-derived key wraps the DEK.
7. Optional independent recovery material can be created.
8. Recovery material is shown during setup and must be saved separately.
9. The created vault becomes the local encrypted store.

CipherNest does not store the master passphrase and does not run a server-side password-reset service.

---

# 12. Master passphrase and recovery model

## Master passphrase

Crypto-bound inputs are bounded to 12–4,096 characters.

The master passphrase:

- is not the per-record encryption key;
- derives a wrapping key using Argon2id;
- unwraps the random DEK;
- is used again for current-master re-authentication before sensitive operations.

## Recovery material

Optional recovery material:

- independently wraps the same random DEK;
- must be stored separately;
- can unlock the vault;
- is not server-managed;
- is not automatically accepted where a sensitive workflow specifically requires the current master passphrase.

Loss of every usable master/recovery path makes the local vault unrecoverable through CipherNest.

## Sensitive actions requiring current-master authorization

Examples include:

- plaintext CSV export;
- biometric enable/disable;
- master-passphrase change;
- manual permanent deletion/empty trash;
- full local-vault deletion;
- protected item operations where configured.

For a re-authentication-protected TOTP item, code generation and setup-URI import/copy remain behind the same item re-authentication gate.

---

# 13. Biometric convenience unlock

Current source supports optional convenience unlock on Android, iOS, and Mac Catalyst.

Design:

1. User is already authorized.
2. Current-master re-authentication is required to configure the feature.
3. OS biometric authentication succeeds.
4. CipherNest creates a fresh random secondary secret.
5. Platform secure storage protects the secondary secret.
6. The secondary secret protects an independent wrapper for the same DEK.

Security properties/limitations:

- master passphrase is not stored for biometric use;
- biometrics do not replace recovery limitations;
- a fresh app process requires master-auth state before convenience unlock can later be used;
- the configured master interval requires periodic master re-authentication;
- backup restore clears local biometric pairing;
- master-passphrase rotation ends the active master-auth session and requires the new master before convenience unlock resumes;
- Windows currently falls back to master-passphrase unlock;
- current source does not claim hardware-bound cryptographic linkage of every secure-storage retrieval to each biometric operation.

See `security/BIOMETRIC_UNLOCK.md`.

---

# 14. Cryptographic design

## Key hierarchy

```text
Master passphrase ─Argon2id─> KEK ─┐
                                   ├─ authenticated wrapped DEK
Recovery material ─Argon2id─> KEK ─┤
                                   ├─ random 256-bit vault DEK
Secondary secret ─Argon2id─> KEK ──┘

Vault DEK ─AES-256-GCM─> records / attachment chunks
Backup passphrase ─Argon2id─> backup key ─AES-GCM─> backup chunks
```

## Current primitives

- Argon2id for passphrase-based key derivation/wrapping;
- AES-256-GCM for authenticated encryption;
- 32-byte keys;
- 12-byte GCM nonces;
- 16-byte GCM authentication tags;
- unique nonce generation per encrypted object/chunk;
- associated data binding record/chunk identity/context.

TOTP uses standard HMAC-based RFC 6238 code generation through SHA-1/SHA-256/SHA-512. Setup-URI parsing/formatting does not alter the vault cryptographic format and does not perform provider/network cryptography.

## New-wrapper KDF defaults

| Setting | Value |
|---|---:|
| Memory | 64 MiB |
| Iterations | 3 |
| Parallelism | 1 |
| Salt | 16 bytes |
| Output | 32 bytes |

## Accepted KDF resource bounds

| Parameter | Minimum | Maximum |
|---|---:|---:|
| Salt | 16 bytes | 64 bytes |
| Memory | 16 MiB | 512 MiB |
| Iterations | 1 | 10 |
| Parallelism | 1 | 16 |

Untrusted metadata is bounded before expensive Argon2 work.

See `security/CRYPTOGRAPHIC_DESIGN.md`.

---

# 15. Vault-header format and compatibility

The local vault header stores version/KDF/wrapped-key metadata required to unlock the random DEK.

Current compatibility:

- historical document version 1 remains readable only with its exact historical schema;
- version 2 is the current write schema;
- future/unknown versions are rejected;
- undocumented v1/v2 hybrid shapes are rejected;
- current mutations deliberately upgrade valid v1 metadata to v2 output.

Parser safety:

- maximum 64 KiB UTF-8;
- maximum JSON depth 16;
- exact case-sensitive root/wrapped-key/KDF property sets;
- duplicate/unknown/missing/wrong-kind/case-variant properties rejected;
- strict validation occurs before typed deserialization/wrapped-key unwrap;
- replacement databases pass the same header validation before active replacement.

See `formats/VAULT_HEADER.md`.

---

# 16. Vault item model and item types

Current persisted enum values:

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

Numeric values are compatibility-sensitive because the encrypted serialized model writes enum values numerically.

## Core item fields

`VaultItem` includes:

- ID;
- type;
- title;
- username/identifier;
- secret;
- URL;
- notes;
- collection;
- tags;
- favorite state;
- custom fields;
- attachment references;
- TOTP algorithm/digits/period;
- created/modified timestamps;
- optional last-accessed timestamp;
- optional review date;
- optional deleted timestamp;
- per-item re-authentication requirement.

`Normalize(...)` trims/canonicalizes appropriate fields, removes empty tags, de-duplicates/sorts tags case-insensitively, and updates modification time.

For TOTP items, the Base32 seed and algorithm/digits/period are persisted in the encrypted item payload. Generated codes and pasted/formatted setup-URI text are **not** separate persisted item fields.

See `formats/VAULT_RECORDS.md` and `API_REFERENCE.md`.

---

# 17. Validation and resource ceilings

Defensive ceilings reduce accidental or hostile CPU/memory/disk/archive/UI work.

## Core storage ceilings

| Resource | Maximum/rule |
|---|---|
| Vault header | 64 KiB UTF-8; JSON depth 16 |
| Decrypted/serialized item JSON | 16 MiB |
| Stored encrypted item envelope | 24 MiB/row |
| Item rows | 100,000 |
| Aggregate encrypted item envelopes | 256 MiB |
| Referenced attachments | 10,000 |

## Item ceilings

| Resource | Maximum/rule |
|---|---|
| Title | required; 256 chars |
| Username | 2,048 chars |
| General secret | 100,000 chars |
| URL | 4,096 chars |
| Collection | 128 chars |
| Tags | 100, each 128 chars |
| Custom fields | 100 |
| Custom-field name | 128 chars |
| Custom-field value | 100,000 chars |
| Attachments/item | 25 |
| Combined item text/metadata | 2,000,000 chars |
| Secure note | 200,000 chars / 5,000 lines |
| Search query | 4,096 trimmed chars |

## TOTP ceilings

| Resource | Maximum/rule |
|---|---|
| Formatted Base32 seed | 4,096 chars |
| Normalized Base32 seed | 16–1,024 chars |
| Setup URI | 8,192 chars |
| Setup-URI query pairs | 16 |
| Setup-URI query-name length | 64 ASCII identifier chars |
| Setup-URI account name | 512 chars; `:` reserved/rejected inside component |
| Setup-URI issuer | 256 chars; `:` reserved/rejected inside component |
| Label separators | at most one issuer/account `:` separator |
| Empty query pair | rejected |
| Duplicate query key | rejected case-insensitively |
| HOTP/counter | rejected |

The URI parser also rejects user-info, custom ports, fragments, multiple label path segments, malformed percent encoding, unsupported settings, malformed Base32, inconsistent issuer metadata, and Unicode Control/Format display metadata.

## Attachment ceilings

- 100 MiB plaintext file;
- 256 KiB normal plaintext chunk;
- 16,384 encrypted chunk loop ceiling;
- 240 UTF-16 code units display name;
- 256 UTF-16 code units media type;
- 512 KiB decrypted text-preview bytes;
- 20,000 displayed preview characters.

## Backup ceilings

- header: 16–16,384 bytes;
- JSON depth: 16;
- accepted chunk: 64 KiB–4 MiB;
- current export chunk: 1 MiB;
- max encrypted chunk indexes: 65,536;
- max ZIP entries: 10,001;
- max aggregate plaintext archive content: 1 GiB.

## Settings ceilings

- file: 64 KiB;
- actual read: 64 KiB + 1 sentinel;
- JSON depth: 16.

## CSV ceilings

- 256 columns;
- 100,000 data rows;
- 256 header characters;
- 1,000,000 field characters;
- 2,000,000 aggregate row characters;
- 20 retained visible warnings.

Authoritative numeric reference: `LIMITS_AND_DEFAULTS.md`.

---

# 18. SQLite schema, migrations, and replacement

Current SQLite schema version: `1`.

Tables:

```text
VaultHeader(
  Id INTEGER PRIMARY KEY CHECK(Id=1),
  HeaderJson TEXT NOT NULL
)

VaultItems(
  Id TEXT PRIMARY KEY,
  Envelope BLOB NOT NULL
)

AppSettings(
  Key TEXT PRIMARY KEY,
  Value TEXT NOT NULL
)

MigrationHistory(
  Version INTEGER PRIMARY KEY,
  AppliedUtc TEXT NOT NULL
)
```

Vault item fields remain inside authenticated encrypted item envelopes rather than plaintext SQL columns/FTS tables.

## Migration rules

- ordered transactional migrations;
- future unsupported schema rejection;
- required table/column shape validation after version resolution;
- forged current-version history cannot replace required schema objects;
- rollback failures must not hide the primary migration failure;
- released migration versions are append-only compatibility history.

## Replacement rules

Before active DB/WAL/SHM mutation, a candidate database must:

1. open read-only;
2. pass `PRAGMA quick_check` with `ok`;
3. match the exact supported schema version;
4. contain required table/column shapes;
5. contain a supported bounded strict vault header;
6. pass item-count/envelope-size/aggregate/ID checks.

Active DB/WAL/SHM components are staged into a unique recovery family. Rollback restores only components actually staged.

See `architecture/DATABASE.md`.

---

# 19. Session security and concurrency

CipherNest distinguishes authentication from possession of a long-lived raw key reference.

## Key leases

Key-using operations receive private 32-byte `VaultKeyLease` copies linked to:

- caller cancellation;
- current unlock-session cancellation.

Leases zero their owned buffers on disposal where practical.

## Lock behavior

Locking:

- removes/zeroes shared key state under synchronization;
- cancels the current unlock-session token;
- invalidates session-linked cancellable work;
- requests conditional clipboard cleanup through application policy where appropriate.

## Transition serialization

Vault creation, master/recovery unlock, secondary unlock, public lock, and full-vault deletion coordinate through a serialized transition gate so a late unlock cannot publish a new session after an already-requested lock.

## Destructive authorization

Full-vault deletion holds a live session key lease while waiting for the transition gate. An intervening lock/re-unlock invalidates that authorization instead of carrying stale current-master authentication into a different session.

## Attachment mutation serialization

Attachment add/remove/permanent-delete operations use a separate cancellable mutation gate so long file work does not block the security lock transition.

See `architecture/SESSION_AND_CONCURRENCY.md` and `security/SESSION_SECURITY.md`.

---

# 20. Search, filters, sorting, and reminders

Search operates only over decrypted authenticated items while unlocked.

Current capabilities:

- local text search;
- favorites;
- collections;
- item-type filter;
- review-due filter;
- favorite/title ordering;
- recently used;
- recently modified;
- title sorting;
- incremental 50-item visual rendering;
- backup reminder;
- review reminder with configurable lead time.

`LastAccessedUtc` is stored inside the encrypted item payload. Opening an item records recent access without changing the user-visible modification timestamp.

---

# 21. Local security audit

The application audit can report:

- weak secrets;
- reused secrets;
- exact duplicate entries;
- missing titles;
- overdue review dates.

TOTP seeds are deliberately excluded from password weakness/reuse heuristics because they are authentication seeds rather than user-chosen passwords. Exact duplicate semantics can still include TOTP parameters.

The in-app audit is **not** an independent security audit of CipherNest itself.

---

# 22. TOTP

Current TOTP implementation:

- seed stored inside authenticated encrypted item payload;
- Base32 normalization/validation;
- SHA-1, SHA-256, SHA-512;
- 6 or 8 digits;
- 15–120-second period;
- default 30 seconds;
- RFC 6238 known-answer coverage;
- manual refresh;
- explicit code copy;
- generated codes are transient and never saved as item fields;
- decoded/hash/counter buffers are zeroed where practical;
- validity-window arithmetic safely clamps at `DateTimeOffset.MaxValue`;
- bounded local TOTP-only `otpauth://totp/...` text import;
- canonical local setup-URI formatting/copy;
- setup-URI copy through the existing timed secret-clipboard service;
- dedicated setup-URI import text cleared after import attempts and Item Editor sensitive-state cleanup.

## TOTP setup-URI behavior

The Application contract is `ITotpUriCodec`, implemented by Infrastructure `TotpUriCodec`.

Import maps:

- `secret=` -> encrypted TOTP Secret field;
- account label -> Username/identifier;
- issuer -> Title when available;
- algorithm/digits/period -> existing TOTP settings.

Parser protections include:

- absolute `otpauth://totp/...` only;
- URI/query/display-metadata ceilings;
- at most one issuer/account label separator;
- `:` rejected inside issuer/account components to avoid format/parse ambiguity;
- empty query pairs rejected;
- duplicate query names rejected case-insensitively;
- malformed percent encodings rejected, including otherwise ignored unknown parameters;
- HOTP/counter rejected;
- unsupported algorithm/digits/period rejected;
- invalid Base32 seed rejected;
- issuer label/query mismatch rejected;
- Unicode Control/Format display metadata rejected.

Formatting validates the same component/seed/settings policy, emits a canonical URI, and rejects encoded output above the URI ceiling.

A setup URI normally contains the long-lived seed. It must be protected like the seed itself and has a materially longer compromise lifetime than one generated code.

Not implemented:

- QR scanning/rendering;
- camera enrollment;
- HOTP/counter support;
- provider/network verification/enrollment;
- browser/application autofill;
- background refresh timer.

Input bounds:

- formatted seed: up to 4,096 chars;
- normalized seed: 16–1,024 chars;
- setup URI: up to 8,192 chars;
- setup-URI query pairs: up to 16.

See `security/TOTP.md`, `API_REFERENCE.md`, `LIMITS_AND_DEFAULTS.md`, and `verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md`.

---

# 23. Password/passphrase generator

Password generation uses the cryptographic random-number generator.

Supported options:

- password/passphrase mode;
- password length 8–256;
- uppercase;
- lowercase;
- digits;
- symbols;
- ambiguous-character exclusion;
- passphrase word count 6–16.

Memorable passphrases use a validated local list of exactly 256 unique lowercase words and default to eight words.

Displayed entropy/strength guidance applies to the randomly generated output and is not a proof against a particular attacker. Editing generated output can reduce the stated selection entropy.

See `security/PASSPHRASE_GENERATOR.md`.

---

# 24. Secure notes

CipherNest uses a deliberately small Markdown-like subset rather than rendering arbitrary HTML.

Supported concepts:

- headings;
- paragraphs;
- bullets;
- checklists;
- fenced code.

Raw HTML is neutralized. Shared limits:

- 200,000 characters;
- 5,000 lines.

The same policy applies across storage/import/editor/preview/checklist operations so a path cannot persist a note that another path later rejects solely because of size.

See `security/SECURE_NOTES.md`.

---

# 25. Encrypted attachments

Attachments are stored separately as authenticated encrypted `.cna` files referenced from encrypted item payloads.

## Storage-name rules

Opaque encrypted storage names are exactly:

```text
<32-character non-empty GUID-N>.cna
```

Total length: 36 characters.

Path separators are rejected before filesystem access. Accepted case variants normalize to canonical lower-case names, and storage identity is bound to the actual attachment ID.

## Metadata rules

Display/media metadata uses rune-aware validation:

- display name max 240 UTF-16 code units;
- media type max 256 UTF-16 code units;
- malformed UTF-16 rejected;
- Unicode Control/Format runes rejected, including supplementary-plane Format code points;
- missing media type defaults to `application/octet-stream`.

## Streaming container

- authenticated chunks;
- item/attachment/chunk identity in associated data;
- collision-resistant `CreateNew` staging;
- final overwrite refused;
- reusable plaintext chunk buffers zeroed where practical;
- tamper/truncation/trailing-data/chunk-count checks.

## Text preview

Small supported UTF-8 TXT/Markdown/CSV/JSON/LOG-family content can be previewed in bounded memory without intentionally creating a plaintext preview file.

## Plaintext export

Explicit export:

1. warns that plaintext leaves the vault boundary;
2. creates a unique temporary app-cache file for OS sharing;
3. attempts cleanup after share returns;
4. reports cleanup failure without exposing sensitive paths.

CipherNest cannot guarantee deletion from OS caches, share providers, destination apps, snapshots, backups, antivirus/indexers, or physical-media remnants.

See `formats/ATTACHMENTS.md`.

---

# 26. Encrypted backup and restore

Encrypted `.cnbak` backup is the recommended transfer/recovery path.

## Backup creation

- separate backup passphrase;
- vault locks before consistent snapshot;
- database snapshot plus encrypted attachment containers;
- bounded ZIP/archive content;
- encrypted/authenticated backup chunks;
- unsafe destinations that resolve to active database/WAL/SHM/recovery/attachment paths are refused;
- unique sibling staging prevents accidental overwrite/reuse.

## Backup format

Current format:

```text
version: 2
magic: CNBK0002
```

Header safety:

- 16–16,384 bytes;
- depth 16;
- exact case-sensitive root/KDF schema;
- duplicate/unknown/missing/case-variant/wrong-type properties rejected before typed deserialization/Argon2.

## Archive safety

- max 10,001 entries;
- max 1 GiB aggregate plaintext archive content;
- expected path layout only;
- duplicate normalized paths rejected;
- encrypted attachment entry sizes constrained to the implemented container envelope;
- actual extracted bytes must exactly equal declared uncompressed entry lengths.

## Restore ordering

1. authenticate/validate backup framing/header/resources;
2. decrypt to isolated staging;
3. validate archive paths/counts/sizes;
4. stage replacement database;
5. run SQLite/schema/header/item/resource validation;
6. only then mutate active state;
7. use uncancelled recovery token for rollback once active mutation begins;
8. clear local biometric pairing after success.

A failed restore must not silently become the active vault.

See `formats/ENCRYPTED_BACKUP.md` and `operations/BACKUP_RECOVERY_RUNBOOK.md`.

---

# 27. CSV import and plaintext export

CSV is interoperability, not the preferred secure-transfer mechanism.

## Import

Explicit mapping targets include:

- Title;
- Username;
- Secret;
- URL;
- Notes;
- Tags;
- Collection;
- Type.

The parser is bounded for columns/rows/fields/aggregate row work and treats header metadata more strictly than arbitrary payload fields. Header names must be non-empty, unique case-insensitively, bounded, and free of Unicode Control/Format characters.

Importing a plaintext source CSV does not remove/encrypt that external source file.

Dedicated TOTP setup-URI import is a separate single-item Item Editor workflow. Generic CSV must not be advertised as a dedicated authenticator migration format.

## Export

Plaintext CSV export requires:

- exact phrase `EXPORT PLAINTEXT`;
- current-master re-authentication;
- explicit warning/confirmation.

Export columns currently include:

```text
Title
Type
Username
Secret
URL
Notes
Tags
Collection
Favorite
ReviewAfterUtc
```

Attachments are not included.

Temporary plaintext staging is removed best-effort in `finally`, but the OS/receiving app can retain copies.

See `formats/CSV_TRANSFER.md`.

---

# 28. Clipboard and plaintext lifecycle

Explicit copy actions exist for:

- username;
- primary secret;
- secret custom field;
- TOTP code;
- TOTP setup URI.

After a secret copy, delayed cleanup tracks only a SHA-256 fingerprint, not the copied plaintext string. Current clipboard content is hashed and compared in fixed time; CipherNest clears only if the current clipboard still matches its previous copy, preserving unrelated content copied later.

A TOTP setup URI normally embeds the long-lived seed. Copying that URI has greater long-term exposure than copying one short-lived generated code even though both use the same conditional clipboard service.

Clipboard limits:

- OS history may retain content;
- clipboard synchronization may retain content;
- other applications/input methods/accessibility services can observe content;
- screenshots/cameras remain external;
- the destination authenticator/app retains pasted data according to its own behavior;
- the fingerprint itself is not a password-storage primitive.

See `security/DATA_LIFECYCLE.md`, `security/SESSION_SECURITY.md`, and `security/TOTP.md`.

---

# 29. Trash, permanent deletion, and full-vault deletion

## Trash

- Move to Trash is reversible.
- Default retention: 30 days.
- Configurable: 1–365 days.
- Routine maintenance can remove expired trash records.

## Manual permanent deletion

Requires:

1. current-master re-authentication;
2. separate destructive confirmation.

The database record is removed before best-effort encrypted attachment cleanup so a database-delete failure does not intentionally leave a surviving record whose attachment files were already removed.

## Empty Trash

Also requires current-master re-authentication plus explicit destructive confirmation.

## Full vault deletion

Requires:

- exact phrase `DELETE MY VAULT`;
- current master passphrase;
- final confirmation;
- live-session authorization while waiting for the serialized transition gate.

After the destructive transition commits, database/recovery artifacts and encrypted attachment storage are both attempted even if one cleanup area fails.

This is logical application-managed deletion, not guaranteed physical sanitization.

---

# 30. Settings and configuration

Full authoritative settings/build reference: [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md).

## User preferences

| Preference | Default | Normalized range/values |
|---|---|---|
| Theme | System | System / Light / Dark |
| Language | System | System / English / Hindi |
| Lock timeout | 60 s | 5–3,600 s |
| Lock on background | true | boolean |
| Clipboard clear | 30 s | 5–300 s |
| Screenshot protection | true | boolean/platform-dependent |
| Biometric unlock | false | boolean + capability/security state |
| Reduced motion | false | boolean |
| Larger interface | false | boolean |
| Trash retention | 30 d | 1–365 d |
| Master re-auth interval | 24 h | 1–168 h |
| Backup reminder | 7 d | 1–365 d |
| Review reminders | true | boolean |
| Review lead | 7 d | 0–365 d |
| Password length | 20 | 8–256 |
| Passphrase words | 8 | 6–16 |

Settings JSON is non-secret configuration and is not a secure secret store.

## Settings-file robustness

- 64 KiB file ceiling;
- independent 64 KiB + 1 actual-read sentinel;
- maximum depth 16;
- malformed/invalid UTF-8/over-depth/oversized local settings fall back to normalized defaults;
- cancellation continues to propagate;
- output is checked against the same ceiling;
- saves use unique sibling staging.

---

# 31. Accessibility

Implemented source support includes:

- semantic names/descriptions;
- selected live/status semantics;
- dynamic larger-interface typography resources;
- reduced-motion preference state;
- responsive layouts;
- System/Light/Dark theme handling;
- wrapping vault actions for narrow/resizable windows.

The TOTP setup-URI field is masked and uses non-secret semantic description text; real URI/seed content must not be injected into accessibility metadata.

Release validation still requires representative testing with:

- TalkBack;
- VoiceOver;
- Narrator;
- keyboard-only desktop navigation;
- focus order;
- OS large text/scaling;
- contrast/readability;
- narrow/large/resizable windows;
- touch-target checks;
- TOTP URI warning/readability and field-clearing behavior.

See `ACCESSIBILITY.md`.

---

# 32. Localization

Current preference values:

```text
System
English
Hindi
```

Neutral English is the fallback catalog. A reviewed `hi-IN` satellite catalog exists for the currently resource-backed interface, including security-sensitive wording that was included in that migration/review.

Not every remaining UI literal has been migrated to resources. Therefore CipherNest does not claim a completely translated Hindi UI.

Language preference must not alter encrypted formats or stored vault semantics.

New TOTP URI warning/error strings must remain security-accurate if/when migrated into additional resource catalogs.

See `architecture/LOCALIZATION.md`.

---

# 33. Privacy-safe diagnostics

Current source does not enable a third-party analytics or crash-reporting provider.

`PrivacySafeExceptionReporter` records only sanitized event metadata such as:

- stable operation identifier;
- exception type;
- HResult;
- severity;
- fixed omission wording.

It intentionally does not record:

- raw exception message;
- raw stack trace;
- passphrases;
- recovery material;
- secondary secrets;
- DEKs/KEKs;
- decrypted vault items;
- TOTP seeds/codes/setup URIs;
- clipboard plaintext;
- private attachments;
- raw secret-bearing CSV rows;
- filesystem paths likely to identify user content.

Sensitive UI paths use fixed user-facing text plus the redacted reporter instead of surfacing raw exception context.

See `privacy/DIAGNOSTICS.md` and `PRIVACY.md`.

---

# 34. Branding and Buy Me a Coffee support

Current original project branding includes:

- app icon/adaptive vector sources;
- splash wordmark;
- `Made by the Sanskar` creator credit;
- monochrome source;
- dark-surface logo variant;
- BMC project-support SVG.

## BMC support surfaces

The optional support URL is:

`https://buymeacoffee.com/sanskarIN`

It is represented through:

- `.github/FUNDING.yml`;
- root README;
- `SUPPORT.md`;
- About;
- Settings highlighted BMC card with `bmc_support.svg`;
- Vault compact `☕ Support` entry.

Funding is voluntary and does not change feature access, privacy/security treatment, support priority, licensing, recovery behavior, or open-source rights.

## Funding-disabled build

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

This removes/hides guarded in-app funding UI while repository funding metadata remains separate.

Release owners must verify the exact target store/region policy before shipping an external funding CTA.

See `branding/ASSETS.md`.

---

# 35. Build prerequisites and commands

## SDK selection

`global.json`:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

Inspect:

```bash
dotnet --info
dotnet workload list
```

## Core verification

PowerShell:

```powershell
./scripts/verify-core.ps1
```

POSIX:

```bash
sh scripts/verify-core.sh
```

## Windows

```powershell
./scripts/verify-windows.ps1
```

## Android

```bash
sh scripts/verify-android.sh
```

## Apple

```bash
sh scripts/verify-apple.sh
```

## Shared build-quality policy

`Directory.Build.props` enables:

- latest language level for normal projects;
- nullable reference analysis;
- implicit usings;
- warnings as errors;
- latest analysis level;
- code-style enforcement;
- deterministic builds;
- CI build metadata when `CI=true`.

The MAUI App project deliberately uses `LangVersion=preview` for the current WinRT/AOT-safe CommunityToolkit MVVM partial observable-property syntax.

See `setup/BUILD.md` and `CONFIGURATION_REFERENCE.md`.

---

# 36. Package/dependency management

Package versions are centrally managed.

Dependency rules:

- keep direct/transitive package graph reviewable;
- do not ignore high-severity advisories without an explicit documented decision;
- update `THIRD_PARTY_NOTICES.md`/license review when package families change;
- retain dependency-review CI for pull requests;
- verify exact restored package/license/advisory state for the release candidate.

A previous SQLite native dependency advisory blocker led to current pins of Microsoft.Data.Sqlite `10.0.10` and SQLitePCLRaw.bundle_e_sqlite3 `2.1.12`.

The TOTP URI continuation intentionally added no external QR/camera/network/parser package.

---

# 37. Automated tests

The repository contains three automated test projects.

## Unit tests

Use for:

- pure policies;
- validators;
- cryptographic known-answer/tamper behavior;
- generators;
- parser/resource rules;
- TOTP code compatibility;
- TOTP setup-URI parse/format round trips and adversarial boundaries;
- settings normalization;
- other deterministic services.

`TotpUriCodecTests` covers canonical/default parsing, explicit/label issuer handling, format/parse round trips, exact/first-over limits, duplicate/empty/malformed query behavior, HOTP/counter rejection, invalid percent encoding including ignored unknown parameters, label-separator ambiguity, metadata restrictions, unsupported settings, invalid seeds, and encoded-output ceilings.

## Integration tests

Use real infrastructure for:

- SQLite;
- vault create/unlock/record round trips;
- migrations;
- backup/restore;
- attachments;
- session cancellation;
- database replacement/recovery;
- CSV import interactions;
- TOTP encrypted persistence;
- corruption/tamper/wrong-passphrase behavior.

## UI/source tests

Protect source/documentation/workflow invariants without requiring a running MAUI device, including:

- routes;
- semantic/source structure;
- privacy-safe error patterns;
- BMC funding surfaces/build switch;
- documentation presence/links/disclaimers;
- WinRT/AOT-safe ViewModel observable-property patterns;
- CI/workflow/script presence;
- TOTP URI local-only architecture/DI/source safety;
- sensitive TOTP URI field-clearing/copy-path behavior;
- security-sensitive source ordering/invariants.

Source tests are regression signals, not runtime platform proof.

---

# 38. Hosted CI and CodeQL baseline

The immutable implementation baseline immediately before the complete-documentation expansion is:

```text
commit: 8566980ff981b8b4072f9010ec7b7ba54aba051e
```

Observed exact-candidate evidence:

| Gate | Result |
|---|---|
| Unit tests | 346 passed |
| Integration tests | 98 passed |
| UI/source tests | 111 passed |
| Total | **555 passed, 0 failed, 0 skipped** |
| Core analyzer builds | Passed; zero build warnings/errors in the test builds |
| Core formatting | Passed |
| Windows default Release | Passed |
| Windows funding-disabled Release | Passed |
| Android Release | Passed |
| iOS simulator Release | Passed |
| Mac Catalyst Release | Passed |
| CodeQL v4 | Passed |

Run identifiers:

```text
CipherNest CI: 31937127961
CodeQL:       31937127900
```

The CodeQL path built analyzable core code and the MAUI application path before analysis.

The hosted Apple line uses an explicitly compatible recorded pairing:

```text
runner: macos-26
.NET SDK: 10.0.302
Xcode: 26.5
workload set: 10.0.300.3
iOS RID: iossimulator-arm64
Mac Catalyst RID: maccatalyst-arm64
```

The August 18 TOTP setup-URI implementation/documentation commits are later than that immutable baseline. They require their own exact-head configured CI/CodeQL evidence before the newer head can be described as an exact-head verified release candidate.

See `verification/CI_GATES.md`, `verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md`, and the verification history.

---

# 39. Security/threat-model summary

## Assets

Protected/sensitive assets include:

- vault data;
- random vault DEK;
- key-lease copies while unlocked;
- master passphrase during entry/derivation;
- recovery material;
- biometric secondary secret;
- TOTP Base32 seeds;
- generated TOTP codes;
- TOTP setup URIs containing seeds;
- encrypted backups;
- encrypted attachments;
- plaintext import data before encryption;
- in-memory previews;
- decrypted values displayed/copied/exported by explicit user action.

## Strongest intended locked-state protection

A copied locked database/attachment store does not contain plaintext item data; the attacker must obtain/derive a valid path to the DEK. Argon2id increases offline guessing cost, but protection still depends substantially on passphrase strength and the recorded KDF parameters.

## TOTP setup-URI trust boundary

The URI parser validates **structure and bounds**, not issuer identity or provider enrollment. A malicious source can provide a structurally valid URI containing attacker-chosen metadata/seed. CipherNest cannot infer that the URI belongs to the intended account. Users must review imported account/issuer/settings before saving.

The parser is local-only and does not make HTTP/provider/camera/QR calls.

## Partial mitigations

CipherNest partially mitigates, but cannot eliminate:

- unlocked-device theft;
- shoulder surfing/screen capture;
- clipboard exposure;
- interactive brute force;
- malicious local apps;
- weak chosen passphrases;
- malicious local import/backup/TOTP-URI files/text;
- biometric subsystem/secure-storage weaknesses;
- logical deletion remnants;
- supply-chain compromise.

## Cannot protect against

- rooted/jailbroken/privileged OS compromise;
- kernel/hypervisor compromise;
- hardware keyloggers/hostile firmware;
- process injection/attacker-controlled user session;
- user-intentional plaintext/seed/setup-URI sharing;
- copies retained by OS/share/clipboard/backup/indexer/destination software;
- guaranteed GC-string erasure;
- guaranteed physical flash sanitization;
- loss of every master/recovery path.

See `security/THREAT_MODEL.md`.

---

# 40. Data lifecycle

Sensitive plaintext can exist transiently:

- while typed into credential fields;
- after authenticated record decryption;
- in ViewModel/UI strings;
- in TOTP setup-URI import/format strings;
- in generated TOTP codes;
- in safe note/text previews;
- in explicit clipboard content;
- in explicit plaintext CSV export;
- in explicit attachment-export temporary files;
- inside destination applications/OS services after a user-requested copy/share.

CipherNest reduces lifetime where practical by:

- zeroing owned byte arrays;
- clearing sensitive ViewModel fields on page disappearance;
- clearing the TOTP setup-URI entry after import attempts and Item Editor sensitive-state cleanup;
- clearing bound credential fields before longer operations where practical;
- using key leases and session cancellation;
- deleting app-controlled temp files best-effort;
- storing only clipboard fingerprints in delayed state;
- keeping diagnostics redacted.

These controls do not provide deterministic erasure of .NET strings, OS buffers, other apps, clipboard history/synchronization, filesystem snapshots, backups, or physical storage remnants.

See `security/DATA_LIFECYCLE.md` and `security/TOTP.md`.

---

# 41. Format/version compatibility

Current version surfaces are independent:

| Surface | Current version/value |
|---|---|
| Product | `0.1.0` |
| Database schema | `1` |
| Core crypto envelope | `1` |
| Vault-header document | current 2; minimum supported 1 |
| Attachment container | magic `CNAT0001` |
| Backup container | version 2 / `CNBK0002` |
| TOTP setup URI | external text interoperability; no CipherNest persisted-format version added |

Compatibility rule: never silently change an incompatible persisted structure under an existing version identifier.

A persisted format/schema change requires:

- explicit version/migration plan;
- known-answer/round-trip/tamper/wrong-key tests;
- old-data compatibility tests;
- restore/backup implications;
- threat-model review;
- documentation updates;
- release notes/checklist updates.

A TOTP URI parser/formatter behavior change requires bounded interoperability tests and documentation/release-claim review even though it does not change the encrypted vault schema.

Canonical format docs:

- `formats/VAULT_HEADER.md`
- `formats/VAULT_RECORDS.md`
- `formats/ATTACHMENTS.md`
- `formats/ENCRYPTED_BACKUP.md`
- `formats/CSV_TRANSFER.md`
- `security/TOTP.md` for the external TOTP setup-URI text boundary.

---

# 42. Release and packaging

A successful developer build is not a release.

Release candidate work includes:

1. freeze one immutable candidate SHA;
2. restore/build/tests/formatting;
3. platform compile gates;
4. CodeQL;
5. dependency/advisory review;
6. third-party-license review;
7. target-device manual validation;
8. TOTP setup-URI representative compatibility validation with synthetic seeds;
9. accessibility/localization/responsive validation;
10. backup/restore/recovery/compatibility validation;
11. signing/provisioning/notarization;
12. package identity/version/icon/splash/permissions review;
13. store privacy declarations;
14. target store/region funding-link policy decision;
15. documentation freeze against the exact candidate;
16. release notes/tag/checksums/provenance where practical.

Signing keys, certificates, store API tokens, private keys, passwords, and other release secrets must remain outside Git history.

See `releases/RELEASE_PROCESS.md`, `releases/PACKAGING.md`, `releases/REPRODUCIBLE_BUILDS.md`, and `RELEASE_CHECKLIST.md`.

---

# 43. Store/distribution policy

Store/distribution owners must validate current rules for:

- external funding/payment links;
- privacy declarations;
- encryption/export-control requirements where applicable;
- permissions/capabilities;
- biometric disclosure;
- TOTP setup-URI claims/screenshots;
- data collection statements;
- age/content classifications where applicable;
- signing/notarization/package identity.

If a particular store/region cannot ship the external BMC CTA, build the app with:

```text
CipherNestEnableFundingLink=false
```

and record that property in provenance.

Store wording may accurately describe bounded local TOTP text-URI import/copy only after the exact packaged candidate is verified. It must not imply QR/camera, HOTP, automatic provider enrollment, universal authenticator compatibility, or clipboard-history deletion.

See `releases/STORE_LISTING_GUIDE.md`.

---

# 44. Security-response and recovery operations

## Security reports

Never request that a user publicly provide:

- real vault database;
- master passphrase;
- backup passphrase;
- recovery material;
- secondary secret;
- cryptographic keys;
- TOTP seed/code/setup URI;
- decrypted backup;
- private attachments;
- plaintext secret CSV;
- signing/store secrets.

Use synthetic reproduction data and the process in `SECURITY.md` / `operations/SECURITY_RESPONSE.md`.

## Recovery operations

Use `operations/BACKUP_RECOVERY_RUNBOOK.md` for:

- safe backup checks;
- restore rehearsals;
- interruption/failure handling;
- active-vault preservation expectations;
- synthetic/disposable validation data.

TOTP interoperability testing must also use synthetic seeds and accounts rather than production enrollment secrets.

---

# 45. Support and troubleshooting

Support entry points:

```text
Business: sanskarin@outlook.in
Support:  supportramsandesh@gmail.com
Repository: https://github.com/sanskarIN/CipherNest
```

Optional development support:

```text
https://buymeacoffee.com/sanskarIN
```

Before requesting help, consult:

- `QUICK_START.md`;
- `USER_GUIDE.md`;
- `FAQ.md`;
- `TROUBLESHOOTING.md`;
- `security/TOTP.md` for TOTP/setup-URI behavior;
- `operations/BACKUP_RECOVERY_RUNBOOK.md`;
- `setup/BUILD.md`.

A useful non-sensitive support report includes:

- app/source version or commit;
- platform/OS version;
- .NET SDK/workload details for build issues;
- fixed/redacted error text;
- synthetic reproduction steps;
- whether the vault still unlocks;
- whether a separately verified encrypted backup exists;
- approximate non-sensitive file counts/sizes where relevant.

Never include a real TOTP setup URI in a support ticket because it normally contains the long-lived seed.

See `SUPPORT.md`.

---

# 46. Contribution and code-review rules

Before merging a change, confirm:

- dependency direction remains intact;
- the right test layer covers the change;
- untrusted input is bounded before expensive work;
- security-sensitive text formats reject ambiguity and malformed encodings;
- cancellation cannot corrupt committed state;
- required rollback cannot be cancelled after a destructive commit point;
- cleanup failures do not hide the primary failure;
- no raw secret/path exception surface was introduced;
- no unintended plaintext index/cache was introduced;
- format/schema compatibility is explicit;
- TOTP setup URIs remain local-only, secret-bearing, and non-persisted as a second field;
- MAUI ViewModel observable properties remain WinRT/AOT-safe;
- build/toolchain changes are verified and documented;
- security/privacy docs reflect attack-surface changes;
- user docs reflect behavioral changes;
- release checklist/test plan include new gates;
- deferred features are not accidentally advertised as complete.

Prefer small focused commits for security-sensitive work:

1. policy/contract;
2. implementation;
3. tests;
4. documentation/release gates;
5. progress ledger.

See `CONTRIBUTING.md`, `DEVELOPER_GUIDE.md`, and `MAINTAINER_GUIDE.md`.

---

# 47. Documentation governance

Documentation must follow current source, not desired future features.

Required rules:

1. Do not call configured CI “passing” without exact-run evidence.
2. Preserve immutable commit/run identifiers with hosted evidence.
3. Never claim an independent security audit unless one actually occurred.
4. Keep managed-memory/plaintext/export/platform limitations visible.
5. Update format/security docs when persistence/crypto/session/interoperability behavior changes.
6. Use synthetic/demo data only in docs/screenshots/examples.
7. Never commit passphrases, recovery material, vault contents, TOTP seeds/setup URIs, signing/store secrets, or private diagnostic artifacts.
8. Keep public application metadata centralized where code consumes it.
9. Do not rewrite historical verification files to pretend they describe later commits.
10. Add/update documentation regression tests when new canonical entry points are introduced.

See `DOCUMENTATION_MAINTENANCE.md`.

---

# 48. Known limitations and external validation gates

Even with a fully green hosted implementation baseline, repository-only automation cannot prove:

- full Android biometric runtime matrix;
- iOS/Mac Catalyst Face ID/Touch ID runtime matrix;
- secure-storage loss/enrollment changes;
- clipboard history/sync/cleanup on every OS, including copied TOTP setup URIs;
- representative third-party authenticator acceptance of every supported TOTP URI combination;
- provider-specific URI quirks;
- background/sleep/resume lifecycle timing;
- screenshot/app-switcher privacy on representative targets;
- OS share-sheet plaintext retention/cleanup;
- accessibility behavior with real assistive technology;
- every responsive layout/scaling combination;
- device/filesystem stress/interleaving behavior;
- historical release migration/backup compatibility across all future versions;
- signing/provisioning/notarization;
- store privacy/policy approval;
- exact release package vulnerability/license state before restore/review;
- independent professional cryptographic/security review.

These are release gates, not hidden completion claims.

---

# 49. Future-version roadmap

High-level future areas include:

- optional carefully designed sync/account architecture;
- collaboration/shared-vault protocol work;
- browser/application autofill;
- Windows Hello convenience unlock;
- TOTP QR/camera scanning/rendering with separate privacy/security review;
- HOTP interoperability if a reviewed product model is added;
- provider enrollment/autofill integration;
- richer document preview/scanning with separate attack-surface review;
- complete localization migration/review;
- additional languages;
- improved performance/large-vault observation;
- independent professional review and remediation;
- evidence-backed signed releases.

Bounded TOTP `otpauth://totp/...` **text** import and canonical formatting/copy have moved out of the future-feature list into implemented source plus target-validation work.

The ordered source/release roadmap is in `NEXT_STEPS.md`.

---

# 50. Quick checklists

## User safety checklist

- [ ] Use a long unique master passphrase.
- [ ] Save recovery material separately if enabled.
- [ ] Create encrypted backups.
- [ ] Store backup passphrase separately.
- [ ] Test restore with controlled/disposable data.
- [ ] Lock the vault when leaving the device.
- [ ] Treat clipboard/export/share as plaintext exposure.
- [ ] Treat TOTP setup URIs like long-lived authentication seeds.
- [ ] Review issuer/account/settings before saving an imported TOTP URI.
- [ ] Do not send real vault/TOTP secrets to support/public issues.
- [ ] Understand that CipherNest has not completed an independent professional security audit.

## Developer checklist

- [ ] Run core verification.
- [ ] Run affected platform verification.
- [ ] Add the right unit/integration/source tests.
- [ ] Preserve security/resource bounds.
- [ ] Reject parser ambiguity/malformed encodings before sensitive use.
- [ ] Preserve version compatibility.
- [ ] Avoid raw exception/secret logging.
- [ ] Update documentation.
- [ ] Keep deferred features unclaimed.

## Release checklist summary

- [ ] Freeze exact SHA.
- [ ] Core tests/format green.
- [ ] Windows/Android/Apple compile gates green.
- [ ] CodeQL reviewed.
- [ ] Dependency review complete.
- [ ] Device/simulator matrix complete.
- [ ] TOTP setup-URI compatibility/clipboard checks complete with synthetic seeds.
- [ ] Accessibility/localization/responsive checks complete.
- [ ] Backup/restore/recovery checks complete.
- [ ] Signing/notarization complete.
- [ ] Store/privacy/funding policy checked.
- [ ] Documentation frozen to exact candidate.
- [ ] Independent audit status stated accurately.

---

# 51. Canonical documentation map

## Start here

- [`QUICK_START.md`](QUICK_START.md)
- [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md)
- [`UI_REFERENCE.md`](UI_REFERENCE.md)
- [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md)
- [`USER_GUIDE.md`](USER_GUIDE.md)
- [`FAQ.md`](FAQ.md)

## Developer/maintainer

- [`DEVELOPER_GUIDE.md`](DEVELOPER_GUIDE.md)
- [`MAINTAINER_GUIDE.md`](MAINTAINER_GUIDE.md)
- [`API_REFERENCE.md`](API_REFERENCE.md)
- [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md)
- [`PROJECT_GLOSSARY.md`](PROJECT_GLOSSARY.md)
- [`DOCUMENTATION_MAINTENANCE.md`](DOCUMENTATION_MAINTENANCE.md)

## Architecture

- [`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md)
- [`architecture/DEPENDENCY_MAP.md`](architecture/DEPENDENCY_MAP.md)
- [`architecture/DATA_FLOW.md`](architecture/DATA_FLOW.md)
- [`architecture/SESSION_AND_CONCURRENCY.md`](architecture/SESSION_AND_CONCURRENCY.md)
- [`architecture/DATABASE.md`](architecture/DATABASE.md)
- [`architecture/LOCALIZATION.md`](architecture/LOCALIZATION.md)

## Security/privacy

- [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md)
- [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md)
- [`security/SESSION_SECURITY.md`](security/SESSION_SECURITY.md)
- [`security/DATA_LIFECYCLE.md`](security/DATA_LIFECYCLE.md)
- [`security/BIOMETRIC_UNLOCK.md`](security/BIOMETRIC_UNLOCK.md)
- [`security/TOTP.md`](security/TOTP.md)
- [`security/SECURE_NOTES.md`](security/SECURE_NOTES.md)
- [`security/PASSPHRASE_GENERATOR.md`](security/PASSPHRASE_GENERATOR.md)
- [`privacy/DIAGNOSTICS.md`](privacy/DIAGNOSTICS.md)
- [`../SECURITY.md`](../SECURITY.md)
- [`../PRIVACY.md`](../PRIVACY.md)

## Formats

- [`formats/VAULT_HEADER.md`](formats/VAULT_HEADER.md)
- [`formats/VAULT_RECORDS.md`](formats/VAULT_RECORDS.md)
- [`formats/ATTACHMENTS.md`](formats/ATTACHMENTS.md)
- [`formats/ENCRYPTED_BACKUP.md`](formats/ENCRYPTED_BACKUP.md)
- [`formats/CSV_TRANSFER.md`](formats/CSV_TRANSFER.md)

## Build/test/release/operations

- [`setup/BUILD.md`](setup/BUILD.md)
- [`TEST_PLAN.md`](TEST_PLAN.md)
- [`TESTING_GUIDE.md`](TESTING_GUIDE.md)
- [`ACCESSIBILITY.md`](ACCESSIBILITY.md)
- [`verification/CI_GATES.md`](verification/CI_GATES.md)
- [`verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md`](verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md)
- [`RELEASE_CHECKLIST.md`](RELEASE_CHECKLIST.md)
- [`releases/RELEASE_PROCESS.md`](releases/RELEASE_PROCESS.md)
- [`releases/PACKAGING.md`](releases/PACKAGING.md)
- [`releases/REPRODUCIBLE_BUILDS.md`](releases/REPRODUCIBLE_BUILDS.md)
- [`releases/STORE_LISTING_GUIDE.md`](releases/STORE_LISTING_GUIDE.md)
- [`operations/BACKUP_RECOVERY_RUNBOOK.md`](operations/BACKUP_RECOVERY_RUNBOOK.md)
- [`operations/SECURITY_RESPONSE.md`](operations/SECURITY_RESPONSE.md)
- [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md)
- [`NEXT_STEPS.md`](NEXT_STEPS.md)

## Project/release state

- [`../README.md`](../README.md)
- [`../PROJECT_STATUS.md`](../PROJECT_STATUS.md)
- [`../CHANGELOG.md`](../CHANGELOG.md)
- [`../what_changed.md`](../what_changed.md)
- [`../SUPPORT.md`](../SUPPORT.md)
- [`../CONTRIBUTING.md`](../CONTRIBUTING.md)

---

# 52. Glossary

**DEK** — Data-encryption key. CipherNest uses a random 256-bit vault DEK to protect vault content.

**KEK** — Key-encryption/wrapping key derived from master/recovery/secondary credential material using Argon2id.

**Authenticated encryption** — Encryption that also detects tampering. CipherNest uses AES-256-GCM.

**Associated data** — Context authenticated but not encrypted; used to bind identities such as row/item/attachment/chunk context.

**Vault header** — Versioned metadata containing wrapped-key/KDF information required to recover the random vault DEK.

**VaultKeyLease** — Short-lived private copy of the active DEK linked to session/caller cancellation and zeroed on disposal where practical.

**Recovery material** — Optional independent credential path that unwraps the same vault DEK; not server reset and not a substitute for every current-master authorization.

**Secondary secret** — Random secret used for optional biometric convenience wrapper; protected through platform secure storage where supported.

**TOTP** — Time-based one-time password generated locally from an encrypted Base32 seed plus algorithm/digits/period settings.

**`otpauth://totp/...` setup URI** — External secret-bearing text format used to transfer TOTP account/issuer/seed/settings. CipherNest supports bounded local TOTP-only text parsing/formatting; the URI is not a separate persisted vault field.

**`.cna`** — CipherNest encrypted attachment container.

**`.cnbak`** — CipherNest authenticated encrypted backup container.

**Plaintext boundary** — A deliberate operation where decrypted/secret-bearing data leaves encrypted storage, such as clipboard copy, TOTP setup-URI copy, attachment export, or CSV export.

**Source/UI test** — Automated repository/source-shape regression test that does not by itself prove physical-device runtime behavior.

**Exact-head evidence** — Test/build/security-analysis evidence tied to one immutable commit SHA. Later commits do not automatically inherit it.

---

## Final documentation note

The pre-documentation implementation baseline `8566980ff981b8b4072f9010ec7b7ba54aba051e` completed the configured hosted CI and CodeQL paths successfully with **555 passing tests** and successful Windows default/funding-disabled, Android, iOS simulator, and Mac Catalyst Release compilation. The August 18 TOTP setup-URI continuation occurs after that immutable baseline and has added/expanded source, tests, documentation, threat/release gates, and a dedicated verification record. The newer final head must execute its own configured CI/CodeQL gates before it is called an exact-head verified release candidate; historical evidence is not silently inherited.
