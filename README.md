# CipherNest

<p align="center">
  <img src="src/CipherNest.App/Resources/Images/ciphernest_logo.svg" alt="CipherNest logo" width="150" />
</p>

<p align="center"><strong>Local-first encrypted vault for passwords, secure notes, identities, credentials, TOTP, and protected documents.</strong></p>

CipherNest is an open-source local-first vault built with C#, .NET 10, and .NET MAUI. Ordinary use does not require a CipherNest account, email address, phone number, application server, or cloud synchronization service.

> **Security status:** CipherNest has **not** completed an independent professional security audit. It uses established primitives and extensive automated hardening/tests, but must not be described as “unhackable”, “military-grade”, “100% secure”, capable of guaranteed managed-memory erasure, or capable of guaranteed physical-media sanitization.

## ☕ Support CipherNest development

<p align="center">
  <a href="https://buymeacoffee.com/sanskarIN" title="Support CipherNest on Buy Me a Coffee">
    <img src="src/CipherNest.App/Resources/Images/bmc_support.svg" alt="BMC — Support CipherNest" width="560" />
  </a>
</p>

<p align="center"><strong>Click the BMC badge above to open the CipherNest Buy Me a Coffee page.</strong></p>

Project support is optional. It does not change feature access, security/privacy treatment, support priority, licensing, recovery behavior, or open-source rights. Store/distribution builds can disable the in-app funding surface while repository funding metadata remains separate.

# Documentation

The complete documentation suite is indexed at **[`docs/README.md`](docs/README.md)**.

For a new reader, use these entry points:

- **[Quick Start](docs/QUICK_START.md)** — first launch, vault creation, items, TOTP, attachments, backup/restore, settings, and contributor bootstrap.
- **[Feature Matrix](docs/FEATURE_MATRIX.md)** — exhaustive implemented, platform-dependent, external-validation, and deferred feature status.
- **[UI & Navigation Reference](docs/UI_REFERENCE.md)** — every current route/page, major control, security gate, and navigation behavior.
- **[Configuration Reference](docs/CONFIGURATION_REFERENCE.md)** — application ID, target frameworks, build flags, packages, settings defaults/bounds, resource limits, and toolchain configuration.
- **[Complete Project Documentation](docs/COMPLETE_PROJECT_DOCUMENTATION.md)** — 52-section end-to-end reference covering the whole project.
- **[User Guide](docs/USER_GUIDE.md)** — detailed end-user workflows.
- **[FAQ](docs/FAQ.md)** — common product/security/build/release questions.
- **[Developer Guide](docs/DEVELOPER_GUIDE.md)** — architecture, DI, security boundaries, extension rules, tests, and review practice.
- **[Maintainer Guide](docs/MAINTAINER_GUIDE.md)** — repository/security/release/support ownership.
- **[API Reference](docs/API_REFERENCE.md)** — internal Application contracts and Domain models.
- **[Limits & Defaults](docs/LIMITS_AND_DEFAULTS.md)** — authoritative safety ceilings, defaults, versions, and timing bounds.

Specialist references:

- Architecture: [Architecture](docs/architecture/ARCHITECTURE.md) · [Dependency Map](docs/architecture/DEPENDENCY_MAP.md) · [Data Flow](docs/architecture/DATA_FLOW.md) · [Session/Concurrency](docs/architecture/SESSION_AND_CONCURRENCY.md) · [Database](docs/architecture/DATABASE.md) · [Localization](docs/architecture/LOCALIZATION.md)
- Security: [Threat Model](docs/security/THREAT_MODEL.md) · [Cryptographic Design](docs/security/CRYPTOGRAPHIC_DESIGN.md) · [Session Security](docs/security/SESSION_SECURITY.md) · [Sensitive Data Lifecycle](docs/security/DATA_LIFECYCLE.md) · [Biometric Unlock](docs/security/BIOMETRIC_UNLOCK.md) · [TOTP](docs/security/TOTP.md) · [Secure Notes](docs/security/SECURE_NOTES.md) · [Passphrase Generator](docs/security/PASSPHRASE_GENERATOR.md)
- Formats: [Vault Header](docs/formats/VAULT_HEADER.md) · [Vault Records](docs/formats/VAULT_RECORDS.md) · [Attachments](docs/formats/ATTACHMENTS.md) · [Encrypted Backup](docs/formats/ENCRYPTED_BACKUP.md) · [CSV Transfer](docs/formats/CSV_TRANSFER.md)
- Build/release: [Build](docs/setup/BUILD.md) · [Testing Guide](docs/TESTING_GUIDE.md) · [Test Plan](docs/TEST_PLAN.md) · [CI Gates](docs/verification/CI_GATES.md) · [Release Checklist](docs/RELEASE_CHECKLIST.md) · [Release Process](docs/releases/RELEASE_PROCESS.md) · [Packaging](docs/releases/PACKAGING.md)
- Operations: [Backup/Recovery Runbook](docs/operations/BACKUP_RECOVERY_RUNBOOK.md) · [Security Response](docs/operations/SECURITY_RESPONSE.md) · [Troubleshooting](docs/TROUBLESHOOTING.md)

