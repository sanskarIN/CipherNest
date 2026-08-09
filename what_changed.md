# What Changed

## 2026-08-09 — Initial implementation

The repository was initialized from the uploaded `03_CipherNest_Secure_Vault_Master_Prompt.md` and implementation was divided internally into architecture, security core, application workflows, MAUI UI, tests, CI, and release documentation so no required layer is silently omitted.

### Repository constraint

The connected GitHub write API accepts commit messages but does not expose an `author.email`/`committer.email` parameter. Therefore the requested `sanskarin@outlook.in` cannot be forced as Git object metadata through this connector. Every commit created for this build includes `Signed-off-by: Sanskar <sanskarin@outlook.in>` in the commit message to preserve the requested identity in the repository history. GitHub itself determines connector commit authorship.

### Security design

- Added an explicit threat model and cryptographic design before implementation.
- Chosen envelope: random 256-bit vault DEK, Argon2id-derived KEK, AES-256-GCM record/key wrapping, unique random nonces, authenticated associated data, and format versioning.
- Added managed-runtime memory-erasure limitations and audit status without making absolute security claims.
- TOTP, cloud sync, autofill, and destructive wipe-on-failure remain deferred until dedicated security review.

### Product implementation

- Multi-project .NET MAUI solution with Domain/Application/Infrastructure/Shared/App boundaries.
- Local encrypted SQLite vault and lifecycle-aware vault service.
- Master-passphrase setup/unlock, optional one-time recovery key, masked secrets, local search, generator, audit, encrypted backup/restore, settings, About/open-source information, clipboard lifecycle, and manual/automatic lock foundation.
- Encrypted streaming attachments, collections, trash retention, review reminders, custom fields, guarded plaintext transfer, master-passphrase rotation, per-item re-authentication, local deletion flow, and privacy-aware developer diagnostics.
- Localization-ready resources, light/dark theming, accessible labels, responsive MAUI layouts, original SVG branding, and platform project metadata.

### Quality

- Unit/integration test source for crypto tampering, wrong passphrase, generator, vault CRUD/search, backup restore, CSV safety, passphrase changes, and destructive lifecycle behavior.
- GitHub Actions CI plus dependency review and CodeQL workflows.
- Security/privacy/support/contribution/release/setup/troubleshooting documentation.

## 2026-08-09 — Continuation: biometric unlock, recent-use organization, attachment export, and hardening

This continuation was implemented as many small, reviewable commits instead of one large commit. All commits created through the connector continue to carry `Signed-off-by: Sanskar <sanskarin@outlook.in>`.

### Optional biometric unlock architecture

- Added secondary-unlock methods to `IVaultService` rather than storing or replaying the master passphrase.
- Extended the versioned vault header with an optional secondary wrapped-key envelope while preserving the existing master and recovery wrappers.
- Enabling biometric unlock requires:
  1. an already unlocked vault,
  2. successful current master-passphrase re-authentication,
  3. successful native biometric authentication,
  4. generation of a fresh random 384-bit secondary secret,
  5. storage of that secondary secret through MAUI `SecureStorage`, and
  6. creation of a separate authenticated wrapper for the same random vault DEK.
- Disabling biometric unlock requires the current master passphrase and removes both the secondary vault-header wrapper and local secure-storage value.
- Added Android native `BiometricPrompt` path for API 28+ and iOS/Mac Catalyst `LocalAuthentication.LAContext` path. Windows intentionally falls back to master-passphrase unlock until a Windows Hello implementation is separately reviewed and tested.
- Added a fresh-process security rule: a new process begins with no remembered master-authentication timestamp, so the master passphrase is required before biometric convenience unlock becomes available.
- Added configurable periodic master-passphrase enforcement from 1 to 168 hours.
- Recovery-key unlock does not satisfy the master-passphrase session requirement because recovery material is not treated as authorization for security-sensitive settings.
- Backup restore clears the local biometric secondary secret and disables the biometric preference so restored metadata cannot silently pair with stale secure-storage material from the current installation.
- Vault deletion clears biometric secure-storage material and the in-memory master-authentication session state.
- Added `docs/security/BIOMETRIC_UNLOCK.md` documenting the design, platform support, and important limitation that the current SecureStorage value retrieval is not claimed to be cryptographically hardware-bound to every biometric prompt.
- Extended `docs/security/THREAT_MODEL.md` for biometric convenience unlock, restored-wrapper mismatch, secure-storage compromise, and plaintext attachment-export risks.

### Biometric UI and settings

- Added an unlock-screen biometric action that appears only when the preference is enabled, a secondary wrapper is configured, the platform reports biometric support, and the periodic master-passphrase rule is satisfied.
- Added fallback messages that always direct users to the master passphrase when biometric capability, enrollment, secure storage, or wrapper authentication fails.
- Added Settings controls for enabling/disabling biometric unlock and configuring the periodic master-passphrase interval.
- Added explicit text that biometrics do not replace the master passphrase or recovery limitations.

### Recent-use organization

- Added encrypted `LastAccessedUtc` data to `VaultItem`; it is part of the encrypted record payload rather than new plaintext database metadata.
- Added `IVaultService.MarkAccessedAsync` and a persistence path that updates access time without changing the item's user-visible modification timestamp.
- Opening an item records recent access locally.
- Added vault sorting options for:
  - Favorites & title
  - Recently used
  - Recently modified
  - Title
