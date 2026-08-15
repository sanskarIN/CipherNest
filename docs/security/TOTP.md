# Time-Based One-Time Passwords (TOTP)

CipherNest supports local generation of RFC 6238-style time-based one-time passwords for vault items whose type is `OneTimePassword`.

This feature is intentionally local-first. CipherNest does not contact an authentication provider to generate a code and does not upload the seed. **Generated one-time codes are not persisted.** The Base32 seed is stored as the item's encrypted `Secret` field inside the existing authenticated vault-record envelope.

## Security status

CipherNest has **not** completed an independent professional security audit. TOTP support does not change that status and must not be described as independently audited or as protection against every account-compromise scenario.

A TOTP seed is equivalent to a long-lived authentication secret. Anyone who obtains the seed can normally generate future valid codes until the service-side TOTP enrollment is replaced. Protect the seed at least as carefully as a password or recovery secret.

## Supported parameters

The current implementation supports:

- HMAC-SHA-1;
- HMAC-SHA-256;
- HMAC-SHA-512;
- 6-digit or 8-digit codes;
- periods from 15 through 120 seconds;
- Base32 seeds using `A-Z` and `2-7`, with case-insensitive input, optional whitespace/hyphen grouping, and optional terminal `=` padding.

The default item settings are SHA-1, 6 digits, and 30 seconds because that combination is widely used. The provider's actual enrollment parameters are authoritative; choosing different parameters produces different codes.

## Resource and parser bounds

TOTP input is treated as untrusted data even though it is usually pasted by the vault owner.

- formatted seed input is capped at 4,096 characters before normalization;
- normalized Base32 seed text is capped at 1,024 characters;
- a normalized seed must contain at least 16 Base32 characters;
- impossible Base32 encoded lengths are rejected;
- invalid alphabet characters, non-terminal padding, and non-zero residual padding bits are rejected;
- unsupported algorithms, digit counts, periods, and pre-Unix-epoch timestamps are rejected before code generation.

These limits are defensive ceilings, not recommendations for unusually large seeds.

## Code generation

`ITotpService` is the application contract and `TotpService` is the current infrastructure implementation.

At generation time CipherNest:

1. validates the algorithm, digit count, and period;
2. normalizes and validates the Base32 seed;
3. decodes the seed into a temporary byte buffer;
4. computes the moving counter from Unix time and the configured period;
5. writes the counter in big-endian order;
6. computes the configured HMAC;
7. applies RFC dynamic truncation;
8. reduces the result to the configured decimal digit count;
9. returns the code with the remaining validity seconds and boundary time;
10. zeroes the decoded seed buffer and temporary stack hash/counter buffers where practical.

The HMAC object is disposed after each calculation. .NET/operating-system managed-memory limitations still apply: CipherNest cannot guarantee deterministic erasure of every string or runtime copy.

## RFC verification

`TotpServiceTests` includes RFC 6238 Appendix B known-answer vectors for SHA-1, SHA-256, and SHA-512 at the standard test timestamps. The tests also cover formatted lowercase Base32 input, configured code lengths, malformed seeds, unsupported settings, input ceilings, and pre-epoch rejection.

Known-answer tests validate compatibility with those published vectors; they are not a substitute for independent cryptographic review.

## Vault storage

A TOTP item uses the normal encrypted `VaultItem` record:

- `Type = OneTimePassword`;
- `Secret` = Base32 seed;
- `TotpAlgorithm` = selected HMAC algorithm;
- `TotpDigits` = 6 or 8;
- `TotpPeriodSeconds` = 15..120;
- issuer/account labels can be represented using the normal title, username/identifier, URL, collection, tags, notes, and custom fields.

The seed and TOTP settings are inside the authenticated encrypted record payload rather than plaintext searchable SQLite columns.

`VaultItemValidator` performs TOTP-specific validation before a `OneTimePassword` record can be saved through normal vault-service paths.

## Editor behavior

The item editor exposes a TOTP section only when the selected item type is `OneTimePassword`.

- code generation is manual through **Refresh code**;
- there is no background refresh timer in the current implementation;
- generated codes are presentation state and are not written back to the vault record;
- changing the seed, algorithm, digit count, period, or item type clears the displayed code;
- re-authentication-protected items cannot generate/display their code until the item has been re-authenticated;
- **Copy code** refreshes the code immediately before copying it.

