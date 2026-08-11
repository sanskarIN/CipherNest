# CipherNest Cryptographic Design

This document describes the cryptographic design implemented by the current local-only CipherNest source. It is a design record, not a claim of independent audit.

## Goals

- Never store the master passphrase.
- Separate passphrase changes from bulk record re-encryption by protecting a random vault data-encryption key (DEK).
- Authenticate encrypted records, wrapped keys, attachment chunks, and backup chunks.
- Keep searchable item metadata encrypted at rest where practical.
- Version every cryptographic envelope that must survive upgrades.
- Reject malformed/tampered inputs before they replace active vault data.
- Bound untrusted KDF/resource parameters before expensive work.
- Bound passphrase inputs, vault-header, serialized-record, stored-envelope, archive-entry/chunk, and aggregate storage resources before large allocations where practical.
- Cancel cancellable key-using work when the unlocked security session ends.
- Minimize lifetime of owned sensitive byte/character buffers and zero them where practical.

## Primitives

Current cryptographic format version: `1`.

- Randomness: `System.Security.Cryptography.RandomNumberGenerator`.
- Password-based KDF: Argon2id through `Konscious.Security.Cryptography.Argon2`.
- Authenticated encryption: AES-256-GCM through `System.Security.Cryptography.AesGcm`.
- SHA-256: used for non-secret structural/fingerprint purposes such as backup-header binding and clipboard comparison state.
- AES key size: 32 bytes.
- AES-GCM nonce size: 12 bytes.
- AES-GCM authentication tag size: 16 bytes.
- Default Argon2id parameters: 64 MiB memory, 3 iterations, parallelism 1, 16-byte random salt, 32-byte output.

CipherNest does not implement a custom cipher, MAC, password hash, or PRNG.

## Passphrase and KDF safety bounds

Passphrase/recovery/secondary/backup inputs passed to the cryptographic boundary must contain 12 through 4,096 characters. New setup/change UI surfaces enforce the same maximum before expensive strength/KDF work. Unwrap attempts outside that range are mapped to `VaultAuthenticationException` so interactive authentication follows the normal failure path rather than exposing an argument-validation distinction.

KDF values can be read from encrypted-container metadata before authentication, so they are treated as untrusted resource requests. The current implementation accepts:

- memory: 16 MiB through 512 MiB;
- iterations: 1 through 10;
- parallelism: 1 through 16;
- salt length: 16 through 64 bytes.

Values outside these ranges are rejected before Argon2 allocation/work. These are parser/resource bounds, not a statement that every accepted low-end combination is recommended for new vault creation. New wrappers use `DefaultKdf`.

Encrypted backup headers additionally require backup format version `2`, salt length 16–64 bytes, chunk size 64 KiB–4 MiB, and the same bounded Argon2 resource ranges. `BackupFormatPolicy.ValidateHeader` executes before backup key derivation.

## Vault key hierarchy

1. On vault creation CipherNest generates a random 256-bit DEK.
2. The master passphrase is encoded to a temporary UTF-8 byte buffer and processed with Argon2id using a random salt and versioned KDF parameters to produce a 256-bit key-encryption key (KEK).
3. AES-256-GCM encrypts/wraps the DEK with the KEK.
4. The vault header stores only format/KDF metadata and authenticated wrapped-DEK material—not the master passphrase or plaintext DEK.
5. Successful unlock derives a KEK, authenticates/decrypts the wrapped DEK, and installs an owned 32-byte DEK buffer for the unlocked session.
6. Key-using work obtains a private 32-byte `VaultKeyLease` copy rather than retaining a reference to the mutable shared session array.
7. Every key lease links the caller token with a per-unlock session cancellation token and zeroes its copied DEK on `Dispose`; if linked-token-source construction fails after accepting the key copy, that copy is zeroed before rethrow.
8. Locking clears the shared session key, cancels the current per-unlock token, and disposes the session cancellation source.
9. Replacing the unlocked session with a new unlock cancels the prior session token and zeroes the previous shared DEK before the new session becomes authoritative.

This lease model reduces races where a lock could otherwise zero the same array being used by an in-flight operation. It does not make an unlocked process resistant to privileged memory inspection.

## Session-transition serialization

Vault creation, master/recovery unlock, secondary unlock, public lock, and full-vault deletion acquire the same service transition gate. This prevents a late-finishing unlock from publishing a fresh session after an already-requested lock merely because KDF/key-unwrapping work completed later.

Security-sensitive master-authenticated mutations retain a live authorization key lease from the authenticated session while waiting for that gate. If another lock/unlock invalidates the session, the old authorization token is cancelled rather than being carried into the replacement session.

Full-vault deletion clears the active session before destructive disk I/O. After that commit point, caller cancellation does not interrupt managed database/attachment cleanup. Database and encrypted-attachment cleanup are attempted independently, with incomplete cleanup reported only after both sides have been attempted.

## Key-wrap associated data

Master/recovery/secondary wrappers authenticate context derived from:

`CipherNest|VaultKey|v<version>|m=<memory>|t=<iterations>|p=<parallelism>`

