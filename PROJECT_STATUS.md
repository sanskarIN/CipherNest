# Project Status

## Current release: 0.1.0 + unreleased hardening

### Completed in source
- A consolidated `docs/COMPLETE_PROJECT_DOCUMENTATION.md` reference and `docs/FAQ.md` now provide complete orientation/support entry points over the canonical specialist documentation; `DocumentationCoverageSourceTests` requires both files and their root/hub links.
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
- Full database deletion attempts the primary DB, WAL, SHM, legacy recovery file, and generated recovery artifacts before reporting aggregate cleanup failure.
- Local vault creation, master/recovery unlock, lock lifecycle, bounded failed-attempt backoff, master-passphrase rotation, and guarded full local-vault deletion.
- Supported vault-header versions are explicit; future/unknown or malformed JSON headers are rejected as authentication failures before key unwrap.
- Master/recovery unlock, secondary unlock, public lock, and full-vault deletion are serialized through the same service transition gate so a late-finishing unlock cannot publish a new session after an already-requested lock.
- Full-vault deletion acquires a live session key lease after current-master re-authentication and waits for the transition gate with that lease token; an intervening lock/unlock invalidates the session and cancels stale destructive authorization.
- Once full-vault deletion clears the session key, managed database and encrypted-attachment cleanup are both attempted with an uncancelled destructive transition, and incomplete deletion is reported generically.
- Session cancellation callback failures are contained after key-state transition so they cannot reverse/mask an already-completed lock/unlock replacement; session cancellation sources are still disposed.
- Key-using vault operations run through private 32-byte `VaultKeyLease` copies linked to caller cancellation plus a per-unlock session token. Locking synchronously removes/zeroes the shared session key and cancels in-flight cancellable work; lease buffers zero on disposal and if linked-token construction fails.
- Integration coverage verifies locking cancels a deliberately blocked decrypted attachment export instead of letting plaintext output continue after session lock.
- Master-passphrase rotation ends the current security session, clears the remembered master-auth timestamp, locks the vault, requests conditional clipboard cleanup, and requires the new master passphrase before biometric convenience unlock can resume.
- Optional biometric unlock source implementation for supported Android, iOS, and Mac Catalyst devices; Windows explicitly falls back to master-passphrase unlock.
- Android biometric source uses the API-28 `BiometricPrompt` baseline without relying on the newer `BiometricManager` as a preflight; Apple authentication cancellation invalidates the native `LAContext`.
- Fresh-process and periodic master-passphrase requirements before biometric convenience unlock can continue.
- Item CRUD for all modeled vault types, encrypted custom fields, collections, tags, favorites, local search, review dates, per-item master re-authentication, trash retention, and encrypted last-accessed timestamps.
- Local TOTP vault items store Base32 seeds and SHA-1/SHA-256/SHA-512 + 6/8-digit + bounded period settings inside the authenticated encrypted record; generated codes are RFC 6238-verified, manual-refresh presentation state and are not persisted. TOTP seed/settings validation is bounded before HMAC/storage use, temporary decoded/hash/counter buffers are zeroed where practical, and code copy uses the existing timed conditional clipboard service.
- Persisted `VaultItemType` numeric values are explicit (`Custom = 8`, `OneTimePassword = 9`) with legacy JSON compatibility tests so adding TOTP cannot reinterpret older Custom records.
- Vault local search rejects trimmed queries longer than 4,096 characters before matching decrypted fields.
- Vault item validation is null-safe at runtime and rejects empty IDs, unknown types, oversized fields, excessive aggregate item text, invalid collections/custom fields, invalid attachment metadata, attachment metadata control characters, duplicate attachment IDs, duplicate encrypted storage names, and opaque attachment storage names that do not match their attachment identifiers.
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
- Encrypted streaming attachments with bounded size/count/chunk count, authenticated storage, MIME normalization, removal, guarded plaintext export, unique temporary export names, and bounded in-memory UTF-8 preview for supported text-family formats.
- Attachment import metadata is normalized before encryption: leaf filenames are limited to 240 characters, media types to 256 characters, control characters are rejected, and absent media type defaults to `application/octet-stream`.
- Attachment add/remove/permanent-delete mutations are serialized through a cancellable attachment-mutation gate, preserving the per-item 25 cap and enforcing the global 10,000 referenced-attachment cap that aligns with backup entry budgeting while still allowing lock to cancel long attachment work.
- Attachment encryption fills normal chunks before encryption where possible, zeroes its reusable plaintext chunk buffer after each chunk and on exit, and uses collision-resistant `CreateNew` staging.
- Attachment encrypted-container minimum/maximum sizes are exposed and backup extraction rejects impossible attachment entry sizes.
- Opaque encrypted attachment storage names are validated as non-empty GUID-based `.cna` filenames without path separators and are bound to the actual attachment ID before filesystem access.
- Permanent item deletion removes the authenticated database row before best-effort encrypted attachment cleanup so a failed database delete cannot leave a surviving item whose files were already intentionally removed.
- Authenticated encrypted backup/restore includes encrypted attachments, consistent pre-backup locking, temporary restore staging, corruption/tamper rejection, rollback-preservation tests, post-restore biometric reset, redacted staging-cleanup reporting, shortened bound backup-passphrase lifetime, pre-Argon2 header bounds, pre-swap SQLite/schema/resource validation, and an explicit encrypted-chunk count ceiling.
- Backup export canonicalizes destinations and rejects the live database, WAL/SHM/recovery files, and encrypted attachment directory; encrypted output staging uses a unique sibling `CreateNew` path.
- Backup export fills normal chunks before encryption and zeroes each reusable plaintext chunk span in `finally`, including write/encryption failure paths.
- Backup creation and restore share one archive resource policy: at most 1 GiB aggregate plaintext archive content and at most 10,001 ZIP entries (10,000 attachment slots plus `vault.db`).
- Backup attachment enumeration is materialized inside guarded filesystem access and sorted before archive creation rather than relying on lazy directory enumeration.
- Backup archive restore rejects duplicate normalized ZIP paths and encrypted attachment entries outside the real attachment-container size envelope while retaining total archive/path/count limits.
- Backup rollback after active-state mutation uses an uncancelled recovery token so caller cancellation cannot cancel the recovery database replacement; attachment recovery directories are uniquely named.
- Generic CSV import with explicit column mapping, strict bounded parsing, per-field/per-row/per-column/logical-row budgets, guarded plaintext CSV export, early export-passphrase clearing, and fixed redacted file-error surfaces.
- CSV row count is checked before parsing an additional row; aggregate row characters are bounded; final-field column enforcement applies at newline/EOF; and the parser reuses a single-character buffer.
- Explicit username/password/custom-secret copy actions with bounded timed clearing. Delayed state retains only a SHA-256 fingerprint, uses fixed-time comparison, zeroes owned fingerprint buffers, cannot be cancelled by the initiating caller after a successful copy, and preserves unrelated newer clipboard content during timer or lock-triggered cleanup.
- Testable session-lock policy covering lock-on-background, inactivity timeout, and fail-closed clock rollback. Lifecycle fallback separately contains/reports secondary lock and clipboard failures so cleanup errors do not escape native `async void` handlers.
- Startup preference restoration contains/report primary errors and separately contains theme/localization/accessibility fallback errors so the fire-and-forget startup task does not leak secondary fallback failures.
- Testable trash-retention policy with routine vault-maintenance cleanup; manual permanent deletion and empty-trash actions require the current master passphrase plus explicit destructive confirmation, with the bound passphrase cleared immediately after authentication.
- Sensitive passphrase/recovery/decrypted ViewModel state is cleared when Unlock, Settings, Transfer, Trash, Item Editor, and Onboarding pages disappear. Bound credential fields are also cleared earlier before longer authentication/file/share operations where practical, within documented managed-memory limitations.
- Crypto-bound master/recovery/backup/secondary passphrases are limited to 12–4,096 characters. Invalid-length unwrap guesses map to normal vault authentication failure; onboarding and Settings reject oversized setup/change inputs before expensive strength/KDF work.
- Screenshot protection on supported implementation paths with honest fallback messaging.
- Settings for theme, language readiness, lock/privacy, reminder intervals, biometrics, generator defaults, storage/cache, backup/restore, import/export, security audit, privacy/threat information, About/legal/acknowledgements, master-passphrase change, and destructive deletion.
- Settings persistence normalizes supported enum/numeric bounds on load/save, restores a valid password character group when password mode has none, falls back to defaults on malformed/unreadable non-secret settings files, rejects files above 64 KiB before JSON parsing, checks serialized output against the same 64 KiB ceiling, uses unique sibling `CreateNew` staging, and best-effort cleans staging without swallowing cancellation.
- Settings load/save, cache cleanup, biometric configuration, backup export/share, restore picker/confirmation/staging, passphrase rotation, and destructive delete platform/storage failures now use fixed UI messages with privacy-safe reporting.
- Transfer picker/import confirmation/plaintext re-authentication/export confirmation/share paths are contained; plaintext CSV staging is removed in `finally` after sharing/failure where permitted, with a redacted cleanup warning if deletion cannot be confirmed.
- Item-editor re-authentication, copy-secret, attachment picker/export/share/removal, and move-to-trash platform failures use fixed privacy-safe reporting; temporary decrypted attachment cleanup remains best-effort and reported.
- Local storage measurement/cache cleanup materializes directory enumeration inside guarded blocks and skips reparse-point directories so lazy enumeration failures/link recursion do not escape the intended fail-soft boundary.
- Dynamic larger-interface typography resources, reduced-motion preference state, light/dark/system theme behavior, semantic labels/live regions, and responsive layouts including wrapping vault actions for narrow windows.
- Neutral-English `.resx` fallback plus a reviewed `hi-IN` satellite catalog, persisted System/English/Hindi preference, parity/source tests, and explicit documentation that not-yet-migrated UI literals can still appear in English without coupling language to vault formats.
- Central privacy-safe unhandled-exception reporting records sanitized operation/type/HResult metadata while intentionally excluding exception messages/stacks and vault content; capability probes, external links, file operations, lifecycle fallback, and security cleanup use this path where applicable.
- Redacted developer diagnostics with best-effort temporary-file deletion after sharing and Settings cache-cleanup fallback.
- In-app security/privacy/audit-status surface, runtime version/build About information, GPL/privacy/terms references, third-party dependency notices, acknowledgements, repository/support contacts, and hidden developer diagnostics.
- Centralized project metadata includes the optional development-support URL `https://buymeacoffee.com/sanskarIN`; About exposes explicit user-initiated repository/creator/support links and GitHub `.github/FUNDING.yml` points to the same support URL.
- Optional development support is documented as voluntary and does not change feature access, privacy/security treatment, support priority, licensing, or recovery behavior. `CipherNestEnableFundingLink=false` builds hide the in-app CTA without source edits.
- Original SVG branding with splash wordmark and `Made by the Sanskar`, primary/adaptive icon sources, monochrome system-mark source, dark-surface logo variant, editable asset guidance, packaging/reproducibility documentation, and store-listing/feature-graphic guidance.
- Unit/integration/UI-source tests cover the current crypto, backup, database, session, migration, CSV, attachment, settings, startup, transfer, item-editor, onboarding, privacy, lifecycle, generator, branding, support metadata, and CI source invariants. The 2026-08-11 hardening gates are recorded in `docs/verification/SECURITY_HARDENING_2026_08_11.md`.
- A canonical complete documentation suite now covers user workflows, developer/maintainer guidance, public application contracts, limits/defaults/glossary, dependency/data-flow/session architecture, sensitive-data lifecycle/session security, vault/attachment/backup/CSV formats, testing/accessibility, backup/recovery and security-response operations, and full release governance. `DocumentationCoverageSourceTests` guards required files/entry-point links/audit disclaimers, and `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md` records the documentation evidence gate.
- Main GitHub CI is configured for core tests/formatting, Windows default/funding-disabled Release compilation, Android Release compilation, and iOS/Mac Catalyst Release compilation, with explicit timeouts and superseded-run cancellation.
- CodeQL is configured to build/analyze the MAUI Android application path in addition to core/integration code; dependency review retains a high-severity failure threshold with bounded/cancelable execution.
- Committed local verification scripts cover core PowerShell/POSIX, Windows, Android, and Apple-host compile gates; `docs/verification/CI_GATES.md` documents release evidence requirements.
- Repository templates, contribution/security/support/privacy/terms files, architecture records, implemented cryptographic design, release/setup/packaging/reproducibility/troubleshooting/test documentation, third-party notices, release checklist, and executable `docs/NEXT_STEPS.md` roadmap are present.


