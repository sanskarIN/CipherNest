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
- attachment/backup path/header/resource policies;
- RFC-compatible TOTP code generation and hostile Base32 input;
- bounded TOTP `otpauth://totp/...` parsing/formatting, defaults, ambiguity rejection, metadata validation, and resource ceilings.

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

TOTP setup-URI parsing/formatting itself is platform-independent and pure enough for UnitTests. Integration coverage remains valuable for the imported fields after they are explicitly saved through the ordinary encrypted TOTP `VaultItem` path; the URI text itself must not become a second persisted field.

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
- TOTP setup-URI field masking/transient cleanup, codec routing, and secure clipboard routing;
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
- a third-party authenticator accepts a generated setup URI or exposes identical provider-specific behavior;
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
- serialized/decrypted/storage byte ceilings;
- TOTP seed/settings validity for `OneTimePassword` items.

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

## 15. TOTP tests

### Code generation

Cover:

- RFC 6238 known-answer vectors for SHA-1/SHA-256/SHA-512;
- 6/8-digit output;
- 15..120-second periods;
- formatted lowercase/grouped Base32 input;
- invalid alphabet;
- impossible encoded lengths;
- invalid supplied padding;
- non-zero residual bits;
- unsupported settings;
- pre-Unix-epoch time;
- `DateTimeOffset.MaxValue` validity-window clamping;
- formatted/normalized input ceilings;
- owned temporary scratch/key/hash/counter buffer cleanup where source/runtime can prove it.

### Setup-URI parser/formatter

Use synthetic seeds only. Cover:

- canonical `otpauth://totp/...` parse;
- standards-compatible defaults when algorithm/digits/period are absent;
- encoded account name and issuer;
- format -> parse round trip;
- URI max 8,192 and first rejected over-limit input;
- query max 16 and first rejected over-limit input;
- account max 512;
- issuer max 256;
- query parameter name max 64 and allowed ASCII syntax;
- wrong schemes;
- HOTP host/type;
- `counter` parameter;
- user-info/custom port/fragment;
- missing secret;
- duplicate query keys case-insensitively;
- invalid percent encoding;
- Control/Format metadata characters, including representative supplementary Unicode Format input;
- label/query issuer mismatch;
- unsupported algorithm/digits/period;
- invalid Base32 secret;
- empty account.

Source/UI coverage should additionally ensure:

- `ITotpUriCodec` is registered to `TotpUriCodec` in DI;
- Item Editor invokes the abstraction instead of implementing a second parser;
- import field is masked;
- import field clears after attempts and when sensitive page state clears;
- setup URI is not added to `VaultItem` as another persisted field;
- `Copy setup URI` uses `IClipboardSecurityService.CopySecretAsync`;
- diagnostic/error text does not include actual setup URI/seed.

### Manual interoperability

Automated round-trip tests do not prove every third-party implementation accepts CipherNest output. On release candidates, use synthetic seeds to test representative compatible authenticators/provider URI forms and record:

- imported account/issuer presentation;
- SHA-1/SHA-256/SHA-512;
- 6/8-digit settings;
- representative periods such as 30/60 seconds;
- exported URI acceptance;
- percent-encoded labels;
- clipboard history/synchronization behavior;
- deliberate HOTP rejection.

Never place a real setup URI or seed in screenshots, CI logs, issues, or support artifacts.

## 16. Settings tests

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

## 17. Session/concurrency tests

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

## 18. Clipboard tests

Pure policy/source tests should verify:

- delay bounds;
- SHA-256 fingerprint-only delayed state;
- fixed-time comparison;
- fingerprint-buffer zeroing;
- initiating caller cancellation does not silently remove scheduled cleanup after a successful copy;
- newer unrelated clipboard value is preserved;
- primary secrets, TOTP codes, and TOTP setup URIs route through the same secret-clipboard policy.

Real-device tests are still required because OS clipboard APIs/history/sync differ by platform. Treat setup-URI clipboard results as long-lived seed exposure, not equivalent to one short-lived code.

