# CipherNest Limits, Defaults, and Versions

This reference collects implemented resource ceilings and defaults that are otherwise spread across Domain/Application/Infrastructure code. Limits are defensive safety boundaries, not recommended target sizes. Raising them requires memory/performance/security review and matching tests/documentation.

## Product and format versions

From `CipherNest.Shared.AppConstants` and current format policies:

| Setting | Current value |
|---|---:|
| Product version | `2.4.8` |
| Database schema version | `1` |
| Cryptographic envelope version | `1` |
| Current vault-header document version | `2` |
| Minimum supported vault-header document version | `1` |
| Encrypted backup format version | `2` |
| Encrypted backup magic | `CNBK0002` |
| Encrypted attachment container magic | `CNAT0001` |
| Database filename | `ciphernest.db` |
| Attachment directory | `attachments` |
| Backup extension | `.cnbak` |

Product version `2.4.8` is current release-preparation metadata. It does not change the database, encrypted-envelope, vault-header, backup, or attachment compatibility versions by itself. A future format/schema change must not silently reuse an old version when compatibility changes.

## Cryptographic sizes/defaults

Current `CryptoService` values:

| Parameter | Value |
|---|---:|
| Data/AES key | 32 bytes / 256 bits |
| AES-GCM nonce | 12 bytes / 96 bits |
| AES-GCM tag | 16 bytes / 128 bits |
| New-wrapper Argon2id memory | 65,536 KiB / 64 MiB |
| New-wrapper Argon2id iterations | 3 |
| New-wrapper Argon2id parallelism | 1 |
| New-wrapper salt | 16 bytes |
| Derived-key output | 32 bytes |

### Accepted KDF metadata resource bounds

| Parameter | Minimum | Maximum |
|---|---:|---:|
| Salt | 16 bytes | 64 bytes |
| Argon2id memory | 16,384 KiB / 16 MiB | 524,288 KiB / 512 MiB |
| Iterations | 1 | 10 |
| Parallelism | 1 | 16 |

Accepted low-end values are compatibility/resource bounds; new wrappers use the default 64 MiB / 3 / 1 parameters.

## Passphrase/recovery input bounds

`CryptoService` accepts passphrase/recovery credential lengths from:

- minimum: 12 characters;
- maximum: 4,096 characters.

The onboarding flow also requires its generator/strength policy before a new vault can be created. Backup Settings requires the same practical 12–4,096 character range before backup/restore work.

The character ceiling exists to bound input/resource work. It is not a recommendation to use extremely long inputs.

## TOTP defaults and input bounds

From `TotpPolicy`, `VaultItem`, and `TotpService`:

| TOTP setting/resource | Current value |
|---|---|
| Default algorithm | SHA-1 |
| Supported algorithms | SHA-1 / SHA-256 / SHA-512 |
| Default digits | 6 |
| Supported digits | 6 or 8 |
| Default period | 30 seconds |
| Supported period | 15–120 seconds |
| Formatted seed input | maximum 4,096 characters |
| Normalized Base32 seed | 16–1,024 characters |
| Base32 alphabet | A-Z / 2-7, case-insensitive input |
| Grouping accepted | whitespace and `-` removed during normalization |
| Padding | optional terminal `=`; impossible lengths/non-zero residual bits rejected |

The TOTP limits bound local parser/HMAC work. A generated code is transient presentation state and is not persisted in the encrypted item record.

### TOTP setup-URI interoperability bounds

From `TotpUriCodec`:

| `otpauth://` resource/rule | Current value |
|---|---|
| Accepted scheme/type | absolute `otpauth://totp/...` only |
| URI text | maximum 8,192 characters |
| Query pairs | maximum 16 |
| Query parameter name | maximum 64 characters; ASCII letters/digits/`-`/`_` only |
| Query pair shape | non-empty `name=value`; empty pairs rejected |
| Query value encoding | percent encoding validated for every pair, including otherwise ignored unknown parameters |
| Account name | maximum 512 characters; `:` rejected inside the component |
| Issuer | maximum 256 characters; `:` rejected inside the component |
| Label | maximum 769 characters before account/issuer splitting |
| Label separator | at most one `:` issuer/account separator; empty issuer prefix rejected |
| User-info | rejected |
| Custom port | rejected |
| URI fragment | rejected |
| Duplicate query keys | rejected case-insensitively |
| HOTP host/type | rejected |
| `counter` parameter | rejected |
| Display metadata Unicode Control/Format runes | rejected |
| Label issuer vs `issuer=` mismatch | rejected when both are present |
| Imported seed/settings | revalidated by `TotpPolicy` |

The `:` rule avoids format→parse reinterpretation because the Key URI label uses `:` as the issuer/account delimiter. Setup URIs normally contain the long-lived Base32 seed. The URI field is sensitive transient UI state, not a separately persisted vault field. Canonical setup-URI copies use the existing timed secret-clipboard path; operating-system clipboard history/synchronization remains outside a guaranteed-erasure boundary.

## Vault storage limits

