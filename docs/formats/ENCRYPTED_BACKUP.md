# CipherNest Encrypted Backup Format

CipherNest encrypted backups are a separate authenticated container used for local transfer/recovery. The format intentionally wraps a consistent SQLite snapshot plus already-encrypted attachment containers instead of exporting every vault field as plaintext.

## 1. File identity

Current extension:

```text
.cnbak
```

Current magic:

```text
CNBK0002
```

Current backup format version:

```text
2
```

Database schema version and vault cryptographic-envelope version are independent from backup format version.

## 2. Backup inputs

A backup archive contains:

```text
vault.db
attachments/<guid-N>.cna  (zero or more)
```

`vault.db` is created through `IVaultStore.CreateConsistentSnapshotAsync`.

Attachment files are already encrypted `.cna` containers. The ZIP stage does not decrypt item records or attachment contents into ordinary plaintext fields.

## 3. Outer container framing

Integers are signed 32-bit big-endian values.

Current framing:

```text
8 bytes   magic = "CNBK0002"
4 bytes   header JSON byte length, big-endian int32
N bytes   UTF-8 JSON BackupHeader

repeat encrypted chunks:
  4 bytes   plaintext archive chunk length, big-endian int32
  12 bytes  AES-GCM nonce
  16 bytes  AES-GCM tag
  M bytes   ciphertext, M = plaintext archive chunk length

4 bytes   end marker = -1
EOF       no trailing unauthenticated bytes
```

Current export chunk size is 1 MiB.

Restore accepts header-declared chunk sizes only within the supported 64 KiB–4 MiB range and enforces a maximum of 65,536 encrypted chunks.

## 4. `BackupHeader`

Current internal header record:

```csharp
BackupHeader(
    int Version,
    byte[] Salt,
    KdfParameters Kdf,
    int ChunkSize,
    DateTimeOffset CreatedUtc)
```

The JSON header is stored before outer payload authentication, so its resource metadata is treated as untrusted until validated.

Restore header-size framing currently requires:

```text
16 <= serialized header bytes <= 16,384
maximum JSON nesting depth = 16
```

For version 2, the root JSON object must contain exactly one each of the case-sensitive `Version`, `Salt`, `Kdf`, `ChunkSize`, and `CreatedUtc` properties. `Kdf` must contain exactly one each of `MemoryKiB`, `Iterations`, and `Parallelism`. Duplicate, unknown, missing, case-variant, or wrong-JSON-type metadata is rejected before typed deserialization/key derivation.

## 5. Header structure and resource validation before Argon2

Before typed deserialization/key derivation, restore validates the declared header length, parses only the bounded header bytes with comments/trailing commas disallowed and a maximum depth of 16, and enforces the exact version-2 root/KDF property sets described above.

After strict JSON structure validation and typed deserialization, restore requires:

```text
Version       exactly 2
Salt          16..64 bytes
ChunkSize     64 KiB..4 MiB
KDF memory    16 MiB..512 MiB
KDF iterations 1..10
KDF parallelism 1..16
```

Missing Salt/Kdf metadata is rejected.

This ordering is security/resource critical: unauthenticated file metadata must not request arbitrary Argon2 memory/CPU work before the application checks supported bounds.

## 6. Backup key derivation

Export generates a fresh random 16-byte salt and uses the current default Argon2id parameters:

```text
Memory      64 MiB
Iterations  3
Parallelism 1
Output      32 bytes
```

The supplied backup passphrase is accepted by the same underlying crypto service passphrase bounds of 12–4,096 characters in the current App workflow.

The derived 32-byte backup key is zeroed when export/restore exits where owned by the service.

## 7. Payload chunk encryption

The ZIP archive is read into filled chunks. Each chunk is AES-256-GCM encrypted using a fresh 12-byte nonce and a 16-byte tag.

The reusable plaintext archive buffer is zeroed after each encrypted chunk and on exit where implemented.

## 8. Chunk associated data

AAD is constructed from:

```text
SHA-256(serialized header JSON)  32 bytes
chunk index                      4-byte big-endian int32
is-final flag                    1 byte (0 or 1)
```