## 19. Lifecycle tests

Policy tests should cover:

- background lock setting;
- inactivity timeout;
- exactly-at-boundary behavior;
- clock rollback fail-closed decision.

Source/runtime tests should ensure lifecycle exception fallback separately contains lock and clipboard cleanup errors.

Device tests must cover suspend/resume/sleep/wake/task switching.

## 20. Biometric tests

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

## 21. Screenshot/privacy tests

On each target:

- enable/disable preference;
- verify supported secure-window behavior;
- inspect app switcher/task snapshots where relevant;
- verify unsupported target messaging is honest;
- confirm protected content remains masked by default independent of screenshot support.

The TOTP setup-URI input must remain masked and must not be placed in screenshots/store media during testing.

## 22. Accessibility tests

See `ACCESSIBILITY.md` for complete matrix. At minimum test:

- TalkBack/VoiceOver/Narrator;
- keyboard-only desktop navigation;
- focus order/visibility;
- semantic names/descriptions/live regions;
- larger-interface + OS text scaling;
- reduced motion;
- 44-DIP target intent;
- light/dark/system contrast;
- narrow/landscape/desktop resizing;
- TOTP setup-URI controls without the actual URI/seed appearing in semantic descriptions.

## 23. Localization tests

Current preference/catalog support includes System, English, and a reviewed `hi-IN` resource-backed catalog for migrated strings.

Test:

- System fallback;
- explicit English preference;
- explicit Hindi preference;
- startup/resume preference application;
- neutral-English missing-resource fallback behavior;
- Hindi key parity/reviewed security-warning values;
- long-string layout resilience, including new TOTP setup-URI UI strings when migrated to resources.

Do not claim complete Hindi/additional-language coverage until every remaining literal is migrated and reviewed.

## 24. Privacy-safe diagnostics tests

Ensure sensitive error paths:

- show fixed UI text;
- use stable reporter operation IDs;
- do not show/log `ex.Message` directly;
- do not include decrypted field/path values;
- do not include TOTP seeds, generated codes, or setup URIs;
- clean temporary diagnostic/share staging best-effort.

Repository/source scans for dangerous patterns are supplementary, not authoritative.

## 25. CI checks

Main CI is configured for:

- core restore/build/test/format on Ubuntu;
- Windows default and funding-disabled Release compilation;
- Android Release compilation;
- iOS + Mac Catalyst Release compilation on macOS;
- explicit timeouts;
- superseded-run cancellation.

Additional workflows include CodeQL MAUI application analysis and dependency review.

See `verification/CI_GATES.md` for exact evidence requirements.

Because CI uses cancel-in-progress concurrency on branch pushes, do not collect final evidence while a multi-commit continuation is still moving `main`. Freeze the intended exact head first, then record only that head's completed runs.

## 26. Testing new bugs

For every fixed bug:

1. Write a test that fails against the broken behavior when feasible.
2. Keep the regression case focused and deterministic.
3. Test the nearest underlying policy/boundary, not only the UI symptom.
4. Add an integration test when the bug crosses persistence/crypto/stream/session boundaries.
5. Add source coverage if a critical ordering/API pattern is otherwise difficult to execute automatically.
6. Update `TEST_PLAN.md`/`RELEASE_CHECKLIST.md` if the class of failure is release-relevant.

## 27. Performance/resource testing

Use synthetic disposable data only.

Measure/review:

- 1k/5k/10k item vaults;
- search/filter/audit latency;
- 50-item UI page rendering;
- encrypted attachment throughput;
- large CSV valid parsing;
- backup/restore near normal large sizes;
- TOTP setup-URI parse/format behavior near valid URI/query/account/issuer ceilings;
- memory use near valid item/record resource ceilings.

Do not build tests that routinely allocate the absolute 100k/256MiB/1GiB ceilings in constrained CI unless the test environment is explicitly designed for it. Policy-level boundary tests can verify arithmetic cheaply.

## 28. Test-data safety

Never use:

- a real user's vault;
- real passwords/tokens;
- real recovery keys;
- real TOTP seeds/setup URIs/current codes;
- real payment-card details;
- real private documents;
- signing credentials;
- production store/API secrets.

Use clearly synthetic fixtures and temporary directories.

## 29. Release evidence

A release candidate should retain:

- exact commit/tag;
- `dotnet --info` / workloads;
- platform SDK/toolchain versions;
- CI run URLs/results;
- CodeQL/dependency/vulnerability review results;
- device/emulator matrix/results;
- representative synthetic TOTP setup-URI interoperability results;
- restore compatibility results;
- signing/package provenance without secrets;
- written exceptions for any not-applicable gate.

## 30. Failure policy

Release is blocked by unresolved:

- failing build/test/format/analyzer gates;
- cryptographic vector/tamper failures;
- migration/restore compatibility break;
- malformed authenticated metadata escaping validation;
- unbounded attacker-controlled resource metadata;
- ambiguous/unsafe security-sensitive TOTP setup-URI parsing;
- stale destructive authorization surviving session transition;
- secret/path leakage in diagnostics;
- known high-severity dependency issue without a documented owned exception.

See `RELEASE_CHECKLIST.md`.

## Attachment metadata adversarial boundary — 2026-08-15

Attachment metadata/storage-name parser regression coverage now includes `AttachmentImportPolicyTests`, `AttachmentStorageNamePolicyTests`, `VaultItemValidatorTests`, `AttachmentMetadataAdversarialTests`, and `AttachmentMetadataSafetySourceTests`.

The deterministic hostile corpus contains exactly 128 inputs across display names, media types, and opaque storage names. It covers ASCII controls, BMP/supplementary Unicode Format characters, malformed isolated UTF-16 surrogates, path separators, dot/whitespace forms, oversized metadata, wrong-length storage names, invalid GUID hex, wrong extensions, and fixed-length separator-bearing names.

This corpus is deterministic regression coverage, not coverage-guided fuzzing or an independent security audit. Device/filesystem validation is still required for OS-specific path, share/export, reparse/link, and cleanup behavior.

## Final repository-side defect-sweep coverage — 2026-08-15

The final source-side hardening adds focused regression coverage for three remaining input/resource boundaries:

1. **TOTP Base32 and time boundary** — RFC vectors remain intact; `DateTimeOffset.MaxValue` no longer overflows result construction; normalization scratch storage is cleared; a deterministic 128-case hostile Base32 corpus is fully executable with unique theory case IDs; source tests keep validation/decoding before HMAC work.
2. **CSV mapped tags** — exact 100-tag input is accepted, while high-cardinality and oversized-tag rows are rejected before item construction; source tests prevent reintroduction of whole-field `string.Split(...)` materialization.
3. **Backup ZIP extraction** — unit tests cover exact-length extraction, over-declared expansion rejection before the extra chunk is written, truncated output, and aggregate-budget rejection before source reads; source tests require the exact bounded-copy path.

The first hosted checkpoint also caught a repository-formatting newline violation and an xUnit duplicate-theory-ID diagnostic that caused one intended surrogate corpus case not to execute independently. Both were corrected before the documentation freeze. The corrected checkpoint at `483428a0146e5e086a03c9356217139712d1ea1c` completed 346 Unit, 98 Integration, and 110 UI/source tests: **554 passed, 0 failed, 0 skipped**, with analyzer builds and configured core formatting checks successful.

That checkpoint is historical once later documentation commits exist; release evidence must always be taken from the exact final candidate head.

## TOTP setup-URI regression boundary — 2026-08-18

The August 18 continuation adds deterministic unit coverage around `TotpUriCodec` and source/UI coverage around Item Editor handling. This boundary is deliberately text-only and local-only: the tests must continue to prove no incidental QR/camera/provider/cloud dependency is required for the implemented parser/formatter surface.

Automated tests reduce parser/resource/regression risk; they do **not** certify compatibility with every authenticator/provider. Exact release candidates still need representative manual interoperability with synthetic secrets plus target-platform clipboard/history/accessibility validation.
