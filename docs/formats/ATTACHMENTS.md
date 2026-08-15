# CipherNest Encrypted Attachment Format

CipherNest attachments are stored as separately authenticated encrypted files so large files can be processed in bounded chunks instead of being embedded into item JSON or fully materialized in memory.

## 1. Logical relationship

A `VaultItem` stores encrypted-at-rest attachment metadata inside its encrypted JSON payload. The actual encrypted bytes live under the CipherNest attachment directory.

Logical reference:

```text
VaultItem
  -> AttachmentReference
       -> Id
       -> DisplayName
       -> MediaType
       -> PlaintextLength
       -> EncryptedFileName
  -> app-data/attachments/<EncryptedFileName>
```

User display names are not used as encrypted storage filenames.

## 2. Storage filename

For an attachment GUID, the canonical opaque storage filename is:

```text
<attachment-guid-N>.cna
```

Example using a synthetic GUID:

```text
0123456789abcdef0123456789abcdef.cna
```

Rules:

- attachment ID must not be `Guid.Empty`;
- the complete opaque filename must be exactly 36 characters;
- the 36-character length check runs before GUID-stem parsing so an oversized hostile string is rejected before any large stem substring/allocation;
- stem must parse as GUID format `N`;
- `.cna` extension is required;
- `/` and `\` path separators are rejected;
- metadata validation binds the filename to the same attachment ID;
- filesystem paths are built only after this validation.

Accepted upper-case GUID/extension input is normalized to canonical lower-case `<guid-N>.cna`; persisted/generated names are already canonical lower-case.

## 3. Container magic/version

Current attachment magic:

```text
CNAT0001
```

This eight-byte prefix identifies the current attachment container framing. Cryptographic record-envelope versioning remains independently defined by `AppConstants.CryptoFormatVersion`.

## 4. Plaintext/chunk limits

Current values:

```text
Chunk size:              256 KiB
Maximum plaintext file:  100 MiB
Maximum chunk index:     16,383
Maximum chunk count:     16,384
```

The 16,384 framing ceiling independently bounds malformed-container loop work even though a valid 100 MiB file needs far fewer 256 KiB chunks.

## 5. Binary framing

Integers are written as signed 32-bit big-endian values.

Container structure:

```text
8 bytes   magic = "CNAT0001"

repeat for each plaintext chunk:
  4 bytes   plaintext length, big-endian int32 (1..262144)
  12 bytes  AES-GCM nonce
  16 bytes  AES-GCM tag
  N bytes   ciphertext, N = plaintext length

4 bytes   end marker = -1 (big-endian int32)
EOF       no trailing bytes allowed
```

For each normal chunk, ciphertext length equals plaintext length because AES-GCM does not add block padding.

Per-chunk framing overhead beyond ciphertext is:

```text
4-byte length + 12-byte nonce + 16-byte tag = 32 bytes
```

## 6. Associated data

Each chunk is authenticated with context containing:

```text
16 bytes  item GUID bytes
16 bytes  attachment GUID bytes
4 bytes   chunk index as big-endian int32
```

Total AAD length:

```text
36 bytes
```

Conceptually:

```text
AAD = ItemId || AttachmentId || ChunkIndexBE
```

This prevents a valid encrypted chunk from being intentionally transplanted to a different item, attachment, or chunk position without authentication failure.

## 7. Encryption flow

`EncryptedAttachmentStore.EncryptAsync` currently:

1. validates non-empty item/attachment IDs;
2. validates readable source stream;
3. requires a 32-byte vault data key;
4. creates the encrypted attachment directory;
5. validates canonical attachment storage identity;
6. creates unique sibling staging:
   `.<final-name>.<guid>.tmp`;
7. opens staging with `FileMode.CreateNew`;
8. writes `CNAT0001`;
9. fills a reusable 256 KiB plaintext buffer before framing a chunk;
10. enforces chunk-index and total-plaintext bounds;
11. generates a fresh nonce through the crypto service;
12. AES-GCM encrypts using item/attachment/chunk AAD;
13. writes length/nonce/tag/ciphertext;
14. zeroes the used plaintext buffer span;
15. writes `-1` end marker;
16. flushes;
17. moves staging to the final path with `overwrite: false`;
18. zeroes the full reusable buffer and best-effort removes staging in `finally`.

If a final-file collision exists, CipherNest fails closed rather than replacing an existing encrypted attachment container.

## 8. Decryption flow

`DecryptToAsync` currently:

1. validates IDs/destination/key/expected plaintext length;
2. validates canonical storage name before path construction;
3. reads/checks magic using fixed-time byte comparison;
4. reads each big-endian plaintext length;
5. stops only on `-1`;
6. bounds the chunk index;
7. requires chunk length between 1 and 256 KiB;
8. reads exactly 12 nonce bytes, 16 tag bytes, and N ciphertext bytes;
9. AES-GCM authenticates/decrypts with the expected item/attachment/chunk AAD;
10. checks cumulative plaintext does not exceed expected length;
11. writes plaintext to caller destination;
12. zeroes the owned plaintext byte array after each write;
13. after the end marker, requires exact expected plaintext length and exact EOF;
14. flushes the destination.

Truncation, trailing bytes, wrong identity/key, modified tag/ciphertext, invalid sizes, or excessive chunk count are rejected.

## 9. Container size envelope

Current exported constants include:

```text
MinimumContainerBytes = 12
MaximumContainerBytes =
  MaximumPlaintextBytes
  + ceil(MaximumPlaintextBytes / ChunkSize) * 32
  + 8-byte magic
  + 4-byte end marker
