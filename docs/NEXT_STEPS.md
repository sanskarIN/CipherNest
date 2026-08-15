# CipherNest Next Steps

This roadmap starts from the current local-first CipherNest source tree. It deliberately distinguishes source work that can be completed in the repository from release gates that require platform SDKs, emulators/simulators, physical devices, signing identities, store accounts, or independent security review.

## Priority 0 — preserve and rerun the proven hosted source baseline

Hosted source verification is no longer only configured: candidate `2327abba1646082a4d94a689d452b1116701cc0b` completed the full configured core/platform matrix and CodeQL successfully. Exact evidence is recorded in `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md`.

The observed baseline includes:

1. 106 UnitTests, 60 IntegrationTests, and 74 UiTests/source tests: **240 passed, 0 failed, 0 skipped**.
2. Analyzer builds and core formatting verification passed.
3. Windows default Release and `CipherNestEnableFundingLink=false` Release builds passed.
4. Android `android-arm64` Release build passed.
5. iOS `iossimulator-arm64` Release build passed.
6. Mac Catalyst `maccatalyst-arm64` Release build passed.
7. CodeQL v4 completed successfully after analyzable core and Android MAUI application builds.
8. The earlier SQLite `NU1903` restore blocker was remediated by the current `Microsoft.Data.Sqlite` 10.0.10 / `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 pins.
9. Windows WinRT/AOT CommunityToolkit diagnostics remain enabled; affected ViewModels now use partial observable properties and the MAUI App project narrowly enables C# preview for that syntax.
10. Apple hosted CI uses the proven `macos-26` / .NET `10.0.302` / Xcode `26.5` / workload-set `10.0.300.3` pairing.

This evidence is historical as soon as a later commit changes source, project files, dependencies, workflows, formats, migrations, resource limits, or security-sensitive behavior. For every release candidate:

- rerun the full main CI and CodeQL gates on the immutable candidate;
- run dependency review through the PR gate and inspect the exact restored advisory/license graph;
- run `DocumentationCoverageSourceTests` and semantic source-to-document review;
- record the exact candidate, SDK/workload/platform-toolchain versions, run identifiers, and conclusions;
- treat any build warning, failed test, migration/restore failure, crypto-vector failure, unbounded parser/storage/resource condition, malformed stored metadata escaping validation, raw secret/path disclosure, materially stale security/recovery/format documentation, or unexpected platform analyzer warning as release-blocking until resolved.

See `docs/verification/CI_GATES.md`, `docs/verification/SECURITY_HARDENING_2026_08_11.md`, `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md`, `docs/verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md`, and `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md`.

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
- Verify username, primary-secret, secret-custom-field, and TOTP-code copy actions all use the same bounded policy.
- Verify copying a TOTP seed/code never introduces a background clipboard lifetime and preserves unrelated newer clipboard content.

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

- Create a disposable vault, enable recovery, add all item types including TOTP, attachments, custom fields, reminders, and notes.
- Verify TOTP save/reopen, re-authentication-protected TOTP access, SHA-1/SHA-256/SHA-512, 6/8 digits, period bounds, malformed Base32 rejection, and device-clock correctness using synthetic seeds.
- Exercise master-passphrase unlock and recovery-key unlock separately.
- Verify recovery material cannot authorize actions that specifically require the current master passphrase.
- Change the master passphrase and confirm the old passphrase no longer opens the master wrapper.
- Verify biometric convenience unlock still follows the documented wrapper/session rules after rotation.
- Exercise move-to-trash, restore, individual permanent deletion, Empty Trash, and retention expiry.
- Verify manual permanent deletion requires current-master re-authentication.
- Verify database record deletion occurs before best-effort attachment cleanup and does not leave a surviving record pointing to intentionally removed files.
- Verify destructive passphrase state is cleared after success, failure, cancellation, and screen exit.
- Verify full local-vault deletion removes CipherNest-managed database/attachment/recovery artifacts and clearly documents physical-remnant limitations.
- Reconfirm strict vault-header compatibility with the exact historical v1/current v2 shapes, 64 KiB byte and 16-level depth boundaries, duplicate/unknown/missing/wrong-kind metadata, v1-to-v2 mutation upgrade, the deterministic 120-case hostile corpus, and malformed replacement-header pre-swap rejection; invalid structures must never reach wrapped-key unwrap.
- Inject disposable malformed programmatic item models (null runtime values, unknown type, empty ID, bad attachment metadata, duplicate attachment IDs/storage names, excessive aggregate text) and confirm validation rejects them without unhandled null dereferences.
- Verify decrypted record ID mismatch/invalid metadata is rejected before reaching search/UI code.
- Exercise the 16 MiB serialized item, 24 MiB stored-envelope, 100,000-item, and 256 MiB aggregate encrypted-record safety budgets using synthetic disposable data where practical without exhausting the test host.

## Priority 3 — backup and transfer confidence

### Encrypted backup/restore

- Test backups with no attachments, many attachments, and large attachments.
- Test a wrong backup passphrase.
- Test corrupted/truncated containers.
- Test unsupported backup version, too-short/too-long salt, hostile KDF parameters, and invalid chunk-size metadata; rejection must happen before Argon2 key derivation.
- Reconfirm strict version-2 backup-header parsing with duplicate/unknown/missing/wrong-type properties, excessive nesting, exact 16,384-byte boundary input, and the deterministic hostile-header corpus; every invalid case must fail before key derivation.
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
- Reconfirm the implemented 64 KiB + 1 sentinel read boundary using exact-limit, oversized, invalid UTF-8, and over-depth JSON fixtures when the settings schema changes.
- Keep the explicit JSON depth ceiling synchronized with the flat `AppPreferences` schema; increasing it requires matching tests and limits documentation.
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
- Continue migrating remaining user-facing literal strings into the neutral/Hindi resource catalogs; do not call the complete UI translated until every security warning and remaining literal is reviewed without weakening meaning.
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
- Extend parser fuzzing beyond the current deterministic CSV-header, settings-JSON, backup-header, vault-header, and attachment-metadata/storage-name adversarial corpora to CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, TOTP Base32 input, and vault-record/envelope semantics.
- Independently review the local TOTP implementation against RFC 6238/HOTP truncation rules, Base32 normalization, seed memory lifetime, same-vault second-factor tradeoffs, clock assumptions, and clipboard exposure.
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
- TOTP QR scanning/rendering, bounded `otpauth://` import/export, and provider/autofill integration;
- Windows Hello convenience unlock;
- rich PDF/binary preview and document scanning;
- pronounceable-password generation;
- additional complete translation catalogs;
- destructive automatic wipe after failed attempts.

