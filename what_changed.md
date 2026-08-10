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
- Added vault sorting options for Favorites & title, Recently used, Recently modified, and Title.
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

The repository contains CI/build/test configuration, but final release gating still requires execution with the actual .NET 10 MAUI workloads and target SDKs. In particular, Android biometric binding/API surface, iOS/Mac Catalyst LocalAuthentication behavior, secure-storage lifecycle, biometric enrollment changes, screenshot blocking, clipboard clearing, app sleep/background behavior, temporary share-file cleanup, accessibility behavior, large attachment streaming, and packaging/signing must be exercised on the real target environments.

Signing certificates, store credentials, API secrets, and private keys remain intentionally absent from the public repository. They must be supplied through protected CI/store configuration and must never be committed.

An independent security audit is still outstanding. The application must not be described as audited, unhackable, military-grade, 100% secure, or appropriate for high-risk use until the relevant implementation and cryptographic design have received independent professional review.

### Deliberately deferred

- cloud synchronization, accounts, collaboration, and server-side storage;
- autofill/type integration with other apps and browsers;
- TOTP seed storage/generation;
- rich document preview and document scanning;
- destructive automatic wipe after failed unlock attempts;
- Windows Hello biometric unlock;
- pronounceable password generation unless a reviewed design is selected.

No deferred feature should be presented by the application as complete.

## 2026-08-09 — Continuation: secure notes, organization, generator, migrations, previews, diagnostics, localization, and release hardening

This pass continued directly from the existing `main` branch and the uploaded CipherNest master build specification. It was intentionally split into many focused commits so cryptographic, persistence, UI, testing, accessibility, documentation, and release changes remain reviewable independently. Every commit created through the connector continues to include `Signed-off-by: Sanskar <sanskarin@outlook.in>`.

### Safe secure notes and checklists

- Added `SafeNotePreview`, `SafeNotePreviewLine`, and explicit line kinds so preview output has a typed model rather than rendering arbitrary HTML.
- Added `ISafeNoteMarkupService` and `SafeNoteMarkupService` in the Application layer.
- Implemented a deliberately small Markdown-like subset: one-to-three-level headings, ordinary paragraphs, `-`/`*` bullets, `- [ ]`/`- [x]` checklists, and fenced code.
- Raw HTML is never interpreted. Angle brackets are neutralized in preview output.
- Added hard limits of 200,000 note characters and 5,000 lines.
- Added local checklist append/toggle primitives with line-length validation.
- Item editor now offers a safe note preview and a secure checklist-entry control.
- Note validation runs before item save so oversized/malformed note work is rejected rather than silently persisted through that UI path.
- Added unit tests for supported syntax, checklist round trips, code fences, and HTML neutralization.
- Added `docs/security/SECURE_NOTES.md` describing the rendering boundary and managed-memory limitation.

### Vault organization, filtering, recent use, and reminders

- Added local item filtering for Favorites, Review due, and every `VaultItemType`.
- Added collection/folder text narrowing without introducing plaintext SQL indexes.
- Retained sorting modes for Favorites & title, Recently used, Recently modified, and Title.
- Added local review-reminder preferences and configurable lead time.
- Vault dashboard calculates review reminders only after decrypting authenticated items while unlocked.
- Changed the recent-use flow so `LastAccessedUtc` is recorded once when the item actually loads. The earlier navigation-level duplicate write was removed.
- Updated the vault filter controls to stack cleanly and changed the bottom action surface to a wrapping `FlexLayout`, improving narrow-phone and resizable-desktop behavior.

### Local security audit expansion

- Added `SecurityFindingKind.DuplicateEntry`.
- Security audit now identifies exact duplicate active entries locally in addition to weak secrets, reused secrets, overdue reviews, and missing titles.
- Duplicate detection uses decrypted in-memory data only while the vault is unlocked; it does not create a plaintext duplicate index on disk.
- Added unit tests covering duplicates, weak/reused secrets, overdue review items, and non-duplicate cases.

### Password/passphrase generator hardening

- Replaced the small memorable-passphrase vocabulary with a source-visible list of exactly 256 unique lowercase words.
- Added startup invariants that fail safely if the word-list count, uniqueness, length, or character rules are violated.
- Raised the memorable-passphrase default to eight words and bounded user selection to 6–16 words.
- Each passphrase word is selected independently with `RandomNumberGenerator`.
- Because the list has 256 entries, the UI reports approximately eight bits of random-selection entropy per generated word when the generated output is kept unchanged.
- The UI explicitly warns that editing generated output can reduce the stated selection entropy.
- Added persisted generator defaults for password/passphrase mode, password length, word count, uppercase/lowercase/digits/symbols, and ambiguous-character exclusion.
- Added a dedicated Generator Defaults Settings page and navigation route.
- Generator page now reloads saved local defaults when it appears.
- Added tests for the exact 256-word invariant, lowercase/uniqueness rules, requested word count, and minimum word bound.
- Added `docs/security/PASSPHRASE_GENERATOR.md` documenting CSPRNG use, selection entropy, heuristic strength estimates, and clipboard limitations.

### Database migration foundation

- Added `DatabaseMigrator` as an explicit ordered transactional schema migration runner.
- Added `MigrationHistory(Version, AppliedUtc)`.
- Database initialization now creates migration history, reads completed version, rejects newer unsupported schema, applies missing migrations transactionally, records completed versions, and verifies the final version.
- Added migration idempotence and future-version rejection integration tests.
- Updated database architecture documentation and ADR/status material so released migrations are treated as append-only compatibility artifacts.

### Cryptographic known-answer and hostile-resource hardening

- Added an Argon2id known-answer test for the current default parameters using fixed passphrase/salt input and expected 32-byte output.
- Found and fixed an untrusted-KDF resource issue: header metadata could previously request excessive Argon2 resource usage before authentication.
- Current accepted parser/resource bounds are salt 16–64 bytes, memory 16–512 MiB, iterations 1–10, parallelism 1–16.
- New wrappers continue to use 64 MiB, 3 iterations, parallelism 1.
- Added hostile-KDF tests.
- Added canonical `docs/security/CRYPTOGRAPHIC_DESIGN.md` covering hierarchy, associated data, KDF bounds, wrappers, records, attachments, backup, known-answer vector, nonce assumptions, memory limits, versioning, and audit status.

