# Attachment Metadata and Storage-Name Hardening Verification — 2026-08-15

This record defines the repository-side verification contract for the August 15, 2026 CipherNest attachment metadata and opaque storage-name hardening. It documents source behavior and deterministic regression coverage. It is not an independent security audit and does not claim exhaustive protection against every malicious filesystem, Unicode/runtime defect, compromised process/device, future format regression, or operating-system path behavior.

## Scope

The covered trust boundary includes attachment metadata carried inside encrypted vault-item JSON and the opaque encrypted attachment filename used before app-data filesystem access:

- `AttachmentReference.DisplayName`;
- `AttachmentReference.MediaType`;
- `AttachmentReference.EncryptedFileName`;
- attachment-ID to opaque-storage-name binding.

The encrypted `.cna` container framing and AES-GCM chunk authentication are separate layers and remain covered by `docs/formats/ATTACHMENTS.md` and existing attachment framing/tamper tests.

## Display-name normalization and stored metadata

`AttachmentImportPolicy.NormalizeDisplayName(...)` still converts an import path-like value to its final leaf component and trims outer whitespace before storage.

A persisted display name is now accepted only when all of the following hold:

1. it is non-null, non-empty, and not whitespace-only;
2. it contains at most 240 UTF-16 code units, matching the existing repository limit;
3. it is already trimmed;
4. it is not `.` or `..`;
5. it contains neither `/` nor `\\`;
6. its UTF-16 sequence decodes completely into Unicode scalar values;
7. no decoded rune has Unicode category `Control` or `Format`.

The `Format` rejection blocks invisible/directional formatting metadata such as bidirectional isolates/overrides and supplementary-plane formatting code points from being persisted as attachment display names. Malformed isolated UTF-16 surrogate input is also rejected rather than silently accepted through a code-unit-only check.

## Media-type metadata

`AttachmentImportPolicy.NormalizeMediaType(...)` still maps missing/whitespace input to `application/octet-stream` and trims non-empty input.

Persisted media-type text now requires:

- non-null/non-whitespace text;
- at most 256 UTF-16 code units;
- no leading or trailing whitespace;
- valid UTF-16 scalar decoding;
- no Unicode `Control` or `Format` rune.

This hardening does not claim to implement a complete RFC MIME grammar parser. Existing App-layer preview policy continues to normalize media types separately before using them for preview decisions.

## Rune-aware validation

The canonical metadata policy uses `Rune.DecodeFromUtf16(...)` and requires `OperationStatus.Done` for every decoded scalar. It classifies each decoded rune with `Rune.GetUnicodeCategory(...)` and rejects `UnicodeCategory.Control` or `UnicodeCategory.Format`.

This is intentionally stronger than the previous `string.Any(char.IsControl)` checks, which operated on UTF-16 code units and did not reject formatting characters.

## Canonical validator reuse

`VaultItemValidator` now calls:

- `AttachmentImportPolicy.IsValidStoredDisplayName(...)`;
- `AttachmentImportPolicy.IsValidStoredMediaType(...)`.

The validator therefore no longer maintains a separate weaker `char.IsControl` interpretation for persisted attachment metadata. Programmatically injected/decrypted item models pass through the same metadata policy used by attachment import normalization.

The existing item validator still independently enforces:

- non-empty attachment IDs;
- plaintext length from 0 through 100 MiB;
- canonical attachment-ID/storage-name binding;
- maximum 25 attachments per item;
- unique attachment IDs;
- unique encrypted storage names within an item;
- aggregate item-text and serialized-record limits elsewhere in the vault pipeline.

## Opaque `.cna` storage-name boundary

The only supported opaque encrypted attachment filename remains:

```text
<32 hexadecimal GUID-N characters>.cna
```

The complete filename length is exactly 36 characters.

`AttachmentStorageNamePolicy.ValidateOpaqueFileName(...)` now rejects any other length before stem extraction/parsing. This avoids allocating a potentially large substring from a hostile oversized string before discovering that the name cannot be canonical.

After the early length check, the policy still requires:

- no `/` or `\\` separator;
- `.cna` extension, compared case-insensitively for normalization compatibility;
- non-empty GUID parsed with exact `N` format;
- canonical lower-case `<guid-N>.cna` return value.

`ValidateForAttachment(...)` additionally requires that the parsed filename GUID equals the supplied non-empty attachment ID.

## Filesystem ordering

`EncryptedAttachmentStore` continues to call `AttachmentStorageNamePolicy.ValidateOpaqueFileName(...)` or `ValidateForAttachment(...)` before combining an attacker-influenced stored name with the encrypted attachment directory.

