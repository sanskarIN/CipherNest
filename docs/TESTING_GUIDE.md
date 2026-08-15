# CipherNest Testing Guide

This guide explains how CipherNest tests are divided, what each layer proves, how to add regression coverage, and what still requires target-platform execution. The release-oriented matrix remains in `TEST_PLAN.md`; configured automation/evidence rules are in `verification/CI_GATES.md`.

## 1. Core testing rule

A committed test is not the same as a passing test.

Do not state that the current candidate passes unit/integration/UI/platform/security tests until the exact candidate has actually run those checks successfully in the relevant environment.

## 2. Test projects

All automated test projects currently target `net10.0` and use xUnit.

### `tests/CipherNest.UnitTests`

References:

- Application
- Domain
- Infrastructure

Use for deterministic, isolated policies/algorithms/security invariants that do not require real platform UI.

Typical coverage includes:

- Argon2id known-answer behavior;
- cryptographic round-trip/tamper/wrong-key/resource bounds;
- generator word-list/options/strength guidance;
- safe-note parser/limits;
- item validation;
- settings normalization;
- session-lock/backoff/trash/clipboard policies;
- attachment/backup path/header/resource policies.

### `tests/CipherNest.IntegrationTests`

References:

- Application
- Domain
- Infrastructure

Use for real interactions among infrastructure components, especially:

- SQLite schema/migrations;
- real encrypted item persistence;
- master/recovery/secondary vault lifecycle;
- encrypted backup/restore;
- database replacement/recovery;
- attachment streaming/tamper/truncation;
- CSV import into a real disposable vault;
- passphrase rotation;
- recent-access writes;
- lock-cancelled in-flight key work.

Integration tests should use disposable temporary directories/data and must never rely on or modify a developer's real CipherNest vault.

### `tests/CipherNest.UiTests`

This project deliberately has no MAUI project reference. It contains source/repository/UI-structure regression checks that can run on a normal .NET host.

Typical checks include:

- expected route/view/source presence;
- accessibility/semantic structure;
- responsive layout source expectations;
- sensitive ViewModel cleanup hooks;
- privacy-safe error reporting source patterns;
- no legacy `.DisplayAlert(` calls;
- session/key/backup/attachment/database ordering invariants visible in source;
- CI workflow and verification-script presence;
- funding/support metadata consistency.

These tests are useful for preventing structural regressions, but they are not device automation.

## 3. Source tests versus runtime tests

A source test can prove that a required call/order/string/pattern is present in source. It cannot prove:

- an Android/iOS/Windows/macOS API behaves correctly at runtime;
- a lifecycle callback arrives in a particular sequence on every OS build;
- an OS share sheet removed a temporary copy;
- a biometric enrollment/device state works;
- a screenshot is actually blocked;
- a screen reader announces content correctly;
- a signing/store package is valid.

Whenever practical, pair source invariants with runtime unit/integration tests and then target-device/manual validation for platform behavior.

## 4. Recommended local verification

### Core PowerShell

```powershell
./scripts/verify-core.ps1
```

### Core POSIX

```bash
sh scripts/verify-core.sh
```

### Windows MAUI compile

```powershell
./scripts/verify-windows.ps1
```

### Android MAUI compile

```bash
sh scripts/verify-android.sh
```

### iOS + Mac Catalyst compile on macOS

```bash
sh scripts/verify-apple.sh
```

See `setup/BUILD.md` for prerequisites.

## 5. Direct core commands

A normal .NET 10 host can run:

```bash
dotnet build tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj -c Release
dotnet build tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj -c Release
dotnet build tests/CipherNest.UiTests/CipherNest.UiTests.csproj -c Release

dotnet test tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj -c Release --no-build
dotnet test tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj -c Release --no-build
dotnet test tests/CipherNest.UiTests/CipherNest.UiTests.csproj -c Release --no-build
```

Repository build policy treats warnings as errors and enables nullable/analyzer/code-style enforcement.

## 6. Formatting gate

Core CI/scripts verify `dotnet format --verify-no-changes` for non-MAUI source/test projects.