### Encrypted attachments and safe document preview

- Added attachment media-type normalization and conservative preview policy.
- Small UTF-8 TXT, Markdown, CSV, JSON, and LOG attachments can be previewed in memory without creating a plaintext preview file first.
- In-memory text preview is capped at 512 KiB, strict UTF-8, sanitized, and display-limited to 20,000 characters.
- Owned byte buffers are zeroed where practical; managed strings cannot be deterministically erased.
- Added multi-megabyte streaming, tamper, and truncation tests.

### Backup corruption and restore preservation tests

- Added integration coverage that corrupts an encrypted backup and verifies restore rejection without replacing the active vault.
- Added wrong-backup-passphrase preservation coverage.
- Existing restore staging/validation/rollback behavior remains production path.

### CSV import parser robustness

- Added malformed-parser corpus tests for unterminated quotes, invalid post-quote characters, duplicate/empty headers, excessive columns, and quoted embedded commas.
- Import remains explicit-column-mapping only.

### Storage and cache management

- Added `IStorageMaintenanceService` and `StorageMaintenanceService`.
- Settings can measure encrypted app-data usage, temporary cache usage, and total local footprint.
- Cache enumeration avoids following reparse-point directory loops.
- Added deliberate temporary-cache cleanup action.
- Redacted diagnostic sharing deletes its temporary cache file after share where possible and surfaces cleanup limitations.

### Privacy-safe centralized diagnostics

- Added `IPrivacySafeExceptionReporter` and `PrivacySafeExceptionReporter`.
- Reporter records only sanitized operation identifier, exception type, HResult, severity, and fixed omission text.
- It intentionally does not log exception messages, stacks, or raw exception objects.
- AppDomain/TaskScheduler/lifecycle failures route through it.
- Added diagnostics privacy source tests and `docs/privacy/DIAGNOSTICS.md`.

### Accessibility and responsive UI

- Added dynamic font-size resources and made LargerInterface functional at runtime.
- ReducedMotion is exposed through runtime resources for motion consumers.
- Accessibility preferences restore at startup/resume.
- Default button minimum height is 44 DIP.
- Added semantic descriptions/live regions and responsive vault actions.

### English-first localization architecture

- Added `AppLanguagePreference` System/English, neutral English `.resx`, localization service, settings persistence, and startup/resume culture application.
- Added `docs/architecture/LOCALIZATION.md`.
- Current release remains honestly English-first; complete Hindi catalog is not claimed.

### In-app security/privacy/legal surfaces

- Added dedicated Security & Privacy page, Settings links, runtime version/build metadata, GPL/privacy/terms references, third-party notices, acknowledgements, repository/support contacts, and honest audit status.
- Added `THIRD_PARTY_NOTICES.md` with release-time exact-package verification requirement.

### Release, packaging, branding, and reproducibility documentation

- Added/expanded packaging, reproducible build, store listing, branding asset, build, release checklist, and test-plan documentation.

### CI and repository quality

- Main core CI restores/builds/runs UnitTests, IntegrationTests, and UiTests.
- Windows CI remains a separate MAUI workload/build gate.
- Dependency-review and CodeQL workflows remain configured.
- Source scans found no TODO/FIXME/NotImplemented/placeholder/fake-service marker at the time of the pass.
- Settings navigation source-generator risk and duplicate recent-access write were fixed.

### Commits created during this continuation

- `feat(notes): add safe note preview model`
- `feat(notes): add safe Markdown service contract`
- `feat(notes): implement bounded safe Markdown subset`
- `test(notes): cover safe Markdown and checklist behavior`
- `feat(notes): register safe note markup service`
- `feat(notes): add safe preview and checklist editing flow`
- `feat(ui): add safe Markdown note preview and checklist controls`
- `feat(vault): add local type collection and review filters`
- `feat(ui): expose vault filters and collection narrowing`
- `feat(audit): add duplicate-entry finding type`
- `feat(audit): detect exact duplicate vault entries locally`
- `test(audit): cover duplicate weak reused and overdue findings`
- `feat(storage): add local storage maintenance contract`
- `feat(storage): implement bounded app data and cache accounting`
- `feat(storage): register storage maintenance service`
- `feat(settings): add local review reminder preferences`
- `feat(reminders): add local review reminder summary`
- `feat(ui): show local review reminders on vault`
- `feat(settings): add reminder and local storage controls`
- `feat(ui): add reminder and storage management settings`
- `feat(generator): add 256-word reviewed local passphrase list`
- `feat(generator): raise memorable passphrase default to eight words`
- `fix(generator): harden passphrase entropy and word-list validation`
- `feat(generator): report passphrase selection entropy conservatively`
- `feat(ui): update passphrase generator guidance and bounds`
- `test(generator): verify local word-list and passphrase bounds`
- `feat(settings): persist password generator defaults`
- `feat(settings): add generator defaults view model`
- `feat(ui): add generator defaults settings page`
- `feat(ui): wire generator defaults page lifecycle`
- `feat(settings): register generator defaults workflow`
- `feat(navigation): register generator defaults route`
- `feat(settings): add generator and security navigation commands`
- `feat(ui): expose generator defaults and security review entry points`
- `feat(generator): load saved generator defaults on open`
- `feat(ui): refresh generator defaults when page appears`
- `feat(accessibility): make typography dynamically scalable`
- `feat(accessibility): apply larger interface preference at runtime`
- `refactor(accessibility): centralize runtime preference application`
- `refactor(accessibility): reuse centralized preference applicator`
- `feat(accessibility): restore interface preferences at startup`
- `feat(database): add ordered transactional migration runner`
- `refactor(database): route schema initialization through migrations`
- `test(database): cover schema migration and future-version rejection`
- `test(crypto): add Argon2id known-answer vector`
- `feat(attachments): define safe in-app preview type policy`
- `feat(attachments): add bounded in-memory text preview`
- `feat(attachments): normalize attachment media types on import`
- `feat(ui): expose safe attachment preview action`
- `test(attachments): cover multi-megabyte streaming round trip`
- `test(attachments): verify tamper and truncation rejection`
- `test(import): exercise malformed CSV parser corpus`
- `test(backup): verify corrupted restore preserves active vault`
- `feat(diagnostics): add privacy-safe exception reporter contract`
- `feat(diagnostics): implement redacted structured exception reporting`
- `feat(diagnostics): register privacy-safe exception reporting`
- `feat(reliability): centralize privacy-safe unhandled exception reporting`
- `feat(localization): add application language preference model`
- `feat(localization): persist language readiness preference`
- `feat(localization): add English resource catalog`
- `feat(localization): add localization service contract`
- `feat(localization): implement English-first resource lookup`
- `feat(localization): register localization service`
- `feat(localization): add language readiness settings workflow`
- `feat(localization): load language preference with settings`
- `feat(ui): expose language readiness setting`
- `feat(localization): apply persisted UI culture on startup and resume`
- `feat(security): add in-app privacy and threat-model surface`
- `feat(ui): wire security information navigation`
- `feat(navigation): add security information route`
- `fix(navigation): open dedicated security information page`
- `docs(licenses): add third-party dependency notices`
- `feat(about): expose security status and third-party notices`
- `feat(about): wire security information navigation`
- `feat(settings): add About and legal navigation`
- `feat(ui): add About legal and acknowledgements entry from settings`
- `feat(about): add runtime build metadata and legal acknowledgements`
- `feat(about): populate version and build from runtime metadata`
- `feat(ui): make vault filters and actions responsive`
- `test(ui): cover navigation accessibility and localization structure`
- `test(ui): cover privacy-safe diagnostics and legal surfaces`
- `ci: include UI structure tests in core quality gate`
- `docs(security): document secure note rendering boundary`
- `docs(security): document password and passphrase generator design`
- `docs(privacy): document redacted diagnostics policy`
- `docs(localization): document English-first resource architecture`
- `docs(release): add store listing and branding guidance`
- `docs(database): document transactional migration behavior`
- `docs(testing): expand security and parser quality gates`
- `docs(release): strengthen release candidate checklist`
- `docs(architecture): refresh security and product decisions`
- `docs(readme): refresh current CipherNest capabilities`
- `docs(privacy): align notice with current local-only data flows`
- `docs(security): extend threat model for preview and diagnostics paths`
- `docs(security): expand responsible disclosure scope`
- `docs(build): add target-specific build and test guidance`
- `docs(release): expand reproducible build guidance`
- `docs(release): add platform packaging guidance`
- `docs(branding): document editable asset sources and generation`
- `docs(architecture): document current layer and security boundaries`
- `fix(security): bound untrusted Argon2 resource parameters`
- `test(security): cover hostile KDF resource parameters`
- `docs(security): add implemented cryptographic design specification`
- `docs(legal): expand local-only recovery and export notices`
- `fix(mvvm): use instance relay commands for settings navigation`
- `fix(vault): record recent access once when item loads`
- `fix(diagnostics): delete temporary redacted export after sharing`
- `docs(changelog): record final security and release hardening`
- `docs(status): include final security tests and release surfaces`
- `docs(testing): add hostile KDF resource validation gate`
- this `what_changed.md` update.