This binds the wrapper to the expected CipherNest vault-key purpose, cryptographic format version, and KDF parameters.

Wrapped-key parsing also requires non-null salt/KDF/nonce/ciphertext/tag members, the expected nonce/tag lengths, and exactly 32 bytes of wrapped DEK ciphertext.

## Vault-header compatibility and resource bound

The current vault-header document version is `2`; version `1` remains the minimum supported historical header. Any version outside the explicit supported range is rejected before key unwrap.

Vault-header JSON is also bounded to 64 KiB UTF-8. The SQLite store checks byte length before materializing header text, and `VaultService` applies the same bound before deserialization for alternate store implementations. Malformed JSON is mapped to vault authentication failure at the service boundary. Future header expansion must fit that budget or deliberately version/review it.

## Recovery key

When enabled during first-run setup, CipherNest generates a random recovery key and uses it as a separate credential to wrap the same DEK. The recovery value is returned to the UI once during setup and is not stored in plaintext by CipherNest.

Loss of both the master passphrase and all usable recovery material is unrecoverable in the local-only design.

## Biometric secondary wrapper

Supported platforms can generate a separate high-entropy random secondary secret. The operating system secure-storage facility stores that secondary secret; CipherNest does not store the master passphrase for biometric unlock.

The secondary secret independently wraps the same DEK. An OS biometric prompt gates the app's convenience-unlock flow. The design does not claim that every platform secure-storage read is cryptographically hardware-bound to a fresh biometric operation. See `BIOMETRIC_UNLOCK.md`.

Backup restore clears the local secure-storage secondary secret and disables the biometric preference until the restored vault is deliberately enrolled again.

## Record encryption and validation boundary

Each `VaultItem` is normalized and serialized to UTF-8 JSON while unlocked. AES-256-GCM encrypts the serialized bytes under a leased DEK copy with a fresh 96-bit random nonce.

Associated data is the item's GUID bytes. The database stores the canonical lower-case GUID `D` string plus the opaque encrypted envelope. Title, username, secret, URL, notes, collection, tags, favorites, custom fields, attachment references, review timestamps, trash state, and recent-use timestamp are encrypted inside the payload.

Current resource budgets are:

- combined item text before serialization: 2,000,000 characters;
- serialized/decrypted item JSON: 16 MiB;
- stored encrypted envelope: 24 MiB per row;
- stored item count: 100,000;
- aggregate stored encrypted-envelope bytes: 256 MiB;
- local search query: 4,096 trimmed characters.

The SQLite store checks count/sum and per-row `length(Envelope)` before BLOB materialization where possible, and enforces matching bounds on writes. `VaultService` independently applies the serialized/decrypted and stored-envelope limits so a custom `IVaultStore` cannot bypass them.

After decryption CipherNest additionally requires payload ID equality with the authenticated SQLite row ID, valid item type/core metadata, supported tags/custom fields/attachments, unique attachment identifiers/storage names, and attachment storage names bound to their actual attachment IDs. Owned plaintext JSON byte arrays are zeroed in `finally` after deserialization/validation.

## Secure-note bounds

Secure-note storage/import/editor/preview operations share `SafeNoteLimits`: maximum 200,000 characters and maximum 5,000 lines. Raw HTML remains neutralized rather than executed.

## Attachment encryption and storage metadata

Attachments are stored as separately authenticated encrypted files and processed in bounded chunks rather than loaded as complete files.

For each chunk AES-256-GCM uses a leased vault DEK copy and fresh nonce; associated data includes item ID, attachment ID, and chunk index; the container stores chunk length/nonce/tag/ciphertext; final plaintext length and structure are checked; truncation, invalid sizes, tampering, trailing data, or excessive chunk counts are rejected.

Normal encryption reads fill a 256 KiB chunk before encryption unless EOF is reached. The reusable plaintext encryption buffer is zeroed after each encrypted chunk and again on exit. Decrypted chunk plaintext arrays are also zeroed after writing. Encrypted attachment staging uses a unique sibling `CreateNew` file and final installation refuses overwrite; a generated-ID collision therefore fails closed instead of replacing an existing encrypted attachment.

The current plaintext attachment size cap is 100 MiB per attachment and the item attachment-count cap is 25. Attachment framing has an explicit maximum chunk count derived from the supported plaintext/chunk-size envelope. The encrypted container exposes a minimum/maximum size envelope derived from this format so backup extraction can reject impossible attachment-entry sizes before staging them.

Encrypted attachment filesystem access accepts only the canonical opaque GUID `N` stem plus `.cna` corresponding to the attachment's actual non-empty identifier. Names containing path separators, wrong extensions, malformed/empty identifiers, or mismatched identifiers are rejected before file access.

Permanent item deletion deletes the authenticated database record first, then attempts best-effort encrypted attachment cleanup.

## Encrypted backups

Backup format magic is `CNBK0002`; backup format version is `2`.