Do not “fix” formatting failures by removing the gate. Format the affected project/source.

## 7. Cryptographic tests

Every cryptographic behavior change should consider:

- deterministic known-answer vectors where suitable;
- correct key/nonce/tag lengths;
- supported version checks;
- tamper rejection;
- wrong-key/wrong-passphrase rejection;
- associated-data binding;
- malformed/null envelope members;
- untrusted KDF resource limits before Argon2;
- plaintext/key-buffer zeroing on failure paths where owned;
- compatibility with existing serialized formats.

The current known-answer vector is documented in `security/CRYPTOGRAPHIC_DESIGN.md`.

## 8. Vault lifecycle tests

Core scenarios:

- create with/without recovery;
- master unlock;
- recovery unlock;
- wrong credential rejection;
- secondary wrapper lifecycle;
- exact v1/v2 vault-header compatibility and v1-to-v2 mutation upgrade;
- future/unsupported/hybrid vault-header rejection;
- 64 KiB byte, 16-level depth, strict root/wrapper/KDF schema, duplicate/unknown/missing/wrong-kind rejection before unwrap;
- deterministic hostile-header corpus with zero unwrap calls;
- master-passphrase rotation;
- lock;
- full-vault deletion authorization;
- lock/unlock/delete transition races;
- stale authorization invalidation;
- client-side failed-attempt backoff reset on success.

## 9. Record-validation tests

For `VaultItemValidator` and decrypted record boundaries test:

- `Guid.Empty`;
- unknown item type;
- missing/oversized strings;
- runtime-null strings/collections despite non-nullable declarations;
- tag/custom-field/attachment counts;
- secure-note character/line boundaries;
- aggregate item-text budget;
- attachment metadata/control characters;
- duplicate attachment IDs/storage names;
- payload-ID versus authenticated-row-ID mismatch;
- serialized/decrypted/storage byte ceilings.

Boundary values should test both maximum accepted and first rejected values where practical.

## 10. Database/migration tests

Use temporary SQLite databases.

Required classes of coverage:

- empty initialization;
- migration idempotence;
- every supported prior schema -> current;
- unsupported future schema;
- forged/malformed migration history;
- required table/column shape;
- rollback primary-error preservation;
- header/resource limits before BLOB/text materialization;
- strict vault-header schema validation on replacement candidates before active database mutation, including legacy-v1 acceptance;
- canonical stored IDs;
- aggregate record limits;
- replacement database validation before active mutation;
- self-replacement/snapshot clobber prevention;
- DB/WAL/SHM unique recovery set;
- component-aware partial rollback;
- complete managed-file-set deletion attempts.

## 11. Backup tests

### Header/framing

Test:

- magic/version;
- 16..16,384-byte header length boundaries;
- strict version-2 root/KDF property allowlists and required-property sets;
- duplicate/unknown/case-variant/wrong-type header metadata;
- 16-level JSON depth and invalid UTF-8/malformed JSON normalization;
- deterministic adversarial header corpus rejected before key derivation;
- salt bounds;
- KDF memory/iteration/parallelism;
- chunk size/count;
- wrong passphrase;
- corrupted/truncated chunks;
- trailing bytes.

Confirm hostile header metadata is rejected before Argon2 work.

### Archive

Test:

- exactly/over entry-count bound;
- aggregate archive bytes;
- unexpected/nested paths;
- case-insensitive normalized duplicates;
- missing `vault.db`;
- valid/invalid `.cna` filename;
- attachment container min/max sizes;
- export/restore resource-policy symmetry.

### Restore atomicity

Test:

- invalid staged SQLite;
- headerless candidate;
- invalid schema/resource candidate;
- current active vault preserved after pre-swap rejection;
- cancellation after active mutation;
- rollback receives uncancelled token;
- partial DB/WAL/SHM recovery;
- attachment rollback/recovery.

## 12. Attachment tests

Cover:

- zero/small/multi-chunk/multi-megabyte files;
- 100 MiB safety boundary where practical without exhausting CI;
- invalid/over chunk count;
- truncated/mutated/tag-invalid container;
- item ID/attachment ID/chunk-index AAD binding;
- canonical storage name;
- path separators/malformed identifiers;
- unique `CreateNew` staging and no final overwrite;
- metadata normalization before encryption;
- plaintext buffer zeroing;
- session cancellation during long attachment work;
- global/per-item attachment budgets.

## 13. Attachment preview tests

Automated/source tests should cover policy/source limits. Device/manual checks should cover actual display behavior.

Cases:

- supported extension/media type;
- unsupported binary type;
- empty text;
- valid UTF-8;
- invalid UTF-8;
- 512 KiB byte limit;
- 20,000 display-character truncation;
- control-character sanitization;
- angle-bracket neutralization;
- protected-item re-authentication requirement;
- no intended plaintext preview file.

## 14. CSV parser/import tests

Cover:

- BOM/no BOM;
- quoted commas/newlines;
- doubled quotes;
- empty fields;
- Unicode;
- duplicate/blank headers;
- unmapped/missing mapped columns;
- EOF inside quotes;
- character after closing quote;
- 256/257 columns including final field at EOF/newline;
- 1,000,000/over field characters;
- 2,000,000/over aggregate row characters;
- 100,000/over data rows;
- invalid rows skipped with fixed warning;
- no secret-bearing warning echo;
- import partial-progress semantics;
- exact exported header/escaping;
- no attachment/custom-field inclusion unless intentionally changed/documented.

## 15. Settings tests

Cover:

- full AppPreferences round trip;
- malformed JSON fallback;
- unreadable file fallback;
- cancellation propagation;
- enum normalization;
- all numeric clamps;
- password-mode no-character-group repair;
- unique staging path;
- `CreateNew` semantics;
- 64 KiB input/output file ceiling;
- actual input reads bounded by a fixed 64 KiB + 1 sentinel byte before JSON deserialization;
- 16-level JSON nesting ceiling;
- invalid UTF-8 fallback;
- UTF-8 BOM compatibility through the bounded-memory path;
- deterministic adversarial JSON corpus returning only normalized preferences;
- no stale staging after success;
- cleanup failure not masking primary result.

## 16. Session/concurrency tests

Concurrency bugs often require controlled scheduling.

Important scenarios:

- lock racing expensive unlock;
- session replacement cancellation;
- key lease disposal/zeroing;
- lock cancels blocked key I/O;
- full-vault deletion waiting behind another transition;
- intervening lock/unlock cancels stale deletion authorization;
- caller cancellation before versus after destructive commit point;
- concurrent read-modify-write item operations;
- attachment mutation gate versus lock responsiveness;
- restore cancellation versus uncancelled rollback.

Prefer deterministic barriers/TaskCompletionSource-controlled fake streams/stores over timing-only sleeps.

## 17. Clipboard tests

Pure policy/source tests should verify:

- delay bounds;
- SHA-256 fingerprint-only delayed state;
- fixed-time comparison;
- fingerprint-buffer zeroing;
- initiating caller cancellation does not silently remove scheduled cleanup after a successful copy;
- newer unrelated clipboard value is preserved.

Real-device tests are still required because OS clipboard APIs/history/sync differ by platform.

## 18. Lifecycle tests

Policy tests should cover:

- background lock setting;
- inactivity timeout;
- exactly-at-boundary behavior;
- clock rollback fail-closed decision.

Source/runtime tests should ensure lifecycle exception fallback separately contains lock and clipboard cleanup errors.

Device tests must cover suspend/resume/sleep/wake/task switching.

## 19. Biometric tests

### Android

Real API-28+ devices/emulators should cover:

- supported/enrolled;
- no enrollment;
- hardware unavailable;
- cancel;
- failed match;
- lockout;
- secure-storage loss/change;
- fallback on unsupported Android version/device.

The current source intentionally avoids depending on a newer `BiometricManager` preflight for the API-28 prompt baseline.

### Apple

Real/simulator-capable environments should cover:

- Face ID/Touch ID support;
- denial/cancel;
- enrollment/device change;
- request-token cancellation and `LAContext` invalidation;
- secure-storage behavior;
- restore invalidating pairing.

### Windows

Confirm no false biometric convenience-unlock UI claim; master-passphrase fallback remains current behavior.

## 20. Screenshot/privacy tests

On each target:

- enable/disable preference;
- verify supported secure-window behavior;
- inspect app switcher/task snapshots where relevant;
- verify unsupported target messaging is honest;
- confirm protected content remains masked by default independent of screenshot support.

## 21. Accessibility tests

See `ACCESSIBILITY.md` for complete matrix. At minimum test:

- TalkBack/VoiceOver/Narrator;
- keyboard-only desktop navigation;
- focus order/visibility;
- semantic names/descriptions/live regions;
- larger-interface + OS text scaling;
- reduced motion;
- 44-DIP target intent;
- light/dark/system contrast;
- narrow/landscape/desktop resizing.

## 22. Localization tests

Current release is English-first.

Test:

- System fallback;
- explicit English preference;
- startup/resume preference application;
- missing-resource fallback behavior;
- long-string layout resilience for future localization.

Do not claim Hindi/additional language completeness without complete reviewed catalogs.

## 23. Privacy-safe diagnostics tests

Ensure sensitive error paths:

- show fixed UI text;
- use stable reporter operation IDs;
- do not show/log `ex.Message` directly;
- do not include decrypted field/path values;
- clean temporary diagnostic/share staging best-effort.

Repository/source scans for dangerous patterns are supplementary, not authoritative.

## 24. CI checks

Main CI is configured for:

- core restore/build/test/format on Ubuntu;
- Windows default and funding-disabled Release compilation;
- Android Release compilation;
- iOS + Mac Catalyst Release compilation on macOS;
- explicit timeouts;
- superseded-run cancellation.

Additional workflows include CodeQL MAUI application analysis and dependency review.

See `verification/CI_GATES.md` for exact evidence requirements.

## 25. Testing new bugs

For every fixed bug:

1. Write a test that fails against the broken behavior when feasible.
2. Keep the regression case focused and deterministic.
3. Test the nearest underlying policy/boundary, not only the UI symptom.
4. Add an integration test when the bug crosses persistence/crypto/stream/session boundaries.
5. Add source coverage if a critical ordering/API pattern is otherwise difficult to execute automatically.
6. Update `TEST_PLAN.md`/`RELEASE_CHECKLIST.md` if the class of failure is release-relevant.

## 26. Performance/resource testing

Use synthetic disposable data only.

Measure/review:

- 1k/5k/10k item vaults;
- search/filter/audit latency;
- 50-item UI page rendering;
- encrypted attachment throughput;
- large CSV valid parsing;
- backup/restore near normal large sizes;
- memory use near valid item/record resource ceilings.

Do not build tests that routinely allocate the absolute 100k/256MiB/1GiB ceilings in constrained CI unless the test environment is explicitly designed for it. Policy-level boundary tests can verify arithmetic cheaply.

## 27. Test-data safety

Never use:

- a real user's vault;
- real passwords/tokens;
- real recovery keys;
- real payment-card details;
- real private documents;
- signing credentials;
- production store/API secrets.

Use clearly synthetic fixtures and temporary directories.

## 28. Release evidence

A release candidate should retain:

- exact commit/tag;
- `dotnet --info` / workloads;
- platform SDK/toolchain versions;
- CI run URLs/results;
- CodeQL/dependency/vulnerability review results;
- device/emulator matrix/results;
- restore compatibility results;
- signing/package provenance without secrets;
- written exceptions for any not-applicable gate.

## 29. Failure policy

Release is blocked by unresolved:

- failing build/test/format/analyzer gates;
- cryptographic vector/tamper failures;
- migration/restore compatibility break;
- malformed authenticated metadata escaping validation;
- unbounded attacker-controlled resource metadata;
- stale destructive authorization surviving session transition;
- secret/path leakage in diagnostics;
- known high-severity dependency issue without a documented owned exception.

See `RELEASE_CHECKLIST.md`.