# Current implemented scope

## Local encrypted vault

- Local SQLite vault with authenticated encrypted item envelopes.
- Searchable vault fields are not persisted in a plaintext SQL/FTS index.
- Random 256-bit vault data-encryption key (DEK).
- Master passphrase is never stored; it derives a wrapping key with Argon2id.
- Optional independent one-time recovery material wraps the same random DEK.
- AES-256-GCM authenticates encrypted records, wrapped keys, backup chunks, and attachment chunks with contextual associated data.
- Explicit versioned vault header, attachment, backup, and database compatibility surfaces.

## Authentication and session security

- Master-passphrase and recovery unlock.
- Optional Android/iOS/Mac Catalyst biometric convenience unlock using a separate random secondary secret protected by platform secure storage.
- Windows currently uses master-passphrase fallback instead of claiming Windows Hello support.
- Fresh-process and configurable periodic master-passphrase requirements before biometric convenience can continue.
- Current-master re-authentication for sensitive/destructive actions.
- Bounded exponential delay for repeated interactive unlock failures.
- Serialized vault create/unlock/secondary-unlock/lock/full-delete transitions.
- Private 32-byte `VaultKeyLease` copies linked to caller + current session cancellation; buffers zero on disposal where practical.
- Lock removes/zeroes shared session key state and cancels session-linked cancellable work.
- Full-vault deletion binds authorization to the live security session while waiting for the transition gate.

## Vault item types

Current persisted item types:

1. Login
2. Secure Note
3. Identity
4. Payment Card Reference
5. Wi-Fi Credential
6. Software License
7. Server/SSH Reference
8. Document
9. Custom
10. Time-Based One-Time Password (TOTP)

Items support encrypted metadata such as tags, collection, favorite status, review dates, custom fields, attachment references, recent-access timestamp, trash state, and optional per-item re-authentication.

## Search, organization, and reminders

- Local text search while unlocked.
- Favorites.
- Collections/folders.
- Item-type filtering.
- Review-due filtering.
- Favorite/title, recently used, recently modified, and title sorting.
- Encrypted `LastAccessedUtc` recent-use tracking.
- Incremental 50-item visual rendering with Load More.
- Local backup reminders.
- Local review reminders.

## Local security audit

The in-app vault-content audit can identify:

- weak secrets;
- reused secrets;
- exact duplicate entries;
- missing titles;
- overdue review dates.

This is not an independent professional audit of the CipherNest codebase.

## TOTP

- Encrypted Base32 seed inside the authenticated vault record.
- SHA-1, SHA-256, SHA-512.
- 6 or 8 digits.
- 15–120-second periods, default 30 seconds.
- RFC 6238-compatible known-answer coverage.
- Explicit manual refresh and code copy.
- Generated codes are transient and are not persisted.
- QR scanning/rendering, `otpauth://` import/export, and autofill/provider integration are intentionally not claimed in the current source.

## Password and passphrase generation

- Cryptographically secure password generation.
- Configurable uppercase/lowercase/digits/symbols.
- Ambiguous-character exclusion.
- Password length 8–256 after normalization.
- Memorable passphrase mode using a validated local 256-word list.
- 6–16 words, default 8.
- Persisted non-secret generator defaults.
- Strength/entropy guidance without claiming formal attack resistance.

## Secure notes

- Bounded Markdown-like safe subset.
- Headings, paragraphs, bullets, checklists, fenced code.
- Raw HTML is neutralized rather than rendered.
- Shared 200,000-character and 5,000-line ceilings across note paths.

## Encrypted attachments

- Bounded streaming authenticated encryption.
- Up to 100 MiB plaintext per attachment.
- Up to 25 attachments per item.
- Up to 10,000 referenced attachments across the vault resource policy.
- Opaque GUID-based `.cna` storage names bound to attachment IDs.
- Rune-aware metadata validation rejects malformed UTF-16 and Unicode Control/Format runes.
- Collision-resistant `CreateNew` staging and no final overwrite on collision.
- Small valid UTF-8 text-family files can be previewed in bounded memory.
- Explicit plaintext attachment export with warning, unique temp staging, OS share flow, and best-effort cleanup.

## Encrypted backup and restore

