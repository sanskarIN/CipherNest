# Post-hardening execution order — 2026-08-11

The repository source-hardening pass is now at the point where the highest-value next work requires executing the exact candidate on real toolchains and target platforms rather than adding more source-only claims.

## 1. Core verification on a clean checkout

Run one of:

- `scripts/verify-core.ps1`
- `scripts/verify-core.sh`

The exact candidate must restore/build UnitTests, IntegrationTests, and UiTests, run all three test projects, and pass the configured formatting/analyzer checks. Treat every compiler error, warning-as-error, failing regression assertion, malformed source-test expectation, package restore problem, or test flake as release-blocking until resolved.

The new test families that need execution include backup/attachment framing, migration-history extremes, crypto/passphrase bounds, malformed vault header handling, search bounds, key-lease constructor cleanup, startup fallback containment, and transfer/Settings/Item Editor platform-boundary source guards.

## 2. Hosted GitHub checks

Review the exact candidate commit after GitHub executes:

- core build/test/format workflow;
- Windows default build;
- Windows `CipherNestEnableFundingLink=false` build;
- Android build;
- iOS build;
- Mac Catalyst build;
- CodeQL including the Android MAUI app target;
- dependency review;
- repository secret/vulnerability scanning where configured.

A configured workflow is not passing evidence until the exact commit has a successful result.

## 3. Platform compile and smoke verification

Run:

- `scripts/verify-windows.ps1` on Windows;
- `scripts/verify-android.sh` with the selected Android workload;
- `scripts/verify-apple.sh` on an appropriate macOS host.

Then launch disposable builds and smoke-test vault create/unlock/lock, CRUD, settings, backup/restore, CSV transfer, attachment import/preview/export, trash, generator, security audit, About/legal surfaces, and navigation.

## 4. Security behavior on real devices

Exercise:

- app background/sleep/resume and inactivity locking;
- clipboard copy/history/replacement/cleanup behavior;
- screenshot protection/fallback messaging;
- Android API-28+ biometric enrollment, cancellation, lockout, hardware-unavailable, enrollment changes, and secure-storage loss;
- iOS/Mac Catalyst Face ID/Touch ID availability, cancellation, enrollment changes, secure storage, and fallback;
- lock cancellation of in-flight decrypted attachment export;
- same-session authorization invalidation during lock/re-unlock races;
- full-vault deletion where one managed filesystem component is made inaccessible;
- file-picker/share cancellation and error behavior for CSV, backups, and decrypted attachment exports;
- temporary plaintext staging cleanup after share/failure.

## 5. Backup and persistence fault matrix

Use disposable data to test:

- wrong backup passphrase;
- corrupted/truncated backup;
- unsupported backup version;
- hostile KDF/header/chunk metadata;
- excessive encrypted chunk count;
- duplicate/unexpected ZIP paths;
- excessive archive bytes/count;
- invalid attachment container sizes;
- staged SQLite signature but invalid CipherNest schema;
- forged/extreme migration history;
- invalid/non-canonical item IDs;
- record count/per-record/aggregate budget violations;
- cancellation after active replacement starts;
- partial DB/WAL/SHM staging/recovery failures.

A failed restore must not intentionally replace the active vault with an invalid candidate.

## 6. Accessibility, localization, and responsive UI

Test TalkBack, VoiceOver, Narrator, keyboard-only navigation, focus order, large OS text, CipherNest Larger Interface, light/dark/system theme, reduced-motion state, phone/tablet/desktop layouts, and security-message announcement behavior. Current localization remains English-first; do not claim a complete Hindi catalog.

## 7. Performance and resource budgets

Measure synthetic 1k/5k/10k vaults, search/audit latency, unlock time, memory, 50-item incremental rendering, CSV import, attachment streaming, and backup/restore. Stay below the explicit safety ceilings rather than benchmarking by exhausting the host.

## 8. Release engineering

Resolve the exact dependency/license graph, review vulnerabilities, verify package identifiers/permissions/privacy declarations/assets, isolate signing material, choose the funding-CTA build property per current distribution policy, and produce signed candidates only from protected release environments.

## 9. Independent review

Before any stronger security marketing claim, obtain independent professional review of the cryptographic envelope, KDF/nonce/AAD choices, recovery/biometric wrappers, key-session lifetime, attachment/backup formats, parsers, database recovery, supply chain, and platform-specific behavior.

The current project must continue to be described as not independently audited until such a review actually occurs.
