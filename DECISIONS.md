# Architecture Decisions

## ADR summary

1. **Local-only release:** no application backend or account dependency.
2. **Random vault key:** records use a random 256-bit data-encryption key; the master passphrase only wraps that key.
3. **Argon2id:** the wrapping key uses a memory-hard KDF with per-vault random salt and versioned parameters.
4. **AES-256-GCM:** authenticated encryption uses platform .NET cryptography with a fresh 96-bit nonce per payload.
5. **Encrypted record payloads:** SQLite stores opaque encrypted envelopes plus non-secret identifiers; searchable fields remain encrypted at rest.
6. **Managed-memory limitation:** code zeroes byte buffers where practical but does not claim deterministic erasure of strings/GC copies.
7. **No plaintext export in 0.1:** encrypted backup is available; plaintext export is withheld until UX and cleanup safeguards receive focused review.
8. **No TOTP/autofill in 0.1:** both expand the security boundary and are deferred to a reviewed release.
9. **MVVM + DI:** UI depends on application abstractions; infrastructure is replaceable and testable.
10. **Security honesty:** unsupported platform controls are documented rather than simulated.