- Preferred transfer/recovery path uses `.cnbak` authenticated encrypted backup.
- Separate backup passphrase.
- Vault locks before consistent snapshot creation.
- Database snapshot plus encrypted attachment containers.
- Strict bounded version-2 backup-header JSON before Argon2.
- Up to 10,001 ZIP entries and 1 GiB aggregate plaintext archive content.
- Duplicate normalized ZIP paths rejected.
- Encrypted attachment size envelope validated.
- Actual extracted bytes must exactly match each declared uncompressed entry length.
- Staged SQLite replacement must pass integrity/schema/header/item/resource checks before active mutation.
- Recovery rollback uses an uncancelled token after active mutation begins.
- Successful restore clears local biometric pairing so it must be configured deliberately again.

## CSV interoperability

- Bounded generic CSV parser/import.
- Explicit source-column mapping.
- Strict header metadata rules including 256-character header limit and Unicode Control/Format rejection.
- Guarded plaintext CSV export requiring `EXPORT PLAINTEXT`, current-master re-authentication, and explicit warning/confirmation.
- Attachments are not silently included in CSV export.
- CSV export is a plaintext boundary; OS/destination copies can persist outside CipherNest.

## Clipboard and sensitive-state handling

- Explicit copy actions for usernames, primary secrets, secret custom fields, and TOTP codes.
- Delayed clipboard state retains a SHA-256 fingerprint, not the copied plaintext.
- Fixed-time fingerprint matching.
- Newer unrelated clipboard content is preserved.
- Lock-triggered cleanup uses the same conditional policy where supported.
- Sensitive ViewModel credential/decrypted fields are cleared on page disappearance where owned by the current page and before several longer operations where practical.
- .NET managed strings and OS/application copies cannot be deterministically erased.

## Trash and deletion

- Configurable trash retention.
- Routine encrypted trash cleanup.
- Restore from Trash.
- Manual permanent delete and Empty Trash require current-master re-authentication plus destructive confirmation.
- Database record deletion precedes best-effort encrypted attachment cleanup.
- Full-vault deletion requires `DELETE MY VAULT`, current-master authentication, final confirmation, and live-session authorization.
- Logical deletion does not claim guaranteed physical sanitization.

## Settings and local preferences

Current settings cover:

- System/Light/Dark theme;
- System/English/Hindi language preference;
- lock timeout;
- lock on background;
- clipboard-clear delay;
- screenshot-protection preference;
- biometric convenience unlock;
- periodic master-passphrase interval;
- reduced motion;
- larger interface;
- trash retention;
- backup reminder;
- review reminders;
- generator defaults;
- local storage/cache inspection and cleanup;
- encrypted backup/restore;
- CSV import/export;
- security/privacy information;
- About/legal/acknowledgements;
- master-passphrase change;
- full local-vault deletion.

Settings JSON is non-secret and bounded to 64 KiB with a 64 KiB + 1 actual-read sentinel and maximum depth 16. Invalid/malformed/oversized non-secret settings fall back to normalized defaults; cancellation continues to propagate.

## Accessibility and localization

- Semantic metadata and selected status/live-region behavior.
- Larger Interface typography support.
- Reduced Motion preference state.
- Responsive phone/desktop/resizable layouts.
- Neutral English fallback resources.
- Persisted System/English/Hindi preference.
- Reviewed `hi-IN` resource-backed catalog.
- Complete translation of every remaining literal is not claimed.

Assistive-technology and representative device/layout validation remain release gates.

## Privacy-safe diagnostics

- No third-party analytics or crash-reporting provider is enabled by current source.
- Central privacy-safe exception reporting intentionally omits raw exception messages/stacks and vault contents.
- Sensitive file/picker/share/storage/backup/restore/settings/item/platform failures use fixed user-facing text plus redacted diagnostics where applicable.

# Resource and format highlights

| Resource | Current ceiling/rule |
|---|---|
| Vault-header UTF-8 | 64 KiB; JSON depth 16 |
| Decrypted/serialized item JSON | 16 MiB |
| Stored encrypted envelope | 24 MiB/row |
| Item rows | 100,000 |
| Aggregate encrypted envelopes | 256 MiB |
| Combined item text/metadata | 2,000,000 chars |
| Attachments | 25/item; 10,000 referenced total |
| Attachment plaintext | 100 MiB/file |
| Secure Note | 200,000 chars / 5,000 lines |
| Search query | 4,096 trimmed chars |
| Settings JSON | 64 KiB; depth 16 |
| Backup archive | 1 GiB plaintext aggregate / 10,001 entries |
| Crypto-bound passphrase input | 12–4,096 chars |
| TOTP normalized seed | 16–1,024 Base32 chars |

