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
- Clipboard cleanup now tracks a zeroed fixed-size SHA-256 fingerprint rather than retaining copied plaintext in delayed timer state, uses fixed-time matching, preserves newer clipboard content, and keeps the security timer independent from the initiating caller cancellation token.
- Testable session-lock, trash-retention, and failed-unlock backoff policies with unit coverage.
- Guarded empty-trash and manual permanent-delete flows requiring the current master passphrase and explicit destructive confirmation.
- Generator defaults persisted locally and an eight-word default memorable-passphrase mode based on a validated 256-entry local word list.
- Explicit passphrase random-selection entropy guidance and unit tests for word-list invariants/bounds.
- Local storage usage inspection and temporary-cache cleanup controls.
- Transactional ordered database migration runner with future-schema rejection and migration tests.
- Argon2id known-answer test vector and explicit hostile KDF resource-bound tests.
- Multi-megabyte attachment streaming, encrypted attachment tamper/truncation, backup corruption, and wrong-backup-passphrase integration coverage.
- Malformed CSV parser robustness corpus and expanded UI/source security regression coverage in the main CI job.
- Dynamic larger-interface typography resources and startup restoration of accessibility preferences.
- English-first localization resource catalog, persisted System/English preference, and resource-backed localization service ready for additional culture catalogs.
- Dedicated in-app security/privacy/threat-limit information surface.
- Privacy-safe centralized exception reporting that omits exception messages/stacks and decrypted vault context.
- Runtime About version/build metadata plus license, privacy, terms, third-party notices, acknowledgements, repository/support details, and audit status.
- Optional project-support metadata at `https://buymeacoffee.com/sanskarIN`, with a user-initiated About action, repository/Support references, GitHub `.github/FUNDING.yml` metadata, and a store-build disable switch.
- Centralized About project/contact metadata bindings to prevent duplicated public URLs/emails from drifting away from `AppConstants`.
- `docs/NEXT_STEPS.md` with ordered build, device-security, recovery, backup/transfer, accessibility/localization, performance, release-engineering, security-review, launch, and later-version work.
- `docs/verification/CI_GATES.md` plus committed core/Windows/Android/Apple verification scripts.
- Main CI compile gates for Windows, Android, iOS, and Mac Catalyst; Windows also compiles the funding-disabled variant.
- Core CI formatting verification, CodeQL MAUI application analysis, bounded workflow timeouts, and superseded-run cancellation.
- Source regression tests for shortened credential binding lifetime, fingerprint-only clipboard state, contained lifecycle fallback, sensitive error redaction, cross-platform CI gates, and rejection of legacy `.DisplayAlert(` calls.
- Splash wordmark/creator credit, monochrome icon source, and dark-surface logo source in addition to the existing original vector branding.
- Third-party dependency notices, implemented cryptographic design specification, secure-note security documentation, passphrase-generator design notes, privacy-safe diagnostics policy, localization architecture guidance, packaging/reproducibility guidance, branding asset documentation, and store-listing guidance.

### Changed
- Restoring a backup clears the local biometric secure-storage secret and disables biometric unlock until it is deliberately configured again.
- Changing the master passphrase clears the bound credential fields before rotation, clears the remembered master-authentication session, locks the vault, attempts conditional clipboard cleanup, and requires the new master passphrase before biometric convenience unlock can resume.
- Unlock, onboarding, plaintext export, Trash deletion, per-item re-authentication, biometric settings, backup/restore, passphrase rotation, and full-vault deletion now clear bound credential fields earlier in their operation where practical; managed-memory limitations remain documented.
- Manual/background/timeout vault locks use conditional clipboard cleanup that does not erase unrelated newer clipboard content.
- Lifecycle fail-closed recovery separately contains/reports secondary lock and clipboard-cleanup failures so cleanup exceptions do not escape native `async void` callbacks.
- Android biometric availability no longer preflights with a manager API newer than the API-28 `BiometricPrompt` baseline; prompt/fallback behavior handles enrollment/hardware/lockout results. Apple request cancellation invalidates the native authentication context.
- Sensitive Settings, backup, restore, vault-delete, CSV transfer, item-open, attachment import/export, and temporary-cleanup failures now use fixed user-facing text with privacy-safe redacted diagnostic events instead of rendering filesystem/context-bearing exception messages.
- Decrypted attachment export staging filenames include a random component to avoid collision/reuse when a prior cleanup could not complete.
- Trash retention cleanup runs during normal vault maintenance rather than depending on the user opening Trash.
- Interactive failed-attempt rate limiting uses an explicit bounded exponential-backoff policy with test coverage.
- KDF metadata read from vault/backup containers is bounded before Argon2 work: salt 16–64 bytes, memory 16–512 MiB, iterations 1–10, and parallelism 1–16.
- Settings distinguish biometric capability/configuration from master-passphrase recovery and sensitive-setting authentication.
- Settings include generator defaults, local review reminders, storage/cache controls, security-audit navigation, privacy/threat information, language readiness, and About/legal navigation.
- Attachment imports normalize declared media types for supported in-app text preview policy.
- Database initialization routes through explicit transactional migration history instead of treating schema creation as an implicit one-time side effect.
- Vault recent-access time is recorded once when an item actually loads, avoiding duplicate encrypted writes during navigation.
- Vault filter controls stack cleanly and primary actions use a wrapping layout for narrow phones and resizable desktop windows.
- MAUI app source uses `DisplayAlertAsync` rather than legacy `.DisplayAlert(` calls.
- Unlock biometric-capability errors use the privacy-safe exception reporter instead of writing exception messages through raw debug output.
- Redacted diagnostics delete their temporary app-cache file after the share request returns where permitted.
- Store-listing guidance requires current policy verification before shipping an external funding/payment CTA; affected store builds must omit/disable the in-app CTA if the applicable policy does not permit it.
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