### Verification status for this continuation

The connected GitHub editing environment can inspect/write source but cannot run the current .NET/MAUI solution, execute hosted push workflows through this connector, start target emulators/simulators, access physical biometrics, or produce signed packages. No build/test/device/audit claim is implied by committed source.

### Scope intentionally left for later reviewed versions

- cloud synchronization, accounts, collaboration, server storage, device enrollment, and conflict resolution;
- autofill/type integration;
- TOTP seed storage/generation;
- Windows Hello biometric convenience unlock;
- rich binary/PDF document preview beyond bounded text preview;
- local document scanning;
- pronounceable password mode pending reviewed design;
- automatic destructive wipe after failed attempts;
- complete Hindi/additional translation catalogs beyond English-first architecture.

## 2026-08-09 — Continuation: clipboard safety, lifecycle session reset, trash hardening, large-vault rendering, sensitive-state cleanup, and branding completion

This pass continued directly from the previous `main` head. It focused on explicit clipboard behavior, lifecycle testability, destructive trash handling, master-passphrase security-session transitions, large local vault rendering, sensitive ViewModel lifetime, and source branding variants. Changes remained atomic and signed off.

### Explicit clipboard actions and clearing policy

- Added dedicated item-editor clipboard partial implementation.
- Added explicit username and secret custom-field copy flows.
- Secret custom-field values are not shown in the quick-copy list.
- Added `ClipboardSafetyPolicy` with bounded delay and replacement-preservation behavior.
- Manual/background/timeout locks attempt clipboard cleanup.
- Cleanup failures route through privacy-safe diagnostics.

### Testable lock and failed-attempt policies

- Added `SessionLockPolicy` and tests for background/inactivity/clock rollback.
- Added `UnlockBackoffPolicy` and tests; backoff starts after repeated failures and caps at five minutes.

### Master-passphrase rotation security transition

- Successful master-passphrase change clears entered credentials and remembered master-authentication session, locks the vault, attempts clipboard cleanup, and routes to unlock.

### Sensitive ViewModel lifetime reduction

- Unlock, Settings, Transfer, Trash, Item Editor, and Onboarding clear sensitive bound state when pages disappear.
- Managed strings remain non-deterministically erasable.

### Trash retention and destructive deletion

- Added deterministic `TrashRetentionPolicy` and tests.
- Routine maintenance removes expired trash.
- Manual permanent delete/empty trash require current-master re-authentication and explicit confirmation.

### Large local vault rendering

- Added incremental 50-item visual rendering and Load More/result-count behavior.

### Unlock diagnostics privacy fix

- Replaced remaining raw biometric capability debug exception output with privacy-safe reporting.

### Branding source completion

- Splash includes CipherNest wordmark and `Made by the Sanskar`.
- Added monochrome system mark and dark-surface logo source.

### Tests and source-quality regression coverage added

- Session lock, clipboard, trash retention, unlock backoff, security-session, destructive deletion, responsive rendering, diagnostics, and branding source tests.
- Indexed source scans found no raw Debug.WriteLine/TODO/FIXME/NotImplemented/placeholder markers at that pass.

### Commits created during this continuation

