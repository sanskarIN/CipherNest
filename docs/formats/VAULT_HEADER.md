# CipherNest Vault Header Format

This document defines the supported local `VaultHeader.HeaderJson` contract for the current CipherNest source tree. The header is security-sensitive metadata used before the vault data key is unwrapped. It is not itself a substitute for the authenticated wrapped-key envelopes that it contains.

## 1. Storage boundary

The header is stored as UTF-8 JSON in the single `VaultHeader` row with `Id = 1`.

Current resource limits:

- minimum persisted UTF-8 length: 1 byte;
- maximum persisted UTF-8 length: 65,536 bytes (64 KiB);
- maximum JSON nesting depth: 16;
- comments: rejected;
- trailing commas: rejected.

`SqliteVaultStore.ReadHeaderAsync` checks SQLite's byte length before materializing the header string and verifies that the materialized UTF-8 length matches the persisted byte length. `VaultHeaderJsonPolicy` repeats the byte boundary and then applies the strict JSON schema before typed deserialization.

## 2. Version 1 root schema

Legacy version 1 remains readable for compatibility. Its root object contains exactly these case-sensitive properties once each:

```text
version
master
recovery
```

Rules:

- `version` must be the JSON integer `1`;
- `master` must be a wrapped-key object;
- `recovery` must be either a wrapped-key object or JSON `null`;
- `secondary` is not valid in a version-1 header;
- unknown, duplicate, case-variant, or missing properties are rejected.

## 3. Version 2 root schema

Version 2 is the current write format. Its root object contains exactly these case-sensitive properties once each:

```text
version
master
recovery
secondary
```

Rules:

- `version` must be the JSON integer `2`;
- `master` must be a wrapped-key object;
- `recovery` may be a wrapped-key object or JSON `null`;
- `secondary` may be a wrapped-key object or JSON `null`;
- unknown, duplicate, case-variant, or missing properties are rejected.

Every current header mutation writes version 2. Reading a valid version-1 header is supported, but changing the master passphrase or changing secondary-unlock metadata upgrades that header to the version-2 shape.

## 4. Wrapped-key object schema

Every non-null `master`, `recovery`, or `secondary` wrapper contains exactly these case-sensitive properties once each:

```text
version
salt
kdf
nonce
ciphertext
tag
```

JSON kinds are fixed:

- `version`: integer number;
- `salt`: Base64 JSON string;
- `kdf`: object;
- `nonce`: Base64 JSON string;
- `ciphertext`: Base64 JSON string;
- `tag`: Base64 JSON string.

The strict JSON policy verifies property identity and JSON kinds. Typed deserialization and `CryptoService` then enforce the cryptographic/resource contract, including supported crypto version, decoded lengths, and KDF bounds, before Argon2 work.

## 5. KDF object schema

`kdf` contains exactly these case-sensitive integer properties once each:

```text
memoryKiB
iterations
parallelism
```

Current resource validation after typed deserialization and before Argon2 requires:

- salt: 16–64 bytes;
- memory: 16 MiB–512 MiB expressed in KiB;
- iterations: 1–10;
- parallelism: 1–16.

The schema layer rejects non-integer JSON values before a `WrappedKeyEnvelope` is created. The cryptographic layer remains responsible for the supported numeric ranges.

## 6. Read ordering

Unlock follows this order:

1. initialize/read the local store;
2. reject an absent or byte-oversized/inconsistent persisted header;
3. parse the bounded JSON with maximum depth 16;
4. enforce the exact version-aware root, wrapper, and KDF property sets;
5. deserialize the validated JSON into the internal header record;
6. re-check supported header version/master presence;
7. call wrapped-key unwrap;
8. inside `CryptoService`, validate wrapper version/decoded lengths/KDF resources before Argon2;
9. publish the data key only after successful authenticated unwrap.

Malformed JSON/schema/storage data is normalized to `VaultAuthenticationException` at the vault-service unlock boundary. No invalid structural header is allowed to reach wrapped-key unwrap.

## 7. Writer self-validation

`VaultService.SerializeHeader(...)` serializes every header written by the service and immediately validates the result through `VaultHeaderJsonPolicy` before persistence.

This prevents a future internal record change from silently emitting a same-version shape that the strict reader would reject. A format-changing root/wrapper/KDF metadata change therefore requires an explicit version/compatibility decision and matching tests/documentation.

## 8. Compatibility rules

- Version 1 remains read-compatible with its original three-property root.
- Version 2 is the only current write format.
- Version 1 plus a `secondary` property is invalid rather than being treated as an undocumented hybrid format.
- Version 2 without `secondary` is invalid even when the intended value would be null.
- Future header versions are rejected until explicitly implemented.
- Property matching is ordinal and case-sensitive even though the internal serializer options are otherwise web-oriented.

## 9. Automated regression coverage

Current automated coverage includes:

- strict policy unit tests for v1/v2 accepted shapes;
- duplicate/unknown/case-variant/missing/wrong-kind metadata;
- wrapped-key and KDF nested-property enforcement;
- non-integer KDF JSON values;
- maximum depth enforcement;
- exact 64 KiB accepted policy boundary and first byte above it;
- legacy v1 unlock compatibility;
- v1-to-v2 upgrade on master-passphrase mutation;
- oversized persisted-header rejection before wrapped-key unwrap;
- a deterministic 120-case hostile-header integration corpus that must never reach wrapped-key unwrap;
- source-regression tests for validation/deserialization/unwrap ordering and writer self-validation.

Deterministic adversarial coverage is a reproducible regression layer. It is not exhaustive coverage-guided fuzzing and is not an independent professional security audit.

## 10. Change checklist

Any change to this format should update, at minimum:

- `src/CipherNest.Infrastructure/Services/VaultHeaderJsonPolicy.cs`;
- `src/CipherNest.Infrastructure/Services/VaultService.cs`;
- `src/CipherNest.Shared/VaultStorageLimits.cs` when limits change;
- this document;
- `docs/LIMITS_AND_DEFAULTS.md`;
- `docs/TEST_PLAN.md`;
- `docs/TESTING_GUIDE.md`;
- `docs/security/CRYPTOGRAPHIC_DESIGN.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/architecture/DATABASE.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`;
- `what_changed.md`;
- compatibility/adversarial/source-regression tests.
