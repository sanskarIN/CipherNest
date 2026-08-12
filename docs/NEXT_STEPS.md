# CipherNest Next Steps

This roadmap starts from the current local-first CipherNest source tree. It deliberately distinguishes source work that can be completed in the repository from release gates that require platform SDKs, emulators/simulators, physical devices, signing identities, store accounts, or independent security review.

## Priority 0 — prove the current source on real build environments

Repository source now contains repeatable verification scripts and configured CI compile gates for core tests/formatting, Windows, Android, iOS, Mac Catalyst, the funding-disabled Windows variant, CodeQL application analysis, and dependency review. The latest source-hardening pass added cancellation-safe backup rollback, protected backup export destinations, duplicate/pathological backup entry bounds, collision-resistant encrypted attachment/settings staging, SQLite DB/WAL/SHM recovery sets, 64 KiB vault-header limits, encrypted-record count/per-record/aggregate budgets, 16 MiB serialized item limits, 2,000,000-character aggregate item-text limits, and pre-swap resource validation. The complete documentation pass also added a canonical documentation hub, user/developer/maintainer/security/format/testing/operations/release manuals and `DocumentationCoverageSourceTests`; those source gates still need execution and semantic source-to-document review on the exact candidate. These gates still need passing evidence from the exact candidate commit.

1. Run `scripts/verify-core.ps1` or `scripts/verify-core.sh` from a clean checkout with the selected .NET 10 SDK. This includes the UI/source project that contains the documentation-completeness regression test.
2. Review `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md`, execute `DocumentationCoverageSourceTests`, and manually compare changed contracts, formats, limits/defaults, session/destructive authorization, platform support, recovery/deletion limitations, and deferred features against the canonical documentation suite. File presence alone is not semantic correctness.
3. Run the platform script on each appropriate host: `scripts/verify-windows.ps1`, `scripts/verify-android.sh`, and `scripts/verify-apple.sh`.
4. Review the main GitHub Actions workflow for the exact candidate: core tests/format, Windows default/funding-disabled builds, Android build, and iOS/Mac Catalyst builds must all complete successfully.
5. Review CodeQL after it builds both analyzable core code and the Android MAUI application target.
6. Review dependency-review, secret-scanning, and vulnerability results for the exact candidate commit.
7. Record exact SDK/workload/platform-toolchain versions used for every successful candidate.
8. Treat any build warning, failed test, migration/restore failure, crypto-vector failure, unbounded parser/storage/resource condition, malformed stored metadata escaping validation, raw secret/path disclosure, materially stale security/recovery/format documentation, or unexpected platform analyzer warning as release-blocking until resolved.
9. Preserve the immutable candidate commit/tag and verification evidence. See `docs/verification/CI_GATES.md`, `docs/verification/SECURITY_HARDENING_2026_08_11.md`, and `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md`.

## Priority 1 — device security validation

### Lock lifecycle

- Verify manual lock clears the vault UI state immediately.
- Verify background locking when enabled.
- Verify inactivity timeout at minimum, typical, and maximum configured values.
- Verify suspend/resume and sleep/wake behavior.
- Verify a clock change does not extend an unlocked session unexpectedly.
- Verify fail-closed lifecycle recovery cannot propagate a second lock/clipboard-cleanup exception from the native lifecycle handler.
- Verify clipboard cleanup is attempted on manual/background/timeout lock.
- Verify locking cancels the current per-session vault key lease token and stops a deliberately blocked decrypted attachment export through cancellation.
- Stress lock/unlock while search, save, attachment import/export, and recent-access writes are active; no operation should intentionally continue with an old session after lock.

### Clipboard behavior

