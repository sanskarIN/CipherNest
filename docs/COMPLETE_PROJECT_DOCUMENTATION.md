# CipherNest — Complete Project Documentation

This document is the consolidated reference for the current CipherNest source tree. It is intended to give users, contributors, maintainers, reviewers, and release engineers one coherent overview while linking to the deeper canonical documents for security-sensitive details.

CipherNest documentation describes implemented source behavior, not aspirational features. If this overview and a specialized document disagree, the current source plus the specialized document and tests take precedence. CipherNest has **not** completed an independent professional security audit. It must not be described as unhackable, military-grade, 100% secure, or capable of deterministic erasure of all managed-memory or operating-system plaintext remnants.

## 1. Project identity

- **Project:** CipherNest
- **Repository:** `https://github.com/sanskarIN/CipherNest`
- **Creator:** Sanskar
- **Primary implementation:** C# / .NET MAUI
- **License:** GPL-3.0-or-later
- **Design:** local-first encrypted vault
- **Business contact:** `sanskarin@outlook.in`
- **Support contact:** `supportramsandesh@gmail.com`
- **Optional development support:** `https://buymeacoffee.com/sanskarIN`

The optional funding link never changes feature access, security/privacy treatment, support priority, licensing, recovery behavior, or open-source rights. Distribution builds can hide the in-app funding CTA with the documented build property when a target store or policy requires it.

## 2. What CipherNest is

CipherNest is a local-first vault for passwords, credentials, identities, secure notes, custom secrets, encrypted attachments, collections, tags, reminders, and related encrypted metadata. It is designed so ordinary operation does not require a CipherNest account, application server, email address, phone number, or cloud synchronization service.

The current source emphasizes:

- local encrypted persistence;
- explicit master-passphrase and recovery authorization boundaries;
- authenticated encryption for records, wrapped keys, attachments, and backups;
- bounded parsers and storage operations;
- fail-closed session transitions;
- privacy-safe error reporting;
- guarded plaintext export and clipboard operations;
- documented platform limitations instead of unsupported security claims;
- automated regression coverage for security-sensitive source invariants.

Local RFC-compatible TOTP generation and a reviewed Hindi resource-backed catalog are now implemented. Cloud synchronization, collaboration, browser/application autofill, TOTP QR/`otpauth://` enrollment interoperability, Windows Hello convenience unlock, rich PDF/binary preview/scanning, and complete migration of the remaining user-facing UI into additional-language catalogs remain future-version work unless and until they are implemented, reviewed, tested, and documented.

## 3. Supported application targets

The MAUI application source targets Android, iOS, Mac Catalyst, and Windows. Hosted CI has compiled these target paths for the recorded baseline candidate, but target compilation is not equivalent to complete physical-device validation.

| Area | Current source position |
| --- | --- |
| Windows | MAUI desktop target; master-passphrase unlock remains the supported unlock path. |
| Android | MAUI target with biometric convenience-unlock implementation using the API-28 `BiometricPrompt` baseline. |
| iOS | MAUI target with platform biometric convenience-unlock integration. |
| Mac Catalyst | MAUI target with platform biometric convenience-unlock integration. |
| Linux | No shipping MAUI application target in the current solution. |

Biometric enrollment changes, lockout, secure-storage behavior, screenshots, clipboard/history, lifecycle callbacks, accessibility services, share sheets, packaging, signing, and store behavior still require target-device validation.

## 4. Repository layout

The solution follows a separated architecture so security-sensitive behavior can be tested outside the UI layer.