Encryption/decryption identity binding remains unchanged: attachment container AAD includes the owning item ID, attachment ID, and chunk index, while the opaque filename must independently match the attachment ID.

## Unit boundary coverage

`AttachmentImportPolicyTests` now covers:

- import path-to-leaf normalization;
- exact 240-character display-name acceptance and 241-character rejection;
- exact 256-character media-type acceptance and 257-character rejection;
- leading/trailing whitespace rejection for stored metadata;
- path-separator and dot-name rejection for persisted display names;
- ordinary control-character rejection;
- BMP Unicode `Format` rejection;
- supplementary-plane Unicode `Format` rejection using `U+E0001`;
- malformed isolated UTF-16 surrogate rejection.

`AttachmentStorageNamePolicyTests` covers:

- canonical lower/upper-case normalization;
- exact 36-character name length;
- 35/37-character rejection;
- a one-million-character hostile name rejected by the early length boundary;
- wrong extension/path separator/non-GUID/empty-GUID rejection;
- attachment-ID/name mismatch rejection.

`VaultItemValidatorTests` verifies persisted attachment metadata is rejected for control characters, Unicode format characters, malformed UTF-16, non-leaf display names, untrimmed media types, mismatched storage names, duplicate IDs, and duplicate storage names.

## Deterministic hostile corpus

`AttachmentMetadataAdversarialTests` contains exactly 128 deterministic hostile inputs:

- 48 display-name cases;
- 40 media-type cases;
- 40 opaque storage-name cases.

The corpus includes ASCII control characters, BMP and supplementary-plane formatting characters, malformed UTF-16, path separators, dot/whitespace forms, oversized metadata, wrong-length storage names, invalid GUID hex, wrong extensions, and separator-bearing fixed-length storage names.

Every display/media case must fail the canonical stored-metadata predicate. Every storage-name case must fail the opaque storage-name policy.

This is reproducible deterministic adversarial regression coverage. It is not coverage-guided or exhaustive fuzzing.

## Source-regression coverage

`AttachmentMetadataSafetySourceTests` requires:

- rune decoding rather than UTF-16 `char` classification;
- explicit `OperationStatus.Done` handling;
- Unicode `Control` and `Format` rejection;
- `VaultItemValidator` reuse of the canonical attachment metadata predicates;
- removal of the old direct `attachment.*.Any(char.IsControl)` checks;
- the exact 36-character opaque filename constant;
- early opaque-name length validation before stem parsing;
- span-based stem parsing rather than a full substring slice.

## Compatibility

This change does not alter the encrypted `CNAT0001` binary container format, its chunk framing, AAD layout, crypto version, backup placement, or canonical `<guid-N>.cna` identity rule.

Existing correctly normalized attachment metadata remains compatible. Stored metadata containing path separators, outer whitespace, malformed UTF-16, Unicode control characters, or Unicode format characters is now treated as invalid metadata and fails validation rather than being surfaced as a valid vault item.

## Documentation synchronization

Keep these surfaces aligned when attachment metadata/storage-name rules change:

- `docs/formats/ATTACHMENTS.md`;
- `docs/formats/VAULT_RECORDS.md`;
- `docs/LIMITS_AND_DEFAULTS.md`;
- `docs/TEST_PLAN.md`;
- `docs/TESTING_GUIDE.md`;
- `docs/NEXT_STEPS.md`;
- `docs/COMPLETE_PROJECT_DOCUMENTATION.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`;
- `what_changed.md`.

## Remaining limitations and next parser work

This work does not prove safety against every malicious `.cna` container, filesystem race/reparse behavior, compromised OS/process, future Unicode/runtime defect, backup archive path issue, or plaintext export destination behavior.

Broader source-side adversarial work still includes:

- CSV row/import semantics beyond header metadata;
- backup ZIP/archive semantics beyond backup-header metadata;
- TOTP Base32 input;
- decrypted/encrypted vault-record envelope semantics.

Independent professional security review remains outstanding.

## Required exact-candidate gates

For an immutable candidate containing this change, repository evidence should include successful execution of:

- UnitTests analyzer build and tests;
- IntegrationTests analyzer build and tests;
- UiTests/source-regression analyzer build and tests;
- configured `dotnet format --verify-no-changes` checks;
- Windows Release build;
- Windows Release with `CipherNestEnableFundingLink=false`;
- Android Release build;
- iOS simulator Release build;
- Mac Catalyst Release build;
- CodeQL analyzable core/application build and analysis.

Any later commit invalidates exact-head evidence for an earlier SHA and requires the configured gates to run again.
