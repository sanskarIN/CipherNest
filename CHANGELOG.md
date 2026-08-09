# Changelog

All notable changes are documented here following Semantic Versioning principles.

## [Unreleased]

### Added
- Optional biometric unlock on supported Android, iOS, and Mac Catalyst devices using an independently generated secondary vault-key wrapper; the master passphrase is never stored for biometric unlock.
- Periodic master-passphrase requirement for biometric sessions, with a fresh app process requiring the master passphrase first.
- Guarded plaintext attachment export through an explicit warning and temporary app-cache cleanup path.
- Safe in-memory preview for bounded UTF-8 TXT, Markdown, CSV, JSON, and LOG attachments without creating a plaintext preview file.
- Encrypted last-accessed timestamps and vault sorting by recently used, recently modified, title, or favorites/title.
- Vault item-type, favorite, review-due, and collection filters plus local review-reminder summaries.
- Incremental 50-item vault rendering with local result counts and an explicit load-more action for large local vaults.
- Safe secure-note Markdown subset with headings, bullets, fenced code, checklists, HTML neutralization, and bounded preview parsing.
- Exact duplicate-entry detection in the local security audit alongside weak/reused/overdue findings.
- Explicit timed-copy actions for usernames, primary secrets, and secret custom fields; secret custom-field values remain hidden in the quick-copy list.
- Testable clipboard clear-safety policy that bounds scheduled clearing and preserves unrelated clipboard content copied afterward.
- Testable session-lock, trash-retention, and failed-unlock backoff policies with unit coverage.
- Guarded empty-trash and manual permanent-delete flows requiring the current master passphrase and explicit destructive confirmation.
- Generator defaults persisted locally and an eight-word default memorable-passphrase mode based on a validated 256-entry local word list.
- Explicit passphrase random-selection entropy guidance and unit tests for word-list invariants/bounds.
- Local storage usage inspection and temporary-cache cleanup controls.
- Transactional ordered database migration runner with future-schema rejection and migration tests.
- Argon2id known-answer test vector and explicit hostile KDF resource-bound tests.
- Multi-megabyte attachment streaming, encrypted attachment tamper/truncation, backup corruption, and wrong-backup-passphrase integration coverage.
- Malformed CSV parser robustness corpus and expanded UI-structure security regression coverage in the main CI job.
- Dynamic larger-interface typography resources and startup restoration of accessibility preferences.
- English-first localization resource catalog, persisted System/English preference, and resource-backed localization service ready for additional culture catalogs.
- Dedicated in-app security/privacy/threat-limit information surface.
- Privacy-safe centralized exception reporting that omits exception messages/stacks and decrypted vault context.
- Runtime About version/build metadata plus license, privacy, terms, third-party notices, acknowledgements, repository/support details, and audit status.
- Optional project-support metadata at `https://buymeacoffee.com/sanskarIN`, with a user-initiated About action, repository/Support references, and GitHub `.github/FUNDING.yml` metadata.
- Centralized About project/contact metadata bindings to prevent duplicated public URLs/emails from drifting away from `AppConstants`.
- `docs/NEXT_STEPS.md` with ordered build, device-security, recovery, backup/transfer, accessibility/localization, performance, release-engineering, security-review, launch, and later-version work.
- Splash wordmark/creator credit, monochrome icon source, and dark-surface logo source in addition to the existing original vector branding.
- Third-party dependency notices, implemented cryptographic design specification, secure-note security documentation, passphrase-generator design notes, privacy-safe diagnostics policy, localization architecture guidance, packaging/reproducibility guidance, branding asset documentation, and store-listing guidance.

### Changed
- Restoring a backup clears the local biometric secure-storage secret and disables biometric unlock until it is deliberately configured again.
- Changing the master passphrase now clears the remembered master-authentication session, locks the vault, attempts clipboard cleanup, and requires the new master passphrase before biometric convenience unlock can resume.
- Manual/background/timeout vault locks attempt immediate clipboard clearing in addition to the normal timed-clear behavior.
- Sensitive credential/decrypted fields are cleared when Unlock, Settings, Transfer, Trash, Item Editor, and Onboarding pages disappear, while managed-memory limitations remain documented.
- Trash retention cleanup now runs during normal vault maintenance rather than depending on the user opening Trash.
- Interactive failed-attempt rate limiting now uses an explicit bounded exponential-backoff policy with test coverage.
- KDF metadata read from vault/backup containers is bounded before Argon2 work: salt 16–64 bytes, memory 16–512 MiB, iterations 1–10, and parallelism 1–16.
- Settings now distinguish biometric capability/configuration from master-passphrase recovery and sensitive-setting authentication.
- Settings now include generator defaults, local review reminders, storage/cache controls, security-audit navigation, privacy/threat information, language readiness, and About/legal navigation.
- Attachment imports normalize declared media types for supported in-app text preview policy.
- Database initialization now routes through explicit transactional migration history instead of treating schema creation as an implicit one-time side effect.
- Vault recent-access time is recorded once when an item actually loads, avoiding duplicate encrypted writes during navigation.
- Vault filter controls now stack cleanly and primary actions use a wrapping layout for narrow phones and resizable desktop windows.
- Unlock biometric-capability errors now use the privacy-safe exception reporter instead of writing exception messages through raw debug output.
- Redacted diagnostics now delete their temporary app-cache file after the share request returns where permitted.
- Store-listing guidance now requires current policy verification before shipping an external funding/payment CTA; affected store builds must omit/disable the in-app CTA if the applicable policy does not permit it.
- CI now restores/builds/runs unit, integration, and UI-structure test projects before the Windows MAUI build gate.
- Release/test/database/architecture/security/privacy/legal documentation was expanded to match implemented behavior and remaining external-validation limits.

## [0.1.0] - 2026-08-09

### Added
- Initial local-first .NET MAUI architecture.
- Versioned Argon2id + AES-256-GCM vault envelope.
- SQLite encrypted-record persistence foundation.
- Vault creation, unlock, CRUD, search, password generation, local audit, encrypted backup, lock lifecycle, settings, and About surfaces.
- Encrypted streaming attachments, collections, trash retention, review reminders, custom fields, and per-item re-authentication.
- Generic CSV import with explicit mapping and guarded plaintext CSV export.
- Master-passphrase rotation, local vault deletion, one-time recovery key flow, backup reminders, and developer diagnostics.
- Security, privacy, architecture, threat-model, testing, setup, and release documentation.