Total AAD length:

```text
37 bytes
```

Conceptually:

```text
AAD = SHA256(HeaderJson) || ChunkIndexBE || FinalFlag
```

This binds each encrypted chunk to the exact serialized header, its position, and the exporter’s final-chunk state.

## 9. Archive resource policy

The unencrypted ZIP staging has explicit resource ceilings:

```text
Maximum entries:          10,001
Maximum aggregate bytes:  1 GiB
```

Entry count is derived from:

```text
1 vault.db + up to 10,000 globally referenced encrypted attachments
```

The same archive resource policy applies during export and restore so the exporter does not intentionally create a normal backup that the same build rejects solely on count/aggregate limits.

## 10. ZIP creation

Current export archive behavior:

- ZIP compression mode: `NoCompression`;
- `vault.db` is added first;
- encrypted attachment directory enumeration is guarded;
- only top-level `.cna` files whose stems parse as GUID format `N` are added;
- attachment source paths are sorted ordinally before archive creation;
- each entry is counted/accounted before addition.

No plaintext vault-field CSV/JSON export occurs inside the encrypted backup creation path.

## 11. Export destination policy

Before creating the final backup, CipherNest canonicalizes the destination and rejects paths that would collide with the active vault storage, including the active database/WAL/SHM/recovery family and encrypted attachment directory.

Encrypted output is first created at a unique sibling staging path using `CreateNew`, then moved to the requested destination.

The active vault database must never be used as a backup output path.

## 12. Restore authentication/decryption

Restore:

1. opens the source read-only;
2. reads/compares `CNBK0002` using fixed-time byte comparison;
3. validates header-length framing;
4. reads the bounded header bytes and validates strict version-2 JSON structure/depth before typed deserialization;
5. deserializes the header and validates version/salt/KDF/chunk resources;
6. derives the backup key;
7. reads each framed encrypted chunk;
8. bounds the chunk index and plaintext length;
9. accounts aggregate archive plaintext size;
10. authenticates/decrypts using header-hash/index/final AAD;
11. writes the ZIP staging archive;
12. zeroes decrypted plaintext chunk arrays;
13. requires the end marker to land exactly at EOF.

Wrong passphrase/tamper authentication is converted to an invalid-backup failure at the service boundary.

Trailing unauthenticated bytes are rejected.

## 13. ZIP restore allowlist

After outer authentication/decryption, ZIP entries are normalized with `/` separators and must match one of:

```text
vault.db
attachments/<guid-N>.cna
```

Attachment path rules:

- exactly one `/` below `attachments`;
- `.cna` extension;
- stem must parse as GUID format `N`;
- no nested/other files.

Normalized duplicate paths are rejected case-insensitively.

The archive must contain `vault.db`.

## 14. Attachment entry bounds

Every `attachments/*.cna` ZIP entry must also fall inside the implemented encrypted attachment container size envelope:

```text
EncryptedAttachmentStore.MinimumContainerBytes
..
EncryptedAttachmentStore.MaximumContainerBytes
```

This prevents a generic 1 GiB archive ceiling from allowing one pathologically large “attachment” that the attachment implementation itself could never accept.

## 15. Staged SQLite validation

Before active replacement, the staged database passes multiple checks.

The backup service first checks the SQLite file signature. The store replacement boundary then validates more deeply before active DB/WAL/SHM mutation, including:

- SQLite `PRAGMA quick_check`;
- exact supported database schema version;
- required table/column shape;
- required vault header;
- bounded vault-header UTF-8 length;
- canonical non-empty stored item IDs;
- maximum item count;
- maximum per-envelope size;
- maximum aggregate encrypted-envelope bytes.

A file that is merely a valid SQLite database is not automatically a valid CipherNest replacement database.

## 16. Pre-restore rollback snapshot

Before replacing the active database, CipherNest creates a consistent rollback snapshot of the current active vault database.

This snapshot is used only if active replacement later fails.

## 17. Database and attachment replacement

Conceptually:

```text
validated staged vault.db
      |
      v
IVaultStore.ReplaceDatabaseAsync
      |
      +--> active DB/WAL/SHM staged to unique recovery set
      +--> candidate installed
      |
      v
current attachments -> unique attachments.previous.<guid>
staged attachments  -> current attachments
```

