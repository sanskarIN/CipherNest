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
CreatedUtc
ModifiedUtc
LastAccessedUtc
ReviewAfterUtc
DeletedUtc
RequiresReauthentication
```

All of these item fields are serialized into encrypted record plaintext. Searchable item content is not copied into a plaintext SQLite search/FTS index.

## 2. Item types

Current enum values:

```text
Login
SecureNote
Identity
PaymentCardReference
WifiCredential
SoftwareLicense
ServerSshReference
Document
Custom
```

Type is part of serialized encrypted item JSON. Unknown runtime enum values are rejected by validation.

## 3. Normalization before save

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

Normalization is not a substitute for validation.

## 4. Validation before serialization

`VaultItemValidator` rejects invalid or resource-hostile item payloads.

Important rules:

- item ID must be a non-empty GUID;
- item type must be defined;
- title is required and at most 256 characters;
- username/identifier at most 2,048 characters;
- secret at most 100,000 characters;
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

## 5. Plaintext serialization budget

The normalized/validated item is serialized to UTF-8 JSON.

Current maximum serialized/decrypted item JSON size:

```text
16 MiB
```

This service-level bound exists even though ordinary item validation should normally reject pathological objects long before that ceiling.

## 6. Record encryption

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

## 7. Record associated data

The item's GUID bytes are supplied as AES-GCM associated data.

Conceptually:

```text
AAD = item.Id bytes
```

This binds the encrypted payload to the authenticated storage row identity. Moving an encrypted envelope to a different item GUID is therefore not intended to produce a valid record.

## 8. SQLite row representation

`VaultItems` stores a structural row identity plus an opaque encrypted envelope.

The persistence contract is represented by:

```csharp
StoredVaultItem(Guid Id, byte[] Envelope)
```

The SQLite store requires the stored ID to use the canonical lower-case GUID `D` string representation and rejects empty/non-canonical identifiers.

Item title, username, secret, URL, notes, collection, tags, favorite state, custom fields, attachment display metadata, review/trash/recent-use timestamps remain inside encrypted payloads.

## 9. Stored envelope resource limits

Current storage resource boundaries:

```text
24 MiB per encrypted envelope
100,000 item rows
256 MiB aggregate encrypted envelopes
```

The SQLite store checks count/aggregate/per-row lengths before materializing large BLOB collections where practical. `VaultService` also enforces compatible service-level bounds so an alternate store cannot intentionally bypass every resource boundary.

## 10. Read/decrypt validation order

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

If authenticated decrypted metadata is malformed, the infrastructure boundary rejects it rather than allowing the object to reach search/UI code.

Owned plaintext JSON byte arrays are zeroed in `finally` after deserialization/validation.

## 11. Attachment references inside items

Attachment files are stored separately, but their logical metadata is part of the encrypted item JSON. An `AttachmentReference` includes the attachment identity/display/media/size/storage information required by the application.

The encrypted storage name is required to be canonically bound to its attachment ID:

```text
<attachment-guid-N>.cna
```

That metadata remains encrypted at rest because it resides inside the encrypted item payload.

## 12. Trash state

`DeletedUtc` is part of the encrypted record payload. Moving an item to Trash therefore does not create a plaintext “deleted” column for searchable item data.

Retention cleanup uses the decrypted item state during normal vault maintenance and then deletes the encrypted record when expired.

## 13. Recent-use/review metadata

`LastAccessedUtc` and `ReviewAfterUtc` are encrypted payload fields.

Opening an item can update `LastAccessedUtc` without changing the user-visible `ModifiedUtc` timestamp.

## 14. Protected-item flag

`RequiresReauthentication` is stored inside encrypted item JSON. The Item Editor uses it to withhold protected content until current-master re-authentication succeeds.

It is an application authorization policy flag, not a separate cryptographic layer per item.

## 15. No plaintext search index

Current local search/filter/audit decrypts authenticated objects while unlocked and operates in memory.

CipherNest intentionally does not maintain a plaintext SQLite FTS/search index for vault titles/usernames/tags/collections/etc.

A future encrypted-index redesign would require a separate privacy/security review.

## 16. Compatibility rules

Any change that affects serialized `VaultItem` compatibility must consider:

- JSON serializer behavior/defaults;
- enum compatibility;
- required versus optional members;
- item validation limits;
- aggregate text accounting;
- encrypted record versioning if framing/AAD changes;
- migration of old records if the new application cannot deserialize them;
- backup/restore compatibility;
- known older releases that must remain readable.

Do not silently reinterpret an older encrypted payload under incompatible semantics.

## 17. Tampering/failure behavior

Expected rejection cases include:

- wrong vault DEK;
- modified nonce/tag/ciphertext;
- changed associated row ID;
- unsupported envelope version;
- malformed/null encrypted envelope members;
- payload ID mismatch;
- malformed runtime-null item metadata;
- unsupported item type;
- resource-limit violations.

These failures must remain fixed/privacy-safe at user-facing surfaces; raw decrypted context should not be logged.

## 18. Managed-memory limitation

After successful deserialization, item strings/objects exist in managed process memory while the vault is unlocked/using them. Clearing ViewModels/references reduces lifetime but cannot guarantee deterministic erasure of immutable .NET strings or GC/runtime copies.

See `../security/DATA_LIFECYCLE.md`.
