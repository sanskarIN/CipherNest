# Unreleased hardening — 2026-08-11

This release-note supplement records source changes completed after the prior `CHANGELOG.md` hardening entry. It does not claim that the current candidate has passed external CI, platform builds, device tests, signing, store review, or independent security audit.

## Security and resource-bound changes

- Encrypted backup framing now has an explicit 65,536-chunk ceiling in addition to the 1 GiB aggregate archive resource budget.
- Backup export fills normal 1 MiB chunks before encryption unless EOF is reached, reducing accidental framing fragmentation from short stream reads.
- The reusable backup plaintext export span is zeroed in `finally` after every encryption/write attempt.
- Encrypted attachment framing has an explicit chunk-count ceiling derived from the 100 MiB plaintext and 256 KiB chunk envelope.
- Attachment encryption fills normal chunks before encryption unless EOF is reached and retains plaintext-buffer zeroing.
- Attachment storage names must be canonical non-empty GUID `.cna` names and must correspond to the actual attachment ID before filesystem access.
- `VaultItemValidator` enforces the attachment-ID/storage-name relationship before decrypted item metadata reaches application/UI code.
- CSV parsing now bounds aggregate characters per row, checks the logical-row ceiling before materializing another data row, retains the 256-column and per-field limits, and uses controlled validator-derived skip warnings.
- Crypto-bound master/recovery/secondary/backup credentials are limited to 12–4,096 characters before KDF work.
- Invalid-length unwrap attempts are normalized to `VaultAuthenticationException` so interactive unlock follows its normal authentication-failure path.
- Malformed/null encrypted-envelope and wrapped-key members fail safely; wrapped DEK ciphertext must be exactly 32 bytes.
- Vault-header malformed JSON is normalized to vault authentication failure rather than leaking JSON parser behavior through unlock.
- Local search rejects trimmed queries longer than 4,096 characters before matching decrypted fields.
- Migration history validation is positive, contiguous, timestamp-validated, bounded to the supported schema range plus one sentinel row, and rejects extreme integer metadata safely.
- Full SQLite deletion attempts the managed DB/WAL/SHM/legacy/generated recovery file set before reporting aggregate cleanup failure.
- Full-vault deletion attempts both managed SQLite/recovery cleanup and encrypted-attachment-root cleanup after the destructive session transition, even if one side reports an I/O/access failure.
- Master-authenticated security mutations retain authorization from the same unlock session while waiting for transition serialization; a lock/re-unlock invalidates stale authorization.
- `VaultKeyLease` zeroes its private 32-byte key copy if linked cancellation-token construction fails as well as on normal disposal.

## Platform/UI failure containment

- Startup preference restoration now separately contains and privacy-safe reports fallback theme, localization, and accessibility failures after a primary startup preference failure.
- Transfer CSV picker, import confirmation, plaintext-export re-authentication, plaintext-export confirmation/share, and staging cleanup now use fixed user-facing text plus privacy-safe operation IDs.
- Plaintext CSV export staging uses a collision-resistant filename and is deleted in `finally` after the share request or failure where permitted; cleanup failure remains visible without exposing the path.
- Item Editor now contains/report per-item re-authentication, secret-copy, attachment picker/export/share/removal, and move-to-trash command failures. The bound re-authentication passphrase is cleared before service work.
- Settings now contains/reports load/save, storage/cache, biometric enable/disable, encrypted-backup export/share, restore picker/confirmation/staging, master-passphrase transition, and destructive-delete confirmation/cleanup failures.
- Backup/master input surfaces enforce the 4,096-character upper credential bound before expensive work.
- Onboarding enforces the 12–4,096 master-passphrase range before generator/vault work and no longer surfaces raw argument exception text.

## Regression coverage

Added or expanded coverage for:

- backup chunk-count policy, full-chunk reads, and plaintext-buffer zeroing;
- attachment chunk-count policy, opaque-name validation, ID/name binding, and storage preconditions;
- CSV aggregate-row/logical-row bounds and parser source invariants;
- crypto passphrase bounds and malformed/null envelope members;
- malformed vault-header JSON and bounded local search input;
- migration-history extreme metadata and bounded validation work;
- full managed database deletion attempts and full-vault database/attachment cleanup attempts;
- same-session security mutation authorization and key-lease constructor failure zeroing;
- startup preference fallback containment;
- transfer, Settings, Item Editor, and onboarding privacy-safe/platform-boundary behavior.

## Verification records

See:

- `docs/verification/CI_GATES.md`
- `docs/verification/SECURITY_HARDENING_2026_08_11.md`
- `docs/TEST_PLAN.md`
- `docs/RELEASE_CHECKLIST.md`
- `docs/security/THREAT_MODEL.md`
- `docs/security/CRYPTOGRAPHIC_DESIGN.md`

The current source still requires execution of the committed .NET/MAUI verification scripts and GitHub workflows on the exact candidate. Physical-device biometric, clipboard, screenshot, lifecycle, file-picker/share, secure-storage, accessibility, package signing, store-policy, and independent professional security-review gates remain external.
