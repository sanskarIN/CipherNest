# Project Status

## Current release: 0.1.0 + unreleased hardening

### Completed in source
- Repository and multi-project solution scaffolding with Domain/Application/Infrastructure/Shared/MAUI/test separation.
- Versioned cryptographic envelope with Argon2id key derivation and AES-256-GCM authenticated encryption.
- Random vault data-encryption key wrapped independently by master passphrase, optional recovery key, and optional biometric secondary secret.
- Encrypted SQLite record persistence with minimized plaintext metadata and a transactional ordered schema-migration runner that rejects unsupported future schema versions.
- Local vault creation, master/recovery unlock, lock lifecycle, failed-attempt rate limiting, master-passphrase rotation, and guarded full local-vault deletion.
- Optional biometric unlock source implementation for supported Android, iOS, and Mac Catalyst devices; Windows explicitly falls back to master-passphrase unlock.
- Fresh-process and periodic master-passphrase requirements before biometric convenience unlock can continue.
- Item CRUD for all modeled vault types, encrypted custom fields, collections, tags, favorites, local search, review dates, per-item master re-authentication, trash retention, and encrypted last-accessed timestamps.
- Vault sorting by favorites/title, recent use, recent modification, and title; filtering by collection, item type, favorites, and review due state.
- Local review-reminder summary with configurable lead time and backup reminders.
- Password generator using cryptographically secure randomness plus configurable character groups and ambiguous-character exclusion.
- Memorable passphrase generator backed by exactly 256 validated unique lowercase local words, 6–16 word bounds, eight-word default, explicit random-selection entropy guidance, and persisted generator defaults.
- Local weak/reused/exact-duplicate/overdue secret audit primitives.
- Secure-note editor with a bounded safe Markdown-like subset, checklist support, fenced code, HTML neutralization, and local safe preview.
- Encrypted streaming attachments with bounded size/count, authenticated storage, MIME normalization, removal, guarded plaintext export, and bounded in-memory UTF-8 preview for supported text-family formats.
- Authenticated encrypted backup/restore including encrypted attachments, consistent pre-backup locking, temporary restore staging, corruption/tamper rejection, and post-restore biometric reset.
- Generic CSV import with explicit column mapping, strict bounded parsing, malformed-input corpus coverage, and guarded plaintext CSV export.
- Clipboard copy lifecycle with configurable timed clearing and documented platform limitations.
- Screenshot protection on supported implementation paths with honest fallback messaging.
- Settings for theme, language readiness, lock/privacy, reminder intervals, biometrics, generator defaults, storage/cache, backup/restore, import/export, security audit, privacy/threat information, master-passphrase change, and destructive deletion.
- Dynamic larger-interface typography resources, reduced-motion preference state, light/dark/system theme behavior, semantic labels/live regions, and responsive scrollable layouts.
- English-first `.resx` resource catalog, persisted System/English preference, and localization service architecture ready for Hindi/additional catalogs without coupling language to vault formats.
- Central privacy-safe unhandled-exception reporting that records sanitized operation/type/HResult metadata while intentionally excluding exception messages/stacks and vault content.
- In-app security/privacy/audit-status surface, About/open-source information, third-party dependency notices, and hidden developer diagnostics.
- Original SVG branding, splash/icon integration, and store-listing/feature-graphic guidance.
- Unit/integration/UI-structure test source including cryptographic tamper/wrong-passphrase and Argon2id known-answer coverage, backup restore, vault workflows, secondary unlock, CSV parser safety, passphrase rotation/deletion, recent-access tracking, safe-note parsing, duplicate audit findings, schema migrations, generator word-list invariants, and multi-megabyte attachment streaming.
- GitHub Actions CI, dependency review, CodeQL, repository templates, contribution/security/support/privacy/terms files, architecture records, release/setup/troubleshooting/test documentation, third-party notices, and release checklist.

### Quality gate requiring external execution or hardware
- The connected GitHub editing environment cannot execute the repository's .NET/MAUI builds, GitHub-hosted push workflow runs, emulator/simulator sessions, or physical-device smoke tests. Source completion is therefore not a claim that the current head has passed those external checks.
- The core CI workflow is configured to restore/build/run UnitTests, IntegrationTests, and UiTests; the Windows job installs the MAUI workload and builds the Windows target with warnings/analyzers enforced by repository build properties.
- Android biometric API bindings and behavior must be compiled and exercised against the selected .NET 10 Android workload and real enrolled/non-enrolled devices.
- iOS and Mac Catalyst biometric behavior, Face ID/Touch ID enrollment changes, secure-storage behavior, and packaging require an appropriate Apple build/test environment.
- Windows packaging needs its normal signing identity for store distribution; Windows biometric unlock is intentionally not enabled in this release.
- Android/iOS/MacCatalyst/Windows store signing keys and credentials are intentionally absent from the repository and must be supplied through protected CI/store configuration.
- Screenshot blocking, clipboard clearing, background/sleep locking, share-sheet plaintext cleanup, accessibility behavior, language fallback, and large-file attachment behavior require final platform-by-platform validation.
- Dependency vulnerability/dependency-review/CodeQL results must be reviewed when GitHub services execute against the current head.
- Third-party license notice families must be checked against the exact restored package metadata before distribution.
- Independent professional cryptographic/security audit remains outstanding; CipherNest must not be marketed as audited, unhackable, military-grade, 100% secure, or suitable for high-risk use until evidence supports those statements.

### Deliberately deferred pending dedicated security/platform review
- Cloud synchronization, accounts, collaboration, server storage, and multi-device conflict resolution.
- Autofill/type integration with other apps and browsers.
- TOTP seed storage/generation.
- Local document scanning and rich binary/PDF document preview beyond the bounded safe text-preview formats.
- Pronounceable-password mode unless a carefully reviewed design is selected.
- Destructive automatic data wipe after failed unlock attempts.
- Windows Hello biometric unlock until a native implementation can be tested and reviewed.
- Additional translated resource catalogs such as Hindi; the preference/resource architecture exists, but the current release ships English content first.

Deferred features are not represented in the UI as complete.
