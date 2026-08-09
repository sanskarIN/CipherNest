# Cryptographic Design

## Envelope version 1

1. Generate a 32-byte random vault DEK with `RandomNumberGenerator.Fill`.
2. Generate a 16-byte random Argon2id salt.
3. Derive a 32-byte key-encryption key (KEK) from UTF-8 passphrase bytes using versioned Argon2id parameters.
4. Wrap the DEK with AES-256-GCM using a fresh 12-byte nonce and 16-byte tag. Header fields that define version/KDF parameters are authenticated as associated data.
5. Encrypt every vault record independently with the DEK, a fresh 12-byte nonce, and a 16-byte tag. Record id and envelope version are bound as associated data.
6. Backups use an independent backup salt/KEK and authenticated container.

No nonce is intentionally reused with the same key. `CryptographicOperations.ZeroMemory` is applied to owned sensitive byte buffers where practical. This is defense-in-depth only; managed strings/GC copies cannot be guaranteed erased.

## KDF parameters

The default profile is intentionally versioned in code. Parameters must be benchmarked on supported devices before production release; changes require compatibility tests and an ADR.

## Review requirements

Do not replace primitives, truncate tags, reduce nonce sizes, change associated data, or change KDF parameters without review and known-answer/compatibility tests. The project has not yet completed an independent audit.