- Verify timed clear on Android, Windows, iOS, and Mac Catalyst where platform APIs permit it.
- Verify copying unrelated new clipboard content prevents CipherNest from clearing that newer value, including when the vault locks before the timer expires.
- Verify scheduled cleanup continues after the initiating UI operation's cancellation token is no longer relevant.
- Verify only a fixed-size SHA-256 fingerprint is retained for delayed comparison rather than the copied plaintext secret.
- Verify platform clipboard-history behavior is accurately described in UI/docs.
- Verify username, primary-secret, and secret-custom-field copy actions all use the same bounded policy.

### Biometrics

- Android: test API 28+, enrolled, not enrolled, cancelled, failed, locked-out, changed-enrollment, hardware-unavailable, and secure-storage-loss cases. The source intentionally avoids using the newer `BiometricManager` as an API-28 preflight; the prompt/fallback path must be tested directly.
- iOS/Mac Catalyst: test Face ID/Touch ID availability, cancellation, enrollment changes, passcode-only fallback, request cancellation, and secure-storage lifecycle.
- Verify a fresh process requires the master passphrase before biometric convenience unlock becomes available.
- Verify the configured periodic master-passphrase interval is enforced.
- Verify backup restore invalidates the local biometric pairing.
- Verify changing the master passphrase ends the current security session and requires the new master passphrase.
- Keep Windows on master-passphrase fallback until a separately reviewed Windows Hello design is implemented and tested.

### Screenshot/privacy controls

- Verify Android secure-window behavior on supported versions.
- Verify unsupported targets display honest fallback messaging rather than claiming protection.
- Test app switcher/task preview behavior where the platform exposes it.

## Priority 2 — destructive, validation, and recovery workflows

- Create a disposable vault, enable recovery, add all item types, attachments, custom fields, reminders, and notes.
- Exercise master-passphrase unlock and recovery-key unlock separately.
- Verify recovery material cannot authorize actions that specifically require the current master passphrase.
- Change the master passphrase and confirm the old passphrase no longer opens the master wrapper.
- Verify biometric convenience unlock still follows the documented wrapper/session rules after rotation.
- Exercise move-to-trash, restore, individual permanent deletion, Empty Trash, and retention expiry.
- Verify manual permanent deletion requires current-master re-authentication.
- Verify database record deletion occurs before best-effort attachment cleanup and does not leave a surviving record pointing to intentionally removed files.
- Verify destructive passphrase state is cleared after success, failure, cancellation, and screen exit.
- Verify full local-vault deletion removes CipherNest-managed database/attachment/recovery artifacts and clearly documents physical-remnant limitations.
- Verify current vault headers remain readable while an unsupported future header or >64 KiB UTF-8 header is rejected before unwrap/deserialization.
- Inject disposable malformed programmatic item models (null runtime values, unknown type, empty ID, bad attachment metadata, duplicate attachment IDs/storage names, excessive aggregate text) and confirm validation rejects them without unhandled null dereferences.
- Verify decrypted record ID mismatch/invalid metadata is rejected before reaching search/UI code.
- Exercise the 16 MiB serialized item, 24 MiB stored-envelope, 100,000-item, and 256 MiB aggregate encrypted-record safety budgets using synthetic disposable data where practical without exhausting the test host.

## Priority 3 — backup and transfer confidence

### Encrypted backup/restore

