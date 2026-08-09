# CipherNest Next Steps

This roadmap starts from the current local-first CipherNest source tree. It deliberately distinguishes source work that can be completed in the repository from release gates that require platform SDKs, emulators/simulators, physical devices, signing identities, store accounts, or independent security review.

## Priority 0 — prove the current source on real build environments

These are the first actions to take before calling the current build release-ready.

1. Run the repository build from a clean checkout with the pinned/current .NET SDK.
2. Run `dotnet workload restore`, solution restore, Release build, all unit tests, all integration tests, and UI-structure tests.
3. Run `dotnet format --verify-no-changes` and keep warnings/analyzers as errors.
4. Verify the Windows MAUI target on Windows 11 using the repository build instructions.
5. Verify the Android target with the selected .NET Android workload, an emulator, and at least one physical device.
6. Verify iOS and Mac Catalyst from an appropriate Apple host using the selected .NET/iOS workloads.
7. Record exact SDK/workload versions used for every successful release candidate.
8. Review GitHub Actions, CodeQL, dependency-review, secret-scanning, and vulnerability results for the exact candidate commit.
9. Treat any build warning, failed test, migration/restore failure, crypto-vector failure, unbounded parser/resource condition, or secret leak as release-blocking until resolved.

## Priority 1 — device security validation

### Lock lifecycle

- Verify manual lock clears the vault UI state immediately.
- Verify background locking when enabled.
- Verify inactivity timeout at minimum, typical, and maximum configured values.
- Verify suspend/resume and sleep/wake behavior.
- Verify a clock change does not extend an unlocked session unexpectedly.
- Verify clipboard cleanup is attempted on manual/background/timeout lock.

### Clipboard behavior

- Verify timed clear on Android, Windows, iOS, and Mac Catalyst where platform APIs permit it.
- Verify copying unrelated new clipboard content prevents CipherNest from clearing that newer value.
- Verify platform clipboard-history behavior is accurately described in UI/docs.
- Verify username, primary-secret, and secret-custom-field copy actions all use the same bounded policy.

### Biometrics

- Android: test enrolled, not enrolled, cancelled, failed, locked-out, changed-enrollment, and secure-storage-loss cases.
- iOS/Mac Catalyst: test Face ID/Touch ID availability, cancellation, enrollment changes, passcode-only fallback, and secure-storage lifecycle.
- Verify a fresh process requires the master passphrase before biometric convenience unlock becomes available.
- Verify the configured periodic master-passphrase interval is enforced.
- Verify backup restore invalidates the local biometric pairing.
- Verify changing the master passphrase ends the current security session and requires the new master passphrase.
- Keep Windows on master-passphrase fallback until a separately reviewed Windows Hello design is implemented and tested.

### Screenshot/privacy controls

- Verify Android secure-window behavior on supported versions.
- Verify unsupported targets display honest fallback messaging rather than claiming protection.
- Test app switcher/task preview behavior where the platform exposes it.

## Priority 2 — destructive and recovery workflows

- Create a disposable vault, enable recovery, add all item types, attachments, custom fields, reminders, and notes.
- Exercise master-passphrase unlock and recovery-key unlock separately.
- Verify recovery material cannot authorize actions that specifically require the current master passphrase.
- Change the master passphrase and confirm the old passphrase no longer opens the master wrapper.
- Verify biometric convenience unlock still follows the documented wrapper/session rules after rotation.
- Exercise move-to-trash, restore, individual permanent deletion, Empty Trash, and retention expiry.
- Verify manual permanent deletion requires current-master re-authentication.
- Verify destructive passphrase state is cleared after success, failure, cancellation, and screen exit.
- Verify full local-vault deletion removes CipherNest-managed database/attachment files and clearly documents physical-remnant limitations.

## Priority 3 — backup and transfer confidence

### Encrypted backup/restore

- Test backups with no attachments, many attachments, and large attachments.
- Test a wrong backup passphrase.
- Test corrupted/truncated containers.
- Test a backup created on one supported platform and restored on another where file/container compatibility is expected.
- Confirm failed restore does not replace the active vault.
- Confirm restored biometric metadata is deliberately invalidated locally.
- Periodically test restore using disposable data instead of assuming backups are valid.

