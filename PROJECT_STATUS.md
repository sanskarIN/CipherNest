# Project Status

## Current release: 0.1.0 + unreleased hardening

### Completed in source
- Repository and multi-project solution scaffolding with Domain/Application/Infrastructure/Shared/MAUI/test separation.
- Versioned cryptographic envelope with Argon2id key derivation and AES-256-GCM authenticated encryption.
- Random vault data-encryption key wrapped independently by master passphrase, optional recovery key, and optional biometric secondary secret.
- Untrusted KDF metadata is resource-bounded before Argon2 work: salt 16–64 bytes, memory 16–512 MiB, iterations 1–10, and parallelism 1–16; new wrappers use the current 64 MiB / 3 iteration / parallelism 1 default.
- Encrypted SQLite record persistence with minimized plaintext metadata and a transactional ordered schema-migration runner that rejects unsupported future schema versions.
- Local vault creation, master/recovery unlock, lock lifecycle, bounded failed-attempt backoff, master-passphrase rotation, and guarded full local-vault deletion.
- Master-passphrase rotation now ends the current security session, clears the remembered master-auth timestamp, locks the vault, attempts clipboard cleanup, and requires the new master passphrase before biometric convenience unlock can resume.
- Optional biometric unlock source implementation for supported Android, iOS, and Mac Catalyst devices; Windows explicitly falls back to master-passphrase unlock.
- Fresh-process and periodic master-passphrase requirements before biometric convenience unlock can continue.
- Item CRUD for all modeled vault types, encrypted custom fields, collections, tags, favorites, local search, review dates, per-item master re-authentication, trash retention, and encrypted last-accessed timestamps.
- Vault sorting by favorites/title, recent use, recent modification, and title; filtering by collection, item type, favorites, and review due state.
- Incremental 50-item vault rendering with result counts and explicit load-more behavior to keep large local result sets from all entering the visual tree at once.
- Local review-reminder summary with configurable lead time and backup reminders.
- Password generator using cryptographically secure randomness plus configurable character groups and ambiguous-character exclusion.
- Memorable passphrase generator backed by exactly 256 validated unique lowercase local words, 6–16 word bounds, eight-word default, explicit random-selection entropy guidance, and persisted generator defaults.
- Local weak/reused/exact-duplicate/overdue secret audit primitives.
- Secure-note editor with a bounded safe Markdown-like subset, checklist support, fenced code, HTML neutralization, and local safe preview.
- Encrypted streaming attachments with bounded size/count, authenticated storage, MIME normalization, removal, guarded plaintext export, and bounded in-memory UTF-8 preview for supported text-family formats.
- Authenticated encrypted backup/restore including encrypted attachments, consistent pre-backup locking, temporary restore staging, corruption/tamper rejection, rollback-preservation tests, and post-restore biometric reset.
- Generic CSV import with explicit column mapping, strict bounded parsing, malformed-input corpus coverage, and guarded plaintext CSV export.
- Explicit username/password/custom-secret copy actions with configurable timed clearing, protection against clearing unrelated newer clipboard content, and immediate cleanup attempts on manual/background/timeout security locks.
- Testable session-lock policy covering lock-on-background, inactivity timeout, and fail-closed clock rollback.
- Testable trash-retention policy with routine vault-maintenance cleanup; manual permanent deletion and empty-trash actions require the current master passphrase plus explicit destructive confirmation.
- Sensitive passphrase/recovery/decrypted ViewModel state is cleared when Unlock, Settings, Transfer, Trash, Item Editor, and Onboarding pages disappear, within documented managed-memory limitations.
- Screenshot protection on supported implementation paths with honest fallback messaging.
- Settings for theme, language readiness, lock/privacy, reminder intervals, biometrics, generator defaults, storage/cache, backup/restore, import/export, security audit, privacy/threat information, About/legal/acknowledgements, master-passphrase change, and destructive deletion.
- Dynamic larger-interface typography resources, reduced-motion preference state, light/dark/system theme behavior, semantic labels/live regions, and responsive layouts including wrapping vault actions for narrow windows.
- English-first `.resx` resource catalog, persisted System/English preference, and localization service architecture ready for Hindi/additional catalogs without coupling language to vault formats.
- Central privacy-safe unhandled-exception reporting that records sanitized operation/type/HResult metadata while intentionally excluding exception messages/stacks and vault content; unlock capability probes and clipboard-cleanup failures use this path instead of raw exception-message logging.
- Redacted developer diagnostics with best-effort temporary-file deletion after sharing and Settings cache-cleanup fallback.
- In-app security/privacy/audit-status surface, runtime version/build About information, GPL/privacy/terms references, third-party dependency notices, acknowledgements, repository/support contacts, and hidden developer diagnostics.
- Original SVG branding with splash wordmark and `Made by the Sanskar`, primary/adaptive icon sources, monochrome system-mark source, dark-surface logo variant, editable asset guidance, packaging/reproducibility documentation, and store-listing/feature-graphic guidance.
- Unit/integration/UI-structure test source including cryptographic tamper/wrong-passphrase, Argon2id known-answer and hostile KDF parameter coverage, backup corruption/wrong-passphrase preservation, vault workflows, secondary unlock, CSV parser safety, passphrase rotation/deletion, recent-access tracking, safe-note parsing, duplicate audit findings, schema migrations, generator word-list invariants, multi-megabyte attachment streaming, attachment tamper/truncation rejection, session-lock policy, clipboard clear policy, trash retention, unlock backoff, sensitive-screen cleanup, large-vault incremental rendering, destructive trash confirmation, accessibility/navigation structure, legal surfaces, branding, and diagnostics-source privacy checks.
- GitHub Actions CI, dependency review, CodeQL, repository templates, contribution/security/support/privacy/terms files, architecture records, implemented cryptographic design, release/setup/packaging/reproducibility/troubleshooting/test documentation, third-party notices, and release checklist.

### Quality gate requiring external execution or hardware
- The connected GitHub editing environment cannot execute the repository's .NET/MAUI builds, GitHub-hosted push workflow runs, emulator/simulator sessions, or physical-device smoke tests. Source completion is therefore not a claim that the current head has passed those external checks.
- The core CI workflow is configured to restore/build/run UnitTests, IntegrationTests, and UiTests; the Windows job installs the MAUI workload and builds the Windows target with warnings/analyzers enforced by repository build properties.
- Android biometric API bindings and behavior must be compiled and exercised against the selected .NET 10 Android workload and real enrolled/non-enrolled devices.
- iOS and Mac Catalyst biometric behavior, Face ID/Touch ID enrollment changes, secure-storage behavior, and packaging require an appropriate Apple build/test environment.
- Windows packaging needs its normal signing identity for store distribution; Windows biometric unlock is intentionally not enabled in this release.
- Android/iOS/MacCatalyst/Windows store signing keys and credentials are intentionally absent from the repository and must be supplied through protected CI/store configuration.
- Screenshot blocking, clipboard API clearing, background/sleep lifecycle callbacks, share-sheet plaintext cleanup, in-memory preview behavior, accessibility behavior, language fallback, responsive layouts, incremental large-vault UX, and large-file attachment behavior require final platform-by-platform validation.
- Dependency vulnerability/dependency-review/CodeQL results must be reviewed when GitHub services execute against the current head.
- Third-party license notice families must be checked against the exact restored package metadata before distribution.
- Exact platform asset/store requirements, including Android themed/monochrome icon wiring and Apple/Windows generated icon outputs, must be verified against current distribution documentation during release packaging.
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
