# CipherNest Vault Record Format

This document describes the logical and encrypted representation of current vault items. It is implementation documentation for format/version review, not a promise that arbitrary future versions will be readable without an explicit compatibility path.

## 1. Domain model

A vault item is represented by `CipherNest.Domain.Models.VaultItem`.

Current encrypted payload fields:

```text
Id
Type
Title
Username
Secret
Url
Notes
Collection
Tags
IsFavorite
CustomFields
Attachments
TotpAlgorithm
TotpDigits
TotpPeriodSeconds
CreatedUtc
ModifiedUtc
LastAccessedUtc
ReviewAfterUtc
DeletedUtc
RequiresReauthentication
```

All of these item fields are serialized into encrypted record plaintext. Searchable item content is not copied into a plaintext SQLite search/FTS index.

The three TOTP settings were added as ordinary encrypted JSON members rather than plaintext database columns. Older records that do not contain them use the domain defaults SHA-1 / 6 digits / 30 seconds.

## 2. Item types and persisted numeric compatibility

`VaultItemType` is serialized by the current `System.Text.Json` options using numeric enum values. Existing values are therefore part of encrypted-record compatibility and are explicitly fixed:

```text
0  Login
1  SecureNote
2  Identity
3  PaymentCardReference
4  WifiCredential
5  SoftwareLicense
6  ServerSshReference
7  Document
8  Custom
9  OneTimePassword
```

New enum values must never be inserted by implicitly shifting an existing persisted value. `VaultItemTypeCompatibilityTests` locks these numbers in source tests.

Type is part of serialized encrypted item JSON. Unknown runtime enum values are rejected by validation.

## 3. TOTP item representation

For `Type = OneTimePassword`:

```text
Secret             = Base32 TOTP seed
TotpAlgorithm      = Sha1 | Sha256 | Sha512
TotpDigits         = 6 | 8
TotpPeriodSeconds  = 15..120
```

The issuer/account label can use existing encrypted fields such as `Title`, `Username`, `Url`, `Collection`, `Tags`, `Notes`, and custom fields. Generated one-time codes are transient editor presentation state and are **not** serialized into `VaultItem`.

`VaultItemValidator` requires a valid bounded Base32 seed and supported settings for a `OneTimePassword` item. Non-TOTP items do not require a TOTP seed.

See `../security/TOTP.md` for generation and threat details.

## 4. Normalization before save

`VaultItem.Normalize(now)` currently:

- trims Title;
- trims Username;
- trims Url;
- trims Collection;
- trims tags;
- removes empty tags;
- removes case-insensitive duplicate tags;
- sorts tags case-insensitively;
- updates `ModifiedUtc` to the supplied time.

The TOTP seed is not rewritten during generic item normalization. TOTP generation performs its own bounded Base32 normalization so the encrypted stored value remains the value the user supplied.

Normalization is not a substitute for validation.

## 5. Validation before serialization

`VaultItemValidator` rejects invalid or resource-hostile item payloads.

Important rules:

- item ID must be a non-empty GUID;
- item type must be defined;
- title is required and at most 256 characters;
- username/identifier at most 2,048 characters;
- secret at most 100,000 characters for the general item model;
- a TOTP formatted seed is additionally capped at 4,096 characters before normalization and 1,024 normalized Base32 characters;
- a TOTP normalized seed must contain at least 16 Base32 characters and use a structurally valid Base32 length/alphabet/padding form;
- TOTP algorithm must be SHA-1, SHA-256, or SHA-512; digits must be 6 or 8; period must be 15..120 seconds;
- URL at most 4,096 characters;
- notes at most 200,000 characters / 5,000 lines;
- collection at most 128 characters;
- at most 100 tags, each non-empty and at most 128 characters;
- at most 100 custom fields;
- custom-field name non-empty / at most 128 characters;
- custom-field value at most 100,000 characters;
- at most 25 attachments per item;
- attachment metadata must be valid/canonical;
- attachment IDs/storage names must be unique within the item;
- total item text/metadata must not exceed 2,000,000 characters.

Runtime-null members caused by malformed deserialization are treated defensively rather than assumed impossible because the CLR model uses non-nullable declarations.

## 6. Plaintext serialization budget

The normalized/validated item is serialized to UTF-8 JSON.

Current maximum serialized/decrypted item JSON size:

```text
16 MiB
```

This service-level bound exists even though ordinary item validation should normally reject pathological objects long before that ceiling.

## 7. Record encryption

The serialized UTF-8 item JSON is encrypted using AES-256-GCM under a private `VaultKeyLease` copy of the active random vault DEK.

A fresh 96-bit nonce is generated for every record encryption.

The encrypted record envelope logically contains:

```text
Version
Nonce (12 bytes)
Ciphertext (same length as plaintext JSON)
Tag (16 bytes)
```

Current cryptographic envelope version:

```text
1
```

Adding the TOTP fields does not change this outer envelope version because framing, nonce/tag lengths, key use, and associated-data rules are unchanged; the encrypted JSON payload gains backward-compatible members with defaults.

## 8. Record associated data

The item's GUID bytes are supplied as AES-GCM associated data.

Conceptually:

```text
AAD = item.Id bytes
```

This binds the encrypted payload to the authenticated storage row identity. Moving an encrypted envelope to a different item GUID is therefore not intended to produce a valid record.