From `CipherNest.Shared.VaultStorageLimits`:

| Resource | Maximum |
|---|---:|
| Vault-header UTF-8 bytes | 64 KiB |
| Vault-header JSON nesting depth | 16 |
| Vault-header JSON schema | exact case-sensitive v1/v2 root + wrapped-key/KDF property sets |
| Serialized/decrypted item JSON | 16 MiB |
| Stored encrypted envelope per row | 24 MiB |
| Encrypted item rows | 100,000 |
| Aggregate stored encrypted-envelope bytes | 256 MiB |
| Referenced attachments across vault | 10,000 |

Vault-header version 1 is read-compatible only with the exact `version`/`master`/`recovery` root; version 2 is the current write format and additionally requires `secondary` (which may be null). Every non-null wrapped-key object and nested KDF object uses an exact case-sensitive property set, and duplicate/unknown/missing/wrong-kind metadata or nesting beyond 16 is rejected before typed header deserialization/wrapped-key unwrap. Replacement-database validation applies the same strict header policy before active DB/WAL/SHM mutation.

SQLite and service-level paths enforce overlapping limits so a custom store cannot intentionally bypass every boundary.

## Vault item validation limits

From `VaultItemValidator`, `TotpPolicy`, and `SafeNoteLimits`:

| Item resource | Maximum / rule |
|---|---|
| Item ID | non-empty GUID |
| Item type | defined `VaultItemType` |
| Title | required, 256 chars |
| Username/identifier | 2,048 chars |
| Secret | 100,000 chars general field ceiling; TOTP has stricter bounds above |
| URL | 4,096 chars |
| Notes | 200,000 chars |
| Note lines | 5,000 |
| Collection | 128 chars |
| Tag count | 100 |
| Tag length | 128 chars; non-empty |
| Custom-field count | 100 |
| Custom-field name | 128 chars; non-empty |
| Custom-field value | 100,000 chars |
| Attachments/item | 25 |
| Attachment plaintext length | 100 MiB |
| Combined item text/metadata | 2,000,000 characters |

Combined text accounting includes core strings, tags, custom-field names/values, and attachment display/media/storage metadata.

## Attachment import and persisted metadata

From `AttachmentImportPolicy` and `AttachmentStorageNamePolicy`:

| Resource | Limit/default/rule |
|---|---|
| Display name | maximum 240 UTF-16 code units; import normalized to trimmed leaf filename |
| Stored display name | trimmed leaf; not `.`/`..`; no `/` or `\\`; valid UTF-16; Unicode Control/Format runes rejected |
| Media type | maximum 256 UTF-16 code units |
| Stored media type | already trimmed; valid UTF-16; Unicode Control/Format runes rejected |
| Missing media type | `application/octet-stream` |
| Opaque encrypted filename | exactly 36 characters: 32-char non-empty GUID-N stem + `.cna` |
| Opaque filename separators | `/` and `\\` rejected before filesystem access |

Metadata classification is rune/code-point aware through `Rune.DecodeFromUtf16` and `Rune.GetUnicodeCategory`. Supplementary-plane `Format` characters and malformed isolated UTF-16 surrogates are rejected rather than slipping through code-unit-only `char.IsControl` checks. `VaultItemValidator` reuses the same display/media predicates for decrypted/programmatically supplied item metadata.

The opaque filename length check runs before stem parsing. This prevents an oversized hostile name from creating a large stem substring before the canonical 36-character requirement is discovered. Accepted case variants are normalized to lower-case `<attachment-guid-N>.cna`, and `ValidateForAttachment` additionally requires the filename GUID to match the attachment ID.

## Encrypted attachment framing

From `EncryptedAttachmentStore` / `AttachmentFormatPolicy`:

| Parameter | Value |
|---|---:|
| Plaintext chunk size | 256 KiB |
| Maximum plaintext file size | 100 MiB |
| Maximum encrypted chunk count | 16,384 |
| Minimum container size | 12 bytes |
| Per-chunk framing overhead | 32 bytes beyond ciphertext (`length + nonce + tag`) |

The chunk-count ceiling is intentionally higher than the number of 256 KiB chunks required for a valid 100 MiB attachment; malformed containers therefore still have an independent bounded loop ceiling.

### Attachment text preview

Current Item Editor preview bounds:

| Resource | Maximum |
|---|---:|
| Decrypted preview bytes | 512 KiB |
| Displayed preview characters | 20,000 |

Preview is limited to supported TXT/Markdown/CSV/JSON/LOG text-family content and requires valid UTF-8. Other formats remain encrypted until explicit export.

## Encrypted backup framing

From `BackupFormatPolicy`:

| Parameter | Minimum | Maximum/current |
|---|---:|---:|
| Format version | — | exactly `2` |
| Header JSON bytes | 16 bytes | 16,384 bytes |
| Header JSON nesting depth | — | 16 |
| Salt | 16 bytes | 64 bytes |
| Chunk size accepted | 64 KiB | 4 MiB |
| Current export chunk size | 1 MiB |
| Encrypted chunk count | 0 | 65,536 maximum indexes |
| KDF memory | 16 MiB | 512 MiB |
| KDF iterations | 1 | 10 |
| KDF parallelism | 1 | 16 |