```

Backup restore uses the same attachment-container size envelope to reject archive entries that cannot be valid CipherNest encrypted attachments before installing them.

## 10. Attachment import and persisted metadata

Before encryption, `AttachmentImportPolicy` normalizes:

- display name to a trimmed leaf filename;
- missing media type to `application/octet-stream`;
- non-empty media type by trimming outer whitespace.

Persisted display-name rules:

```text
Maximum length: 240 UTF-16 code units
Must already be trimmed
Must be a leaf name (no / or \\)
`.` and `..` rejected
Unicode Control and Format runes rejected
Malformed UTF-16 rejected
```

Persisted media-type rules:

```text
Maximum length: 256 UTF-16 code units
Must already be trimmed
Unicode Control and Format runes rejected
Malformed UTF-16 rejected
Missing import value -> application/octet-stream
```

Unicode classification is rune/code-point aware. The policy decodes with `Rune.DecodeFromUtf16(...)`, requires complete scalar decoding, and rejects both `UnicodeCategory.Control` and `UnicodeCategory.Format`, including supplementary-plane formatting code points. This prevents the previous code-unit-only gap where invisible/directional formatting metadata could survive attachment validation.

`VaultItemValidator` reuses the same canonical display-name/media-type predicates rather than maintaining a weaker duplicate metadata check. The current per-item attachment cap is 25; the global referenced-attachment storage/backup cap is 10,000.

The media-type metadata boundary is intentionally not described as a complete MIME grammar implementation. The App preview layer separately normalizes media types before deciding whether a format is previewable.

## 11. Text preview

The App provides bounded in-memory preview for supported text-family attachments:

- TXT;
- Markdown;
- CSV;
- JSON;
- LOG.

Current preview boundaries:

```text
Maximum decrypted bytes:      512 KiB
Maximum displayed characters: 20,000
Encoding:                      strict UTF-8
```

The preview flow sanitizes unsupported control characters and neutralizes angle brackets. It does not intentionally create a plaintext preview file.

The decoded managed string remains subject to .NET memory-lifetime limitations.

## 12. Plaintext export

Explicit attachment export is outside the encrypted attachment-container format.

The App:

- warns the user;
- creates a unique temporary plaintext cache path;
- decrypts through `IVaultService.ExportAttachmentAsync`;
- invokes the OS share sheet;
- attempts deletion in `finally`;
- reports cleanup failure without showing the sensitive path.

Destination applications/OS services can retain plaintext copies outside CipherNest control.

## 13. Deletion ordering

Permanent item deletion snapshots attachment storage names, deletes the encrypted database record first, then attempts best-effort encrypted attachment-file cleanup.

This ordering avoids intentionally deleting attachment files before knowing that the authoritative item record was removed.

Logical file deletion is not physical media sanitization.

## 14. Backup relationship

Encrypted `.cna` files are inserted into encrypted backup ZIP staging as already-encrypted files under:

```text
attachments/<guid-N>.cna
```

Backup archive restore:

- accepts only one-level canonical GUID `.cna` entries beneath `attachments/`;
- rejects normalized duplicates;
- checks each entry against `MinimumContainerBytes`/`MaximumContainerBytes`;
- later item metadata validation requires canonical attachment-ID/storage-name binding.

## 15. Compatibility rules

Changes to any of these require a reviewed version/compatibility decision:

- magic bytes;
- chunk-size/framing semantics;
- nonce/tag sizes;
- end marker;
- integer endianness;
- AAD layout;
- storage-name identity rules;
- persisted attachment metadata acceptance rules;
- maximum file/container resources.

Do not change framing under `CNAT0001` if old and new implementations would interpret the same bytes differently.

The August 15, 2026 metadata hardening does not change `CNAT0001` binary framing. Correctly normalized existing metadata remains compatible; malformed UTF-16, Control/Format-bearing metadata, non-leaf display names, and untrimmed stored metadata now fail validation.

## 16. Verification

The repository-side hardening contract is recorded in `../verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md`. It includes boundary tests, source-regression guards, and an exactly 128-input deterministic hostile metadata/storage-name corpus. That corpus is reproducible regression coverage, not exhaustive coverage-guided fuzzing or an independent security audit.

## 17. Security limitations

The attachment format does not claim to hide:

- encrypted file existence;
- encrypted container size;
- filesystem timestamps/metadata;
- access timing;
- plaintext after an explicit export/preview inside process memory;
- data from a privileged attacker controlling the OS/process.

See `../security/THREAT_MODEL.md` and `../security/DATA_LIFECYCLE.md`.
