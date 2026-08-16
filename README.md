# CipherNest

<p align="center">
  <img src="src/CipherNest.App/Resources/Images/ciphernest_logo.svg" alt="CipherNest logo" width="150" />
</p>

<p align="center"><strong>Local-first encrypted vault for passwords, secure notes, identities, credentials, TOTP, and protected documents.</strong></p>

CipherNest is a local-first, open-source password, secure-note, identity, credential, and encrypted-document vault built with .NET MAUI and C#.

> **Security status:** CipherNest has not yet undergone an independent professional security audit. It uses established primitives and a deliberately small security-sensitive core, but must not be described as “unhackable”, “military-grade”, or “100% secure”.

## ☕ Support CipherNest development

<p align="center">
  <a href="https://buymeacoffee.com/sanskarIN" title="Support CipherNest on Buy Me a Coffee">
    <img src="src/CipherNest.App/Resources/Images/bmc_support.svg" alt="BMC — Support CipherNest" width="560" />
  </a>
</p>

<p align="center"><strong>Click the BMC badge above to open the CipherNest Buy Me a Coffee page.</strong></p>

Project support is optional. It does not change feature access, security/privacy treatment, support priority, licensing, recovery behavior, or open-source rights. Store/distribution builds can disable the in-app funding surface while repository funding metadata remains separate.

## Current release

