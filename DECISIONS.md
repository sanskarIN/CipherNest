# Architecture Decisions

## ADR summary

1. **Local-only release:** no application backend, account, mobile-number identity, or cloud dependency exists in the current release.
2. **Random vault key:** records use a random 256-bit data-encryption key; the master passphrase only wraps that key.
3. **Argon2id:** passphrase-derived wrapping keys use a memory-hard KDF with per-wrapper random salt and versioned parameters.
4. **AES-256-GCM:** authenticated encryption uses maintained .NET cryptography with a fresh 96-bit nonce per encrypted payload/chunk.
5. **Encrypted record payloads:** SQLite stores opaque encrypted envelopes plus non-secret identifiers; searchable item fields remain encrypted at rest.
6. **Managed-memory limitation:** code zeroes owned byte buffers where practical but never claims deterministic erasure of strings or GC copies.
7. **Recovery key is an independent wrapper:** optional recovery material wraps the same random vault key and is shown once; CipherNest does not store a recoverable plaintext copy.
8. **Biometrics use an independent random secondary secret:** the master passphrase is not placed in OS secure storage. The secondary wrapper is optional, platform-gated, periodically yields to master-passphrase authentication, and is invalidated locally after backup restore.
9. **Plaintext export is exceptional:** encrypted backup remains the recommended path. CSV plaintext export requires an exact warning phrase, current master-passphrase confirmation, a second warning dialog, a sensitive share label, and explicit cache-cleanup guidance.
10. **Attachments are chunk-encrypted:** large files are processed in bounded chunks. Small UTF-8 text-family files may be previewed in memory; other formats require explicit plaintext export.
11. **Safe-note rendering is deliberately small:** headings, bullets, checklists, code fences, and paragraphs are supported; raw HTML is not rendered.
12. **Passphrase generation has explicit entropy accounting:** the bundled list contains exactly 256 unique lowercase words, each selected independently with a CSPRNG. The UI reports selection entropy conservatively and warns that edits can reduce it.
13. **Search is in-process while unlocked:** no plaintext FTS index is created. Local search/filter/audit runs over authenticated decrypted objects in memory.
14. **Recent-use metadata stays encrypted:** `LastAccessedUtc` lives inside each encrypted item rather than a plaintext SQL index.
15. **Schema changes are ordered migrations:** migration history is transactional, future unsupported schemas are rejected, and released migration versions are append-only.
16. **Privacy-safe exception reporting:** centralized diagnostics omit exception messages/stacks and never intentionally log vault values, passphrases, keys, recovery material, clipboard content, TOTP seeds/codes, or plaintext attachments.
17. **Reviewed staged localization:** neutral English remains the fallback catalog; explicit English and Hindi preferences are supported for the reviewed resource-backed surface, while untranslated UI is still documented honestly.
18. **TOTP stays inside the existing encrypted-record boundary:** a `OneTimePassword` item stores its Base32 seed and parameters only in the authenticated encrypted record, generates RFC-compatible codes locally on explicit refresh, never persists generated codes, and uses the existing explicit timed clipboard policy when a code is copied. QR enrollment/import and autofill remain separate future designs.
19. **Autofill/cloud sync remain deferred:** each expands the security boundary and requires a dedicated protocol/platform threat review before implementation.
20. **MVVM + DI:** UI depends on application abstractions; infrastructure remains replaceable/testable; platform behavior is isolated where practical.
21. **Security honesty:** unsupported platform controls, external audit status, storage-erasure limits, and release-verification gaps are documented rather than simulated or marketed away.