- Test backups with no attachments, many attachments, and large attachments.
- Test a wrong backup passphrase.
- Test corrupted/truncated containers.
- Test unsupported backup version, too-short/too-long salt, hostile KDF parameters, and invalid chunk-size metadata; rejection must happen before Argon2 key derivation.
- Verify backup export refuses a destination equal to the active `vault.db`, its WAL/SHM/recovery names, or any path inside the encrypted attachment store.
- Verify backup encrypted staging is collision-resistant and opened with create-new semantics.
- Test duplicate normalized ZIP entry names, unexpected/nested paths, excessive entry count/aggregate size, and encrypted attachment entries smaller/larger than the implemented `.cna` container envelope.
- Test a backup created on one supported platform and restored on another where file/container compatibility is expected.
- Confirm failed restore does not replace the active vault.
- Confirm a staged database with a valid SQLite signature but missing/wrong CipherNest schema, missing/oversized vault header, non-canonical item IDs, or over-budget encrypted records is rejected before active DB/WAL/SHM mutation.
- Confirm replacement runs SQLite `quick_check`, exact schema-version validation, required table/column validation, and storage-resource validation.
- Confirm forged current `MigrationHistory` without required schema objects is rejected.
- Force cancellation after the first active database replacement attempt and confirm rollback is invoked with an uncancelled recovery token.
- Inject partial DB/WAL/SHM staging failures and confirm rollback restores only components that actually moved; an unstaged sidecar must not be deleted.
- Confirm unique recovery naming prevents a stale previous-recovery directory/file from blocking a later restore.
- Confirm restored biometric metadata is deliberately invalidated locally.
- Verify backup passphrase UI state is cleared before file-picker/share work and restore staging cleanup failures remain redacted.
- Periodically test restore using disposable data instead of assuming backups are valid.
- Use `docs/operations/BACKUP_RECOVERY_RUNBOOK.md` as the operational validation record and compare behavior with `docs/formats/ENCRYPTED_BACKUP.md`.

### CSV import/export

- Test realistic CSVs with quoted commas, embedded newlines, empty fields, Unicode, duplicate headers, excessive columns, malformed quoted fields, and large rows.
- Specifically verify the 256-column bound applies when the excessive field is the final field at newline/EOF.
- Confirm import never guesses which column is a secret without explicit user mapping.
- Confirm plaintext export requires current-master re-authentication plus the exact confirmation phrase.
- Confirm the bound master-passphrase field is cleared immediately after the authentication decision.
- Confirm plaintext export never silently includes encrypted attachments.
- Confirm temporary plaintext cache cleanup behaves as documented.
- Confirm file/path-bearing exceptions are not rendered directly into the UI; the user sees fixed messages while the privacy-safe reporter receives only redacted diagnostic metadata.

### Attachment export/preview/storage

- Test safe in-memory text preview at zero length, normal size, maximum allowed size, invalid UTF-8, unsupported media type, and display truncation boundary.
- Test encrypted attachment streaming at multiple sizes including multi-megabyte inputs.
- Verify encryption zeroes the reusable plaintext chunk buffer after each chunk and on exit where practical.
- Verify encrypted attachment staging uses a unique `CreateNew` sibling path and final installation refuses overwrite; a forced destination collision must fail without replacing the existing `.cna` file.
- Verify opaque storage names accept only GUID `.cna` names without separators before app-data file access.
- Test explicit plaintext export warning and unique temporary-file naming.
- Verify temporary plaintext cleanup reports failure without exposing the path and does not overwrite a previous unresolved staging file.
- Verify that cancellation and share-sheet failures do not leave application-managed plaintext longer than necessary.

### Settings and local cache

- Round-trip the complete current `AppPreferences` model.
- Persist malformed/out-of-range enum/numeric values in a disposable settings file and verify normalization/fallback behavior.
- Verify password mode cannot persist with every character group disabled; passphrase mode may keep those groups irrelevant/off.
- Verify malformed/unreadable settings files fall back to defaults while cancellation is still propagated.
- Verify successful settings saves use unique sibling staging and leave no `.*.tmp` artifact.
- Exercise inaccessible/unreadable cache subdirectories and reparse-point directories; usage/cleanup should fail softly without recursing through links.

## Priority 4 — accessibility, localization, and responsive UI

