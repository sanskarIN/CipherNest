# Settings JSON Hardening Verification — 2026-08-15

This record defines the repository-side verification contract for the August 15, 2026 CipherNest settings JSON hardening. It documents implemented source behavior and automated regression coverage. It is not an independent security audit and does not claim that every malformed or adversarial filesystem condition has been exhaustively proven safe.

## Scope

CipherNest stores non-secret application preferences in a local JSON file through `JsonSettingsStore`. The settings file is still untrusted parser input because it can be truncated, malformed, manually edited, replaced, corrupted, or changed by another local process while CipherNest is reading it.

The hardening in this continuation covers the settings JSON parser/resource boundary before parsed values can be published to application settings state.

## Implemented read boundary

`JsonSettingsStore.MaximumSettingsFileBytes` remains 64 KiB.

The loader now applies two separate size checks:

1. an early `FileStream.Length` check rejects a file that is already larger than 64 KiB before parsing work starts;
2. the actual read path uses a fixed 64 KiB + 1 byte buffer and never passes more than the supported budget to `System.Text.Json`.

The extra sentinel byte is intentional. If a settings file was inside the limit when its initial length was observed but grows while the stream is being consumed, the loader can detect that the read crossed the supported budget and fall back safely instead of feeding unbounded input into JSON deserialization.

Deserialization occurs from the bounded in-memory byte range, not directly from the original filesystem stream.

## JSON depth boundary

`JsonSettingsStore.MaximumSettingsJsonDepth` is 16.

`AppPreferences` is a flat settings schema and does not require deep JSON nesting. The explicit depth ceiling reduces parser work available to malformed or adversarial nested input while preserving the current valid schema.

Over-depth JSON is treated the same as other malformed JSON and falls back to default preferences.

## Invalid JSON and UTF-8 behavior

The current loader behavior is:

- missing file: return default preferences;
- malformed JSON: return default preferences;
- invalid UTF-8 JSON: return default preferences;
- over-depth JSON: return default preferences;
- file larger than 64 KiB: return default preferences;
- actual read crossing 64 KiB after the initial size observation: return default preferences;
- unreadable/unauthorized local settings file: return default preferences;
- valid UTF-8 JSON with the normal UTF-8 BOM: remain readable through the bounded-memory path;
- cancellation: propagate cancellation rather than converting it into a settings fallback.

The fallback path does not publish raw parser/filesystem exception messages to the application as preference values.

## Normalization after successful parsing

Every successfully parsed `AppPreferences` instance still passes through `AppPreferencesPolicy.Normalize(...)` before publication.

Normalization enforces the current application contract, including:

- defined theme and language enum values;
- lock timeout range 5–3,600 seconds;
- clipboard-clear range 5–300 seconds;
- trash retention range 1–365 days;
- periodic master-passphrase range 1–168 hours;
- backup reminder range 1–365 days;
- review reminder lead range 0–365 days;
- password length range 8–256;
- passphrase word-count range 6–16;
- at least one enabled password character group when password mode is active.

The JSON parser boundary and preferences normalization are complementary controls. A syntactically valid JSON document is not automatically trusted as semantically valid settings.

## Automated runtime coverage

`JsonSettingsStoreBoundsTests` verifies:

- a 64 KiB + 1 byte file falls back before normal parsing;
- a valid JSON document padded with whitespace to exactly 64 KiB remains readable;
- valid UTF-8 JSON with a UTF-8 BOM remains readable through bounded-memory deserialization;
- invalid UTF-8 falls back safely;
- excessive nesting beyond the explicit depth ceiling falls back safely;
- saved settings remain within the file budget and round-trip through normalization;
- malformed JSON falls back safely.

`JsonSettingsStoreTests` continues to verify:

- current preference round-trip behavior;
- relative-path behavior;
- whitespace-path rejection;
- out-of-range persisted values normalize correctly;
- malformed JSON fallback.

## Deterministic adversarial corpus

`JsonSettingsAdversarialTests` adds a fixed-seed corpus that combines:

- valid empty/default JSON;
- null/scalar/array roots;
- undefined enum values;
- extreme numeric values;
- duplicate property names;
- malformed date values;
- unknown nested properties;
- password-generator all-disabled persisted state;
- truncated delimiters/quotes;
- embedded control characters;
- randomized mixtures of JSON punctuation, whitespace, number characters, escapes, ASCII text, Unicode text, zero-width characters, and NULs.

Every corpus input must return an `AppPreferences` value that satisfies the published normalization invariants. A malformed corpus entry may fall back to defaults; a parseable entry may preserve supported values, but neither path may escape the normalized application contract.

The corpus is deterministic so failures are reproducible. It is a regression layer, not exhaustive fuzzing.

## Source-regression coverage

`SettingsJsonSafetySourceTests` requires production source to retain:

- the 64 KiB file ceiling;
- the 16-level JSON depth ceiling;
- the 64 KiB + 1 bounded read allocation;
- a loop bounded by that fixed buffer;
- rejection when actual bytes read exceed the supported settings budget;
- deserialization from bounded memory;
- `AppPreferencesPolicy.Normalize(...)` before successful parsed values are returned;
- no `ReadToEnd`-style unbounded settings read path.

Source-regression assertions supplement runtime tests; they do not replace behavioral execution.

## Documentation synchronization

The following files should remain aligned with this contract when settings persistence changes:

- `docs/COMPLETE_PROJECT_DOCUMENTATION.md`
- `docs/LIMITS_AND_DEFAULTS.md`
- `docs/TEST_PLAN.md`
- `docs/NEXT_STEPS.md`
- `docs/TESTING_GUIDE.md`
- `PROJECT_STATUS.md`
- `CHANGELOG.md`
- `what_changed.md`

The consolidated project documentation now mirrors the 64 KiB + 1 actual-read boundary, 16-level JSON depth ceiling, invalid UTF-8/over-depth fallback, cancellation behavior, BOM compatibility, normalization, and output-size/staging rules rather than retaining the older size-only summary.

Any future settings schema expansion that genuinely requires nesting deeper than 16 must update the source constant, tests, limits documentation, consolidated documentation, and this verification contract together rather than silently weakening the parser boundary.

## Remaining limitations

This work does not prove safety against every local filesystem race, storage-driver failure, hostile operating-system behavior, or arbitrary future `System.Text.Json` implementation defect.

The deterministic corpus is not a coverage-guided fuzzer. Broader parser/adversarial work remains useful for settings JSON as the schema evolves and for other trust boundaries including backup archives/header metadata, attachment metadata/storage names, TOTP Base32 input, vault records, vault-header deserialization, and CSV row/import semantics beyond the current deterministic corpus.

Settings are intentionally non-secret preferences. Master passphrases, recovery keys, biometric secondary secrets, vault encryption keys, vault item secrets, and decrypted attachment content must not be added to this JSON settings store.

## Required candidate gates

For an exact candidate commit containing this change, repository evidence should include successful execution of the configured gates:

- UnitTests build/test with analyzers;
- IntegrationTests build/test with analyzers;
- UiTests/source-regression build/test with analyzers;
- configured `dotnet format --verify-no-changes` checks;
- Windows Release build;
- Windows Release build with `CipherNestEnableFundingLink=false`;
- Android Release build;
- iOS simulator Release build;
- Mac Catalyst Release build;
- CodeQL core/application build and analysis.

Historical green runs from earlier commits do not prove a later candidate. This direct documentation-finalization commit is intended to create the immutable candidate head that receives the normal push-triggered CI and CodeQL runs after all temporary reconciliation helpers have removed themselves. Record hosted evidence only for that exact immutable head being evaluated.