- `feat(clipboard): add explicit username and custom-secret copy flows`
- `feat(ui): expose temporary copy actions for username and custom secrets`
- `feat(memory): clear item-editor sensitive state when leaving page`
- `fix(memory): clear all decrypted item fields when editor closes`
- `feat(vault): add incremental rendering for large local vaults`
- `feat(ui): add vault result counts and incremental load action`
- `feat(lock): add testable session lock policy`
- `test(lock): cover background and inactivity lock policy`
- `refactor(lock): use tested lock policy in app lifecycle`
- `feat(lock): register session lock policy`
- `feat(clipboard): add testable clear-safety policy`
- `refactor(clipboard): enforce bounded clear policy and preserve newer clipboard values`
- `test(clipboard): cover bounded clearing and replacement preservation`
- `feat(clipboard): clear clipboard when lifecycle security locks vault`
- `feat(clipboard): clear clipboard on explicit manual vault lock`
- `feat(trash): add deterministic retention policy`
- `test(trash): cover expiry cutoff and retention bounds`
- `test(trash): avoid ambiguous collection-expression inference`
- `refactor(trash): use shared retention policy for expiry cleanup`
- `feat(trash): run retention cleanup during normal vault maintenance`
- `feat(trash): require master re-authentication for manual permanent deletion`
- `feat(ui): add guarded empty-trash and permanent-delete controls`
- `fix(memory): clear trash re-authentication secret when leaving page`
- `feat(unlock): extract deterministic failed-attempt backoff policy`
- `refactor(unlock): use shared exponential backoff policy`
- `test(unlock): cover failed-attempt backoff schedule and cap`
- `feat(memory): add explicit unlock-passphrase cleanup hook`
- `fix(diagnostics): keep unlock capability errors privacy-safe`
- `feat(branding): add CipherNest wordmark and creator credit to splash`
- `feat(branding): add monochrome CipherNest system mark source`
- `feat(branding): add dark-surface logo variant`
- `fix(diagnostics): report manual-lock clipboard cleanup failures safely`
- `fix(security): require fresh master session after passphrase change`
- `feat(memory): add settings sensitive-state cleanup hook`
- `fix(memory): clear settings passphrases when leaving page`
- `feat(memory): add transfer sensitive-state cleanup hook`
- `fix(memory): clear plaintext-export credentials when leaving transfer page`
- `feat(memory): add onboarding credential cleanup hook`
- `fix(memory): clear onboarding passphrase and recovery material on exit`
- `test(ui): cover security-session clipboard trash and branding hardening`
- `docs(testing): add clipboard lock trash and session-transition gates`
- `docs(branding): record splash monochrome and dark logo sources`
- `docs(security): cover clipboard rotation trash and sensitive-state hardening`
- `docs(changelog): record session clipboard trash paging and branding hardening`
- `docs(status): record clipboard session trash paging and branding completion`
- `docs(readme): align current clipboard trash session and paging behavior`
- `docs(release): add session clipboard trash paging and branding checks`
- this `what_changed.md` update.

### Final follow-up after the continuation ledger

- Added `fix(memory): clear trash passphrase immediately after destructive authentication`.
- Manual permanent-delete and Empty Trash flows now clear destructive passphrase immediately after re-authentication decision.
- Indexed source logging scans remained clean at that pass.
- Hosted CI remained an external gate.

## 2026-08-09 — Continuation: project support link, executable next-step roadmap, and store-build policy

This pass added the requested support URL `https://buymeacoffee.com/sanskarIN` across relevant repository/application surfaces, added a source-controlled execution roadmap, and made the in-app funding CTA build-toggleable for store compliance.

### Project-support URL and metadata

- Added `AppConstants.BuyMeACoffeeUrl`.
- Added `.github/FUNDING.yml` custom link.
- Added support URL to README and SUPPORT.
- About includes voluntary project-support action plus repository/creator actions.
- Support remains explicitly voluntary and does not alter features, security/privacy treatment, support priority, GPL rights, or recovery.

### Centralized About metadata

- About binds product/contact/repository/profile/support metadata from shared constants.
- External links are HTTPS-only and launcher failures use privacy-safe diagnostics.

### Store/distribution build-time funding switch

- Added `BuildFeatureFlags.IsFundingLinkEnabled` and `CipherNestEnableFundingLink` MSBuild property.
- Explicit `false` defines `CIPHERNEST_DISABLE_FUNDING_LINK` and hides in-app funding surface.
- Repository funding metadata is unaffected.

### Current store-policy handling

- Store guidance requires current policy verification for exact target/region/distribution.
- Builds can use `-p:CipherNestEnableFundingLink=false` instead of source edits.

### Executable next-step roadmap

- Added `docs/NEXT_STEPS.md` with priorities for build proof, device security, destructive/recovery, backup/transfer, accessibility/localization, performance, release engineering, security review, launch/open-source operations, and future reviewed versions.

### Documentation and release-gate synchronization

Updated README, SUPPORT, FUNDING, PROJECT_STATUS, CHANGELOG, NEXT_STEPS, RELEASE_CHECKLIST, TEST_PLAN, BUILD, PACKAGING, STORE_LISTING_GUIDE, and progress ledger.

### Commits created during this continuation

- `feat(metadata): add Buy Me a Coffee project support URL`
- `feat(about): expose project support and external link actions`
- `feat(about): open repository creator and support links safely`
- `docs(readme): add Buy Me a Coffee and next-step roadmap links`
- `docs(support): add optional project support link`
- `chore(funding): add Buy Me a Coffee project support link`
- `docs(roadmap): add actionable CipherNest next-step plan`
- `test(ui): cover Buy Me a Coffee support surface and funding metadata`
- `fix(about): use async alert API for external link failures`
- `docs(store): add funding-link policy verification gate`
- `docs(status): add project support metadata and executable next-step roadmap`
- `refactor(about): bind public project metadata to shared constants`
- `test(ui): require centralized About metadata bindings`
- `docs(changelog): record project support and next-step roadmap additions`
- `docs(release): add funding-link and roadmap release gates`
- `feat(build): add compile-time funding-link feature flag`
- `feat(build): allow store builds to disable funding CTA`
- `feat(about): make funding surfaces build-toggleable`
- `feat(about): honor funding-link build policy`
- `test(ui): cover store-toggleable funding CTA`
- `docs(store): document build-time funding CTA switch`
- `docs(build): document funding CTA build override`
- `docs(packaging): add store-specific funding CTA build rule`
- `fix(about): route external link failures through privacy-safe diagnostics`
- `test(ui): cover privacy-safe external link failure handling`
- `docs(readme): document optional funding-link build switch`
- `docs(roadmap): add funding CTA packaging decision`
- `docs(testing): add funding metadata and external link regression gates`
- `fix(build): require explicit false to disable funding CTA`
- `docs(store): align funding switch with explicit false semantics`
- `docs(build): align funding switch with explicit false semantics`
- this `what_changed.md` update.

