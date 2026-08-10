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

## KDF safety bounds

KDF values can be read from encrypted-container metadata before authentication, so they are treated as untrusted resource requests.

The current implementation accepts:

- memory: 16 MiB through 512 MiB;
- iterations: 1 through 10;
- parallelism: 1 through 16;
- salt length: 16 through 64 bytes.

Values outside these ranges are rejected before Argon2 allocation/work. These are parser/resource bounds, not a statement that every accepted low-end combination is recommended for new vault creation. New wrappers use `DefaultKdf`.

Encrypted backup headers additionally require:

- backup format version `2`;
- salt length 16–64 bytes;
- chunk size 64 KiB–4 MiB;
- the same bounded Argon2 resource ranges above.

`BackupFormatPolicy.ValidateHeader` executes before backup key derivation so malformed/hostile backup metadata cannot request excessive Argon2 work first.

## Vault key hierarchy

1. On vault creation CipherNest generates a random 256-bit DEK.
2. The master passphrase is encoded to a temporary UTF-8 byte buffer and processed with Argon2id using a random salt and versioned KDF parameters to produce a 256-bit key-encryption key (KEK).
3. AES-256-GCM encrypts/wraps the DEK with the KEK.
4. The vault header stores only format/KDF metadata and authenticated wrapped-DEK material—not the master passphrase or plaintext DEK.
5. Successful unlock derives a KEK, authenticates/decrypts the wrapped DEK, and installs an owned 32-byte DEK buffer for the unlocked session.
6. Key-using work obtains a private 32-byte `VaultKeyLease` copy rather than retaining a reference to the mutable shared session array.
7. Every key lease links the caller token with a per-unlock session cancellation token and zeroes its copied DEK on `Dispose`.
8. Locking clears the shared session key, cancels the current per-unlock token, and disposes the session cancellation source. Cancellable database/attachment operations using that session therefore observe cancellation.
9. Replacing the unlocked session with a new unlock cancels the prior session token and zeroes the previous shared DEK before the new session becomes authoritative.

This lease model reduces races where a lock could otherwise zero the same array being used by an in-flight operation. It does not make an unlocked process resistant to privileged memory inspection.

## Session-transition serialization

Vault creation already uses the service transition semaphore. Master/recovery unlock, secondary unlock, public lock, and full-vault deletion also acquire the same transition gate.

This prevents a late-finishing unlock from publishing a fresh session after an already-requested lock merely because KDF/key-unwrapping work completed later. Full-vault deletion additionally acquires a live authorization key lease after current-master re-authentication and waits for the transition gate with that lease token. If another lock/unlock invalidates that session while deletion is waiting, the destructive operation is cancelled instead of proceeding on stale authorization.

The transition semaphore is an application-process coordination control, not a cryptographic primitive and not a substitute for platform process isolation.

## Key-wrap associated data

Master/recovery/secondary wrappers authenticate context derived from:

`CipherNest|VaultKey|v<version>|m=<memory>|t=<iterations>|p=<parallelism>`

This binds the wrapper to the expected CipherNest vault-key purpose, cryptographic format version, and KDF parameters.

## Vault-header compatibility

The current vault-header document version is `2`; version `1` remains the minimum supported historical header. Any version outside the explicit supported range is rejected before key unwrap. This is separate from `CryptoFormatVersion`, which versions the encrypted envelope format itself.

Future header fields must not be assumed backward-compatible merely because JSON deserialization can ignore/accept unknown structure. Header-version support must be deliberately extended and tested.

## Recovery key

When enabled during first-run setup, CipherNest generates a random recovery key and uses it as a separate credential to wrap the same DEK. The recovery value is returned to the UI once during setup and is not stored in plaintext by CipherNest.

Loss of both the master passphrase and all usable recovery material is unrecoverable in the local-only design.

## Biometric secondary wrapper

Supported platforms can generate a separate high-entropy random secondary secret. The operating system secure-storage facility stores that secondary secret; CipherNest does not store the master passphrase for biometric unlock.

The secondary secret independently wraps the same DEK. An OS biometric prompt gates the app's convenience-unlock flow. The design does not claim that every platform secure-storage read is cryptographically hardware-bound to a fresh biometric operation. See `BIOMETRIC_UNLOCK.md`.

Backup restore clears the local secure-storage secondary secret and disables the biometric preference until the restored vault is deliberately enrolled again.

## Record encryption and validation boundary

Each `VaultItem` is normalized and serialized to UTF-8 JSON while unlocked. AES-256-GCM encrypts the serialized bytes under a leased DEK copy with a fresh 96-bit random nonce.

Associated data is the item's GUID bytes. The database stores the GUID plus the opaque encrypted envelope. Title, username, secret, URL, notes, collection, tags, favorites, custom fields, attachment references, review timestamps, trash state, and recent-use timestamp are encrypted inside the payload.

Decryption authenticates the envelope before JSON deserialization. After deserialization CipherNest also requires:

- `VaultItem.Id` to equal the authenticated SQLite row ID used as associated data;
- the item type/identifier/core strings/collections to satisfy null-safe validation;
- tags/custom fields/attachments to satisfy supported count/size constraints;
- attachment identifiers and encrypted storage names to be unique within the item.

Malformed authenticated payload objects therefore do not intentionally pass into search/UI code solely because AES-GCM authentication succeeded. Owned plaintext JSON byte arrays are zeroed in `finally` after deserialization/validation.

## Secure-note bounds

Secure-note storage/import/editor/preview operations share `SafeNoteLimits`:

- maximum 200,000 characters;
- maximum 5,000 lines.

The intent is to prevent one save/import path from persisting a note that the bounded safe renderer rejects solely because another path used looser size limits. Raw HTML remains neutralized rather than executed.