- Search results continue to be decrypted and processed only while the vault is unlocked, then use the selected sort order locally.

### Attachment export

- Added a complete attachment export command and matching UI action.
- Export is blocked until per-item master re-authentication is satisfied when that item requires it.
- Before export, the user receives a warning that export must create a temporary plaintext copy and that the operating system, receiving app, cloud provider, backups, or caches can retain copies.
- The temporary plaintext file is created under the application cache with a sanitized filename, passed to the operating-system share surface, and deletion is attempted immediately after the share request returns.
- If temporary-file deletion cannot be confirmed, CipherNest displays a cleanup warning instead of pretending deletion succeeded.
- The encrypted source attachment remains unchanged.

### Added/updated tests

- Added `SecondaryUnlockIntegrationTests` covering successful secondary unlock, rejection of a wrong secondary secret, master-passphrase requirement for disabling the wrapper, and removal of the wrapper.
- Added `RecentAccessIntegrationTests` verifying that access timestamps persist while `ModifiedUtc` remains unchanged.
- Existing crypto, backup, vault, CSV parser, passphrase-change, deletion, attachment, and UI-structure test sources remain in place.

### Documentation consistency fixes

- Updated `README.md` so it no longer incorrectly says plaintext export is excluded; it now distinguishes recommended encrypted backup from explicitly warned plaintext CSV/attachment export.
- Updated `CHANGELOG.md` with an Unreleased section for biometric unlock, recent-use sorting, guarded attachment export, and associated tests.
- Updated `PROJECT_STATUS.md` so completed, hardware-dependent, externally verified, and deliberately deferred work are separated accurately.
- Updated the threat model and added the dedicated biometric design document.

### Commits created during this continuation

The continuation was intentionally split into atomic commits, including:

- `feat(security): add secondary biometric unlock contract`
- `feat(security): implement secondary wrapped-key unlock`
- `feat(biometrics): add biometric unlock service contract`
- `feat(biometrics): implement native biometric authentication and secure storage`
- `feat(biometrics): register biometric unlock service`
- `feat(settings): persist biometric unlock preference`
- `feat(settings): add guarded biometric enable and disable flows`
- `feat(unlock): add optional biometric unlock flow`
- `feat(ui): expose biometric unlock on lock screen`
- `feat(ui): refresh biometric capability when unlock page appears`
- `feat(ui): add biometric controls to settings`
- `test(security): cover secondary wrapped-key unlock lifecycle`
- `docs(security): document biometric unlock design and limitations`
- `feat(security): add in-memory master-auth session state`
- `feat(security): register master-auth session state`
- `feat(unlock): enforce periodic master passphrase before biometrics`
- `feat(settings): expose periodic master-passphrase interval`
- `feat(ui): add periodic master-auth setting`
- `feat(attachments): add guarded plaintext attachment export`
- `feat(ui): add guarded attachment export action`
- `feat(vault): track encrypted last-accessed timestamps`
- `feat(vault): add recent-access update contract`
- `feat(vault): persist recent access without changing modified time`
- `feat(vault): add recent-use sorting and access tracking`
- `feat(ui): add vault sorting controls`
- `test(vault): cover encrypted recent-access tracking`
- `docs(changelog): record biometric recent-use and transfer features`
- `docs(security): extend threat model for biometric unlock`
- `docs(status): update implemented and deferred CipherNest scope`
- `docs(readme): align current security and transfer capabilities`
- this progress-file update.

### Verification and remaining external gates

The connected GitHub environment can inspect and edit repository content but does not provide a local .NET/MAUI SDK, Android emulator, Apple simulator, Windows packaging environment, or physical biometric hardware. Therefore this work does **not** claim that platform builds or physical-device tests have passed merely because the source has been committed.

The repository contains CI/build/test configuration, but final release gating still requires execution with the actual .NET 10 MAUI workloads and target SDKs. In particular, the Android biometric binding/API surface, iOS/Mac Catalyst LocalAuthentication behavior, secure-storage lifecycle, biometric enrollment changes, screenshot blocking, clipboard clearing, app sleep/background behavior, temporary share-file cleanup, accessibility behavior, large attachment streaming, and packaging/signing must be exercised on the real target environments.

Signing certificates, store credentials, API secrets, and private keys remain intentionally absent from the public repository. They must be supplied through protected CI/store configuration and must never be committed.

An independent security audit is still outstanding. The application must not be described as audited, unhackable, military-grade, 100% secure, or appropriate for high-risk use until the relevant implementation and cryptographic design have received independent professional review.

### Deliberately deferred

These items remain intentionally outside the current local-only release until dedicated design/review work is completed:

- cloud synchronization, accounts, collaboration, and server-side storage;
- autofill/type integration with other apps and browsers;
- TOTP seed storage/generation;
- rich document preview and document scanning;
- destructive automatic wipe after failed unlock attempts;
- Windows Hello biometric unlock;
- pronounceable password generation unless a reviewed design is selected.

No deferred feature should be presented by the application as complete.
