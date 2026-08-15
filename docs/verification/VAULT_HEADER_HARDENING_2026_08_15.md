# Vault Header Hardening Verification — 2026-08-15

This record defines the repository-side verification contract for the August 15, 2026 CipherNest local vault-header hardening. It documents implemented source behavior and automated regression coverage. It is not an independent security audit and does not claim exhaustive proof against every malformed database, filesystem/storage race, future runtime defect, or cryptographic implementation flaw.

## Scope

The local `VaultHeader.HeaderJson` value is processed before master/recovery/secondary wrapped-key unwrap. It therefore forms a security-sensitive parser/resource boundary even though the wrapped-key envelopes themselves are authenticated cryptographically.

This continuation hardens that boundary without removing compatibility for the original version-1 header shape.

## Persisted byte boundary

`SqliteVaultStore.ReadHeaderAsync` already queries `length(CAST(HeaderJson AS BLOB))` before retrieving the header text and rejects lengths outside 1..65,536 bytes. It then verifies `Encoding.UTF8.GetByteCount(headerJson)` matches the persisted byte count.

`VaultHeaderJsonPolicy.Validate(...)` repeats the 64 KiB byte ceiling before JSON parsing. `VaultStorageLimits.MaximumVaultHeaderJsonDepth` now fixes the JSON nesting ceiling at 16.

## Version-aware strict root schema

Version 1 remains accepted only with exactly these case-sensitive properties:

- `version`;
- `master`;
- `recovery`.

Version 2 is accepted only with exactly:

- `version`;
- `master`;
- `recovery`;
- `secondary`.

Duplicate, unknown, missing, and case-variant root metadata is rejected. Version 1 carrying `secondary` is rejected as an undocumented hybrid shape. Version 2 missing `secondary` is rejected even when a caller might otherwise infer null.

## Strict wrapped-key schema

Each non-null master/recovery/secondary wrapper must contain exactly one each of:

- `version` — integer;
- `salt` — string;
- `kdf` — object;
- `nonce` — string;
- `ciphertext` — string;
- `tag` — string.

Unknown, duplicate, missing, or wrong-kind wrapper metadata fails before typed header deserialization.

## Strict KDF JSON schema

Each wrapper's `kdf` object must contain exactly one integer each of:

- `memoryKiB`;
- `iterations`;
- `parallelism`.

The JSON-shape policy does not replace cryptographic resource validation. After typed deserialization, `CryptoService` still validates the supported wrapper version, salt/nonce/ciphertext/tag lengths, KDF memory/iteration/parallelism ranges, and passphrase bounds before Argon2 work.

## Parser configuration

`VaultHeaderJsonPolicy` parses the already byte-bounded string using `JsonDocument` with:

- trailing commas disabled;
- comments disallowed;
- maximum depth 16.

Malformed JSON or excessive nesting is normalized to `VaultAuthenticationException` by `VaultService.ReadHeaderUnlockedAsync`.

## Read ordering

The source contract requires:

1. persisted byte-length enforcement in `SqliteVaultStore` before header materialization;
2. `VaultHeaderJsonPolicy.Validate(headerJson)` before `JsonSerializer.Deserialize<VaultHeaderDocument>(...)`;
3. typed header/version/master checks after structural validation;
4. `UnlockAsync` obtaining the validated header before `_crypto.UnwrapKey(...)`;
5. `CryptoService.UnwrapKey` validating decoded wrapper/KDF resource metadata before Argon2 derivation.

Invalid structural headers must not reach wrapped-key unwrap.

## Writer self-validation and legacy upgrade

All `VaultService` header writes now go through `SerializeHeader(...)`, which validates the freshly serialized JSON before persistence.

Current writes use version 2. Importantly, a legitimate version-1 vault remains readable, but any current header mutation deliberately writes version 2. This fixes the previous possibility that master-passphrase rotation on a v1 header could preserve `version = 1` while the current four-field internal record serializer emitted `secondary: null`.

