# Time-Based One-Time Passwords (TOTP)

CipherNest supports local generation of RFC 6238-style time-based one-time passwords for vault items whose type is `OneTimePassword`, plus bounded `otpauth://totp/...` setup-URI import and export for interoperability with compatible authenticator workflows.

This feature is intentionally local-first. CipherNest does not contact an authentication provider to generate a code, parse a setup URI, or format a setup URI, and it does not upload the seed. **Generated one-time codes are not persisted.** The Base32 seed is stored as the item's encrypted `Secret` field inside the existing authenticated vault-record envelope.

## Security status

CipherNest has **not** completed an independent professional security audit. TOTP support does not change that status and must not be described as independently audited or as protection against every account-compromise scenario.

A TOTP seed is equivalent to a long-lived authentication secret. Anyone who obtains the seed can normally generate future valid codes until the service-side TOTP enrollment is replaced. An `otpauth://` setup URI normally contains that same seed and must be protected at least as carefully as the seed itself.

## Supported parameters

The current implementation supports:

- HMAC-SHA-1;
- HMAC-SHA-256;
- HMAC-SHA-512;
- 6-digit or 8-digit codes;
- periods from 15 through 120 seconds;
- Base32 seeds using `A-Z` and `2-7`, with case-insensitive input, optional whitespace/hyphen grouping, and optional terminal `=` padding;
- local `otpauth://totp/...` parsing;
- local canonical `otpauth://totp/...` formatting;
- account-name and issuer metadata;
- default URI parameters of SHA-1, 6 digits, and 30 seconds when those fields are absent.

HOTP is intentionally not supported by the current item model or URI codec. A URI with host/type `hotp` or a `counter` parameter is rejected rather than being silently converted to TOTP.

The default item settings are SHA-1, 6 digits, and 30 seconds because that combination is widely used. The provider's actual enrollment parameters are authoritative; choosing different parameters produces different codes.

## Resource and parser bounds

TOTP input is treated as untrusted data even though it is usually pasted by the vault owner.

### Base32 seed bounds

- formatted seed input is capped at 4,096 characters before normalization;
- normalized Base32 seed text is capped at 1,024 characters;
- a normalized seed must contain at least 16 Base32 characters;
- impossible Base32 encoded lengths are rejected;
- invalid alphabet characters, non-terminal padding, and non-zero residual padding bits are rejected;
- unsupported algorithms, digit counts, periods, and pre-Unix-epoch timestamps are rejected before code generation.

### `otpauth://` URI bounds

`TotpUriCodec` applies a separate interoperability boundary:

- URI text is capped at 8,192 characters;
- only absolute `otpauth://totp/...` URIs are accepted;
- user-info, custom ports, fragments, HOTP, and `counter` are rejected;
- the URI must contain exactly one label path segment;
- query processing is capped at 16 pairs;
- every query pair must be non-empty `name=value` syntax;
- duplicate query keys are rejected case-insensitively to avoid ambiguous security-sensitive values;
- query names are limited to a small ASCII identifier form with a 64-character ceiling;
- percent encoding/control characters are validated for every query value, including otherwise ignored future/unknown parameters;
- account names are capped at 512 characters;
- issuers are capped at 256 characters;
- the decoded label may contain at most one `:` issuer/account separator;
- an empty issuer prefix before a separator is rejected;
- `:` is rejected inside the account and issuer components so formatter output cannot be reinterpreted on parse;
- Unicode Control and Format characters are rejected from display metadata;
- an issuer encoded in the label and an explicit `issuer=` parameter must match when both are present;
- imported seed/algorithm/digits/period values are routed through the same authoritative TOTP validation used by code generation.

The colon rule is deliberate. The Key URI label uses `:` to separate issuer from account. Allowing additional colons inside either component would make a formatted URI capable of parsing back into different metadata.

These limits are defensive ceilings, not recommendations for unusually large seeds, labels, or URIs.

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

## Setup-URI interoperability

`ITotpUriCodec` is the application contract and `TotpUriCodec` is the current infrastructure implementation.

### Import

The item editor accepts a sensitive `otpauth://totp/...` text value. On successful import CipherNest updates:

- `Secret` from `secret=`;
- algorithm;
- digits;
- period;
- username/identifier from the account label;
- title from the issuer when available, otherwise from the account label.

The dedicated URI-entry field is cleared after the import attempt and is also cleared when the item editor clears sensitive state on page disappearance.

The import operation does not contact the issuer/provider, does not verify that a server-side enrollment exists, and does not prove that the imported seed belongs to the displayed account. Users should review imported metadata before saving.