## Attachment encryption and storage metadata

Attachments are stored as separately authenticated encrypted files and processed in bounded chunks rather than loaded as complete files.

For each chunk:

- AES-256-GCM uses a leased vault DEK copy and a fresh nonce;
- associated data includes the item ID, attachment ID, and chunk index;
- the container stores chunk length, nonce, tag, and ciphertext;
- final plaintext length and container structure are checked during decryption;
- truncation, invalid sizes, tampering, or trailing data are rejected.

The reusable plaintext encryption buffer is zeroed after each encrypted chunk and again on exit. Decrypted chunk plaintext arrays are also zeroed after writing to the requested destination. Temporary encryption staging cleanup is best-effort and is structured not to replace the original encryption failure.

The current plaintext attachment size cap is 100 MiB per attachment and the item attachment-count cap is 25. Attachment metadata is validated for supported name/media-type/length/identifier bounds and duplicate IDs/storage names.

Encrypted attachment filesystem access accepts only an opaque GUID `N` stem plus `.cna`; names containing path separators, wrong extensions, or malformed identifiers are rejected before `Path.Combine`/file access.

Permanent item deletion deletes the authenticated database record first, then attempts best-effort encrypted attachment cleanup. This ordering avoids intentionally deleting files first and leaving a surviving record that references missing attachments if database deletion fails.

## Encrypted backups

Backup format magic is `CNBK0002`; backup format version is `2`.

1. CipherNest takes a consistent SQLite snapshot.
2. The snapshot and encrypted attachment containers are packed into a bounded ZIP payload without plaintext vault-record extraction.
3. A separate backup passphrase derives a backup key using Argon2id with a fresh salt and recorded KDF parameters.
4. The archive is encrypted in 1 MiB AES-GCM chunks.
5. Backup chunk associated data includes SHA-256 of the serialized backup header, chunk index, and final-chunk flag.
6. Restore validates magic/header-size framing, backup version/salt/KDF/chunk bounds **before** Argon2, authenticated chunks, total archive size, entry count, and allowed entry paths.
7. The staged `vault.db` must then pass SQLite `PRAGMA quick_check`, exactly match the supported database schema version, and expose the required current table/column shapes before the active database or WAL/SHM sidecars are touched.
8. Replacement uses `.previous`/rollback paths. Candidate validation happens before active-file mutation, and secondary rollback errors are prevented from replacing the original migration/copy failure where implemented.
9. Backup restore clears local biometric pairing after a successful restore because restored wrapper metadata may not correspond to the current device secure-storage secret.

Encrypted backup remains the recommended transfer mechanism. Plaintext CSV/attachment export is an interoperability escape hatch outside this cryptographic boundary.

## Database migration relationship

Database schema versioning is independent from cryptographic-envelope and backup versions. `DatabaseMigrator` records ordered transactional migrations, rejects a future schema version, and validates the required current schema shape after migration/version resolution.

A forged current-version `MigrationHistory` row without required tables/columns is therefore rejected. Migration rollback is best-effort with an uncancelled rollback token; a secondary rollback error is not intended to replace the original migration failure.

## Known-answer test

`tests/CipherNest.UnitTests/CryptoKnownAnswerTests.cs` pins the current Argon2id behavior for:

- passphrase: `CipherNest known answer 2026!`
- salt: bytes `00 01 02 ... 0f`
- memory: 65536 KiB
- iterations: 3
- parallelism: 1
- output length: 32 bytes

Expected lowercase hexadecimal output:

`fcb4490def165d2cd21b4ddc4ed5a7608bf668bc1ca9d3c3421875beea35c60f`

Changing a KDF library/runtime implementation must preserve supported vectors or explicitly version/migrate the format.

## Memory limitations

CipherNest calls `CryptographicOperations.ZeroMemory` on owned sensitive byte buffers where feasible. Current examples include shared session DEKs, leased DEK copies, KDF/passphrase UTF-8 buffers, decrypted/encrypted attachment working plaintext, serialized record plaintext bytes, recovery-key random bytes, clipboard fingerprints/hashing buffers, and other owned crypto intermediates. Password-generator `char[]` and passphrase-selection arrays are cleared after use where practical.

.NET strings, serializer-created objects, JIT/runtime copies, OS buffers, UI controls, and garbage-collected memory cannot be guaranteed to be erased deterministically. Clearing a local string reference or array of string references does not erase the immutable managed string object that may still exist elsewhere.

The design therefore minimizes lifetimes and avoids unnecessary plaintext copies but does not claim memory-forensics resistance on a compromised/unlocked process.

## Nonce assumptions

AES-GCM security requires nonce uniqueness for a given key. CipherNest generates a fresh 96-bit nonce from the cryptographic RNG for each encrypted envelope/chunk. It does not derive nonces from counters persisted across crashes. Random 96-bit nonce collision probability is expected to remain negligible at the supported local-vault scale, but this assumption remains part of the threat/design review.

## Versioning and review

- `CryptoFormatVersion` controls record/key-wrapper encrypted-envelope compatibility.
- Vault-header document versioning is separate and explicitly range-checked.
- Backup versioning is independent because backup framing differs from record envelopes.
- Database schema versioning is independent and handled by transactional migrations plus current-shape validation.
- A cryptographic format change requires design review, known-answer/compatibility tests, tamper tests, migration/restore planning, changelog entries, and an update to this document/threat model before release.
- Changes to the key-lease/session-transition model, backup-header resource policy, or active-database replacement boundary require focused concurrency/security tests even when `CryptoFormatVersion` itself does not change.

## Audit status

This design has **not** completed an independent professional cryptographic/security audit. Do not use the existence of this document, automated tests, configured CI, or open source as evidence that CipherNest is unhackable or suitable for high-risk use.
