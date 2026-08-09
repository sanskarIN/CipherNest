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

## Primitives

Current cryptographic format version: `1`.

- Randomness: `System.Security.Cryptography.RandomNumberGenerator`.
- Password-based KDF: Argon2id through `Konscious.Security.Cryptography.Argon2`.
- Authenticated encryption: AES-256-GCM through `System.Security.Cryptography.AesGcm`.
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

## Vault key hierarchy

1. On vault creation CipherNest generates a random 256-bit DEK.
2. The master passphrase is encoded to a temporary UTF-8 byte buffer and processed with Argon2id using a random salt and versioned KDF parameters to produce a 256-bit key-encryption key (KEK).
3. AES-256-GCM encrypts/wraps the DEK with the KEK.
4. The vault header stores only format/KDF metadata and authenticated wrapped-DEK material—not the master passphrase or plaintext DEK.
5. Successful unlock derives a KEK, authenticates/decrypts the wrapped DEK, and keeps an owned DEK byte buffer only for the unlocked session.
6. Lock zeroes that owned byte buffer where practical and drops the active reference.

## Key-wrap associated data

Master/recovery/secondary wrappers authenticate context derived from:

`CipherNest|VaultKey|v<version>|m=<memory>|t=<iterations>|p=<parallelism>`

This binds the wrapper to the expected CipherNest vault-key purpose, cryptographic format version, and KDF parameters.

## Recovery key

When enabled during first-run setup, CipherNest generates a random recovery key and uses it as a separate credential to wrap the same DEK. The recovery value is returned to the UI once during setup and is not stored in plaintext by CipherNest.

Loss of both the master passphrase and all usable recovery material is unrecoverable in the local-only design.

## Biometric secondary wrapper

Supported platforms can generate a separate high-entropy random secondary secret. The operating system secure-storage facility stores that secondary secret; CipherNest does not store the master passphrase for biometric unlock.

The secondary secret independently wraps the same DEK. An OS biometric prompt gates the app's convenience-unlock flow. The design does not claim that every platform secure-storage read is cryptographically hardware-bound to a fresh biometric operation. See `BIOMETRIC_UNLOCK.md`.

Backup restore clears the local secure-storage secondary secret and disables the biometric preference until the restored vault is deliberately enrolled again.

## Record encryption

Each `VaultItem` is normalized and serialized to UTF-8 JSON while unlocked. AES-256-GCM encrypts the serialized bytes under the DEK with a fresh 96-bit random nonce.

Associated data is the item's GUID bytes. The database stores the GUID plus the opaque encrypted envelope. Title, username, secret, URL, notes, collection, tags, favorites, custom fields, attachment references, review timestamps, trash state, and recent-use timestamp are encrypted inside the payload.

Decryption authenticates the envelope before JSON deserialization. Owned plaintext byte arrays are zeroed after deserialization where practical.

## Attachment encryption

Attachments are stored as separately authenticated encrypted files and processed in bounded chunks rather than loaded as complete files.

For each chunk:

- AES-256-GCM uses the vault DEK and a fresh nonce;
- associated data includes the item ID, attachment ID, and chunk index;
- the container stores chunk length, nonce, tag, and ciphertext;
- final plaintext length and container structure are checked during decryption;
- truncation, invalid sizes, tampering, or trailing data are rejected.

The current plaintext attachment size cap is 100 MiB per attachment and the item attachment-count cap is 25.

## Encrypted backups

Backup format magic is `CNBK0002`; backup format version is `2`.

1. CipherNest takes a consistent SQLite snapshot.
2. The snapshot and encrypted attachment containers are packed into a bounded ZIP payload without plaintext vault-record extraction.
3. A separate backup passphrase derives a backup key using Argon2id with a fresh salt and recorded KDF parameters.
4. The archive is encrypted in 1 MiB AES-GCM chunks.
5. Backup chunk associated data includes SHA-256 of the serialized backup header, chunk index, and final-chunk flag.
6. Restore validates magic/header bounds, KDF resource bounds, authenticated chunks, archive size/count/path rules, and SQLite signature before replacing active data.
7. Replacement uses staging/rollback paths so authentication/parser failure does not intentionally replace the active vault.

Encrypted backup remains the recommended transfer mechanism. Plaintext CSV/attachment export is an interoperability escape hatch outside this cryptographic boundary.

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

CipherNest calls `CryptographicOperations.ZeroMemory` on owned sensitive byte buffers where feasible. .NET strings, serializer-created objects, JIT/runtime copies, OS buffers, UI controls, and garbage-collected memory cannot be guaranteed to be erased deterministically.

The design therefore minimizes lifetimes and avoids unnecessary plaintext copies but does not claim memory-forensics resistance on a compromised/unlocked process.

## Nonce assumptions

AES-GCM security requires nonce uniqueness for a given key. CipherNest generates a fresh 96-bit nonce from the cryptographic RNG for each encrypted envelope/chunk. It does not derive nonces from counters persisted across crashes. Random 96-bit nonce collision probability is expected to remain negligible at the supported local-vault scale, but this assumption remains part of the threat/design review.

## Versioning and review

- `CryptoFormatVersion` controls record/key-wrapper encrypted-envelope compatibility.
- Backup versioning is independent because backup framing differs from record envelopes.
- Database schema versioning is independent and handled by transactional migrations.
- A cryptographic format change requires design review, known-answer/compatibility tests, tamper tests, migration/restore planning, changelog entries, and an update to this document/threat model before release.

## Audit status

This design has **not** completed an independent professional cryptographic/security audit. Do not use the existence of this document, automated tests, or open source as evidence that CipherNest is unhackable or suitable for high-risk use.
