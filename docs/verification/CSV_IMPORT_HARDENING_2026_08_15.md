# CSV Import Hardening Verification — 2026-08-15

This record defines the repository-side verification contract for the August 15, 2026 CipherNest CSV import trust-boundary hardening. It is a source/test verification record, not a claim that every malformed input has been exhaustively proven safe.

## Scope

The change hardens imported CSV header metadata before it can reach the mapping interface or import-column lookup path.

Implemented source rules:

- CSV input remains strict UTF-8; malformed UTF-8 is rejected.
- One optional UTF-8 BOM is accepted only at the beginning of the stream.
- Header column count remains bounded to 256.
- Each header name is now bounded to 256 UTF-16 characters even though ordinary CSV fields retain the larger general field budget.
- Empty or whitespace-only header names remain rejected.
- Header names containing Unicode control characters are rejected.
- Header names containing Unicode `Format` category characters are rejected, including invisible formatting marks and bidirectional controls.
- Header names remain case-insensitively unique.
- These checks execute in `ValidateHeader(...)`, which is shared by header preview and actual import before mapping dictionaries are built.

The dedicated header ceiling is intentionally smaller than the generic field ceiling because a header is mapping/display metadata. Allowing a million-character or visually deceptive header would provide no useful interoperability benefit and would unnecessarily expand the UI/resource/spoofing surface.

## Runtime integration coverage

`CsvParserRobustnessTests` now verifies:

- 257-character header names are rejected;
- 256-character header names remain accepted;
- NUL and tab controls are rejected in header names;
- embedded line breaks parsed from quoted header fields are rejected by header validation;
- zero-width formatting characters are rejected;
- bidirectional formatting controls are rejected;
- prior malformed-quote, duplicate/empty-header, strict UTF-8, BOM, UTF-16 rejection, column-count, row-budget, quoted-comma, and escaped-quote coverage remains in place.

The suite also includes a deterministic adversarial corpus seeded with a fixed pseudo-random value. Generated cases mix ordinary characters, delimiters, quotes, line endings, controls, Unicode text, and invisible/bidirectional formatting characters. Every corpus case must either:

1. be rejected with the parser's public `InvalidDataException` boundary; or
2. produce headers satisfying all published header invariants.

The corpus is deterministic so any regression is reproducible in local and hosted CI rather than depending on ambient randomness.

## Source-regression coverage

`CsvSafetySourceTests` requires the production source to retain:

- `MaxHeaderNameChars = 256`;
- the dedicated header-length check;
- `char.IsControl(...)` rejection;
- Unicode `Format` category rejection;
- stable privacy-safe error text for oversized/unsafe header metadata.

This source test complements runtime behavior tests. It does not replace them.

## Documentation synchronization

The following canonical documentation is expected to remain synchronized with this contract:

- `docs/formats/CSV_TRANSFER.md`
- `docs/LIMITS_AND_DEFAULTS.md`
- `docs/TEST_PLAN.md`
- `PROJECT_STATUS.md`
- `CHANGELOG.md`
- `what_changed.md`

The CSV format document also corrects earlier wording about BOM handling: the implementation performs strict UTF-8 decoding without alternate-encoding auto-detection and explicitly accepts one initial UTF-8 BOM.

## Required current-head gates

Before this continuation can be treated as a verified source candidate, the exact final `main` head must complete the configured repository gates, including:

- UnitTests build/test with analyzers;
- IntegrationTests build/test with analyzers;
- UiTests/source-regression build/test with analyzers;
- configured `dotnet format --verify-no-changes` checks;
- direct/transitive NuGet vulnerability audit configured by CI;
- Windows MAUI Release compilation, including the funding-disabled variant;
- Android Release compilation;
- iOS simulator Release compilation;
- Mac Catalyst Release compilation;
- CodeQL core/application build and analysis.

A successful historical run from an earlier commit is not evidence for a later final tree.

## Remaining limits

This deterministic adversarial corpus is a practical regression layer, not exhaustive parser fuzzing or a mathematical proof of bug absence. Broader fuzzing opportunities remain for CSV row/import semantics, encrypted backup framing/archive metadata, attachment metadata and storage names, settings JSON, TOTP Base32 parsing, vault records, and vault-header deserialization.

Target-platform file providers, share sheets, storage permissions, large-file behavior, accessibility/layout of mapping controls, and OS lifecycle behavior still require appropriate platform/device validation. Independent professional security review remains outstanding; this work must not be described as an audit or as proving CipherNest unhackable, military-grade, 100% secure, or suitable for high-risk use.

## Hosted evidence

No hosted run is recorded in this section until the final documentation/ledger commit is present on `main` and the exact resulting head has completed the required CI and CodeQL workflows. Record the final commit and run identifiers here only after observing successful completion.
