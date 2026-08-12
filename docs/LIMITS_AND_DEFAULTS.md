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

From `VaultItemValidator` and `SafeNoteLimits`:

| Item resource | Maximum / rule |
|---|---|
| Item ID | non-empty GUID |
| Item type | defined `VaultItemType` |
| Title | required, 256 chars |
| Username/identifier | 2,048 chars |
| Secret | 100,000 chars |
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
| Field characters | 1,000,000 |
| Aggregate characters in one row | 2,000,000 |
| User-visible retained import warnings | 20 |

Column enforcement applies to the final field at newline/EOF as well as comma-terminated fields. Header names must be non-empty and case-insensitively unique.

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

Attachments are not included in plaintext CSV export.

## Application preference defaults and normalization

From `AppPreferences` and `AppPreferencesPolicy`:

| Preference | Default | Normalized range/values |
|---|---:|---|
| Theme | System | System / Light / Dark |
| Language | System | System / English |
| Lock timeout | 60 s | 5–3,600 s |
| Lock on background | true | boolean |
| Clipboard clear | 30 s | 5–300 s |
| Screenshot protection | true | boolean/platform dependent |
| Biometric unlock | false | boolean/platform/configuration dependent |
| Reduced motion | false | boolean |
| Larger interface | false | boolean |
| Trash retention | 30 days | 1–365 days |
| Require master after | 24 h | 1–168 h |
| Backup reminder | 7 days | 1–365 days |
| Review reminders | true | boolean |
| Review reminder lead | 7 days | 0–365 days |
| Generator passphrase mode | false | boolean |
| Password length | 20 | 8–256 |
| Passphrase word count | 8 | 6–16 |
| Uppercase | true | boolean |
| Lowercase | true | boolean |
| Digits | true | boolean |
| Symbols | true | boolean |
| Exclude ambiguous | true | boolean |

If password mode has uppercase/lowercase/digits/symbols all disabled after deserialization, normalization restores lowercase so password generation still has a valid character source. That repair is not required in passphrase mode because character groups are unused there.

## Settings file resource bound

The current JSON settings persistence path rejects/avoids replacing settings when the non-secret preferences file exceeds its 64 KiB safety budget. Malformed/unreadable non-secret settings fall back to normalized defaults; cancellation still propagates.

## Trash and review timing

- Trash retention range: 1–365 days; default 30.
- Review reminder lead range: 0–365 days; default 7.
- Backup reminder range: 1–365 days; default 7.
- Periodic master-passphrase interval: 1–168 hours; default 24.

## Failed interactive unlock delay

The current client-side bounded schedule is:

| Failed attempt count | Delay |
|---:|---:|
| 1–4 | none |
| 5 | 5 s |
| 6 | 10 s |
| 7 | 20 s |
| 8 | 40 s |
| 9 | 80 s |
| 10 | 160 s |
| 11+ | 300 s cap |

This does not protect a copied database from offline guessing.

## Vault UI result paging

The current vault screen incrementally renders matching local results in pages of 50 items. Search/filter/sort still operates locally over decrypted data while unlocked; this is a visual-tree responsiveness boundary, not a plaintext persistent index.

## Generator passphrase word list

- exactly 256 validated unique lowercase words;
- requested word count: 6–16;
- default: 8 words;
- random selection uses `RandomNumberGenerator`;
- each independent uniform selection from 256 words corresponds to approximately 8 bits of random-selection entropy before user editing.

## Release/build defaults

- Nullable analysis: enabled.
- Warnings as errors: enabled.
- Analyzer level: latest.
- Code style enforced in build.
- Deterministic managed build setting: enabled.
- Funding link: enabled unless `CipherNestEnableFundingLink=false` is explicitly supplied to the MAUI build.

## Review rule for changing a limit

For a security/resource limit change, update together:

1. implementation constant/policy;
2. all validation/persistence paths that mirror it;
3. unit/integration/source tests;
4. `TEST_PLAN.md` and `RELEASE_CHECKLIST.md`;
5. affected format/security/architecture docs;
6. this reference;
7. `CHANGELOG.md`, `PROJECT_STATUS.md`, and `what_changed.md` when appropriate.