### Current TOTP/localization release validation
- RFC 6238 known-answer tests cover SHA-1, SHA-256, and SHA-512 at the published test timestamps.
- TOTP unit tests cover formatted Base32 input, malformed alphabet/length/padding, code digit counts, period/algorithm bounds, and pre-epoch rejection.
- Integration coverage round-trips a synthetic TOTP item through real SQLite + VaultService encryption and checks that the encrypted envelope does not contain the synthetic Base32 seed as plaintext UTF-8 bytes.
- Source tests guard explicit TOTP refresh/copy UI, no background timer, documentation security claims, Hindi neutral/satellite key parity, and runtime language wiring.
- Physical-device clipboard/history, clock correctness, accessibility, language layout, and lifecycle behavior remain release gates.

### Hosted verification evidence
- Exact hosted candidate `2327abba1646082a4d94a689d452b1116701cc0b` completed `CipherNest CI` run `31697433940` successfully.
- Core analyzer builds completed with 0 warnings / 0 errors for UnitTests, IntegrationTests, and UiTests.
- Runtime tests completed with **106 Unit + 60 Integration + 74 UI/source = 240 passed, 0 failed, 0 skipped**.
- Core `dotnet format --verify-no-changes` checks completed successfully.
- Windows Release builds completed successfully for both the default funding-enabled configuration and `CipherNestEnableFundingLink=false`.
- Android Release compilation completed successfully for `android-arm64`.
- iOS simulator Release compilation completed successfully for `iossimulator-arm64`.
- Mac Catalyst Release compilation completed successfully for `maccatalyst-arm64`.
- Apple hosted verification used `macos-26`, .NET SDK `10.0.302`, Xcode `26.5`, and the Xcode-26.5-compatible .NET workload set `10.0.300.3`.
- CodeQL v4 run `31697433730` completed successfully after building analyzable core and the Android MAUI application path.
- Detailed exact-run evidence is recorded in `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md`.