### Export/copy

**Copy setup URI** formats the current TOTP seed/settings into a canonical local `otpauth://totp/...` URI. When a username is present, CipherNest uses it as the account name and uses the title as issuer metadata. If the username is empty, the title is used as the account label without duplicating it as issuer metadata.

Account/issuer values containing `:` are rejected because the character is reserved as the label delimiter and would otherwise make round-trip interpretation ambiguous.

The resulting URI is copied through the existing secret clipboard service and therefore receives the configured best-effort timed cleanup behavior. The URI is not persisted as a separate vault field.

### What is not implemented

- QR scanning;
- QR rendering;
- camera-based enrollment;
- automatic provider enrollment;
- provider/network verification;
- browser/application autofill based on TOTP;
- HOTP URI import/export.

Those features require separate platform/privacy/security design and testing.

## RFC and interoperability verification

`TotpServiceTests` includes RFC 6238 Appendix B known-answer vectors for SHA-1, SHA-256, and SHA-512 at the standard test timestamps. The tests also cover formatted lowercase Base32 input, configured code lengths, malformed seeds, unsupported settings, input ceilings, and pre-epoch rejection.

`TotpUriCodecTests` covers canonical parsing, standards-compatible defaults, explicit/label issuer behavior, format/parse round trips, wrong schemes, HOTP/counter rejection, case-insensitive duplicate parameters, empty query pairs, invalid settings, mismatched issuer metadata, URI/query ceilings, exact and first-over account/issuer bounds, additional/empty label separators, invalid percent encoding including unknown parameters, control/format characters, invalid secrets, and encoded formatter ceilings.

Known-answer and parser tests validate deterministic repository behavior; they are not a substitute for independent cryptographic or interoperability review across every third-party authenticator/provider.

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

The pasted/formatted setup URI is not stored as a separate property in `VaultItem`.

## Editor behavior

The item editor exposes a TOTP section only when the selected item type is `OneTimePassword`.

- code generation is manual through **Refresh code**;
- there is no background refresh timer in the current implementation;
- generated codes are presentation state and are not written back to the vault record;
- changing the seed, algorithm, digit count, period, or item type clears the displayed code;
- re-authentication-protected items cannot generate/display/import/export their TOTP material until the item has been re-authenticated;
- **Copy code** refreshes the code immediately before copying it;
- **Import URI** parses a local sensitive setup URI and clears the dedicated import text afterward;
- **Copy setup URI** formats the current TOTP configuration and copies it using the timed secret-clipboard path.

A manual refresh design keeps lifetime and lifecycle behavior explicit. A future automatic countdown/refresh implementation would require lifecycle, cancellation, accessibility, backgrounding, and sensitive-state review before release.

## Clipboard boundary

Copying a TOTP code or setup URI is an explicit user action and uses the same `IClipboardSecurityService` policy as other copied secrets. CipherNest attempts timed conditional cleanup and avoids clearing unrelated newer clipboard content.

Clipboard cleanup is best effort. Operating systems may preserve clipboard history, synchronize clipboard content, expose it to accessibility/input services, or allow another application to read it before cleanup. Users can read the generated code directly instead of copying it when clipboard exposure is undesirable.

Copying the Base32 seed through the generic Secret action or copying a setup URI carries greater long-term risk than copying one short-lived code because either can normally generate future codes.

## Audit behavior

The password security audit deliberately excludes `OneTimePassword` item seeds from password-strength and password-reuse findings. A Base32 TOTP seed is not a user-chosen account password, so evaluating it with password heuristics would be misleading.

Exact-duplicate detection still applies, and the duplicate signature includes the TOTP algorithm, digit count, and period.

## Backups and exports

Encrypted CipherNest backup naturally carries TOTP items because it carries the encrypted vault database. Restoring a backup therefore restores the seed/settings represented by those encrypted records.

Dedicated local `otpauth://totp/...` interoperability is now implemented for single TOTP items. Plaintext CSV remains a separate generic transfer surface and should not be described as a dedicated authenticator-migration format. QR scanning/rendering, provider enrollment, cloud synchronization, and browser autofill remain unimplemented.

Do not put real TOTP seeds or setup URIs in screenshots, documentation examples, test fixtures committed to the repository, issue reports, logs, or support messages.

## Threat considerations

TOTP reduces some risks associated with password-only authentication at the service that consumes the code, but storing password and TOTP seed in the same vault also creates a combined compromise boundary. If an attacker obtains the unlocked vault or its decrypted contents, both factors stored there may be exposed.