### CSV import/export

- Test realistic CSVs with quoted commas, embedded newlines, empty fields, Unicode, duplicate headers, excessive columns, malformed quoted fields, and large rows.
- Confirm import never guesses which column is a secret without explicit user mapping.
- Confirm plaintext export requires current-master re-authentication plus the exact confirmation phrase.
- Confirm plaintext export never silently includes encrypted attachments.
- Confirm temporary plaintext cache cleanup behaves as documented.

### Attachment export/preview

- Test safe in-memory text preview at zero length, normal size, maximum allowed size, invalid UTF-8, unsupported media type, and display truncation boundary.
- Test encrypted attachment streaming at multiple sizes including multi-megabyte inputs.
- Test explicit plaintext export warning and temporary-file cleanup.
- Verify that cancellation and share-sheet failures do not leave application-managed plaintext longer than necessary.

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

## Priority 5 — performance and scale

- Populate disposable vaults with 1,000, 5,000, and 10,000 synthetic entries.
- Measure unlock time, search latency, audit latency, memory usage, and incremental list rendering.
- Verify the 50-item visual paging path keeps scrolling/rendering responsive.
- Measure encrypted attachment import/export throughput for representative file sizes.
- Measure backup creation/restore time for large disposable vaults.
- Do not introduce plaintext searchable indexes merely to improve speed; any encrypted indexing/search redesign needs a privacy review first.

## Priority 6 — release engineering

- Lock exact release SDK/workload/package versions after successful validation.
- Review every direct and transitive dependency license against the exact restored graph.
- Review known vulnerabilities and document any accepted exception with rationale and expiry.
- Generate platform packages only from protected release environments.
- Keep signing keys, certificates, passwords, store API keys, and recovery material out of Git history.
- Prepare signed Android, Windows, iOS, and Mac Catalyst artifacts where distribution targets require them.
- Verify package identifiers, display name, version/build values, privacy declarations, permissions, icons, splash assets, and screenshots.
- Validate the monochrome/adaptive icon source and dark-surface logo on target systems.
- Verify store copy makes no unsupported security claims.
- Use only synthetic/demo vault content in screenshots and marketing images.

## Priority 7 — security review before broader claims

- Obtain an independent review of the cryptographic envelope, KDF bounds, nonce strategy, associated data, recovery flow, secondary biometric wrapper, attachment format, backup format, and migration strategy.
- Review memory-lifetime assumptions around managed strings and decrypted ViewModels.
- Review plaintext export/share-sheet behavior and OS-specific data remnants.
- Review parser fuzzing opportunities for CSV, backup archives, attachment metadata, and vault-header deserialization.
- Review rollback/downgrade behavior for future crypto/database format versions.
- Review dependency/supply-chain pinning and release provenance.
- Keep the product wording at “not independently audited” until an actual audit is completed and its scope is known.

## Priority 8 — launch and open-source operations

- Publish release notes tied to an immutable commit/tag.
- Publish checksums for distributable artifacts where practical.
- Keep `SECURITY.md`, `SUPPORT.md`, `PRIVACY.md`, `TERMS.md`, `THIRD_PARTY_NOTICES.md`, `CHANGELOG.md`, and `PROJECT_STATUS.md` synchronized with the release.
- Triage bug reports without asking users to upload vault contents, passphrases, recovery keys, decrypted backups, or secret-bearing diagnostics.
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

Each one changes the attack surface materially and should receive its own architecture decision, threat-model update, privacy review, test plan, migration/compatibility plan, and release gate before implementation.

## Recommended immediate execution order

1. Clean build + all tests + formatting/analyzers.
2. Fix every compiler/test/CI problem found.
3. Android + Windows smoke tests.
4. Apple builds/smoke tests on an appropriate host.
5. Real-device biometric, lifecycle, screenshot, clipboard, and secure-storage validation.
6. Backup/restore and transfer compatibility matrix.
7. Accessibility/localization/responsive-layout pass.
8. Performance/large-vault measurements.
9. Dependency/license/security review.
10. Signed release-candidate packaging.
11. Independent security review before stronger marketing claims.
12. Tag/release only after every applicable release-checklist gate has evidence.
