# Test Plan

Release candidates must cover:

- Crypto known-answer/round-trip vectors and nonce-size validation.
- Wrong passphrase and tampered header/record rejection.
- Backup corruption, wrong backup passphrase, and restore atomicity.
- Schema creation and migration idempotence.
- Large encrypted attachment streaming bounds.
- CRUD/search/audit while unlocked and denial while locked.
- Clipboard explicit-copy and timed-clear behavior on supported platforms.
- Lock on background, manual lock, timeout, and resume behavior.
- Theme/accessibility checks: large text, keyboard focus, screen reader labels, contrast, reduced motion.
- Android/Windows smoke tests plus iOS/MacCatalyst on an Apple build host.
- Dependency vulnerability scanning and secret scanning.

A release is blocked by failing tests, unresolved high-severity dependency findings, known secret leakage, or an unreviewed cryptographic format change.