Each one changes the attack surface materially and should receive its own architecture decision, threat-model update, privacy review, test plan, migration/compatibility plan, documentation/data-flow/format updates, and release gate before implementation.

## Recommended immediate execution order

1. Preserve the green hosted source baseline and rerun full CI/CodeQL/documentation gates on the exact release candidate after any later source/project/package/workflow change.
2. Execute Android + Windows smoke tests with disposable data.
3. Execute Apple interactive simulator/device smoke tests on the proven-compatible toolchain or another explicitly verified toolchain.
4. Perform real-device biometric, lifecycle, screenshot, clipboard, secure-storage, lock-cancellation, share-sheet, and plaintext-cleanup validation.
5. Run the backup/restore/database-replacement and transfer compatibility/recovery matrix using `docs/operations/BACKUP_RECOVERY_RUNBOOK.md`.
6. Run accessibility/localization/responsive-layout validation using `docs/ACCESSIBILITY.md`.
7. Perform performance/large-vault measurements.
8. Run pull-request dependency review, inspect the exact restored dependency/license/advisory graph, and complete secret/security review.
9. Decide the optional funding-CTA value for each distribution/store target and record it in release provenance.
10. Freeze release documentation against the exact candidate and complete `docs/releases/RELEASE_PROCESS.md`/`docs/RELEASE_CHECKLIST.md` evidence.
11. Produce signed/notarized release-candidate packages in protected environments.
12. Obtain independent security review before stronger marketing claims.
13. Tag/release only after every applicable release-checklist gate has evidence.
