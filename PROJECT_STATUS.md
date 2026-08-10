# Project Status

## Current release: 0.1.0 + unreleased hardening

### Completed in source
- Repository and multi-project solution scaffolding with Domain/Application/Infrastructure/Shared/MAUI/test separation.
- Versioned cryptographic envelope with Argon2id key derivation and AES-256-GCM authenticated encryption.
- Random vault data-encryption key wrapped independently by master passphrase, optional recovery key, and optional biometric secondary secret.
- Untrusted KDF metadata is resource-bounded before Argon2 work: salt 16–64 bytes, memory 16–512 MiB, iterations 1–10, and parallelism 1–16; new wrappers use the current 64 MiB / 3 iteration / parallelism 1 default.
- Backup restore validates backup format version, salt length, KDF bounds, and chunk size before Argon2 key derivation.
- Encrypted SQLite record persistence with minimized plaintext metadata and a transactional ordered schema-migration runner that rejects unsupported future schema versions.
- Migration completion validates required current table/column shapes, rejects forged current-version history that omits required schema objects, and preserves the original migration error if rollback itself fails.
- Vault storage budgets are explicit and enforced: 64 KiB vault-header UTF-8, 16 MiB serialized/decrypted item JSON, 24 MiB per stored encrypted envelope, 100,000 item rows, 256 MiB aggregate encrypted-envelope bytes, 10,000 referenced attachments total, and 2,000,000 aggregate item-text characters before serialization.
- SQLite header/count/aggregate/per-row length checks run before text/BLOB materialization where practical; stored item IDs must use the canonical lower-case GUID `D` form.
- Replacement vault databases are validated read-only with SQLite `quick_check`, exact supported schema version, required table/column shape, required/bounded vault header, canonical item IDs, and encrypted-record count/per-record/aggregate budgets before active database/WAL/SHM mutation.
- SQLite replacement stages the active database, WAL, and SHM into a unique recovery file set. Component-aware rollback restores only components that actually staged, preventing deletion of an unstaged sidecar during partial recovery.
- Full database deletion removes the primary SQLite database before WAL/SHM sidecars so failure to delete the primary file does not intentionally dismantle sidecars first.
- Local vault creation, master/recovery unlock, lock lifecycle, bounded failed-attempt backoff, master-passphrase rotation, and guarded full local-vault deletion.
- Supported vault-header versions are explicit; future/unknown header versions are rejected before key unwrap.
- Master/recovery unlock, secondary unlock, public lock, and full-vault deletion are serialized through the same service transition gate so a late-finishing unlock cannot publish a new session after an already-requested lock.
- Full-vault deletion acquires a live session key lease after current-master re-authentication and waits for the transition gate with that lease token; an intervening lock/unlock invalidates the session and cancels stale destructive authorization.
- Once full-vault deletion clears the session key, database deletion proceeds with an uncancelled token and the locked-state event is emitted only when the destructive session transition actually occurred.
- Session cancellation callback failures are contained after key-state transition so they cannot reverse/mask an already-completed lock/unlock replacement; session cancellation sources are still disposed.
- Key-using vault operations run through private 32-byte `VaultKeyLease` copies linked to caller cancellation plus a per-unlock session token. Locking synchronously removes/zeroes the shared session key and cancels in-flight cancellable work; lease buffers zero on disposal.
- Integration coverage verifies locking cancels a deliberately blocked decrypted attachment export instead of letting plaintext output continue after session lock.
- Master-passphrase rotation ends the current security session, clears the remembered master-auth timestamp, locks the vault, requests conditional clipboard cleanup, and requires the new master passphrase before biometric convenience unlock can resume.
- Optional biometric unlock source implementation for supported Android, iOS, and Mac Catalyst devices; Windows explicitly falls back to master-passphrase unlock.
- Android biometric source uses the API-28 `BiometricPrompt` baseline without relying on the newer `BiometricManager` as a preflight; Apple authentication cancellation invalidates the native `LAContext`.
- Fresh-process and periodic master-passphrase requirements before biometric convenience unlock can continue.
- Item CRUD for all modeled vault types, encrypted custom fields, collections, tags, favorites, local search, review dates, per-item master re-authentication, trash retention, and encrypted last-accessed timestamps.
- Vault item validation is null-safe at runtime and rejects empty IDs, unknown types, oversized fields, excessive aggregate item text, invalid collections/custom fields, invalid attachment metadata, attachment metadata control characters, duplicate attachment IDs, and duplicate encrypted storage names.
- Decrypted vault records must match their authenticated SQLite row ID, remain inside serialized/decrypted byte budgets, and pass item metadata validation before they leave the infrastructure boundary; plaintext record buffers are zeroed on all exits.
- Vault sorting by favorites/title, recent use, recent modification, and title; filtering by collection, item type, favorites, and review due state.
- Incremental 50-item vault rendering with result counts and explicit load-more behavior to keep large local result sets from all entering the visual tree at once.
- Local review-reminder summary with configurable lead time and backup reminders.
- Password generator using cryptographically secure randomness plus configurable character groups and ambiguous-character exclusion.
- Memorable passphrase generator backed by exactly 256 validated unique lowercase local words, 6–16 word bounds, eight-word default, explicit random-selection entropy guidance, and persisted generator defaults.
- Password and passphrase generator temporary arrays are cleared after construction of the returned managed string where practical.
- Local weak/reused/exact-duplicate/overdue secret audit primitives.
- Secure-note editor with a bounded safe Markdown-like subset, checklist support, fenced code, HTML neutralization, and local safe preview.
- Secure-note storage/import/editor operations share a centralized 200,000-character / 5,000-line policy, preventing save/import paths from exceeding renderer size limits.
- Encrypted streaming attachments with bounded size/count, authenticated storage, MIME normalization, removal, guarded plaintext export, unique temporary export names, and bounded in-memory UTF-8 preview for supported text-family formats.
- Attachment import metadata is normalized before encryption: leaf filenames are limited to 240 characters, media types to 256 characters, control characters are rejected, and absent media type defaults to `application/octet-stream`.
- Attachment add/remove/permanent-delete mutations are serialized through a cancellable attachment-mutation gate, preserving the per-item 25 cap and enforcing the global 10,000 referenced-attachment cap that aligns with backup entry budgeting while still allowing lock to cancel long attachment work.
- Attachment encryption zeroes its reusable plaintext chunk buffer after each chunk and on exit; encrypted staging uses a collision-resistant `CreateNew` sibling path and refuses final overwrite so an identifier/path collision fails closed.
- Attachment encrypted-container minimum/maximum sizes are exposed and backup extraction rejects impossible attachment entry sizes.
- Opaque encrypted attachment storage names are validated as GUID-based `.cna` filenames without path separators before filesystem access.
- Permanent item deletion removes the authenticated database row before best-effort encrypted attachment cleanup so a failed database delete cannot leave a surviving item whose files were already intentionally removed.
- Authenticated encrypted backup/restore includes encrypted attachments, consistent pre-backup locking, temporary restore staging, corruption/tamper rejection, rollback-preservation tests, post-restore biometric reset, redacted staging-cleanup reporting, shortened bound backup-passphrase lifetime, pre-Argon2 header bounds, and pre-swap SQLite/schema/resource validation.
- Backup export canonicalizes destinations and rejects the live database, WAL/SHM/recovery files, and encrypted attachment directory; encrypted output staging uses a unique sibling `CreateNew` path.
- Backup creation and restore share one archive resource policy: at most 1 GiB aggregate plaintext archive content and at most 10,001 ZIP entries (10,000 attachment slots plus `vault.db`). Export therefore rejects an attachment/snapshot set that would intentionally exceed this build's restore envelope.
- Backup attachment enumeration is materialized inside guarded filesystem access and sorted before archive creation rather than relying on lazy directory enumeration.
- Backup archive restore rejects duplicate normalized ZIP paths and encrypted attachment entries outside the real attachment-container size envelope while retaining total archive/path/count limits.
- Backup rollback after active-state mutation uses an uncancelled recovery token so caller cancellation cannot cancel the recovery database replacement; attachment recovery directories are uniquely named.
- Generic CSV import with explicit column mapping, strict bounded parsing, malformed-input corpus coverage, guarded plaintext CSV export, early export-passphrase clearing, and fixed redacted file-error surfaces.
- CSV maximum-column enforcement applies to final newline/EOF fields as well as delimiter-terminated fields, and the parser reuses a single-character buffer rather than allocating one for every character.
- Explicit username/password/custom-secret copy actions with bounded timed clearing. Delayed state retains only a SHA-256 fingerprint, uses fixed-time comparison, zeroes owned fingerprint buffers, cannot be cancelled by the initiating caller after a successful copy, and preserves unrelated newer clipboard content during timer or lock-triggered cleanup.
- Testable session-lock policy covering lock-on-background, inactivity timeout, and fail-closed clock rollback. Lifecycle fallback separately contains/reports secondary lock and clipboard failures so cleanup errors do not escape native `async void` handlers.
- Testable trash-retention policy with routine vault-maintenance cleanup; manual permanent deletion and empty-trash actions require the current master passphrase plus explicit destructive confirmation, with the bound passphrase cleared immediately after authentication.
- Sensitive passphrase/recovery/decrypted ViewModel state is cleared when Unlock, Settings, Transfer, Trash, Item Editor, and Onboarding pages disappear. Bound credential fields are also cleared earlier before longer authentication/file/share operations where practical, within documented managed-memory limitations.
- Screenshot protection on supported implementation paths with honest fallback messaging.
- Settings for theme, language readiness, lock/privacy, reminder intervals, biometrics, generator defaults, storage/cache, backup/restore, import/export, security audit, privacy/threat information, About/legal/acknowledgements, master-passphrase change, and destructive deletion.
- Settings persistence normalizes supported enum/numeric bounds on load/save, restores a valid password character group when password mode has none, falls back to defaults on malformed/unreadable non-secret settings files, rejects files above 64 KiB before JSON parsing, checks serialized output against the same 64 KiB ceiling, uses unique sibling `CreateNew` staging, and best-effort cleans staging without swallowing cancellation.
- Local storage measurement/cache cleanup materializes directory enumeration inside guarded blocks and skips reparse-point directories so lazy enumeration failures/link recursion do not escape the intended fail-soft boundary.
- Sensitive Settings/Transfer/attachment/item-open filesystem or cryptographic failures use fixed user-facing text plus privacy-safe redacted diagnostic events instead of directly rendering raw exception/path messages.
- Dynamic larger-interface typography resources, reduced-motion preference state, light/dark/system theme behavior, semantic labels/live regions, and responsive layouts including wrapping vault actions for narrow windows.
- English-first `.resx` resource catalog, persisted System/English preference, and localization service architecture ready for Hindi/additional catalogs without coupling language to vault formats.
- Central privacy-safe unhandled-exception reporting records sanitized operation/type/HResult metadata while intentionally excluding exception messages/stacks and vault content; capability probes, external links, file operations, lifecycle fallback, and security cleanup use this path where applicable.
- Redacted developer diagnostics with best-effort temporary-file deletion after sharing and Settings cache-cleanup fallback.
- In-app security/privacy/audit-status surface, runtime version/build About information, GPL/privacy/terms references, third-party dependency notices, acknowledgements, repository/support contacts, and hidden developer diagnostics.
- Centralized project metadata includes the optional development-support URL `https://buymeacoffee.com/sanskarIN`; About exposes explicit user-initiated repository/creator/support links and GitHub `.github/FUNDING.yml` points to the same support URL.
- Optional development support is documented as voluntary and does not change feature access, privacy/security treatment, support priority, licensing, or recovery behavior. `CipherNestEnableFundingLink=false` builds hide the in-app CTA without source edits.
- Original SVG branding with splash wordmark and `Made by the Sanskar`, primary/adaptive icon sources, monochrome system-mark source, dark-surface logo variant, editable asset guidance, packaging/reproducibility documentation, and store-listing/feature-graphic guidance.
- Unit/integration/UI-source tests include cryptographic tamper/wrong-passphrase, Argon2id known-answer and hostile KDF parameter coverage, backup header/path/archive/resource bounds, backup corruption/wrong-passphrase preservation, vault workflows, secondary unlock, vault-header compatibility/size bounds, serialized/cancellation-safe session transitions, live-session destructive authorization, lock-cancelled attachment export, cancellable key-lease source invariants, CSV parser/final-column safety, passphrase rotation/deletion, recent-access tracking, safe-note parsing/shared limits, duplicate audit findings, settings normalization/persistence/staging/64 KiB file bounds, schema migrations/forged-history/replacement preservation, SQLite component-aware recovery and deletion ordering, encrypted-record storage bounds, aggregate item-text validation, generator word-list/temp-array invariants, multi-megabyte attachment streaming, attachment import/buffer/storage-name/staging/metadata/mutation-budget/tamper/truncation checks, session-lock policy, clipboard fingerprint/clear policy, trash retention, unlock backoff, sensitive-screen cleanup, shortened credential lifetime, lifecycle fallback containment, sensitive error-surface redaction, no legacy `.DisplayAlert(` calls, guarded storage enumeration, decrypted-record validation, large-vault incremental rendering, destructive trash confirmation, accessibility/navigation structure, legal surfaces, branding, funding/support-link consistency, CI gate presence, and diagnostics-source privacy checks.
- Main GitHub CI is configured for core tests/formatting, Windows default/funding-disabled Release compilation, Android Release compilation, and iOS/Mac Catalyst Release compilation, with explicit timeouts and superseded-run cancellation.
- CodeQL is configured to build/analyze the MAUI Android application path in addition to core/integration code; dependency review retains a high-severity failure threshold with bounded/cancelable execution.
- Committed local verification scripts cover core PowerShell/POSIX, Windows, Android, and Apple-host compile gates; `docs/verification/CI_GATES.md` documents release evidence requirements.
- Repository templates, contribution/security/support/privacy/terms files, architecture records, implemented cryptographic design, release/setup/packaging/reproducibility/troubleshooting/test documentation, third-party notices, release checklist, and executable `docs/NEXT_STEPS.md` roadmap are present.

