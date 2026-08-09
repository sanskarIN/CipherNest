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
- Database initialization now:
  1. creates migration history if required,
  2. reads the highest completed version,
  3. rejects a database created by a newer unsupported schema,
  4. applies missing migrations transactionally,
  5. records completed versions, and
  6. verifies the final version equals `AppConstants.DatabaseSchemaVersion`.
- Added migration idempotence and future-version rejection integration tests.
- Updated database architecture documentation and ADR/status material so released migrations are treated as append-only compatibility artifacts.

### Cryptographic known-answer and hostile-resource hardening

- Added an Argon2id known-answer test for the current default parameters using fixed passphrase/salt input and expected 32-byte output.
- During the source audit, found a real hardening issue: KDF parameters stored in a vault/backup header were minimum-bounded but did not have upper resource bounds. A malicious unauthenticated container could therefore request excessive Argon2 memory/iterations/parallelism before authentication completed.
- Fixed that issue by validating untrusted KDF metadata before Argon2 allocation/work.
- Current accepted parser/resource bounds are:
  - salt length: 16–64 bytes,
  - memory: 16 MiB–512 MiB,
  - iterations: 1–10,
  - parallelism: 1–16.
- New key wrappers continue to use the current default of 64 MiB, 3 iterations, parallelism 1.
- Wrapped-key validation maps hostile/out-of-bounds KDF metadata to vault authentication failure rather than honoring the resource request.
- Added dedicated hostile-KDF tests for too-small/too-large memory, invalid iterations/parallelism, and a malicious wrapped-key envelope.
- Added the previously missing canonical `docs/security/CRYPTOGRAPHIC_DESIGN.md` so in-app and repository security references no longer point to a nonexistent design document.
- The cryptographic design document now records key hierarchy, associated data, KDF bounds, recovery/biometric wrappers, record/attachment/backup encryption, known-answer vector, nonce assumptions, memory limitations, versioning, and audit status.

### Encrypted attachments and safe document preview

- Added attachment media-type normalization and a conservative preview policy.
- Small UTF-8 TXT, Markdown, CSV, JSON, and LOG attachments can now be previewed in memory without first creating a plaintext preview file.
- In-memory text preview is capped at 512 KiB, requires strict UTF-8 decoding, replaces unsafe control characters, neutralizes angle brackets, and limits displayed text to 20,000 characters.
- CipherNest zeroes the owned byte buffer after preview where practical and explicitly states that the resulting managed `string` cannot be deterministically erased.
- Other file formats remain encrypted until the user explicitly selects guarded plaintext export.
- Attachment import records a normalized/inferred media type for supported text formats rather than blindly treating every selected file as `application/octet-stream`.
- Added an 8 MiB streaming attachment integration test that verifies round-trip SHA-256 equality without whole-file buffering.
- Added encrypted attachment tamper and truncation tests.
- Existing explicit plaintext attachment export warning/cache-cleanup path remains in place.

### Backup corruption and restore preservation tests

- Added integration coverage that corrupts an encrypted backup and verifies restore rejection occurs without replacing the current active vault.
- Added wrong-backup-passphrase preservation coverage.
- Existing restore staging/validation/rollback behavior remains the production path.
- Updated test/release documentation so restore preservation is an explicit quality gate rather than an implied assumption.

### CSV import parser robustness

- Added malformed-parser corpus tests covering unterminated quotes, invalid characters after closing quotes, duplicate/empty headers, excessive columns, and quoted embedded commas.
- Existing parser field/row/column bounds remain enforced.
- Import remains explicit-column-mapping only; no silent guessing of secret fields was introduced.

### Storage and cache management

- Added `IStorageMaintenanceService` and `StorageMaintenanceService`.
- Settings can measure encrypted app-data usage, temporary cache usage, and total local footprint.
- Cache enumeration avoids following reparse-point directory loops.
- Settings now provides a deliberate temporary-cache cleanup action with a warning that it does not delete the encrypted vault, attachment store, or backups kept in app data.
- Redacted diagnostic sharing now deletes its temporary cache file after the share request returns where the OS permits it; if deletion cannot be confirmed, the UI directs the user to the cache-cleanup control.

### Privacy-safe centralized diagnostics

- Added `IPrivacySafeExceptionReporter` and `PrivacySafeExceptionReporter`.
- The reporter records only a sanitized operation identifier, exception type, HResult, severity, and fixed text saying the sensitive exception details were omitted.
- It deliberately does not log the `Exception` object, exception message, or stack trace because those can contain file paths or application/user context.
- `AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` now route through this privacy-safe reporter.
- Lifecycle preference/resume failures also use this reporter before the app takes its fail-closed lock/default behavior.
- Added UI/source-structure tests that specifically check the reporter source does not log exception messages/stacks or pass the raw exception object to the error logger.
- Added `docs/privacy/DIAGNOSTICS.md`.

### Accessibility and responsive UI

- Added dynamic font-size resources for body, caption, title, and controls.
- `LargerInterface` now changes runtime typography rather than being a stored setting with no effect.
- `ReducedMotion` preference is also applied to runtime resource state for feature-level motion consumers.
- Accessibility preferences are restored at startup/resume.
- Default button minimum height is 44 device-independent units.
- Added/retained semantic descriptions and live-region metadata on security-sensitive screens.
- Vault actions now wrap instead of forcing a horizontally scrolling navigation strip.
- Added UI-structure checks for core routes, semantic metadata, localization structure, responsive action wrapping, legal surfaces, and diagnostics-source privacy constraints.

