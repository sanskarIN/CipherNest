# Changelog

All notable changes are documented here following Semantic Versioning principles.

## [Unreleased]

### Added
- Optional biometric unlock on supported Android, iOS, and Mac Catalyst devices using an independently generated secondary vault-key wrapper; the master passphrase is never stored for biometric unlock.
- Periodic master-passphrase requirement for biometric sessions, with a fresh app process requiring the master passphrase first.
- Guarded plaintext attachment export through an explicit warning and temporary app-cache file cleanup.
- Encrypted last-accessed timestamps and vault sorting by recently used, recently modified, title, or favorites/title.
- Integration coverage for secondary wrapped-key unlock and recent-access persistence.
- Dedicated biometric-unlock security design and limitations documentation.

### Changed
- Restoring a backup clears the local biometric secure-storage secret and disables biometric unlock until it is deliberately configured again.
- Settings now distinguish biometric capability/configuration from master-passphrase recovery and sensitive-setting authentication.

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