- No account, email, phone number, application server, or cloud synchronization is required.
- Vault records and attachments are encrypted locally; searchable item fields are not stored in plaintext SQL indexes.
- The master passphrase is never stored. A random 256-bit vault data-encryption key is wrapped with an Argon2id-derived key.
- AES-256-GCM authenticates encrypted records, wrapped keys, backups, and attachment chunks using unique nonces and contextual associated data.
- Optional one-time recovery material provides an independent wrapped-key path and must be retained separately by the user.
- Optional Android/iOS/Mac Catalyst biometric convenience unlock uses a separately generated random secondary secret protected by platform secure storage; Windows currently falls back to the master passphrase. Android uses the API-28 `BiometricPrompt` baseline and Apple request cancellation invalidates its native authentication context.
- Master-passphrase re-authentication is required periodically after biometric use and for security-sensitive actions such as plaintext export, biometric configuration, manual permanent deletion, and vault deletion. Changing the master passphrase ends the current security session and requires the new passphrase before biometric convenience unlock can resume.
- Vault master/recovery unlock, secondary unlock, public lock, and full-vault deletion transitions are serialized. Full-vault deletion must keep a live session authorization while waiting for that transition gate, so an intervening lock/unlock cancels stale destructive authorization.
- Key-using vault work runs with private 32-byte key leases linked to the current unlock session and caller cancellation. Locking zeroes the shared session key and cancels active leases; lease copies zero on disposal. An integration test specifically blocks a plaintext attachment export and requires lock to cancel it.
- Repeated interactive unlock failures use a bounded exponential delay. This is a client-side control and is not claimed to stop offline guessing against a copied database.
- Supported vault-header versions are explicit; future/unknown header versions are rejected before key unwrap. Header metadata is capped at 64 KiB UTF-8 before deserialization.
- Local search, favorites, collections, item-type filters, review reminders, recent-use sorting, and weak/reused/duplicate-secret audit operate only over decrypted data while unlocked. Large matching result sets render incrementally in 50-item pages.
- Local TOTP items keep the Base32 seed and SHA-1/SHA-256/SHA-512 + 6/8-digit + period settings inside the authenticated encrypted record, generate RFC 6238-compatible codes only while unlocked, use explicit refresh/copy actions, and do not persist generated codes. QR/`otpauth://` enrollment and autofill integration are not claimed.
- Decrypted records must match their authenticated SQLite row ID and pass null-safe metadata validation before reaching application/search/UI code.
- Resource limits bound stored/decrypted item work: 16 MiB serialized/decrypted item JSON, 24 MiB per stored encrypted envelope, 100,000 rows, 256 MiB aggregate encrypted-envelope bytes, and 2,000,000 aggregate item-text characters. These are safety ceilings, not recommended target sizes.
- Trash has configurable retention; routine vault maintenance removes expired encrypted trash records. Manual permanent deletion and empty-trash actions require current-master re-authentication plus explicit confirmation. Permanent item deletion removes its database record before best-effort encrypted attachment cleanup.
- Password generation uses `RandomNumberGenerator`; memorable passphrases use a validated 256-word local list with explicit random-selection entropy guidance and configurable defaults. Temporary generator arrays are cleared after constructing the returned managed string where practical.
- Secure notes support a deliberately small Markdown-like subset plus checklists; raw HTML is not rendered. Stored/imported notes share the renderer's 200,000-character and 5,000-line limits.
- Attachments are encrypted in bounded streaming chunks. Reusable plaintext chunk buffers are zeroed where practical. Encrypted staging uses unique `CreateNew` files and refuses final overwrite. Small UTF-8 text-family attachments can be previewed in memory; other formats require explicit plaintext export.
- Opaque encrypted attachment storage names must be GUID-based `.cna` filenames without path separators. Attachment metadata enforces size/count/uniqueness bounds before save/use.
- Temporary decrypted export names include a random component and cleanup failures are reported without displaying the path.
- Encrypted backup/restore includes encrypted attachments and is the recommended transfer path. Backup header version/salt/KDF/chunk metadata is validated before Argon2 derivation.
- Backup export refuses destinations that resolve to the active database, WAL/SHM/recovery files, or encrypted attachment directory and uses a unique encrypted sibling staging file.
- Backup restore rejects duplicate normalized ZIP paths and encrypted attachment entries outside the implemented container-size envelope in addition to total archive/path/count limits.
- A staged replacement database must pass SQLite `quick_check`, exact supported schema version, required table/column validation, bounded vault-header/resource checks, and canonical item-ID checks before active DB/WAL/SHM mutation.
- SQLite replacement stages DB/WAL/SHM into unique recovery names. Rollback restores only components that actually moved, and encrypted-backup rollback uses an uncancelled recovery token once active mutation begins so request cancellation cannot cancel the recovery replacement.
- Database migrations validate required schema shape after version resolution, reject forged-current history with missing schema objects, and prevent rollback errors from hiding the original migration failure.
- Generic CSV import and deliberately guarded plaintext CSV/attachment export are available for interoperability; warnings explain that operating systems and destination apps can retain plaintext copies. CSV final-field column limits are enforced at newline/EOF as well as delimiters.
- Username, primary-secret, and secret custom-field clipboard writes require explicit copy actions. Delayed cleanup retains only a SHA-256 fingerprint rather than the copied plaintext secret, uses fixed-time matching, and preserves unrelated newer clipboard content. Manual/background/timeout locks use the same conditional cleanup policy where the platform permits it.
- Sensitive credential/decrypted ViewModel fields are cleared when sensitive pages disappear. Bound passphrase fields are also cleared before longer authentication/file/share operations where practical, while .NET managed-memory limitations are documented rather than hidden.
- Lifecycle fallback separately contains and privacy-safe reports lock/clipboard cleanup failures so a second cleanup exception is not allowed to escape the native lifecycle callback.
- Sensitive Settings, transfer, backup, restore, item-open, and attachment file failures use fixed user-facing text plus redacted diagnostic events instead of directly surfacing exception messages that can contain paths/context.
- Settings include theme, larger-interface/reduced-motion preferences, local reminder controls, biometric configuration, generator defaults, storage/cache inspection, security information, backup/restore, import/export, and destructive local-vault deletion.
- Settings persistence normalizes supported enum/numeric bounds on load/save, falls back safely on malformed/unreadable files, and uses unique sibling staging with best-effort cleanup. Cache/storage enumeration is guarded and does not recurse through reparse-point directories.
- Neutral English remains the fallback resource catalog; persisted System/English/Hindi preferences are supported for the reviewed resource-backed interface, while not-yet-migrated UI literals may still appear in English.
- Central exception reporting intentionally omits exception messages/stacks and vault content. No third-party analytics or crash-reporting service is enabled.
- Original vector branding includes launcher/adaptive sources, a splash wordmark with `Made by the Sanskar`, a monochrome source, a dark-surface logo variant, and an original BMC project-support badge.

## Documentation

The complete documentation suite is indexed at [`docs/README.md`](docs/README.md).

Primary entry points:

- [Complete Project Documentation](docs/COMPLETE_PROJECT_DOCUMENTATION.md) — consolidated project reference covering architecture, security, storage, features, limits, build/test/release flow, support, and external validation gates.
- [FAQ](docs/FAQ.md) — common user, security, platform, backup, build, CI, release, and support questions.
- [User Guide](docs/USER_GUIDE.md) — setup, unlock, items, attachments, backup/restore, CSV transfer, settings, trash, deletion, and recovery limitations.
- [Developer Guide](docs/DEVELOPER_GUIDE.md) — architecture, DI, contracts, persistence, session rules, testing, review, and extension guidance.
- [Maintainer Guide](docs/MAINTAINER_GUIDE.md) — repository/security/release/support ownership.
- [API Reference](docs/API_REFERENCE.md) — current Application contracts and Domain models.
- [Limits and Defaults](docs/LIMITS_AND_DEFAULTS.md) — implemented resource ceilings, defaults, versions, and timing bounds.
- [Architecture and Data Flow](docs/architecture/ARCHITECTURE.md) · [Data Flow](docs/architecture/DATA_FLOW.md) · [Session/Concurrency](docs/architecture/SESSION_AND_CONCURRENCY.md) · [Dependency Map](docs/architecture/DEPENDENCY_MAP.md).
- [Threat Model](docs/security/THREAT_MODEL.md) · [Cryptographic Design](docs/security/CRYPTOGRAPHIC_DESIGN.md) · [TOTP Security](docs/security/TOTP.md) · [Session Security](docs/security/SESSION_SECURITY.md) · [Sensitive Data Lifecycle](docs/security/DATA_LIFECYCLE.md).
- [Vault Records](docs/formats/VAULT_RECORDS.md) · [Encrypted Attachments](docs/formats/ATTACHMENTS.md) · [Encrypted Backup](docs/formats/ENCRYPTED_BACKUP.md) · [CSV Transfer](docs/formats/CSV_TRANSFER.md).
- [Testing Guide](docs/TESTING_GUIDE.md) · [Accessibility](docs/ACCESSIBILITY.md) · [Release Process](docs/releases/RELEASE_PROCESS.md) · [Backup/Recovery Runbook](docs/operations/BACKUP_RECOVERY_RUNBOOK.md) · [Security Response Runbook](docs/operations/SECURITY_RESPONSE.md).
- [Verified `main` Baseline — 2026-08-15](docs/verification/VERIFIED_MAIN_BASELINE_2026_08_15.md) — exact candidate, test counts, platform build results, CodeQL result, and remaining external validation limits.

Documentation is required to follow current source behavior rather than planned features; governance rules are in [`docs/DOCUMENTATION_MAINTENANCE.md`](docs/DOCUMENTATION_MAINTENANCE.md).

## Verification and build

Requirements: a current .NET 10 SDK with the .NET MAUI workload and platform SDKs for the desired target.

Committed verification entry points:

- `scripts/verify-core.ps1` or `scripts/verify-core.sh`
- `scripts/verify-windows.ps1`
- `scripts/verify-android.sh`
- `scripts/verify-apple.sh`

GitHub CI is configured for core restore/build/test/format gates plus Windows, Android, iOS, and Mac Catalyst Release compilation. Windows CI also compiles the funding-disabled variant. CodeQL v4 builds the MAUI Android application path in addition to analyzable core code.

The last fully observed immutable baseline before the 2026-08-16 presentation/support-surface pass is `d405bb3ae0a88f4abfcdcb574227c372683dd790`. For that exact candidate:

- **346 UnitTests passed**;
- **98 IntegrationTests passed**;
- **110 UiTests/source tests passed**;
- **554 total passed, 0 failed, 0 skipped**;
- all three analyzer-enabled test-project builds completed with 0 warnings and 0 errors;
- configured core formatting passed;
- Windows default Release passed;
- Windows funding-disabled Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI Android application builds.

Exact run identifiers and limitations are recorded in [`docs/verification/VERIFIED_MAIN_BASELINE_2026_08_15.md`](docs/verification/VERIFIED_MAIN_BASELINE_2026_08_15.md). Any later commit is a new candidate and must rerun the configured gates before inheriting exact-head verification status.

Compile/static-analysis evidence does not replace physical-device biometric/lifecycle/clipboard/screenshot/accessibility tests, signing/notarization/store validation, pull-request dependency review, or an independent professional security audit.

The optional in-app Buy Me a Coffee surface is enabled by default. A distribution build that must omit the external funding CTA can use:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

Verify the current policy for the exact target store/region before choosing that setting. The build switch affects the app UI only; repository funding metadata remains available separately.

Platform packaging and signing require target SDKs/identities that are deliberately kept outside this repository. See `docs/setup/BUILD.md`, `docs/verification/CI_GATES.md`, `docs/TROUBLESHOOTING.md`, `docs/TEST_PLAN.md`, `docs/RELEASE_CHECKLIST.md`, `docs/NEXT_STEPS.md`, `docs/security/THREAT_MODEL.md`, `docs/security/BIOMETRIC_UNLOCK.md`, `docs/security/SECURE_NOTES.md`, and `docs/security/PASSPHRASE_GENERATOR.md`.

## Repository

Source: https://github.com/sanskarIN/CipherNest  
Creator: https://www.github.com/sanskarIN  
Business: sanskarin@outlook.in  
Support: supportramsandesh@gmail.com  
Support development: [☕ Buy Me a Coffee](https://buymeacoffee.com/sanskarIN)

Made by the Sanskar

## License

CipherNest is licensed under GPL-3.0-or-later. See `LICENSE`. Third-party dependencies retain their own licenses; see `THIRD_PARTY_NOTICES.md`.