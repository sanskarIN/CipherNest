# Backup Header Hardening Verification — 2026-08-15

This record defines the repository-side verification contract for the August 15, 2026 CipherNest encrypted-backup header hardening. It documents implemented source behavior and automated regression coverage. It is not an independent security audit and does not claim exhaustive proof against every malformed file, filesystem race, runtime defect, or future parser implementation.

## Scope

CipherNest `.cnbak` files place an unauthenticated JSON header before the authenticated encrypted payload. That header carries version, salt, KDF, chunk-size, and creation-time metadata. Because key derivation happens after the header is read, the header is a security-sensitive trust boundary even though the encrypted payload is authenticated later.

The hardening in this continuation covers the JSON structure and resource metadata accepted before backup key derivation.

## Header framing bounds

`BackupFormatPolicy` now centralizes the current header framing/parser limits:

- minimum header bytes: 16;
- maximum header bytes: 16,384;
- maximum JSON depth: 16.

`BackupFormatPolicy.ValidateHeaderLength(...)` is used by restore before allocating and reading the declared header body. `BackupHeaderJsonPolicy.Validate(...)` applies the same byte-length contract to the actual header bytes before deserialization.

## Strict version-2 JSON schema

For backup format version 2, the header root must be one JSON object containing exactly these case-sensitive properties once each:

- `Version` — JSON number;
- `Salt` — JSON string;
- `Kdf` — JSON object;
- `ChunkSize` — JSON number;
- `CreatedUtc` — JSON string.

Unknown root properties are rejected. Duplicate root properties are rejected. Case-variant aliases such as `version` are not accepted as a second spelling of `Version`.

The `Kdf` object must contain exactly these case-sensitive numeric properties once each:

- `MemoryKiB`;
- `Iterations`;
- `Parallelism`.

Unknown, missing, duplicate, or non-numeric KDF properties are rejected before key derivation.

## JSON parser bounds

`BackupHeaderJsonPolicy` parses the bounded header with `JsonDocument` using:

- trailing commas disabled;
- comments disallowed;
- maximum depth `BackupFormatPolicy.MaximumHeaderJsonDepth` (16).

Malformed JSON, invalid UTF-8, or excessive nesting is normalized by the restore boundary to an invalid-data backup failure. No key derivation is allowed to start for those inputs.

## Resource validation before Argon2

After strict JSON structure validation and typed deserialization, existing resource validation still runs before `ICryptoService.DeriveKey(...)`:

- format version exactly 2;
- salt length 16–64 bytes;
- chunk size 64 KiB–4 MiB;
- KDF memory 16 MiB–512 MiB;
- KDF iterations 1–10;
- KDF parallelism 1–16.

Strict JSON shape validation and numeric/resource validation are complementary controls. Syntactically valid JSON does not automatically make unauthenticated KDF metadata trusted.

## Export self-validation

The exporter now passes its freshly serialized version-2 header through `BackupHeaderJsonPolicy.Validate(...)` before writing it. This keeps the writer and reader aligned if the internal header record changes later: an incompatible same-version header schema should fail during development/testing instead of being silently emitted.

## Automated unit coverage

`BackupFormatPolicyTests` covers:

- minimum/maximum header byte boundaries;
- first rejected byte below/above the supported range;
- explicit 16-level JSON-depth constant;
- existing version/salt/KDF/chunk resource bounds;
- existing encrypted-container and chunk-index bounds.

`BackupHeaderJsonPolicyTests` covers:

- the current strict version-2 header;
- duplicate root metadata;
- duplicate KDF metadata;
- unknown root/KDF metadata;
- case-variant unexpected metadata;
- missing required root/KDF properties;
- wrong JSON value kinds;
- excessive JSON nesting.

## Restore-boundary integration coverage

`BackupHeaderValidationIntegrationTests` verifies that restore rejects the following before `DeriveKey(...)`:

- hostile KDF parameters;
- unsupported format version;
- duplicate root metadata;
- unexpected root metadata;
- excessive nesting;
- declared header length above the maximum;
- truncated header bytes;
- malformed JSON.

It also verifies that a structurally valid header padded with JSON whitespace to exactly 16,384 bytes remains inside the supported framing boundary and reaches the guarded derivation call.

## Deterministic adversarial corpus

`BackupHeaderAdversarialIntegrationTests` currently builds a fixed-seed corpus of exactly 90 hostile header inputs, and therefore retains the documented requirement of at least 80 hostile header inputs. It combines:

- empty/object/array/null roots;
- missing required metadata;
- invalid JSON value kinds;
- invalid Base64 salt text;
- hostile KDF numeric values;
- duplicate KDF/root properties;
- unknown metadata with deterministic randomized printable payloads;
- deterministic invalid UTF-8/random byte sequences.

Every corpus member must fail as `InvalidDataException` with zero calls to `ICryptoService.DeriveKey(...)`.

This corpus is intentionally deterministic so failures are reproducible. It is a regression layer, not exhaustive coverage-guided fuzzing.

## Source-regression coverage

`BackupFormatSourceTests` anchors its ordering assertion inside `RestoreEncryptedAsync` and requires source ordering that keeps:

1. declared header-length validation before header allocation/read;
2. strict `BackupHeaderJsonPolicy.Validate(headerJson)` before typed deserialization;
3. `BackupFormatPolicy.ValidateHeader(...)` after deserialization;
4. `_crypto.DeriveKey(...)` only after both structural and resource validation.

The source test also requires the strict-depth constant and duplicate/unexpected metadata rejection paths to remain present.

Source-regression checks supplement runtime tests; they do not replace behavioral execution.

## Compatibility rule

The strict property set applies to the current `CNBK0002` / version-2 schema. Adding, removing, renaming, or reinterpreting header properties is a backup-format compatibility change and should not be introduced under version 2 without an explicit compatibility/migration strategy and matching tests.

## Documentation synchronization

When encrypted-backup header behavior changes, keep these files aligned:

- `docs/formats/ENCRYPTED_BACKUP.md`;
- `docs/LIMITS_AND_DEFAULTS.md`;
- `docs/TEST_PLAN.md`;
- `docs/TESTING_GUIDE.md`;
- `docs/NEXT_STEPS.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`;
- `what_changed.md`.

## Remaining limitations

This work does not prove safety against every hostile filesystem race, operating-system/storage failure, future `System.Text.Json` defect, encrypted-payload mutation, ZIP/archive attack, or restore-state failure. Existing archive, database replacement, rollback, attachment, and device-level gates remain separate requirements.

Broader parser/adversarial work is still useful for CSV row/import semantics, backup ZIP/archive semantics beyond the header corpus, attachment metadata/storage names, TOTP Base32 input, vault records, and vault-header deserialization.

## Required candidate gates

For an exact immutable candidate containing this change, repository evidence should include successful execution of:

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

The candidate must be frozen after the final direct commit while these gates execute. Any source, test, workflow, or documentation commit after that point creates a new candidate and requires fresh exact-head evidence.

Historical green runs from earlier commits do not prove a later candidate. Hosted evidence is valid only for the exact immutable head that executed those gates.