### Verification limits and next execution point

The connected GitHub environment still cannot execute .NET 10/MAUI workloads, physical-device behavior, store review, signing, or hosted push workflows. Release validation still requires compiling normal/funding-disabled variants and executing all target gates.

## 2026-08-10 — Continuation: cross-platform verification, transient-secret lifetime, clipboard fingerprinting, and platform compile hardening

This continuation followed `docs/NEXT_STEPS.md` Priority 0 and security-hardening work. It expanded configured build proof across every current MAUI target family, added reproducible local verification entry points, hardened native biometric source, contained lifecycle fallback failures, redesigned delayed clipboard cleanup to avoid retaining plaintext secrets, migrated legacy MAUI alert calls, shortened credential lifetime, and removed path/context-bearing raw exception messages from high-risk UI surfaces.

### Cross-platform CI expansion

- Added Android Release compile job.
- Added iOS/Mac Catalyst compile job on macOS.
- Windows builds normal and funding-disabled variants.
- Added core formatting verification.
- Added workflow concurrency/cancel-in-progress and timeouts.
- CodeQL now also builds Android MAUI application path.
- Dependency review also has bounded/cancelable execution.
- Added source tests requiring cross-platform CI gates and verification scripts.

### Reproducible local verification scripts

Added:

- `scripts/verify-core.ps1`
- `scripts/verify-core.sh`
- `scripts/verify-windows.ps1`
- `scripts/verify-android.sh`
- `scripts/verify-apple.sh`
- `docs/verification/CI_GATES.md`

### Native biometric source hardening

- Removed Android `BiometricManager` preflight mismatch from API-28 path.
- Apple cancellation invalidates `LAContext` and checks cancellation.

### Lifecycle fail-closed containment

- Added contained fallback lock/clipboard cleanup with separate privacy-safe reporting.

### Clipboard fingerprint-only delayed cleanup

- Replaced delayed plaintext retention with SHA-256 fingerprint state.
- Uses fixed-time comparison and zeroing owned hash buffers.
- Security timer is independent from initiating caller cancellation after successful copy.
- Lock/timer cleanup only clear if current clipboard still matches CipherNest copy.

### MAUI warnings-as-errors API cleanup

- Converted Transfer, Settings, Trash, Item Editor legacy `DisplayAlert` to `DisplayAlertAsync`.
- Added repository-wide source test rejecting `.DisplayAlert(`.

### Shortened credential binding lifetime

- Unlock, Onboarding, plaintext export, Trash, item re-authentication, biometric settings, backup/restore, master-passphrase rotation, and full-vault deletion clear bound credential properties earlier before long work where practical.

### Backup/restore cleanup and biometric rollback reporting

- Restore temp cleanup is privacy-safe reported.
- Biometric enable rollback cleanup failure is reported separately without masking original failure.

### Redacted sensitive UI error surfaces

- Settings storage/backup/restore/change/delete, Transfer CSV/import/export/cache, and Item attachment/load paths use fixed UI messages and privacy-safe reporter rather than raw exception messages.

### Attachment plaintext staging hardening

- Plaintext attachment export names include attachment ID plus random GUID.
- Cleanup handles IO/access failures and reports without leaking path.

### Added source regression coverage

- Sensitive credential lifetime
- Lifecycle fail-closed containment
- Clipboard fingerprint-only state
- Sensitive error-surface redaction
- MAUI API usage
- CI gate requirements.

### Documentation synchronized

Updated CI gates, build, roadmap, test plan, release checklist, threat model, project status, changelog, README, and this ledger.

### Commits created during this continuation

- `ci: add Android MAUI compile gate`
- `ci: add Apple MAUI compile gates`
- `ci: compile funding-disabled Windows variant`
- `ci: verify core formatting`
- `ci: bound workflow runtime and cancel superseded runs`
- `ci(codeql): analyze Android MAUI application code`
- `ci(codeql): bound analysis runtime and cancel superseded runs`
- `ci(deps): bound dependency review runtime`
- `chore(verify): add PowerShell core verification script`
- `chore(verify): add POSIX core verification script`
- `chore(verify): add Windows MAUI verification script`
- `chore(verify): add Android MAUI verification script`
- `chore(verify): add Apple MAUI verification script`
- `test(ci): require cross-platform build and verification gates`
- `fix(android): avoid BiometricManager API-level mismatch`
- `fix(apple): cancel native biometric prompt with request token`
- `fix(lifecycle): keep fail-closed cleanup exceptions contained`
- `feat(clipboard): add fixed-time secret fingerprint matching`
- `test(clipboard): cover fingerprint matching and bounds`
- `refactor(clipboard): retain only secret fingerprints for cleanup`
- `test(clipboard): use fingerprint policy for clear decisions`
- `refactor(clipboard): remove plaintext comparison policy`
- `test(clipboard): require fingerprint-only delayed cleanup source`
- `fix(maui): use async alerts in transfer workflow`
- `fix(memory): clear plaintext export passphrase after authentication`
- `fix(maui): use async alerts in settings workflows`
- `fix(maui): use async alerts in trash workflows`
- `fix(memory): clear trash passphrase before destructive prompt`
- `fix(maui): use async alerts in item editor workflows`
- `test(maui): reject legacy DisplayAlert calls repository-wide`
- `fix(memory): clear item reauthentication passphrase after use`
- `fix(memory): clear unlock passphrase before authentication work`
- `fix(memory): clear onboarding passphrase before vault creation`
- `fix(memory): shorten biometric settings passphrase lifetime`
- `fix(memory): clear backup passphrase before file and share flows`
- `fix(diagnostics): report restore staging cleanup failures`
- `fix(memory): shorten rotation and vault-deletion credential lifetime`
- `test(security): enforce short-lived credential bindings`
- `test(lifecycle): enforce contained fail-closed cleanup`
- `fix(privacy): redact sensitive settings failure messages`
- `fix(privacy): redact plaintext transfer failure messages`
- `fix(privacy): redact attachment and item-open failures`
- `test(privacy): enforce redacted sensitive error surfaces`
- `docs(verification): document reproducible CI and local gates`
- `docs(build): add reproducible verification scripts and CI gates`
- `docs(roadmap): advance source verification and privacy hardening`
- `docs(testing): add cross-platform CI credential lifetime and privacy gates`
- `docs(security): document fingerprint clipboard and redacted UI failures`
- `docs(status): record cross-platform CI and transient-secret hardening`
- `docs(changelog): record CI credential clipboard and privacy hardening`
- `docs(readme): reflect cross-platform verification and transient-secret hardening`
- `docs(release): add cross-platform CI and privacy hardening gates`
- this progress-file update.