- `src/CipherNest.Domain` — domain models, enums, validation concepts, and value-level rules.
- `src/CipherNest.Application` — use-case contracts, application services, authorization/session policy, transfer/generator/audit orchestration, and application-facing interfaces.
- `src/CipherNest.Infrastructure` — cryptography, encrypted persistence, SQLite, file storage, attachment containers, backup/restore, settings persistence, platform-adjacent implementations, and concrete services.
- `src/CipherNest.Shared` — shared project metadata and cross-project constants.
- `src/CipherNest.App` — .NET MAUI application, pages, ViewModels, resources, converters, platform heads, branding, lifecycle wiring, and dependency injection.
- `tests/CipherNest.UnitTests` — isolated policy, validation, generator, parser, and service tests.
- `tests/CipherNest.IntegrationTests` — persistence, migration, backup, restore, attachment, lock/session, filesystem, and cross-service integration tests.
- `tests/CipherNest.UiTests` — source/UI contract tests, documentation gates, resource/branding checks, ViewModel behavior, and UI-adjacent regression coverage.
- `docs` — canonical project documentation.
- `scripts` and `build/scripts` — committed verification entry points.
- `.github/workflows` — CI, CodeQL, and dependency-review automation.

Dependency direction and ownership are described in `docs/architecture/ARCHITECTURE.md` and `docs/architecture/DEPENDENCY_MAP.md`.

## 5. Security architecture

### 5.1 Vault data-encryption key

CipherNest generates a random 256-bit vault data-encryption key. Vault data is encrypted with that key rather than using the master passphrase directly as a record-encryption key.

### 5.2 Master passphrase

The master passphrase is not stored. CipherNest derives a wrapping key using bounded Argon2id parameters and uses that key to unwrap the random vault key. New wrappers use the current documented Argon2id defaults while readers reject hostile or unsupported KDF metadata before expensive derivation.

Crypto-bound master/recovery/backup/secondary passphrase inputs are bounded to 12–4,096 characters so malformed input cannot trigger unbounded work.

### 5.3 Recovery material

Optional recovery material provides an independent wrapped-vault-key path. It is not an application-server reset mechanism and must be stored separately by the user. Recovery authorization does not automatically gain every privilege reserved for current-master re-authentication.

### 5.4 Biometric convenience unlock

Supported Android, iOS, and Mac Catalyst paths can protect a separately generated random secondary secret using platform security facilities. That secondary secret unwraps an additional copy of the vault key. A fresh process and configured intervals still require the master passphrase. Restore and master-passphrase rotation invalidate or reset convenience-unlock state according to current source behavior.

Windows intentionally falls back to master-passphrase unlock in the current release.

### 5.5 Authenticated encryption

CipherNest uses AES-256-GCM authenticated encryption with unique nonces and contextual associated data for encrypted objects such as records, wrapped keys, backup chunks, and attachment chunks. Detailed framing/version rules are in `docs/security/CRYPTOGRAPHIC_DESIGN.md` and the format documents.

### 5.6 Session key leases

Unlocked key-using operations receive private 32-byte vault-key lease copies linked to both caller cancellation and the current unlock-session cancellation token. Locking removes and zeroes the shared session key and cancels in-flight cancellable work. Lease buffers are zeroed on disposal where practical.

Unlock, recovery unlock, secondary unlock, public lock, and full-vault deletion transitions are serialized so a late unlock cannot republish a session after a requested lock. Destructive full-vault deletion also requires live authorization while waiting for the transition gate.

### 5.7 Managed-memory limitation

CipherNest clears owned byte arrays and sensitive ViewModel properties where practical, but .NET strings and operating-system/application copies cannot be deterministically erased. Documentation and UI must not claim otherwise.


### 5.8 Local TOTP generation

`OneTimePassword` vault items store a Base32 TOTP seed plus algorithm/digit/period settings inside the same authenticated encrypted item payload used by other vault fields. `TotpService` generates RFC 6238-compatible codes locally using HMAC-SHA-1, HMAC-SHA-256, or HMAC-SHA-512, with 6/8-digit output and bounded 15–120-second periods. Generated codes are transient presentation state and are not persisted.

Seed parsing is bounded before HMAC work, decoded seed/hash/counter buffers are zeroed where practical, and RFC 6238 known-answer vectors guard compatibility. The editor uses explicit manual refresh and explicit clipboard copy rather than a background timer. TOTP seeds are excluded from password-strength/reuse heuristics, while exact duplicate detection still includes the TOTP parameters. See `docs/security/TOTP.md`.