### Quality gate requiring external execution or hardware
- The connected GitHub editing environment cannot execute the repository's .NET/MAUI builds, GitHub-hosted push workflow runs, emulator/simulator sessions, or physical-device smoke tests. Source completion is therefore not a claim that the current head has passed those external checks.
- The main workflow is configured to restore/build/run UnitTests, IntegrationTests, UiTests, verify core formatting, compile Windows default/funding-disabled variants, compile Android, and compile iOS/Mac Catalyst. Passing evidence must be reviewed on the exact candidate commit.
- Newly added tests for archive export/restore symmetry, 64 KiB settings bounds, attachment import metadata, total attachment budgeting/mutation serialization, database deletion ordering, and cancellation-safe destructive session transitions require execution on the exact current head before any passing claim.
- Concurrency behavior around lock/unlock/delete transition ordering, attachment mutations, restore cancellation, and filesystem rollback timing requires execution/stress validation on the exact candidate; source serialization and source tests are not proof of all runtime interleavings.
- Android biometric bindings and runtime behavior must be exercised with the selected .NET Android workload on API-28+ devices/emulators covering enrollment, absence, cancellation, lockout, hardware availability, and secure-storage loss.
- iOS and Mac Catalyst biometric behavior, Face ID/Touch ID enrollment changes, cancellation, secure-storage behavior, and packaging require an appropriate Apple build/test environment.
- Windows packaging needs its normal signing identity for store distribution; Windows biometric unlock is intentionally not enabled in this release.
- Android/iOS/MacCatalyst/Windows store signing keys and credentials are intentionally absent from the repository and must be supplied through protected CI/store configuration.
- Screenshot blocking, real clipboard/history behavior, background/sleep lifecycle callbacks, session-cancellation timing, attachment-mutation cancellation timing, share-sheet plaintext cleanup, in-memory preview behavior, accessibility behavior, language fallback, responsive layouts, incremental large-vault UX, large-file attachment behavior, and filesystem replacement/recovery behavior require final platform-by-platform validation.
- The exact current policy for an external Buy Me a Coffee/funding call to action must be checked for every target store/distribution/region before packaging. If a store build cannot expose it, use `CipherNestEnableFundingLink=false` and record that build property in release provenance.
- Dependency vulnerability/dependency-review/CodeQL results must be reviewed when GitHub services execute against the exact head.
- Third-party license notice families must be checked against the exact restored package metadata before distribution.
- Exact platform asset/store requirements, including Android themed/monochrome icon wiring and Apple/Windows generated icon outputs, must be verified against current distribution documentation during release packaging.
- Independent professional cryptographic/security audit remains outstanding; CipherNest must not be marketed as audited, unhackable, military-grade, 100% secure, or suitable for high-risk use until evidence supports those statements.

### Next steps

The ordered release/development plan is maintained in `docs/NEXT_STEPS.md`; verification details are in `docs/verification/CI_GATES.md`. The immediate sequence is execute the configured clean build/tests/format/analyzers and platform compile gates, fix every resulting issue, perform platform smoke/real-device security validation, stress session-transition/attachment-mutation/restore-cancellation/filesystem-recovery behavior, test backup/restore/database-replacement and transfer compatibility, complete accessibility/localization/responsive checks, measure performance/large-vault behavior against the current resource budgets, review dependencies/licenses/security, package signed candidates, obtain independent security review, and only then create an evidence-backed tagged release.

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