### Final source hygiene check for this continuation

Indexed repository searches returned no `.DisplayAlert(`, `BiometricManager`, `Debug.WriteLine`, TODO/FIXME/NotImplemented unfinished markers, or selected old raw sensitive error strings at that pass.

### Verification limits retained

Configured workflows/tests were not executed by the connector. Platform/device/store/signing/audit gates remain external.

## 2026-08-10 — Continuation: session-key leases, persistence and restore validation, settings normalization, attachment metadata, parser bounds, and source hardening

This continuation started from the cross-platform verification/hardening head and concentrated on integrity boundaries that are easy to miss in a local encrypted vault: settings-file corruption, malformed authenticated metadata, restore/database replacement, attachment filesystem metadata, CSV parser bounds, lock-vs-I/O races, secure-note limit consistency, memory lifetime of owned buffers, and destructive-session races. The work was intentionally divided into focused commits and continues to use `Signed-off-by: Sanskar <sanskarin@outlook.in>` on connector-created commits.

### Settings normalization and write durability

- Added `AppPreferencesPolicy` in the Application layer as the central normalization boundary for persisted non-secret preferences.
- Invalid `AppThemePreference`/`AppLanguagePreference` enum values normalize to `System`.
- Security/settings ranges are normalized consistently on load and save:
  - lock timeout: 5–3600 seconds;
  - clipboard clear: 5–300 seconds;
  - trash retention: 1–365 days;
  - periodic master check: 1–168 hours;
  - backup reminder: 1–365 days;
  - review lead: 0–365 days;
  - password length: 8–256;
  - passphrase words: 6–16.
- Password mode cannot persist with every character group disabled; lowercase is restored as a safe valid default. Passphrase mode may leave those character-group settings off because they are not used there.
- `JsonSettingsStore` normalizes on read and write, falls back to defaults on malformed JSON, and attempts to remove a stale `.tmp` staging file without allowing cleanup failure to mask the original write result.
- Added policy tests plus real JSON-store round-trip, malformed-JSON fallback, out-of-range normalization, and temp-file assertions.

### Vault item and attachment metadata validation

- Expanded `VaultItemValidator` to be defensive against runtime-null values produced by malformed JSON despite non-nullable model declarations.
- Item validation now rejects empty item IDs, unknown item types, null/oversized strings, missing collections, invalid tags/custom fields, and existing field/count limits without relying on callers to have produced well-formed CLR objects.
- Attachment metadata validation now checks attachment ID, display name, media type, 100 MiB plaintext bound, encrypted storage-name presence/size, and per-item uniqueness of both attachment IDs and storage names.
- Added focused tests for field/count limits, null runtime payloads, unknown enum values, attachment metadata bounds, and duplicate attachment metadata.

### Shared secure-note storage and renderer limits

- Added `SafeNoteLimits` as the single policy for 200,000-character and 5,000-line secure-note bounds.
- `SafeNoteMarkupService` parsing, checklist append, and checklist toggle use the shared constants.
- `VaultItemValidator` applies the same character/line limits to every item save path.
- CSV import/programmatic saves can no longer persist a note that the secure-note renderer rejects solely because an earlier path allowed a larger note.
- Added validation coverage for both shared character and line boundaries.

### Attachment plaintext-buffer and staging cleanup hardening

- `EncryptedAttachmentStore` zeroes its reusable plaintext encryption buffer after each encrypted chunk and again on exit.
- Temporary attachment-encryption staging cleanup is best-effort for I/O/access errors so a cleanup failure does not replace the original encryption failure.
- Existing decrypted chunk buffers remain zeroed after destination writes.
- Added source regression coverage for plaintext-buffer zeroing and non-masking temp cleanup.

### Opaque attachment storage-name policy