## 6. Vault records and item behavior

CipherNest supports the modeled vault item types and common metadata including collections, tags, favorites, review dates, encrypted custom fields, attachments, recent-access metadata, and trash state.

Record handling includes:

- authenticated row-ID binding;
- canonical lower-case GUID `D` identifiers for stored item rows;
- null-safe runtime validation;
- rejection of unknown item types and empty IDs;
- duplicate attachment-ID/storage-name detection;
- metadata control-character checks;
- aggregate text and serialized-record bounds;
- encrypted storage budgets before materialization where practical;
- encrypted last-accessed metadata rather than plaintext searchable indexes.

Local search and audit operations work over decrypted in-memory objects while unlocked. Large result sets render incrementally instead of adding all matches to the visual tree at once.

The canonical logical-record contract is `docs/formats/VAULT_RECORDS.md`.

## 7. Storage and SQLite

CipherNest persists encrypted records in SQLite with minimized plaintext metadata. Schema changes use an ordered migration runner. Migration completion validates required current schema objects and rejects forged current-version history that omits required objects.

Replacement databases are validated before active mutation. Validation includes SQLite integrity checks, exact supported schema version, required table/column shape, bounded vault header, canonical item IDs, and encrypted-record resource budgets.

Database replacement accounts for the main database plus WAL/SHM sidecars. Recovery staging uses unique names, and rollback restores only components actually moved so a partial staging failure does not incorrectly delete an unstaged sidecar.

See `docs/architecture/DATABASE.md`.

## 8. Implemented resource ceilings

CipherNest intentionally rejects extreme or malformed inputs before they can consume unbounded memory, CPU, archive entries, rows, or parser state. Important current safety ceilings include:

| Resource | Current ceiling / rule |
| --- | --- |
| Vault header | 64 KiB UTF-8 |
| Serialized/decrypted item JSON | 16 MiB |
| Stored encrypted item envelope | 24 MiB per record |
| Vault item rows | 100,000 |
| Aggregate encrypted record bytes | 256 MiB |
| Aggregate item text | 2,000,000 characters |
| Referenced attachments | 10,000 total |
| Attachments per item | 25 |
| Secure note | 200,000 characters / 5,000 lines |
| Search query | 4,096 trimmed characters |
| Attachment filename | 240 characters |
| Attachment media type | 256 characters |
| Backup archive | 1 GiB aggregate plaintext archive content |
| Backup ZIP entries | 10,001 maximum (`vault.db` plus attachment slots) |
| Settings JSON | 64 KiB; actual reads use a 64 KiB + 1 sentinel boundary; maximum nesting depth 16 |
| Passphrase input | 12–4,096 characters for crypto-bound passphrases |
| TOTP formatted seed | 4,096 characters maximum before normalization |
| TOTP normalized Base32 seed | 16–1,024 characters; SHA-1/SHA-256/SHA-512; 6/8 digits; 15–120 s period |

These are defensive ceilings, not recommended operating targets. The authoritative complete table is `docs/LIMITS_AND_DEFAULTS.md`.

## 9. Attachments

Attachments are encrypted as bounded streaming containers. The implementation uses opaque GUID-based `.cna` storage names, authenticated chunk framing, collision-resistant staging, and no-overwrite installation semantics.

Before filesystem access, storage names are validated as leaf GUID-based `.cna` names without separators and are bound to the expected attachment identifier. Metadata is normalized and bounded before encryption.

Small supported UTF-8 text-family attachments can be previewed in bounded memory. Other formats require explicit plaintext export. Plaintext export crosses the vault boundary and can leave copies in operating-system caches, share destinations, history, backups, or third-party applications.

See `docs/formats/ATTACHMENTS.md`.

## 10. Backup and restore

Encrypted `.cnbak` backup is the preferred transfer/recovery path. Backups include the encrypted database and encrypted attachment containers.

The backup implementation:

- validates container version, salt length, KDF bounds, and chunk metadata before Argon2 derivation;
- enforces a bounded plaintext archive and entry count;
- rejects duplicate normalized ZIP paths and unsupported nested/unexpected paths;
- checks attachment-entry sizes against the real encrypted attachment-container envelope;
- refuses destinations resolving to the live database, WAL/SHM/recovery files, or encrypted attachment directory;
- uses unique create-new encrypted staging;
- validates a staged database before active replacement;
- preserves rollback capability after active mutation begins;
- uses an uncancelled recovery token for rollback once destructive replacement has started;
- invalidates local biometric pairing after restore.

A failed restore must not silently replace the active vault. Operational recovery steps are in `docs/operations/BACKUP_RECOVERY_RUNBOOK.md`; format details are in `docs/formats/ENCRYPTED_BACKUP.md`.

## 11. CSV import/export

CSV exists for interoperability, not as the preferred secure transfer format.

Import uses explicit column mapping and bounded parsing. The parser limits logical rows, field sizes, column counts, row characters, and final-field column handling at newline/EOF. Import must not guess which column contains a secret without explicit mapping.

Plaintext CSV export requires current-master re-authentication and explicit confirmation. Attachments are not silently exported into CSV. Temporary plaintext staging is removed in `finally` where permitted, but operating-system or destination copies cannot be guaranteed erased.

See `docs/formats/CSV_TRANSFER.md`.

## 12. Clipboard policy

Copying usernames, primary secrets, and secret custom-field values is an explicit user action. Delayed cleanup retains a SHA-256 fingerprint rather than the plaintext secret, compares fingerprints in fixed time, and avoids clearing unrelated newer clipboard content.

Clipboard cleanup is best-effort because operating systems may maintain clipboard history or synchronize clipboard content. Lock-triggered cleanup and timer-triggered cleanup follow the same conditional policy where platform APIs permit it.

## 13. Secure notes and generators

Secure notes use a bounded Markdown-like subset with checklists and fenced code. Raw HTML is neutralized rather than rendered. The shared note policy prevents storage/import/editor paths from exceeding renderer bounds.

Password generation uses cryptographically secure randomness with configurable groups and ambiguous-character exclusion. Memorable passphrases use a validated local 256-word list with 6–16 word bounds and an eight-word default. Generator temporary arrays are cleared after constructing the returned managed string where practical.

See `docs/security/SECURE_NOTES.md` and `docs/security/PASSPHRASE_GENERATOR.md`.

## 14. Trash, deletion, and destructive operations

Items can move to trash and can be restored until retention cleanup or explicit permanent deletion. Manual permanent deletion and Empty Trash require current-master re-authentication plus destructive confirmation.

Permanent item deletion removes the authenticated database row before best-effort encrypted attachment cleanup so a failed database delete does not intentionally leave a surviving record whose encrypted files were already removed.

Full local-vault deletion attempts the database, sidecars/recovery artifacts, and encrypted attachment storage. This is logical application-managed deletion, not a claim of guaranteed physical-media erasure.

## 15. Settings, privacy, and diagnostics

Settings cover theme, language readiness, lock/privacy controls, reminders, biometrics, generator defaults, storage/cache inspection, backup/restore, import/export, audit, security/privacy information, About/legal/acknowledgements, passphrase rotation, and destructive local deletion.

Settings persistence rejects files already above 64 KiB and independently bounds the actual read to a fixed 64 KiB + 1 sentinel byte before bounded-memory JSON deserialization. JSON nesting is capped at 16; invalid UTF-8, over-depth, malformed, or unreadable non-secret settings fall back to defaults, while cancellation continues to propagate. Valid parses are normalized for enum/numeric bounds and safe generator defaults, UTF-8 BOM compatibility is preserved, serialized output is checked against the 64 KiB ceiling, and saves use unique sibling staging.

Central diagnostic reporting records sanitized operation/type/HResult-style metadata and intentionally excludes exception messages, stacks, vault contents, passphrases, recovery keys, plaintext secrets, TOTP seeds/codes, and full user file paths. No third-party analytics/crash service is enabled by the current source.