See [Limits & Defaults](docs/LIMITS_AND_DEFAULTS.md) for the authoritative complete table.

# Architecture

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

- `CipherNest.Shared` — product/version/storage constants and small cross-layer primitives.
- `CipherNest.Domain` — framework-independent domain records/enums.
- `CipherNest.Application` — use-case abstractions, policies, validators, application exceptions/DTOs.
- `CipherNest.Infrastructure` — cryptography, SQLite, migrations, encrypted attachments/backups, CSV, generators, TOTP, audit implementations.
- `CipherNest.App` — MAUI UI, DI, navigation, lifecycle, biometrics, secure storage, clipboard, screenshot controls, localization, accessibility, picker/share, diagnostics.
- `tests/CipherNest.UnitTests` — deterministic pure-service/policy/crypto/parser tests.
- `tests/CipherNest.IntegrationTests` — real persistence/vault/backup/attachment/migration/session tests.
- `tests/CipherNest.UiTests` — source/UI/documentation/workflow regression guards that do not require booting a MAUI target.

# Build and verification

Prerequisites depend on the desired target. Start with:

```bash
dotnet --info
dotnet workload list
```

Canonical verification entry points:

```text
scripts/verify-core.ps1
scripts/verify-core.sh
scripts/verify-windows.ps1
scripts/verify-android.sh
scripts/verify-apple.sh
```

See [Build & Run](docs/setup/BUILD.md) for direct target commands and toolchain details.

## Current immutable pre-documentation implementation baseline

The complete documentation expansion is grounded in exact implementation commit:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

For that exact SHA:

- **346 UnitTests passed**;
- **98 IntegrationTests passed**;
- **111 UI/source tests passed**;
- **555 total passed, 0 failed, 0 skipped**;
- analyzer-enabled core test builds completed with zero build warnings/errors;
- configured core formatting passed;
- Windows default Release passed;
- Windows `CipherNestEnableFundingLink=false` Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI application builds.

Exact runs:

```text
CipherNest CI: 31937127961
CodeQL:       31937127900
```

This is exact evidence for that immutable implementation commit only. Documentation commits made afterward must run their own configured gates before the later head is called an exact-head verified release candidate. See [`docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md`](docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md).

Historical verification records remain preserved in `docs/verification/` with their original commit/run context, including the earlier 240-test and 554-test baselines.

# What CI does not prove

Hosted compile/tests/static analysis do not replace:

- physical-device Android biometric enrollment/denial/cancellation/lockout tests;
- iOS/Mac Catalyst Face ID/Touch ID and secure-storage runtime tests;
- Windows/iOS/macOS/Android clipboard history/cleanup behavior;
- real lifecycle/background/sleep/resume timing;
- screenshot/app-switcher privacy behavior;
- OS share-sheet plaintext-retention/cleanup behavior;
- TalkBack/VoiceOver/Narrator/keyboard/focus/large-text accessibility validation;
- representative responsive layouts;
- stress/interleaving/filesystem-recovery validation;
- signing/provisioning/notarization;
- store privacy/policy/submission review;
- exact release dependency/license/advisory review;
- independent professional cryptographic/security review.

# Funding-link build switch

The optional in-app Buy Me a Coffee surface is enabled by default. A distribution build that must omit the external funding CTA can use:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

This affects guarded in-app funding UI only; `.github/FUNDING.yml` remains separate. Verify the exact store/region policy before choosing the release setting and record it in release provenance.

# Deferred future-version work

The current source does not claim complete support for:

- cloud synchronization/accounts/collaboration/server storage;
- multi-device conflict resolution;
- browser/app autofill;
- Windows Hello convenience unlock;
- TOTP QR scanning/rendering and `otpauth://` import/export;
- TOTP provider/autofill enrollment;
- rich binary/PDF preview and document scanning;
- pronounceable-password mode;
- destructive wipe after failed unlock attempts;
- complete translation of every remaining literal into Hindi;
- additional complete language catalogs.

See [Next Steps](docs/NEXT_STEPS.md) and [Feature Matrix](docs/FEATURE_MATRIX.md).

# Repository and support

Source: https://github.com/sanskarIN/CipherNest  
Creator: https://www.github.com/sanskarIN  
Business: sanskarin@outlook.in  
Support: supportramsandesh@gmail.com  
Support development: [☕ Buy Me a Coffee](https://buymeacoffee.com/sanskarIN)

**Made by the Sanskar**

# License

CipherNest is licensed under **GPL-3.0-or-later**. See `LICENSE`. Third-party dependencies retain their own licenses; see `THIRD_PARTY_NOTICES.md` and perform exact restored dependency/license review for each distribution candidate.
