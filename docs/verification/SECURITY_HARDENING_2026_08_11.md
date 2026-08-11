# CipherNest security hardening gates — 2026-08-11

This document records additional source and execution gates introduced during the persistence, framing, session, transfer, and platform-boundary hardening continuation. It complements `CI_GATES.md`, `TEST_PLAN.md`, and `RELEASE_CHECKLIST.md`.

## Encrypted backup framing

A release candidate must preserve all of the following:

- Backup format version, salt length, Argon2id resource parameters, and chunk size are validated before key derivation.
- Encrypted backup streams have an explicit maximum encrypted-chunk count in addition to the aggregate decrypted archive byte budget.
- Export reads fill each normal chunk before encryption unless EOF is reached, avoiding accidental fragmentation caused by short stream reads.
- The reusable plaintext export buffer is zeroed after every encrypted chunk, including when encryption or output writing throws.
- Restore rejects trailing unauthenticated bytes, malformed chunk lengths, excessive chunk counts, duplicate archive paths, unexpected archive paths, excessive entry counts, excessive aggregate bytes, and encrypted attachment containers outside supported size bounds.
- Restore continues to validate the staged SQLite database and exact supported schema before active database replacement.

## Encrypted attachment framing and identity

- Encrypted attachment streams have a bounded chunk count in addition to their 100 MiB plaintext limit.
- Attachment export/import framing fills chunks where practical and zeroes owned plaintext buffers.
- Opaque attachment filenames must be canonical non-empty GUID `N` identifiers ending in `.cna`.
- An attachment's opaque filename must correspond to that attachment's own identifier before encryption/decryption filesystem access.
- `VaultItemValidator` enforces the same storage-identity relationship before decrypted item metadata leaves the infrastructure boundary.
- Attachment stream, identifier, destination/source capability, and 32-byte data-key preconditions remain enforced at the attachment-store boundary.

## CSV import resource bounds

- Input streams must be readable; export streams must be writable.
- Per-field, per-row, column-count, and logical-row-count limits are all enforced.
- The logical row ceiling is checked before parsing another data row so a row beyond the supported limit is not materialized first.
- Import validation warnings are generated from controlled item-validation output. Unexpected save-time exceptions are not rendered verbatim.

## Passphrase and envelope bounds

- Crypto-bound passphrases/recovery material are accepted only within the supported 12–4,096-character range.
- Invalid-length unwrap attempts are mapped to `VaultAuthenticationException` rather than escaping the normal authentication-failure path as argument errors.
- Malformed/null encrypted-envelope members and malformed wrapped-key members fail safely.
- Wrapped vault-key ciphertext remains exactly one 32-byte DEK.

## Vault header and search boundaries

- Vault header UTF-8 size is bounded before deserialization.
- Malformed header JSON and unsupported header versions map to vault authentication failure rather than leaking JSON parser exceptions through unlock flows.
- Search input is bounded to 4,096 trimmed characters before field matching across decrypted local items.

## Session and destructive transitions

- Master/recovery unlock, secondary unlock, public lock, and destructive vault deletion remain serialized through the transition gate.
- Security-sensitive mutations that require current-master authentication retain a key lease from the authenticated session while waiting for the transition gate; an intervening session transition cancels that authorization.
- Vault key leases zero their private key copy both on normal disposal and if linked cancellation-token construction fails.
- Once full-vault deletion clears the active key, caller cancellation no longer interrupts managed deletion work.
- Full-vault deletion attempts both the managed SQLite/recovery file set and the encrypted attachment root even if one side reports an I/O/access failure, then reports incomplete deletion generically.
- SQLite database deletion attempts DB, WAL, SHM, legacy recovery, and generated recovery artifacts before reporting aggregate cleanup failure.

## Migration-history validation

- Migration history must be positive, contiguous, timestamp-parseable, and within the supported schema range.
- Validation work is explicitly bounded to the supported schema range plus one sentinel row; hostile extreme integer metadata must fail without integer overflow.
- Forged current-version history still must not bypass required schema-shape validation.

## Platform/UI exception boundaries

- Startup preference restoration is fire-and-forget only because the task internally catches its primary failure and separately contains theme, localization, and accessibility fallback failures.
- CSV picker, import confirmation, plaintext-export re-authentication, plaintext-export confirmation, share flow, and staging cleanup use fixed UI text plus privacy-safe diagnostics.
- Plaintext CSV staging is deleted in `finally` after share/failure where the OS permits it; failure to confirm deletion is reported and surfaced without revealing its path.
- Item attachment picker, decrypted export/share, attachment removal, move-to-trash confirmation, copy-secret, and per-item re-authentication paths use privacy-safe failure reporting.
- Settings load/save, cache confirmation/cleanup, biometric enable/disable, backup export/share, restore picker/confirmation/staging, passphrase change, and vault-delete confirmation/cleanup use privacy-safe failure reporting.
- No sensitive path in these flows should render raw exception messages, stacks, filesystem paths, passphrases, keys, or decrypted values.

## Required automated checks

Before release, run the committed core verification script for the host platform and ensure the unit, integration, and source-structure test projects execute successfully. In particular, the candidate must include and pass coverage for:

- backup chunk-count and plaintext-buffer zeroing;
- attachment chunk-count and opaque-name/attachment-ID binding;
- CSV aggregate-row and logical-row limits;
- invalid-length unwrap authentication failures;
- null/malformed crypto envelopes;
- malformed vault header JSON;
- maximum search query length;
- migration extreme-version metadata;
- complete SQLite deletion-attempt ordering;
- key-lease constructor-failure zeroing;
- startup fallback containment;
- transfer/settings/item-editor privacy-safe platform failure surfaces.

Source assertions are regression guards, not runtime proof. Real Windows/Android/iOS/Mac Catalyst builds, target-device lifecycle/biometric/clipboard/screenshot behavior, file picker/share behavior, package signing, store review, dependency review, CodeQL, and independent security review remain separate release gates.