See `docs/privacy/DIAGNOSTICS.md` and `PRIVACY.md`.

## 16. Accessibility and localization

The application includes semantic labels/live regions, responsive layouts, dynamic larger-interface typography, reduced-motion preference state, and light/dark/system theme behavior.

Neutral English remains the fallback resource catalog. Persisted System/English/Hindi preferences are supported, and the reviewed `hi-IN` satellite catalog covers the currently resource-backed interface including security-critical local-only, audit-status, recovery-limitation, and language-status messages. CipherNest does not claim that every remaining literal UI string is translated; unmigrated strings may still appear in English.

Source semantics are not an accessibility certification. TalkBack, VoiceOver, Narrator, keyboard-only navigation, large text, focus, touch-target, and responsive-layout behavior require target-device testing.

See `docs/ACCESSIBILITY.md` and `docs/architecture/LOCALIZATION.md`.

## 17. Build prerequisites and commands

Use a current .NET 10 SDK with the .NET MAUI workload and the platform SDK/toolchain needed for the desired target.

Canonical build instructions: `docs/setup/BUILD.md`.

Committed verification entry points include:

```text
scripts/verify-core.ps1
scripts/verify-core.sh
scripts/verify-windows.ps1
scripts/verify-android.sh
scripts/verify-apple.sh
```

The optional in-app funding surface can be removed from a distribution build without source edits:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

The correct value must be chosen after checking the exact target store/region policy.

## 18. Automated testing and CI

The repository contains unit, integration, and UI/source test projects. Main GitHub CI is configured for core analyzer builds/tests/format verification plus Windows, Android, iOS, and Mac Catalyst Release compilation. Windows also compiles the funding-disabled configuration. CodeQL builds analyzable core code and the Android MAUI application path.

The recorded hosted baseline candidate `2327abba1646082a4d94a689d452b1116701cc0b` completed:

- 106 UnitTests;
- 60 IntegrationTests;
- 74 UiTests/source tests;
- 240 total passed, 0 failed, 0 skipped;
- core formatting verification;
- Windows default Release;
- Windows funding-disabled Release;
- Android `android-arm64` Release;
- iOS simulator `iossimulator-arm64` Release;
- Mac Catalyst `maccatalyst-arm64` Release;
- CodeQL v4.

Those results are historical evidence for that exact commit. Any later release candidate must rerun the gates. See `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md`, `docs/verification/CI_GATES.md`, and `docs/verification/POST_BASELINE_CHECKLIST_2026_08_13.md`.

## 19. Release process

A release candidate must reconcile source, tests, documentation, dependency state, packaging configuration, privacy/security claims, and store requirements against one immutable candidate.

Release work includes:

1. restore/build/test/format/CodeQL/dependency-review gates;
2. target-device manual validation;
3. dependency vulnerability/license review;
4. exact SDK/workload/toolchain recording;
5. signing/notarization from protected environments;
6. package-identity/version/icon/splash/privacy/permission validation;
7. store-policy review for optional external funding links;
8. documentation freeze against the exact candidate;
9. release notes, tag, checksums/provenance where practical;
10. preservation of signing keys, store credentials, and recovery secrets outside Git history.

See `docs/releases/RELEASE_PROCESS.md`, `docs/releases/PACKAGING.md`, `docs/releases/REPRODUCIBLE_BUILDS.md`, `docs/releases/STORE_LISTING_GUIDE.md`, and `docs/RELEASE_CHECKLIST.md`.

## 20. Security response and support operations

Security reports must be handled without asking users to upload vault contents, master passphrases, recovery keys, decrypted backups, or secret-bearing diagnostics. Use `SECURITY.md` and `docs/operations/SECURITY_RESPONSE.md`.

Backup/recovery troubleshooting must use synthetic or user-controlled safe evidence and the runbook in `docs/operations/BACKUP_RECOVERY_RUNBOOK.md`.

General support guidance is in `SUPPORT.md`. Common product/build/security questions are in `docs/FAQ.md`.

