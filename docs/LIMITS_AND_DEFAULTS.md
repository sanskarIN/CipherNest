# CipherNest Limits, Defaults, and Versions

This reference collects implemented resource ceilings and defaults that are otherwise spread across Domain/Application/Infrastructure code. Limits are defensive safety boundaries, not recommended target sizes. Raising them requires memory/performance/security review and matching tests/documentation.

## Product and format versions

From `CipherNest.Shared.AppConstants` and current format policies:

| Setting | Current value |
|---|---:|
| Product version | `0.1.0` |
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

A future format/schema change must not silently reuse an old version when compatibility changes.

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

## Vault storage limits

From `CipherNest.Shared.VaultStorageLimits`:

| Resource | Maximum |
|---|---:|
| Vault-header UTF-8 bytes | 64 KiB |
| Serialized/decrypted item JSON | 16 MiB |
| Stored encrypted envelope per row | 24 MiB |
| Encrypted item rows | 100,000 |
| Aggregate stored encrypted-envelope bytes | 256 MiB |
| Referenced attachments across vault | 10,000 |

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

## Attachment import metadata

From `AttachmentImportPolicy`:

| Resource | Limit/default |
|---|---|
| Display name | maximum 240 characters; normalized to leaf filename |
| Media type | maximum 256 characters |
| Missing media type | `application/octet-stream` |
| Control characters | rejected in display name/media type |

Encrypted storage identity is canonicalized to `<attachment-guid-N>.cna` and path separators are rejected before filesystem access.

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
| Salt | 16 bytes | 64 bytes |
| Chunk size accepted | 64 KiB | 4 MiB |
| Current export chunk size | 1 MiB |
| Encrypted chunk count | 0 | 65,536 maximum indexes |
| KDF memory | 16 MiB | 512 MiB |
| KDF iterations | 1 | 10 |
| KDF parallelism | 1 | 16 |

The header bounds are validated before Argon2 derivation during restore.

## Backup archive limits

From `BackupArchivePolicy` and vault attachment budget:

| Resource | Maximum |
|---|---:|
| Archive entries | 10,001 (`vault.db` + up to 10,000 attachments) |
| Aggregate plaintext ZIP/archive content | 1 GiB |

Restore additionally restricts paths to the expected database/attachment layout, rejects normalized duplicate paths, and checks encrypted attachment entry sizes against the actual attachment-container envelope.

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

Attachments are not included in plaintext CSV export. The current CSV surface is generic and does not claim dedicated TOTP/`otpauth://` interoperability.