## 9. SQLite row representation

`VaultItems` stores a structural row identity plus an opaque encrypted envelope.

The persistence contract is represented by:

```csharp
StoredVaultItem(Guid Id, byte[] Envelope)
```

The SQLite store requires the stored ID to use the canonical lower-case GUID `D` string representation and rejects empty/non-canonical identifiers.

Item title, username, secret/TOTP seed, URL, notes, collection, tags, favorite state, custom fields, attachment display metadata, TOTP settings, review/trash/recent-use timestamps remain inside encrypted payloads.

## 10. Stored envelope resource limits

Current storage resource boundaries:

```text
24 MiB per encrypted envelope
100,000 item rows
256 MiB aggregate encrypted envelopes
```

The SQLite store checks count/aggregate/per-row lengths before materializing large BLOB collections where practical. `VaultService` also enforces compatible service-level bounds so an alternate store cannot intentionally bypass every resource boundary.

## 11. Read/decrypt validation order

Conceptual record read:

```text
read bounded StoredVaultItem
      |
      v
obtain session-linked VaultKeyLease
      |
      v
AES-GCM authenticate/decrypt using stored row GUID as AAD
      |
      v
bounded plaintext UTF-8 JSON bytes
      |
      v
deserialize VaultItem
      |
      +--> payload Id must equal authenticated row Id
      +--> VaultItemValidator must succeed
      |
      v
return decrypted domain object
```

If authenticated decrypted metadata is malformed, including malformed TOTP metadata on a TOTP item, the infrastructure boundary rejects it rather than allowing the object to reach search/UI code.

Owned plaintext JSON byte arrays are zeroed in `finally` after deserialization/validation.

## 12. Attachment references inside items

Attachment files are stored separately, but their logical metadata is part of the encrypted item JSON. An `AttachmentReference` includes the attachment identity/display/media/size/storage information required by the application.

The encrypted storage name is required to be canonically bound to its attachment ID:

```text
<attachment-guid-N>.cna
```

That metadata remains encrypted at rest because it resides inside the encrypted item payload.

## 13. Trash state

`DeletedUtc` is part of the encrypted record payload. Moving an item to Trash therefore does not create a plaintext “deleted” column for searchable item data.

Retention cleanup uses the decrypted item state during normal vault maintenance and then deletes the encrypted record when expired.

## 14. Recent-use/review metadata

`LastAccessedUtc` and `ReviewAfterUtc` are encrypted payload fields.

Opening an item can update `LastAccessedUtc` without changing the user-visible `ModifiedUtc` timestamp.

## 15. Protected-item flag

`RequiresReauthentication` is stored inside encrypted item JSON. The Item Editor uses it to withhold protected content until current-master re-authentication succeeds.

It is an application authorization policy flag, not a separate cryptographic layer per item. TOTP item code generation follows the same protected-item gate and does not generate/display a code before required re-authentication.

## 16. No plaintext search index

Current local search/filter/audit decrypts authenticated objects while unlocked and operates in memory.

CipherNest intentionally does not maintain a plaintext SQLite FTS/search index for vault titles/usernames/tags/collections/TOTP metadata/etc.

A future encrypted-index redesign would require a separate privacy/security review.

## 17. Compatibility rules

Any change that affects serialized `VaultItem` compatibility must consider:

- JSON serializer behavior/defaults;
- enum numeric compatibility;
- required versus optional members;
- defaults for newly added encrypted JSON fields;
- item validation limits;
- aggregate text accounting;
- encrypted record versioning if framing/AAD changes;
- migration of old records if the new application cannot deserialize them;
- backup/restore compatibility;
- known older releases that must remain readable.

Do not silently reinterpret an older encrypted payload under incompatible semantics. Existing enum numeric values are a compatibility contract unless an explicit migration/version boundary is introduced.

## 18. Tampering/failure behavior

Expected rejection cases include:

- wrong vault DEK;
- modified nonce/tag/ciphertext;
- changed associated row ID;
- unsupported envelope version;
- malformed/null encrypted envelope members;
- payload ID mismatch;
- malformed runtime-null item metadata;
- unsupported item type;
- malformed TOTP seed/settings for a TOTP item;
- resource-limit violations.

These failures must remain fixed/privacy-safe at user-facing surfaces; raw decrypted context, TOTP seed, or generated code should not be logged.

## 19. Managed-memory limitation

After successful deserialization, item strings/objects—including a TOTP seed—exist in managed process memory while the vault is unlocked/using them. Clearing ViewModels/references reduces lifetime but cannot guarantee deterministic erasure of immutable .NET strings or GC/runtime copies.

See `../security/DATA_LIFECYCLE.md` and `../security/TOTP.md`.

## Attachment metadata validation addendum — 2026-08-15

`AttachmentReference` values inside decrypted item JSON are validated before records leave the infrastructure boundary. Display names and media types now reuse `AttachmentImportPolicy` persisted-metadata predicates: outer whitespace/non-leaf display forms, malformed UTF-16, and Unicode Control/Format runes are rejected. Opaque storage names remain bound to their attachment IDs and use the exact 36-character GUID-N `.cna` form.

The encrypted vault-record JSON format itself is unchanged by this policy hardening. Existing correctly normalized metadata remains compatible; newly rejected malformed/invisible-format metadata is treated as invalid decrypted record content.