- Test Android TalkBack, iOS VoiceOver, Windows Narrator, and macOS VoiceOver where supported.
- Verify semantic descriptions/live regions announce security errors and state changes without reading masked secret values unexpectedly.
- Verify keyboard-only navigation on desktop targets.
- Verify focus order and visible focus states.
- Verify OS large-text settings plus CipherNest's larger-interface option.
- Verify reduced-motion preference on every feature that adds animation in future versions.
- Test narrow phone width, portrait/landscape, tablet-sized surfaces, and resizable desktop windows.
- Verify 44-DIP minimum touch targets and contrast in light/dark/system modes.
- Continue migrating remaining user-facing literal strings into resource catalogs.
- Add a complete Hindi catalog only when every security warning can be translated/reviewed without weakening meaning.
- Execute and record the complete target/accessibility matrix in `docs/ACCESSIBILITY.md` rather than treating source semantic metadata as certification.

## Priority 5 — performance and scale

- Populate disposable vaults with 1,000, 5,000, and 10,000 synthetic entries while remaining comfortably below the 100,000-item/256 MiB encrypted-record safety ceilings.
- Measure unlock time, search latency, audit latency, memory usage, and incremental list rendering.
- Verify the 50-item visual paging path keeps scrolling/rendering responsive.
- Measure encrypted attachment import/export throughput for representative file sizes.
- Measure backup creation/restore time for large disposable vaults.
- Measure large valid CSV import after the parser's reusable character-buffer change; verify no new per-character allocation regression.
- Profile valid records near—but not at—the aggregate item-text/serialized-record budgets to confirm rejection paths are cheap and ordinary records remain unaffected.
- Do not introduce plaintext searchable indexes merely to improve speed; any encrypted indexing/search redesign needs a privacy review first.

## Priority 6 — release engineering

- Follow `docs/releases/RELEASE_PROCESS.md` as the end-to-end candidate/evidence/signing/provenance process and reconcile every applicable item in `docs/RELEASE_CHECKLIST.md`.
- Lock exact release SDK/workload/package versions after successful validation.
- Review every direct and transitive dependency license against the exact restored graph.
- Review known vulnerabilities and document any accepted exception with rationale and expiry.
- Generate platform packages only from protected release environments.
- Keep signing keys, certificates, passwords, store API keys, and recovery material out of Git history.
- Prepare signed Android, Windows, iOS, and Mac Catalyst artifacts where distribution targets require them.
- Verify package identifiers, display name, version/build values, privacy declarations, permissions, icons, splash assets, and screenshots.
- Validate the monochrome/adaptive icon source and dark-surface logo on target systems.
- Verify store copy makes no unsupported security claims.
- Verify the current policy for the optional `https://buymeacoffee.com/sanskarIN` in-app funding CTA on each exact store/region/distribution target.
- If a target policy requires the funding CTA to be absent, build that app package with `-p:CipherNestEnableFundingLink=false` and record the chosen value in release provenance.
- Use only synthetic/demo vault content in screenshots and marketing images.
- Freeze the canonical documentation suite against the exact candidate: `docs/README.md`, user/developer/maintainer/security/format/testing/operations/release docs, root README/security/privacy/support/contributing surfaces, changelog/project status, and audit wording must match the shipped artifact.

## Priority 7 — security review before broader claims

- Obtain an independent review of the cryptographic envelope, KDF bounds, nonce strategy, associated data, recovery flow, secondary biometric wrapper, attachment format, backup format, and migration strategy.
- Review the per-session `VaultKeyLease` design: synchronized shared-key replacement/zeroing, copied 32-byte leases, linked session/caller cancellation, lock cancellation ordering, nested leases, and failure/Dispose zeroing.
- Review the new storage budgets and where checks occur before materialization/serialization; verify alternate store implementations cannot bypass service-level bounds.
- Review backup destination canonicalization, duplicate ZIP handling, attachment-container size derivation, rollback cancellation semantics, and DB/WAL/SHM partial recovery.
- Review memory-lifetime assumptions around managed strings and decrypted ViewModels; source clears bound credential properties earlier and zeroes several owned arrays, but managed string copies cannot be deterministically erased.
- Review the SHA-256 clipboard-fingerprint approach, OS clipboard/history behavior, and plaintext export/share-sheet data remnants.
- Review parser fuzzing opportunities for CSV, backup archives/header metadata, attachment metadata/storage names, settings JSON, vault records, and vault-header deserialization.
- Review rollback/downgrade behavior for future crypto/database/vault-header format versions.
- Review dependency/supply-chain pinning and release provenance.
- Review `docs/security/THREAT_MODEL.md`, `CRYPTOGRAPHIC_DESIGN.md`, `SESSION_SECURITY.md`, `DATA_LIFECYCLE.md`, and exact format documents against implementation as part of the independent/internal review scope.
- Keep the product wording at “not independently audited” until an actual audit is completed and its scope is known.