## 21. Current external validation gates

Even with passing hosted source gates, the following remain outside what repository-only automation can prove:

- complete Android biometric runtime matrix;
- iOS/Mac Catalyst Face ID/Touch ID runtime and secure-storage matrix;
- Windows/iOS/macOS/Android clipboard-history behavior;
- lifecycle/background/sleep/wake behavior on real targets;
- screenshot/app-switcher privacy behavior;
- share-sheet and temporary plaintext-remnant behavior;
- accessibility-service behavior;
- narrow/large/resizable layout behavior on representative devices;
- packaging signing/notarization and store review;
- store-specific funding-link policy;
- broader concurrency/stress/interleaving validation;
- independent professional security review.

These are release gates, not hidden claims of completion. `docs/NEXT_STEPS.md` is the ordered roadmap.

## 22. Canonical documentation map

Use this consolidated document for orientation, then the specialized sources for authoritative detail:

### User and project
- `README.md`
- `docs/README.md`
- `docs/USER_GUIDE.md`
- `docs/FAQ.md`
- `PROJECT_STATUS.md`
- `CHANGELOG.md`
- `what_changed.md`

### Developer and maintainer
- `docs/DEVELOPER_GUIDE.md`
- `docs/MAINTAINER_GUIDE.md`
- `docs/API_REFERENCE.md`
- `docs/LIMITS_AND_DEFAULTS.md`
- `docs/PROJECT_GLOSSARY.md`
- `docs/DOCUMENTATION_MAINTENANCE.md`

### Architecture
- `docs/architecture/ARCHITECTURE.md`
- `docs/architecture/DATABASE.md`
- `docs/architecture/DATA_FLOW.md`
- `docs/architecture/DEPENDENCY_MAP.md`
- `docs/architecture/SESSION_AND_CONCURRENCY.md`
- `docs/architecture/LOCALIZATION.md`

### Security/privacy
- `docs/security/THREAT_MODEL.md`
- `docs/security/CRYPTOGRAPHIC_DESIGN.md`
- `docs/security/SESSION_SECURITY.md`
- `docs/security/DATA_LIFECYCLE.md`
- `docs/security/BIOMETRIC_UNLOCK.md`
- `docs/security/SECURE_NOTES.md`
- `docs/security/PASSPHRASE_GENERATOR.md`
- `docs/privacy/DIAGNOSTICS.md`
- `SECURITY.md`
- `PRIVACY.md`

### Formats
- `docs/formats/VAULT_RECORDS.md`
- `docs/formats/ATTACHMENTS.md`
- `docs/formats/ENCRYPTED_BACKUP.md`
- `docs/formats/CSV_TRANSFER.md`

### Build/test/release/operations
- `docs/setup/BUILD.md`
- `docs/TEST_PLAN.md`
- `docs/TESTING_GUIDE.md`
- `docs/ACCESSIBILITY.md`
- `docs/verification/CI_GATES.md`
- `docs/RELEASE_CHECKLIST.md`
- `docs/releases/RELEASE_PROCESS.md`
- `docs/releases/PACKAGING.md`
- `docs/releases/REPRODUCIBLE_BUILDS.md`
- `docs/releases/STORE_LISTING_GUIDE.md`
- `docs/operations/BACKUP_RECOVERY_RUNBOOK.md`
- `docs/operations/SECURITY_RESPONSE.md`
- `docs/TROUBLESHOOTING.md`
- `docs/NEXT_STEPS.md`

## 23. Documentation change rule

Any source change affecting cryptography, authentication, authorization, persistence, migration, record/attachment/backup/CSV formats, resource limits, plaintext handling, clipboard behavior, diagnostics, lifecycle/session concurrency, biometric behavior, platform capabilities, legal/support metadata, build/release configuration, or security claims must update the corresponding specialized documentation in the same release work.

The documentation coverage source tests are intentionally part of the repository so deletion or unlinking of required documentation becomes a visible regression rather than silent documentation drift.
