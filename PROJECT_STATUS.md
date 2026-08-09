# Project Status

## Current release: 0.1.0 + unreleased hardening

### Completed in source
- Repository and multi-project solution scaffolding.
- Domain/application/infrastructure/MAUI separation.
- Versioned cryptographic envelope with Argon2id key derivation and AES-256-GCM authenticated encryption.
- Random vault data-encryption key wrapped independently by master passphrase, optional recovery key, and optional biometric secondary secret.
- Encrypted SQLite record persistence with minimized plaintext metadata.
- Local vault creation, master/recovery unlock, lock lifecycle, failed-attempt rate limiting, master-passphrase rotation, and guarded full local-vault deletion.
- Optional biometric unlock source implementation for supported Android, iOS, and Mac Catalyst devices; Windows explicitly falls back to master-passphrase unlock.
- Fresh-process and periodic master-passphrase requirements before biometric convenience unlock can be used.
- Item CRUD for all modeled vault types, encrypted custom fields, collections, tags, favorites, local search, review reminders, per-item master re-authentication, trash retention, and encrypted last-accessed timestamps.
- Vault sorting by favorites/title, recent use, recent modification, and title.
- Password/passphrase generator using cryptographically secure randomness and local strength estimation.
- Local weak/reused/overdue secret audit primitives.
- Encrypted streaming attachments with bounded size/count, authenticated storage, removal, and guarded plaintext export through the operating-system share surface.
- Authenticated encrypted backup/restore including encrypted attachments, consistent pre-backup locking, temporary restore staging, corruption/tamper rejection, and post-restore biometric reset.
- Generic CSV import with explicit column mapping, strict bounded parsing, and guarded plaintext CSV export.
- Clipboard copy lifecycle with configurable timed clearing and documented platform limitations.
- Screenshot protection on supported implementation paths with honest fallback messaging.
- Appearance, privacy, lock, trash, backup, biometric, accessibility-readiness, transfer, security, About, developer diagnostics, and destructive-action settings.
- MAUI navigation, localization-ready resources, light/dark themes, adaptive layouts, semantic/accessibility labels, and original SVG branding.
- Unit/integration/UI-structure test source including cryptographic tamper/wrong-passphrase, backup restore, vault workflows, secondary unlock, CSV parser safety, passphrase rotation/deletion, and recent-access tracking.
- GitHub Actions CI, dependency review, CodeQL, repository templates, contribution/security/support/privacy/terms files, architecture records, release/setup/troubleshooting/test documentation.

### Quality gate requiring external execution or hardware
- The connected GitHub editing environment cannot execute `dotnet workload`, MAUI builds, emulator/simulator runs, or physical-device smoke tests. The repository includes CI/build scripts for those checks, but source completion is not a claim that those checks have passed.
- Android biometric API bindings and behavior must be compiled and exercised against the selected .NET 10 Android workload and physical enrolled/non-enrolled devices.
- iOS and Mac Catalyst biometric behavior, Face ID/Touch ID enrollment changes, secure-storage behavior, and packaging require Apple hardware/build infrastructure.
- Windows packaging needs its normal signing identity for store distribution; Windows biometric unlock is intentionally not enabled in this release.
- Android/iOS/MacCatalyst/Windows store signing keys and credentials are intentionally absent from the repository and must be supplied through protected CI/store configuration.
- Screenshot blocking, clipboard clearing, background/sleep locking, share-sheet plaintext cleanup, accessibility behavior, and large-file attachment streaming require final platform-by-platform manual validation.
- Dependency vulnerability results and static-analysis findings must be reviewed when CI services execute.
- Independent professional cryptographic/security audit remains outstanding; CipherNest must not be marketed as audited, unhackable, or suitable for high-risk use until that review occurs.

### Deliberately deferred pending dedicated security/platform review
- Cloud synchronization, accounts, collaboration, server storage, and multi-device conflict resolution.
- Autofill/type integration with other apps and browsers.
- TOTP seed storage/generation.
- Document scanning and rich document preview.
- Pronounceable-password mode unless a carefully reviewed design is selected.
- Destructive automatic data wipe after failed unlock attempts.
- Windows Hello biometric unlock until a native implementation can be tested and reviewed.

Deferred features are not represented in the UI as complete.