The declared and actual header byte bounds are validated before Argon2 derivation during restore. Version-2 header JSON additionally uses an explicit 16-level parser depth ceiling and exact case-sensitive root/KDF property sets; duplicate, unknown, missing, case-variant, and wrong-type metadata is rejected before typed deserialization/key derivation.

## Backup archive limits

From `BackupArchivePolicy` and vault attachment budget:

| Resource | Maximum |
|---|---:|
| Archive entries | 10,001 (`vault.db` + up to 10,000 attachments) |
| Aggregate plaintext ZIP/archive content | 1 GiB |

Restore additionally restricts paths to the expected database/attachment layout, rejects normalized duplicate paths, and checks encrypted attachment entry sizes against the actual attachment-container envelope.

## Settings JSON limits

From `JsonSettingsStore` and `AppPreferencesPolicy`:

| Resource | Maximum / behavior |
|---|---:|
| Settings file bytes | 64 KiB |
| JSON nesting depth | 16 |
| Read buffer | 64 KiB + 1 sentinel byte |
| Malformed/invalid UTF-8 JSON | falls back to normalized defaults |
| Oversized JSON | falls back to normalized defaults |

The settings loader first rejects an already-oversized file by length, then independently reads through a fixed 64 KiB + 1 byte buffer before deserializing from bounded memory. This second boundary prevents a file that changes after the initial length snapshot from causing unbounded parser input. JSON nesting is capped at 16 because the persisted `AppPreferences` schema is flat; malformed, over-depth, or invalid UTF-8 content is treated as unreadable local settings and falls back safely. Cancellation is not converted into a fallback and continues to propagate to the caller.

All successfully parsed settings still pass through `AppPreferencesPolicy.Normalize(...)`, which clamps numeric ranges, rejects undefined persisted enum values through safe defaults, and restores at least one password character group when password mode would otherwise have none.

## CSV parser/import limits

From `CsvTransferService`:

| Resource | Maximum |
|---|---:|
| Columns | 256 |
| Data rows | 100,000 |
| Header name characters | 256 |
| Field characters | 1,000,000 |
| Aggregate characters in one row | 2,000,000 |
| User-visible retained import warnings | 20 |

Column enforcement applies to the final field at newline/EOF as well as comma-terminated fields. Header names must be non-empty, no longer than 256 characters, case-insensitively unique, and free of Unicode control/`Format` characters before they are exposed to import mapping UI. Header preview and actual import pass the 256-character ceiling directly into the streaming parser, while post-parse validation repeats it as defense in depth. Unsafe Unicode classification is rune/code-point aware, so supplementary-plane `Format` characters are rejected rather than being treated only as isolated UTF-16 surrogate code units. The dedicated header-name limit is intentionally stricter than the generic field limit because headers are mapping/display metadata rather than vault payload data.

## CSV export columns

Current plaintext export writes:

```text
Title
Type
Username
Secret
URL
Notes
Tags
Collection
Favorite
ReviewAfterUtc
```

Attachments are not included in plaintext CSV export. The CSV surface remains generic rather than acting as a dedicated authenticator migration format; dedicated bounded single-item TOTP `otpauth://totp/...` import/formatting now exists separately in the item editor.

## Final parser/extraction bounds synchronization — 2026-08-15

The final repository-side hardening reuses and enforces these existing limits earlier at untrusted-input boundaries:

- Tags: at most **100 per vault item** and **128 UTF-16 code units per tag**. CSV mapped Tags parsing enforces these bounds before `VaultItem` construction and materializes at most 100 accepted tag strings.
- TOTP Base32: normalized seed remains **16..1,024 characters**, formatted input remains capped at **4,096 characters**, and the final validity timestamp is clamped at `DateTimeOffset.MaxValue` when the next period boundary is not representable.
- Backup ZIP restore: aggregate uncompressed archive content remains capped at **1 GiB** and entry count at `VaultStorageLimits.MaximumAttachmentCountTotal + 1`; actual extracted bytes must now exactly equal each entry's declared uncompressed length, using a reusable **128 KiB** extraction buffer.

These are resource/safety ceilings, not recommended target sizes for ordinary data.

## TOTP setup-URI bounds synchronization — 2026-08-18

The `TotpUriCodec` continuation adds a separate bounded text/parser surface without changing vault-record or cryptographic-envelope versions. URI import reuses the existing authoritative `TotpPolicy` seed/settings validation after URI-specific structure and metadata validation. Setup-URI text is intentionally not persisted as a second copy of the seed.

The final hardening pass additionally rejects ambiguous labels with extra `:` separators, rejects `:` inside account/issuer components, rejects an empty issuer prefix before a separator, rejects empty query pairs, and validates percent encoding/control characters for every query value even when the parameter itself is unknown and ignored. These rules preserve deterministic format→parse semantics and prevent malformed extension parameters from bypassing structural validation.