### English-first localization architecture

- Added `AppLanguagePreference` with `System` and `English`.
- Added a neutral English `.resx` catalog under `Resources/Localization/AppStrings.resx`.
- Added `ILocalizationService` / `LocalizationService` using resource lookup and saved UI-culture preference.
- Added language preference to `AppPreferences` and Settings.
- Saved language preference is applied at startup/resume and when changed in Settings.
- Added `docs/architecture/LOCALIZATION.md` with the steps for future Hindi/additional catalogs.
- Current release is still honestly described as English-first: the resource/service/preference architecture exists, but not every existing literal XAML string has been migrated to a resource binding and no complete Hindi catalog is claimed.

### In-app security/privacy/legal surfaces

- Added a dedicated Security & Privacy page covering local-only data flow, protection goals, partial mitigations, out-of-scope threats, recovery limitations, audit status, and responsible disclosure.
- Settings now links directly to the local security audit and Security & Privacy page.
- About now displays runtime application version/build plus cryptographic format and database schema versions instead of only static version text.
- About now includes GPL-3.0-or-later/open-source information, privacy/terms references, third-party notices, acknowledgements, repository/support contacts, and honest audit status.
- Settings now provides an About/legal/acknowledgements entry point.
- Added `THIRD_PARTY_NOTICES.md` for runtime/test dependency license families with an explicit release-time exact-package verification requirement.
- Expanded `PRIVACY.md`, `TERMS.md`, `SECURITY.md`, the threat model, architecture docs, and ADR summary to match current behavior.

### Release, packaging, branding, and reproducibility documentation

- Added `docs/releases/PACKAGING.md` with Android, Windows, iOS, and Mac Catalyst release/signing guidance.
- Expanded `docs/releases/REPRODUCIBLE_BUILDS.md` with clean-checkout/environment/package-feed/unsigned-comparison/signing guidance while explicitly refusing an unverified bit-for-bit reproducibility claim.
- Added `docs/releases/STORE_LISTING_GUIDE.md` with honest product positioning, privacy screenshot rules, security-claim restrictions, and asset verification gates.
- Added `docs/branding/ASSETS.md` documenting the editable SVG asset sources, generation path, adaptive/small-icon rules, monochrome derivation, watermark placement, and future favicon handling.
- Expanded `docs/setup/BUILD.md` with target-specific core/Windows/Android/iOS/Mac Catalyst build and test commands.
- Expanded `docs/RELEASE_CHECKLIST.md` and `docs/TEST_PLAN.md` to cover KDF resource bounds, migration compatibility, backup corruption, attachment streaming/tamper, safe preview, parser robustness, diagnostics privacy, localization/accessibility, platform biometrics, screenshot/clipboard/lifecycle behavior, dependency/license review, signing, and store assets.

### CI and repository quality

- Main core CI job now restores, builds, and runs UnitTests, IntegrationTests, and UiTests.
- Windows CI remains a separate MAUI workload/build gate.
- Existing dependency-review and CodeQL workflows remain part of the repository security gate.
- Repeated source searches found no `TODO`, `FIXME`, `NotImplementedException`, placeholder/fake-service marker, or similarly named unfinished implementation marker in the indexed repository source at the time of this pass.
- Normalized Settings navigation RelayCommands to instance methods to avoid unnecessary source-generator compatibility risk.
- Removed the duplicate recent-access write from vault navigation, leaving item-load as the single source of recent-access persistence.

### Commits created during this continuation

This continuation intentionally used many small commits, including the following commit messages in repository history:

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

The connected GitHub editing environment can inspect and write repository source but cannot run the current .NET/MAUI solution, execute the GitHub-hosted push workflows through this connector, start Android/iOS/macOS/Windows emulators, access physical biometric hardware, or produce signed store packages. Accordingly:

- no claim is made that the current repository head has passed compilation merely because the source was committed;
- no claim is made that device-specific biometric, screenshot, clipboard, lifecycle, accessibility, localization, file-picker/share, or secure-storage behavior has passed physical-device validation;
- no claim is made that CI/CodeQL/dependency review has passed the current head until those GitHub services actually execute and their results are reviewed;
- no claim is made that third-party license notices have been reconciled against the exact restored package metadata until release restore occurs;
- no claim is made that generated store artifacts are byte-for-byte reproducible or meet current store packaging requirements until the documented release process verifies them;
- no claim is made that CipherNest is independently audited, unhackable, military-grade, 100% secure, or appropriate for high-risk use.

The repository now contains the build/test/release/packaging/reproducibility instructions needed to perform those external gates with the required SDKs, target hardware, signing identities, and store environments. Signing keys, certificates, store credentials, API secrets, and private keys remain deliberately absent from source control.

### Scope intentionally left for later reviewed versions

The following remain deliberate future-version items instead of fake or placeholder current features:

- cloud synchronization, accounts, collaboration, server storage, device enrollment, and conflict resolution;
- autofill/type integration with browsers and other applications;
- TOTP seed storage/generation;
- Windows Hello biometric convenience unlock;
- rich binary/PDF document preview beyond the bounded safe text-preview path;
- local document scanning;
- pronounceable password mode until a reviewed design is selected;
- automatic destructive wipe after failed attempts;
- complete Hindi/additional translation catalogs beyond the English-first resource/preference architecture.

Those deferrals follow the uploaded master prompt's own security-review/future-version conditions. They are documented in `PROJECT_STATUS.md`, `DECISIONS.md`, the threat model, and related security/release documents, and they must not be presented as complete in the current UI.
