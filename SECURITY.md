# Security Policy

## Reporting a vulnerability

Do **not** publish exploitable details in a public GitHub issue. Email `sanskarin@outlook.in` with the affected CipherNest version/commit, platform, reproduction steps using synthetic data, impact, and any suggested mitigation. Avoid attaching real vaults, master passphrases, recovery keys, biometric secondary secrets, exported plaintext, cryptographic keys, or private documents.

The project will acknowledge reports when maintainers are available, investigate reproducible findings, and publish fixes/advisories when appropriate. No guaranteed response window is promised.

## In scope

- local client and lock/unlock lifecycle;
- cryptographic envelopes, key wrapping, KDF parameters, nonce/AAD handling, and migration/version parsing;
- optional recovery and biometric secondary-unlock wrappers;
- encrypted SQLite persistence and migration behavior;
- encrypted attachment container/streaming/preview/export logic;
- encrypted backup/restore and temporary staging;
- plaintext CSV import/export safeguards and import parser bounds;
- clipboard/screenshot protections where the platform implementation claims support;
- privacy-safe diagnostics/redacted developer exports;
- dependency/supply-chain findings that affect shipped CipherNest code.

## Documented environmental limitations

Social engineering, intentionally shared plaintext, compromised/rooted/jailbroken operating systems, hostile firmware/hardware, physical cameras, platform clipboard-history retention, destination-app copies after export, and attacks requiring already-authorized arbitrary privileged code execution are documented limitations rather than claims of full protection.

A limitation can still be reported if CipherNest's documentation or UI materially misrepresents it.

## Audit status

The project has **not** completed an independent professional security audit. Users with high-risk threat models should wait for an appropriate audit and independently review `docs/security/THREAT_MODEL.md` and `docs/security/CRYPTOGRAPHIC_DESIGN.md` before relying on CipherNest.