A manual refresh design keeps lifetime and lifecycle behavior explicit. A future automatic countdown/refresh implementation would require lifecycle, cancellation, accessibility, backgrounding, and sensitive-state review before release.

## Clipboard boundary

Copying a TOTP code is an explicit user action and uses the same `IClipboardSecurityService` policy as other copied secrets. CipherNest attempts timed conditional cleanup and avoids clearing unrelated newer clipboard content.

Clipboard cleanup is best effort. Operating systems may preserve clipboard history, synchronize clipboard content, expose it to accessibility/input services, or allow another application to read it before cleanup. Users can read the generated code directly instead of copying it when clipboard exposure is undesirable.

Copying the Base32 seed through the generic Secret action is also explicit and carries greater long-term risk because the seed can generate future codes.

## Audit behavior

The password security audit deliberately excludes `OneTimePassword` item seeds from password-strength and password-reuse findings. A Base32 TOTP seed is not a user-chosen account password, so evaluating it with password heuristics would be misleading.

Exact-duplicate detection still applies, and the duplicate signature includes the TOTP algorithm, digit count, and period.

## Backups and exports

Encrypted CipherNest backup naturally carries TOTP items because it carries the encrypted vault database. Restoring a backup therefore restores the seed/settings represented by those encrypted records.

Plaintext CSV behavior must be reviewed separately before claiming dedicated TOTP interoperability. The current TOTP feature does **not** add QR scanning, QR rendering, `otpauth://` import/export, cloud synchronization, browser autofill, or automatic provider enrollment.

Do not put real TOTP seeds in screenshots, documentation examples, test fixtures committed to the repository, issue reports, logs, or support messages.

## Threat considerations

TOTP reduces some risks associated with password-only authentication at the service that consumes the code, but storing password and TOTP seed in the same vault also creates a combined compromise boundary. If an attacker obtains the unlocked vault or its decrypted contents, both factors stored there may be exposed.

CipherNest does not claim to protect a TOTP account when:

- malware can read the unlocked process or screen;
- a malicious/compromised OS can capture clipboard or input/output;
- the user exports/copies the seed to an untrusted destination;
- the authentication provider accepts a bypass/recovery path controlled by an attacker;
- the device clock is materially wrong and the provider does not compensate;
- the underlying service enrollment is compromised.

For high-value accounts, consider whether keeping the second-factor seed on a separate trusted device better matches your threat model.

## Release validation

Before release, maintainers should:

- run all RFC known-answer tests;
- verify invalid Base32 forms fail without unbounded work;
- verify 6- and 8-digit rendering on target platforms;
- verify code generation after save/reopen and after re-authentication;
- verify seed/settings survive encrypted backup/restore through normal record compatibility tests;
- verify generated code is cleared when relevant editor state changes;
- verify explicit clipboard behavior and lock cleanup on supported platforms;
- verify security audit exclusions and duplicate behavior;
- verify no TOTP seed/code is emitted through diagnostics;
- verify documentation does not claim QR import, autofill, full second-factor isolation, or independent audit.

## Final repository-side TOTP hardening — 2026-08-15

The final repository pass adds three implementation guarantees without changing the RFC 6238 code-generation algorithm:

- the mutable normalization `char[]` owned by `TotpPolicy.NormalizeSecret(...)` is cleared in a `finally` block on success and failure;
- Base32 normalization/decoding still completes before HMAC construction, including impossible-length, supplied-padding, invalid alphabet, and non-zero residual-bit rejection;
- a validity window that would extend beyond `DateTimeOffset.MaxValue` is clamped to `DateTimeOffset.MaxValue` instead of throwing after a valid code has already been computed.

`TotpBase32AdversarialTests` now contains exactly 128 deterministic hostile seeds with explicit numeric case IDs so every row is independently discoverable by xUnit. The corpus covers malformed length/padding, invalid digits and punctuation, Unicode control/format/non-ASCII forms, isolated UTF-16 surrogates, oversized normalized/formatted input, and non-zero residual bits. A source-regression test preserves parser-before-HMAC ordering and cleanup of owned mutable key/counter/hash/scratch buffers.

These cleanups narrow the lifetime of mutable buffers owned by CipherNest. They do **not** make immutable managed `string` copies of a seed deterministically erasable, and they do not provide independent second-factor separation when the TOTP seed and login secret live in the same unlocked vault.