- Added `AttachmentStorageNamePolicy`.
- Filesystem-facing encrypted attachment names must be a GUID `N` stem plus `.cna`.
- `/` and `\` separators, wrong extensions, malformed identifiers, and other path-like values are rejected before `Path.Combine`/file access.
- Valid names normalize to lowercase GUID `.cna` form.
- Added unit coverage for accepted generated names and rejected malformed/separator/wrong-extension cases.

### Vault-header compatibility boundary

- Explicitly defined supported vault-header range: minimum version 1, current version 2.
- New writes use current version 2.
- Future/unknown versions and missing master-wrapper metadata are rejected before key unwrap.
- Added integration coverage proving a future header version cannot unlock and current headers remain unlockable.

### Decrypted-record identity and metadata validation

- `VaultService.DecryptItem` now validates the authenticated plaintext object before returning it from Infrastructure.
- The payload `VaultItem.Id` must equal the SQLite row ID that was authenticated as associated data.
- The null-safe `VaultItemValidator` must report no metadata errors.
- Invalid authenticated JSON payloads therefore fail at the infrastructure boundary instead of reaching local search/UI code and failing later with unrelated null/path errors.
- Serialized plaintext record bytes remain zeroed in `finally` regardless of validation outcome.
- Added source regression coverage for row-ID binding, validator use, fixed failure text, and plaintext zeroing.

### Permanent item-deletion ordering

- Permanent item deletion snapshots encrypted attachment storage names, removes the authenticated database record first, and only then performs best-effort encrypted attachment cleanup.
- This avoids a database-delete failure leaving a still-present item whose attachment files were already intentionally removed.
- Cleanup tolerates I/O/access/invalid-storage-name failures as best effort; logical database deletion remains authoritative.
- Added source ordering tests.

### Backup-header validation before Argon2

- Added `BackupFormatPolicy` with explicit current format version and untrusted metadata bounds.
- Backup restore validates:
  - format version 2;
  - salt 16–64 bytes;
  - Argon2 memory 16–512 MiB;
  - iterations 1–10;
  - parallelism 1–16;
  - chunk size 64 KiB–4 MiB.
- Header validation occurs before `_crypto.DeriveKey`, preventing hostile backup metadata from requesting excessive Argon2 work before rejection.
- Missing salt/KDF metadata is rejected as invalid backup header data.
- Backup export staging cleanup was also changed to non-masking best-effort cleanup.
- Added unit tests for accepted/default header parameters and a corpus of out-of-range version/salt/KDF/chunk values plus a source-order test proving validation precedes derivation.

### Database migration shape validation and rollback preservation

- `DatabaseMigrator` now verifies required current table/column shapes after reaching `DatabaseSchemaVersion`:
  - `VaultHeader(Id, HeaderJson)`;
  - `VaultItems(Id, Envelope)`;
  - `AppSettings(Key, Value)`;
  - `MigrationHistory(Version, AppliedUtc)`.
- A forged `MigrationHistory` row that merely claims the current version no longer makes an incomplete database look valid.
- Migration rollback uses `CancellationToken.None` and catches secondary SQLite/invalid-state rollback failures so the original migration exception is preserved.
- Added integration coverage for forged current-version history with missing required tables.

### Replacement database validation before active-file mutation

- `SqliteVaultStore.ReplaceDatabaseAsync` validates the candidate file before removing active WAL/SHM sidecars or moving the active database.
- Candidate validation opens the staged DB read-only and requires:
  - `PRAGMA quick_check;` = `ok`;
  - exact supported database schema version;
  - required current table/column shape.
- Invalid staged databases leave the active database untouched.
- Added integration coverage that writes a marker header to the active vault, supplies a structurally invalid replacement, requires rejection, and verifies the active header survives.
- If the actual replacement copy fails after the old DB was moved to `.previous`, CipherNest attempts rollback while preserving the original copy failure even if rollback-file movement also hits an I/O/access problem.
- Added source tests for validation-before-mutation and recovery ordering.

### CSV final-field column bound and parser allocation cleanup

- Fixed a parser off-by-one: the final field at newline/EOF now passes through the same `AddField` maximum-column check as comma-terminated fields.
- A row can no longer end with a 257th field and bypass the 256-column cap.
- Added an integration test importing a 257-column data row into a real unlocked disposable vault and requiring zero imported items.
- Reused a single one-character buffer inside `CsvParser` rather than allocating a new array for every input character.

### Per-session cancellable vault key leases

- Added `VaultKeyLease` to eliminate operations retaining a reference to the same mutable `_dataKey` array that `LockAsync` zeroes.
- A lease owns a private 32-byte DEK copy, links the caller cancellation token with the current per-unlock session token, and zeroes its key on `Dispose`.
- Invalid key material supplied to a lease is zeroed before constructor failure.
- `VaultService` now synchronizes access to the session key and session CTS.
- Key-sensitive operations use leases, including record reads/writes, re-authentication, secondary-wrapper changes, master-wrapper rotation, permanent deletion, and attachment import/remove/export.
- Record persistence checks the lease token before encryption and before the database write.
- Record reads check session cancellation while decrypting and before returning results.
- Locking clears/zeroes the shared session DEK and cancels/disposes the active session token, while each in-flight lease remains an independent buffer that is cancelled and then zeroed on disposal.
- Replacing an unlocked session cancels the previous session and zeroes its shared key.
- Added source tests requiring key copies, linked cancellation, zeroing, session cancellation, and removal of the old direct `RequireKey()` pattern.

### Lock cancels in-flight plaintext attachment export

- Added `VaultLockCancellationIntegrationTests`.
- The test creates an encrypted multi-chunk attachment and exports to a destination stream that deliberately blocks when plaintext writing begins.
- After write begins, the test locks the vault.
- The export must terminate with cancellation and the vault must report locked.
- This is an automated concurrency invariant for the application/session boundary; target platform share-sheet behavior still requires device validation.

### Serialized vault security transitions

- Master/recovery unlock, secondary unlock, public lock, creation, and full-vault deletion now coordinate through the service transition semaphore.
- A lock cannot be overtaken by a concurrently running unlock merely because the unlock's KDF/key-unwrapping work finishes later and publishes a session afterward.
- Shared session clearing is centralized in `ClearSessionKey`, which zeroes the DEK under synchronization and cancels/disposes the current session CTS.
- Added source tests that require the same gate in master unlock, secondary unlock, lock, and full-vault deletion.

### Live-session authorization for full-vault deletion

- Full-vault deletion still requires successful current-master re-authentication.
- It then acquires a live `VaultKeyLease` before waiting on the serialized transition gate.
- The gate wait uses that authorization lease token and checks it again before destroying the current session.
- If an intervening lock/unlock invalidates that session while deletion is waiting, deletion is cancelled rather than proceeding under stale re-authentication from a previous security session.
- Added source coverage requiring the live authorization lease and session-linked gate wait.

### Generator temporary-array cleanup

- Password generation now clears its temporary `char[]` after constructing the returned managed string.
- Passphrase generation clears the temporary selected-word-reference array after `string.Join` creates the returned string.
- This reduces extra application-held copies but does not claim deterministic erasure of the returned immutable .NET string.
- Added source regression tests for both cleanup calls.

### Guarded local storage/cache enumeration

- Fixed a lazy-enumeration reliability issue in `StorageMaintenanceService`: `Directory.EnumerateFiles/EnumerateDirectories` could throw while iterating outside the apparent try/catch.
- Measurement and top-level cache cleanup now materialize the relevant enumeration inside guarded blocks.
- Reparse-point directories remain excluded from recursive traversal.
- Added source tests requiring guarded materialization and reparse-point handling.

### Tests added/expanded in this continuation

Added or expanded:

- `AppPreferencesPolicyTests`
- `JsonSettingsStoreTests`
- `VaultItemValidatorTests`
- `AttachmentStoreSecuritySourceTests`
- `AttachmentStorageNamePolicyTests`
- `VaultHeaderCompatibilityIntegrationTests`
- `VaultDeletionOrderingSourceTests`
- `BackupFormatPolicyTests`
- `BackupFormatSourceTests`
- `DatabaseMigrationTests`
- `DatabaseReplacementSourceTests`
- `CsvColumnLimitIntegrationTests`
- `VaultLockCancellationIntegrationTests`
- `VaultKeyLeaseSourceTests`
- `DecryptedRecordValidationSourceTests`
- `GeneratorMemorySourceTests`
- `StorageMaintenanceSourceTests`
- `VaultSessionTransitionSourceTests`
- existing secure-note tests plus shared note-limit validation cases.

### Documentation synchronized in this continuation

Updated:

- `docs/security/CRYPTOGRAPHIC_DESIGN.md`
- `docs/security/THREAT_MODEL.md`
- `docs/architecture/DATABASE.md`
- `docs/TEST_PLAN.md`
- `docs/RELEASE_CHECKLIST.md`
- `docs/NEXT_STEPS.md`
- `PROJECT_STATUS.md`
- `CHANGELOG.md`
- `README.md`
- this `what_changed.md` continuation ledger.

### Commits created during this continuation

- `feat(settings): add centralized preference normalization policy`
- `fix(settings): normalize persisted preferences and clean temp writes`
- `test(settings): cover preference normalization bounds`
- `test(settings): cover settings round trip corruption and normalization`
- `test(vault): cover item validation limits and collection bounds`
- `fix(memory): zero attachment plaintext buffers after encryption`
- `fix(attachments): keep staging cleanup from masking encryption failures`
- `test(attachments): enforce plaintext buffer zeroing and safe cleanup`
- `fix(vault): reject unsupported vault header versions`
- `fix(vault): delete records before best-effort attachment cleanup`
- `test(vault): reject unsupported future header versions`
- `test(vault): enforce header compatibility and deletion ordering`
- `feat(backup): add explicit untrusted header validation policy`
- `test(backup): cover untrusted header resource bounds`
- `fix(backup): validate untrusted header before Argon2 work`
- `fix(backup): reference KDF parameters from application contract`
- `test(backup): enforce header validation before key derivation`
- `fix(database): validate migrated schema and preserve original failures`
- `test(database): reject forged current schema history`
- `refactor(database): expose current schema validation within infrastructure`
- `fix(database): validate replacement vault before active-file swap`
- `test(database): preserve active vault on invalid replacement`
- `feat(attachments): add opaque storage filename validation policy`
- `fix(attachments): validate opaque storage names before file access`
- `test(attachments): reject malformed opaque storage names`
- `fix(import): enforce CSV column bound on final field`
- `perf(import): reuse CSV parser character buffer`
- `test(import): reject excessive columns in data rows`
- `feat(security): add cancellable zeroing vault key lease`
- `fix(security): isolate vault operations with cancellable key leases`
- `fix(memory): zero rejected vault key lease material`
- `test(security): verify lock cancels in-flight plaintext attachment export`
- `test(security): enforce cancellable zeroing vault key leases`
- `test(security): use generic completion source for lock cancellation test`
- `fix(test): import KDF contract from application namespace`
- `feat(validation): validate attachment metadata and uniqueness`
- `test(validation): cover attachment metadata and uniqueness rules`
- `fix(database): preserve replacement failure during rollback attempt`
- `test(database): enforce replacement validation and rollback ordering`
- `fix(memory): clear generator temporary secret arrays`
- `test(generator): enforce temporary secret-array cleanup`
- `fix(validation): reject null and invalid decrypted item metadata safely`
- `test(validation): cover runtime-null and invalid-enum item payloads`
- `feat(notes): centralize secure-note size limits`
- `refactor(notes): use shared secure-note limits`
- `fix(validation): align stored note limits with safe preview bounds`
- `test(notes): align vault validation with shared note limits`
- `fix(test): use unambiguous string separator in note limit test`
- `fix(vault): validate decrypted record identity and metadata`
- `test(vault): enforce decrypted record validation boundary`
- `fix(storage): guard lazy directory enumeration failures`
- `test(storage): enforce guarded directory enumeration`
- `docs(testing): add settings database key-lease and validation gates`
- `docs(security): document key leases restore validation and metadata bounds`
- `docs(database): document schema-shape and replacement validation boundaries`
- `docs(release): add restore key-lease settings and metadata gates`
- `docs(roadmap): add latest persistence key-lease and validation follow-ups`
- `docs(status): record persistence session and metadata hardening`
- `fix(security): serialize vault lock unlock and deletion transitions`
- `test(security): enforce serialized vault session transitions`
- `fix(security): require live session while vault deletion waits for gate`
- `test(security): require live-session authorization for vault deletion`
- `docs(changelog): record persistence session and parser hardening`
- `docs(readme): reflect key leases restore validation and settings hardening`
- `docs(security): update cryptographic design for key leases and restore validation`
- `docs(status): add serialized session transitions and live deletion authorization`
- `docs(testing): add serialized session transition race gates`
- `docs(release): add session transition and stale-authorization gates`
- this progress-file update.

### Final indexed source hygiene check

Immediately before this progress update, indexed repository searches returned no matches for:

- `TODO FIXME NotImplementedException`
- raw `ex.Message`
- legacy `.DisplayAlert(`
- `BiometricManager`
- the removed direct `RequireKey()` pattern.

These searches are source-review signals only and do not prove compilation, execution, or platform behavior.

### Verification limits retained

The GitHub connector used for this work can inspect/write repository content but cannot execute the .NET 10/MAUI workloads, run the direct-push GitHub Actions jobs as a local substitute, launch Android/iOS/macOS/Windows target environments, exercise real biometric/clipboard/screenshot/lifecycle behavior, sign packages, or perform store review.

Accordingly this continuation does **not** claim that the newly added settings tests, backup-header tests, schema/replacement tests, vault-header tests, key-lease cancellation tests, session-transition source tests, attachment-name/metadata tests, CSV bound tests, note-limit tests, storage source tests, or the configured CI jobs have passed the final head merely because their source is present.

The immediate next execution point remains evidence collection from `docs/NEXT_STEPS.md` Priority 0 and `docs/verification/CI_GATES.md`: execute clean core/platform verification, fix every resulting compiler/analyzer/test/workload issue, then continue through transition-race/device-security validation, backup/restore/database-replacement compatibility, transfer behavior, accessibility/localization, performance, dependency/license review, signed packaging, store-policy checks, and independent security review.

No signing credential, store credential, API secret, private key, vault secret, recovery material, or production analytics/crash token was added to source control during this continuation.