On success, previous attachment recovery material is best-effort removed.

## 18. Restore failure after active mutation

If database/attachment replacement fails after mutation begins:

- rollback database replacement is attempted with `CancellationToken.None`;
- current partial attachment state is removed best-effort;
- previous attachments are moved back when available;
- secondary rollback failure is contained so the original restore failure remains the primary exception.

The uncancelled rollback token is deliberate. If caller cancellation caused forward restore to fail, reusing that same cancelled token would make rollback immediately cancellable too.

## 19. Biometric relationship

A successful restore is followed by App-level invalidation of local biometric convenience state:

- clear secondary secret from platform secure storage;
- clear remembered master-authentication state;
- disable biometric preference locally;
- require deliberate re-enrollment against the restored vault.

## 20. Temporary working data

Export uses a unique temporary working directory containing:

```text
vault.db snapshot
payload.zip
```

Restore uses a unique temporary working directory containing:

```text
payload.zip
staged/vault.db
staged/attachments/*
rollback.db
```

These contain encrypted vault database/attachment material rather than ordinary plaintext fields, but remain sensitive application recovery artifacts. The service attempts best-effort recursive cleanup in `finally`.

## 21. What encrypted backup protects—and does not

Encrypted backup protects confidentiality/integrity of the outer backup contents under the backup passphrase/KDF/AES-GCM design.

It does not protect against:

- loss/deletion of the backup file;
- loss of the backup passphrase;
- an attacker guessing a weak backup passphrase offline;
- privileged malware reading the passphrase while entered;
- application/runtime implementation bugs;
- an untested restore path;
- unsupported future format changes.

Users should keep more than one appropriately protected backup and periodically test restoration using disposable data.

## 22. Compatibility/versioning rule

Changing any of these can require a new backup format version:

- magic/framing;
- header schema/semantics;
- KDF interpretation;
- chunk/AAD construction;
- nonce/tag sizes;
- end marker/trailing-data semantics;
- ZIP layout/allowlist;
- restore compatibility guarantees.

Do not reinterpret `CNBK0002`/version 2 bytes incompatibly without an explicit migration/backward-compatibility strategy and tests.

## 23. Testing requirements

Backup format changes require coverage for:

- wrong passphrase;
- corrupted/tampered/truncated container;
- invalid magic/version/header size;
- strict version-2 header schema: duplicate/unknown/missing/wrong-type properties and excessive nesting rejected before Argon2;
- deterministic adversarial backup-header corpus with zero key-derivation calls for hostile inputs;
- hostile salt/KDF/chunk metadata rejected before Argon2;
- chunk-count bounds;
- duplicate/unexpected archive paths;
- entry-count/aggregate-size bounds;
- impossible `.cna` entry sizes;
- invalid/headerless/over-budget staged database;
- active-vault preservation after failed restore;
- cancellation during replacement plus uncancelled rollback;
- cross-platform backup compatibility where expected.

See `../TEST_PLAN.md` and `../operations/BACKUP_RECOVERY_RUNBOOK.md`.

## Final ZIP extraction accounting hardening — 2026-08-15

Restore no longer treats `ZipArchiveEntry.Length` as sufficient proof of extraction cost. `BackupArchivePolicy.CopyEntryExactlyAsync(...)` validates the declared entry length against the remaining **1 GiB aggregate archive budget before reading**, then streams through a reusable 128 KiB buffer and independently counts actual decompressed output.

The copy rejects an input chunk before writing it if the chunk would make actual output exceed the declared uncompressed length. End-of-entry is accepted only when the actual copied byte count exactly equals the declared length. A shorter stream is rejected as truncated/inconsistent, and aggregate accounting remains overflow-safe through the shared archive policy.

This closes the specific declared-metadata-versus-actual-output accounting gap while preserving the current encrypted backup format and ZIP path/entry-count/attachment-container policies. It is deterministic resource-bound hardening, not a claim of exhaustive ZIP fuzzing or protection against every runtime/decompressor defect.