### Quality gate requiring external execution or hardware
- The repository has exact hosted compile/test/format/CodeQL evidence for candidate `2327abba1646082a4d94a689d452b1116701cc0b`; later release candidates must rerun these gates rather than inheriting the result automatically.
- The connected editing environment does not itself provide interactive target emulators/simulators, physical devices, store signing/notarization, store review, or an independent professional security audit.
- Concurrency behavior around lock/unlock/delete transition ordering, attachment mutations, restore cancellation, and filesystem rollback timing has automated policy/integration coverage, but broader stress/interleaving validation remains required on the exact release candidate.
- Android biometric bindings and runtime behavior must be exercised with the selected .NET Android workload on API-28+ devices/emulators covering enrollment, absence, cancellation, lockout, hardware availability, and secure-storage loss.
- iOS and Mac Catalyst biometric behavior, Face ID/Touch ID enrollment changes, cancellation, secure-storage behavior, and packaging require interactive Apple simulator/device validation even though hosted compilation now passes.
- Windows packaging needs its normal signing identity for store distribution; Windows biometric unlock is intentionally not enabled in this release.
- Android/iOS/MacCatalyst/Windows store signing keys and credentials are intentionally absent from the repository and must be supplied through protected CI/store configuration.
- Screenshot blocking, real clipboard/history behavior, background/sleep lifecycle callbacks, session-cancellation timing, attachment-mutation cancellation timing, share-sheet plaintext cleanup, in-memory preview behavior, accessibility behavior, language fallback, responsive layouts, incremental large-vault UX, large-file attachment behavior, and filesystem replacement/recovery behavior require final platform-by-platform validation.
- The exact current policy for an external Buy Me a Coffee/funding call to action must be checked for every target store/distribution/region before packaging. If a store build cannot expose it, use `CipherNestEnableFundingLink=false` and record that build property in release provenance.
- Pull-request dependency review remains a separate configured gate; the exact candidate's hosted restores no longer surface the previously observed `NU1903` SQLite blocker, and CodeQL succeeded.
- Third-party license notice families must be checked against the exact restored package metadata before distribution.
- Exact platform asset/store requirements, including Android themed/monochrome icon wiring and Apple/Windows generated icon outputs, must be verified against current distribution documentation during release packaging.
- Independent professional cryptographic/security audit remains outstanding; CipherNest must not be marketed as audited, unhackable, military-grade, 100% secure, or suitable for high-risk use until evidence supports those statements.

### Next steps

The ordered release/development plan is maintained in `docs/NEXT_STEPS.md`; verification details are in `docs/verification/CI_GATES.md`, `docs/verification/SECURITY_HARDENING_2026_08_11.md`, `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md`, `docs/verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md`, and `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md`. The immediate sequence is preserve the green hosted source baseline while executing platform smoke/real-device security validation, stress session-transition/attachment-mutation/restore-cancellation/filesystem-recovery behavior, test backup/restore/database-replacement and transfer compatibility on target environments, complete accessibility/localization/responsive checks, measure performance/large-vault behavior against the current resource budgets, review dependencies/licenses/security, package signed candidates, obtain independent security review, and only then create an evidence-backed tagged release.

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