CipherNest does not claim to protect a TOTP account when:

- malware can read the unlocked process or screen;
- a malicious/compromised OS can capture clipboard or input/output;
- the user exports/copies the seed or setup URI to an untrusted destination;
- the authentication provider accepts a bypass/recovery path controlled by an attacker;
- the device clock is materially wrong and the provider does not compensate;
- the underlying service enrollment is compromised.

A structurally valid URI is not proof of issuer identity. A malicious source can provide attacker-chosen issuer/account/seed data that passes syntax/resource validation. Review imported metadata before saving.

For high-value accounts, consider whether keeping the second-factor seed on a separate trusted device better matches your threat model.

## Release validation

Before release, maintainers should:

- run all RFC known-answer tests;
- run all `TotpUriCodecTests`;
- verify invalid Base32 forms fail without unbounded work;
- verify malformed/oversized/duplicate/empty-query/HOTP setup URIs are rejected without network work;
- verify multi-colon/empty-issuer labels and colon-bearing account/issuer formatter inputs are rejected deterministically;
- verify malformed percent encoding in unknown extension parameters is rejected rather than ignored;
- verify 6- and 8-digit rendering on target platforms;
- verify URI import populates account/issuer/settings correctly and clears the sensitive URI-entry field;
- verify copied setup URIs round-trip with representative compatible authenticators using synthetic seeds only;
- verify code generation after save/reopen and after re-authentication;
- verify seed/settings survive encrypted backup/restore through normal record compatibility tests;
- verify generated code is cleared when relevant editor state changes;
- verify explicit clipboard behavior and lock cleanup on supported platforms for codes and setup URIs;
- verify security audit exclusions and duplicate behavior;
- verify no TOTP seed/code/setup URI is emitted through diagnostics;
- verify documentation does not claim QR import, provider enrollment, autofill, full second-factor isolation, universal compatibility, or independent audit.

## Final repository-side TOTP hardening — 2026-08-15

The August 15 repository pass added three implementation guarantees without changing the RFC 6238 code-generation algorithm:

- the mutable normalization `char[]` owned by `TotpPolicy.NormalizeSecret(...)` is cleared in a `finally` block on success and failure;
- Base32 normalization/decoding completes before HMAC construction, including impossible-length, supplied-padding, invalid alphabet, and non-zero residual-bit rejection;
- a validity window that would extend beyond `DateTimeOffset.MaxValue` is clamped to `DateTimeOffset.MaxValue` instead of throwing after a valid code has already been computed.

`TotpBase32AdversarialTests` contains exactly 128 deterministic hostile seeds with explicit numeric case IDs so every row is independently discoverable by xUnit. The corpus covers malformed length/padding, invalid digits and punctuation, Unicode control/format/non-ASCII forms, isolated UTF-16 surrogates, oversized normalized/formatted input, and non-zero residual bits. A source-regression test preserves parser-before-HMAC ordering and cleanup of owned mutable key/counter/hash/scratch buffers.

These cleanups narrow the lifetime of mutable buffers owned by CipherNest. They do **not** make immutable managed `string` copies of a seed deterministically erasable, and they do not provide independent second-factor separation when the TOTP seed and login secret live in the same unlocked vault.

## `otpauth://` interoperability expansion — 2026-08-18

The August 18 continuation adds bounded TOTP setup-URI import/export without adding QR, camera, provider, cloud, or HOTP behavior.

Repository-side guarantees added in this pass include:

- application `TotpUriProfile` model;
- `ITotpUriCodec` abstraction;
- `TotpUriCodec` bounded parser/formatter;
- strict TOTP-only scheme/type validation;
- duplicate-query rejection;
- URI/query/display-metadata ceilings;
- issuer consistency validation;
- import field clearing after attempts and on page exit;
- setup-URI copy through the existing timed secret clipboard service;
- unit and UI/source regression tests for the new boundary.

### Final ambiguity hardening

The final August 18 parser pass additionally:

- rejects more than one decoded `:` label separator;
- rejects `:` inside account/issuer components during formatting and parsing;
- rejects an empty issuer prefix before a label separator;
- rejects empty query pairs instead of silently dropping them;
- validates percent encoding/control characters for all query values, including well-named unknown extension parameters;
- adds regression tests for each of those cases and for the exact 16-query-pair boundary.

These rules are intended to keep canonical formatting deterministic and prevent malformed/ambiguous extension data from being silently normalized into a different interpretation.