## Unit coverage

`VaultHeaderJsonPolicyTests` covers:

- accepted version-1 and version-2 shapes;
- duplicate/unknown/case-variant/missing root metadata;
- v1/v2 incompatible property sets;
- future versions;
- duplicate/unknown/missing/wrong-kind wrapper metadata;
- duplicate/unknown/missing/non-integer KDF metadata;
- excessive nesting;
- exact 65,536-byte policy acceptance;
- 65,537-byte policy rejection.

## Runtime integration coverage

`VaultHeaderStrictValidationIntegrationTests` covers:

- unlock of a synthetically downgraded but historically valid v1 header;
- deliberate v1-to-v2 upgrade when the master passphrase is changed;
- a structurally valid synthetic v2 header reaching exactly one unwrap call;
- an oversized persisted SQLite header failing as `VaultAuthenticationException` with zero unwrap calls.

## Deterministic adversarial corpus

`VaultHeaderAdversarialIntegrationTests` contains exactly 120 fixed hostile cases. They cover malformed roots, missing metadata, duplicates, case variants, nested wrapper/KDF mutations, wrong JSON kinds, invalid Base64 strings, truncation/trailing-comma input, excessive depth, and fixed-seed randomized unknown root/wrapper/KDF properties.

Every member must fail as `VaultAuthenticationException`, leave the vault locked, and produce zero wrapped-key unwrap calls.

This deterministic corpus is reproducible regression coverage. It is not exhaustive coverage-guided fuzzing.

## Source-regression coverage

`VaultHeaderSafetySourceTests` requires:

- version 1/2 constants and the 16-level depth bound;
- ordinal case-sensitive root/wrapper/KDF property allowlists;
- version-aware `secondary` rules;
- strict policy validation before typed deserialization;
- validated header acquisition before unwrap;
- malformed structural/storage failures normalized through the authentication boundary;
- writer self-validation;
- explicit version-2 upgrade on master/secondary header mutations;
- absence of direct `WriteHeaderAsync(JsonSerializer.Serialize(...))` bypasses.

## Compatibility rule

The currently supported local header versions are 1 and 2 only. A change to property names, required/optional fields, nested wrapped-key/KDF shape, or compatibility interpretation is a local vault-format change. Such a change requires an explicit version/migration decision, regression fixtures, format-document updates, and exact-head CI evidence.

## Documentation synchronization

Keep these surfaces aligned when the local vault-header contract changes:

- `docs/formats/VAULT_HEADER.md`;
- `docs/LIMITS_AND_DEFAULTS.md`;
- `docs/architecture/DATABASE.md`;
- `docs/security/CRYPTOGRAPHIC_DESIGN.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/TEST_PLAN.md`;
- `docs/TESTING_GUIDE.md`;
- `docs/NEXT_STEPS.md`;
- `docs/COMPLETE_PROJECT_DOCUMENTATION.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`;
- `what_changed.md`.

## Remaining limitations

This work does not prove safety against every malicious SQLite/database construction, filesystem race, future `System.Text.Json` or SQLite defect, memory remanence, compromised process/device, or cryptographic side channel. The deterministic corpus does not replace coverage-guided fuzzing or independent professional review.

Broader remaining parser/adversarial work still includes CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, attachment metadata/storage names, TOTP Base32 input, and encrypted/decrypted vault-record envelope semantics.

## Required exact-candidate gates

For an immutable candidate containing this change, repository evidence should include successful execution of:

- UnitTests build/test with analyzers;
- IntegrationTests build/test with analyzers;
- UiTests/source-regression build/test with analyzers;
- configured `dotnet format --verify-no-changes` checks;
- Windows Release build;
- Windows Release with `CipherNestEnableFundingLink=false`;
- Android Release build;
- iOS simulator Release build;
- Mac Catalyst Release build;
- CodeQL analyzable core/application build and analysis.

Any commit after that evidence invalidates it for the later head and requires the configured gates to run again.