## Priority 8 — launch and open-source operations

- Publish release notes tied to an immutable commit/tag.
- Publish checksums for distributable artifacts where practical.
- Keep `docs/README.md`, `docs/USER_GUIDE.md`, `docs/MAINTAINER_GUIDE.md`, `docs/DOCUMENTATION_MAINTENANCE.md`, `SECURITY.md`, `SUPPORT.md`, `PRIVACY.md`, `TERMS.md`, `THIRD_PARTY_NOTICES.md`, `CHANGELOG.md`, and `PROJECT_STATUS.md` synchronized with the release.
- Triage bug reports without asking users to upload vault contents, passphrases, recovery keys, decrypted backups, or secret-bearing diagnostics.
- Use `docs/operations/SECURITY_RESPONSE.md` for security reports and `docs/operations/BACKUP_RECOVERY_RUNBOOK.md` for recovery/support validation.
- Use GitHub issues/discussions for public feature planning where appropriate.
- The optional development-support link is `https://buymeacoffee.com/sanskarIN`; support must remain voluntary and must not change security/privacy treatment, support priority, or GPL feature access.

## Priority 9 — later reviewed versions

The following should remain separate future-version projects rather than being rushed into the local-only core:

- cloud synchronization and account/device protocols;
- collaboration/sharing;
- browser/app autofill;
- TOTP seed storage and generation;
- Windows Hello convenience unlock;
- rich PDF/binary preview and document scanning;
- pronounceable-password generation;
- additional complete translation catalogs;
- destructive automatic wipe after failed attempts.

Each one changes the attack surface materially and should receive its own architecture decision, threat-model update, privacy review, test plan, migration/compatibility plan, documentation/data-flow/format updates, and release gate before implementation.

## Recommended immediate execution order

1. Run the committed core verification script and execute/review `DocumentationCoverageSourceTests` plus the semantic documentation review in `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md`.
2. Run the platform verification scripts and inspect the exact GitHub CI/CodeQL/dependency-review results.
3. Fix every compiler/analyzer/test/workload/documentation mismatch found; do not waive a security-sensitive failure just to package a candidate.
4. Execute the backup rollback/path/archive, SQLite resource/recovery, header/storage-budget, attachment staging, settings staging, framing/passphrase, and documentation source tests on the exact candidate.
5. Android + Windows smoke tests.
6. Apple builds/smoke tests on an appropriate host.
7. Real-device biometric, lifecycle, screenshot, clipboard, secure-storage, lock-cancellation, share-sheet, and plaintext-cleanup validation.
8. Backup/restore/database-replacement and transfer compatibility/recovery matrix using `docs/operations/BACKUP_RECOVERY_RUNBOOK.md`.
9. Accessibility/localization/responsive-layout pass using `docs/ACCESSIBILITY.md`.
10. Performance/large-vault measurements.
11. Dependency/license/security review.
12. Store-policy decision for the optional funding CTA and record the build setting.
13. Freeze release documentation against the exact candidate and complete `docs/releases/RELEASE_PROCESS.md`/`docs/RELEASE_CHECKLIST.md` evidence.
14. Signed release-candidate packaging.
15. Independent security review before stronger marketing claims.
16. Tag/release only after every applicable release-checklist gate has evidence.
