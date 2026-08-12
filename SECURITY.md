# Security Policy

## Reporting a vulnerability

Do **not** publish exploitable details in a public GitHub issue. Email `sanskarin@outlook.in` with the affected CipherNest version/commit, platform, reproduction steps using synthetic data, impact, and any suggested mitigation.

Avoid attaching or sending real:

- vault databases/contents;
- master or backup passphrases;
- recovery keys/material;
- biometric secondary secrets;
- exported plaintext;
- cryptographic keys;
- private documents/attachments;
- signing/store credentials.

The project will acknowledge reports when maintainers are available, investigate reproducible findings, and publish fixes/advisories when appropriate. No guaranteed response window is promised.

Maintainer triage/fix/disclosure procedure is documented in `docs/operations/SECURITY_RESPONSE.md`.

## In scope

- local client and lock/unlock lifecycle;
- cryptographic envelopes, key wrapping, KDF parameters, nonce/AAD handling, format/version parsing, and owned key/buffer lifetime;
- optional recovery and biometric secondary-unlock wrappers;
- session transition ordering, key leases, cancellation, re-authentication, and stale destructive authorization;
- encrypted SQLite persistence, migration, snapshot, replacement, rollback, and deletion behavior;
- encrypted attachment container/streaming/preview/export logic;
- encrypted backup/restore and temporary staging;
- plaintext CSV import/export safeguards and parser/resource bounds;
- clipboard/screenshot protections where the platform implementation claims support;
- privacy-safe diagnostics/redacted developer exports;
- dependency/supply-chain findings that affect shipped CipherNest code;
- documentation/UI claims that materially overstate actual protection.

## Documented environmental limitations

Social engineering, intentionally shared plaintext, compromised/rooted/jailbroken operating systems, hostile firmware/hardware, physical cameras, platform clipboard-history retention, destination-app copies after export, managed-string non-erasure, physical storage remnants, and attacks requiring already-authorized arbitrary privileged code execution are documented limitations rather than claims of full protection.

A limitation can still be reported if CipherNest's documentation or UI materially misrepresents it.

## Security design references

Before assessing a finding, review the current documented boundaries:

- `docs/security/THREAT_MODEL.md`
- `docs/security/CRYPTOGRAPHIC_DESIGN.md`
- `docs/security/SESSION_SECURITY.md`
- `docs/security/DATA_LIFECYCLE.md`
- `docs/architecture/SESSION_AND_CONCURRENCY.md`
- `docs/formats/VAULT_RECORDS.md`
- `docs/formats/ATTACHMENTS.md`
- `docs/formats/ENCRYPTED_BACKUP.md`
- `docs/formats/CSV_TRANSFER.md`

The complete documentation index is `docs/README.md`.

## Coordinated disclosure

When a finding is reproducible and affects released users, maintainers should prefer a coordinated fix/release path before publishing unnecessary weaponizable detail when circumstances permit. Public advisories should identify affected/fixed versions, impact, mitigation/upgrade action, and reporter credit if requested without exposing private report data.

## Audit status

The project has **not** completed an independent professional security audit. Users with high-risk threat models should wait for an appropriate audit and independently review the threat/cryptographic/session/data-lifecycle documentation before relying on CipherNest.

Internal testing, open source, configured CI, and this security policy are not substitutes for an independent professional audit.