1. CipherNest takes a consistent SQLite snapshot.
2. The snapshot and encrypted attachment containers are packed into a bounded ZIP payload without plaintext vault-record extraction.
3. A separate backup passphrase derives a backup key using Argon2id with a fresh salt and recorded KDF parameters.
4. The archive is encrypted in nominal 1 MiB AES-GCM chunks; normal reads fill a chunk unless EOF is reached.
5. Backup chunk associated data includes SHA-256 of the serialized backup header, chunk index, and final-chunk flag.
6. Backup framing rejects more than 65,536 encrypted chunks in addition to the aggregate archive-byte budget.
7. The reusable plaintext archive buffer span is zeroed in `finally` after each chunk encryption/write attempt.
8. Export canonicalizes the requested destination and rejects the active database, WAL/SHM/recovery files, and encrypted attachment directory. Encrypted staging uses a unique sibling `CreateNew` file.
9. Restore validates magic/header-size framing and backup version/salt/KDF/chunk bounds before Argon2, authenticates encrypted chunks, enforces the chunk-count ceiling, bounds total archive size/entry count/paths, rejects duplicate normalized ZIP paths, and rejects attachment entries outside the implemented encrypted-container size envelope.
10. The staged `vault.db` must pass SQLite `PRAGMA quick_check`, exact database schema version, valid migration history, required table/column shape, required bounded vault header, canonical item IDs, item count, per-record envelope size, and aggregate envelope size before active DB/WAL/SHM mutation.
11. Active SQLite DB/WAL/SHM files are staged into unique recovery names. Component-aware rollback restores only components that actually moved.
12. Encrypted-backup rollback uses an uncancelled recovery token once active-state mutation begins, so caller cancellation cannot cancel the rollback database replacement. Secondary rollback errors remain best-effort and do not intentionally replace the original restore failure.
13. Backup restore clears local biometric pairing after a successful restore because restored wrapper metadata may not correspond to the current device secure-storage secret.

Encrypted backup remains the recommended transfer mechanism. Plaintext CSV/attachment export is an interoperability escape hatch outside this cryptographic boundary.

## Database migration relationship

Database schema versioning is independent from cryptographic-envelope and backup versions. `DatabaseMigrator` records ordered transactional migrations, rejects unsupported version metadata, and validates the required current schema shape after migration/version resolution.

Migration history must contain positive contiguous version rows with parseable round-trip timestamps. Validation work is bounded to the supported schema range plus one sentinel row, and extreme integer metadata is rejected before narrowing to an application version. A forged current-version history row without required tables/columns is still rejected. Migration rollback is best-effort with an uncancelled rollback token; a secondary rollback error is not intended to replace the original migration failure.

## Known-answer test

`tests/CipherNest.UnitTests/CryptoKnownAnswerTests.cs` pins the current Argon2id behavior for passphrase `CipherNest known answer 2026!`, salt bytes `00 01 02 ... 0f`, memory 65536 KiB, 3 iterations, parallelism 1, and 32-byte output.

Expected lowercase hexadecimal output:

`fcb4490def165d2cd21b4ddc4ed5a7608bf668bc1ca9d3c3421875beea35c60f`

Changing a KDF library/runtime implementation must preserve supported vectors or explicitly version/migrate the format.

## Memory limitations

CipherNest calls `CryptographicOperations.ZeroMemory` on owned sensitive byte buffers where feasible. Current examples include shared session DEKs, leased DEK copies including constructor-failure cleanup, KDF/passphrase UTF-8 buffers, backup export plaintext chunk spans, decrypted/encrypted attachment working plaintext, serialized record plaintext bytes, recovery-key random bytes, clipboard fingerprints/hashing buffers, and other owned crypto intermediates. Password-generator `char[]` and passphrase-selection arrays are cleared after use where practical.

.NET strings, serializer-created objects, JIT/runtime copies, OS buffers, UI controls, and garbage-collected memory cannot be guaranteed to be erased deterministically. Clearing a local string reference or array of string references does not erase the immutable managed string object that may still exist elsewhere.

## Nonce assumptions

AES-GCM security requires nonce uniqueness for a given key. CipherNest generates a fresh 96-bit nonce from the cryptographic RNG for each encrypted envelope/chunk. Random 96-bit nonce collision probability is expected to remain negligible at the supported local-vault scale, but this assumption remains part of the threat/design review.

## Versioning and review

- `CryptoFormatVersion` controls record/key-wrapper encrypted-envelope compatibility.
- Vault-header document versioning is separate and explicitly range/size/JSON checked.
- Backup versioning is independent because backup framing differs from record envelopes.
- Database schema versioning is independent and handled by transactional migrations plus current-shape/resource validation.
- A cryptographic format change requires design review, known-answer/compatibility tests, tamper tests, migration/restore planning, changelog entries, and an update to this document/threat model before release.
- Changes to key-lease/session transitions, storage budgets, backup-header/archive/framing policy, attachment framing/storage identity, or active-database replacement/recovery require focused concurrency/security tests even when `CryptoFormatVersion` itself does not change.

## Audit status

This design has **not** completed an independent professional cryptographic/security audit. Do not use the existence of this document, automated tests, configured CI, or open source as evidence that CipherNest is unhackable or suitable for high-risk use.
