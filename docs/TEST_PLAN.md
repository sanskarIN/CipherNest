# Test Plan

Release candidates must cover:

- Argon2id known-answer vectors using fixed passphrase/salt/parameters.
- AES-GCM round-trip, nonce-size validation, associated-data binding, record tamper rejection, and wrong-key rejection.
- Wrong master passphrase, recovery-key behavior, secondary biometric-wrapper behavior, and per-item re-authentication rules.
- Backup corruption, wrong backup passphrase, authenticated attachment restore, and restore atomicity.
- Schema creation, migration idempotence, ordered migration history, and rejection of unsupported future schemas.
- Multi-megabyte encrypted attachment streaming round trips across multiple 256 KiB chunks, truncation/tamper rejection, and 100 MB bounds.
- Safe text-attachment preview type/size/UTF-8 limits and no-temp-file behavior where practical to automate.
- CRUD/search/filter/sort/audit while unlocked and denial while locked.
- Weak, reused, exact-duplicate, missing-title, and overdue-review audit findings.
- Safe secure-note Markdown subset, checklist editing, HTML neutralization, line/character bounds, and fenced-code behavior.
- Password generator selected-group guarantees, ambiguous-character exclusion, 256-entry passphrase-list invariants, requested word counts, and passphrase lower bounds.
- CSV valid quoting, duplicate/empty headers, unterminated quotes, characters after closing quotes, excessive columns/fields/rows, and malformed-parser corpus coverage.
- Plaintext export confirmation phrase, master-passphrase re-authentication, explicit warning, temporary-cache cleanup path, and no attachment inclusion.
- Clipboard explicit-copy and timed-clear behavior on supported platforms, including platform history limitations.
- Manual lock, lock on background, lock after inactivity/resume, and fail-closed lifecycle error handling.
- Biometric availability/enrollment/change/cancel/failure flows on real Android and Apple targets; fallback to master passphrase on unsupported platforms.
- Screenshot-protection behavior where supported, with explicit fallback verification elsewhere.
- Theme/accessibility checks: large interface setting, OS large text, keyboard focus, screen-reader labels/live regions, contrast, touch target size, and reduced-motion behavior.
- English resource fallback, saved System/English language preference, and layout resilience when future localized strings expand.
- Privacy-safe diagnostics: operation identifiers may be logged, while exception message, stack, vault fields, passphrases, keys, and decrypted attachments are absent.
- Android/Windows smoke tests plus iOS/MacCatalyst builds and smoke tests on an appropriate Apple host/device or simulator.
- Dependency vulnerability scanning, dependency review, CodeQL, and secret scanning.

## Current automated source coverage

The repository includes unit/integration tests for cryptographic behavior, vault lifecycle, backup/restore, passphrase rotation, secondary unlock wrappers, generator behavior, safe-note parsing, local audit findings, schema migration, malformed CSV parsing, and multi-megabyte attachment streaming. UI structure tests are included in the core CI job.

Device-specific biometric, screenshot, clipboard, lifecycle, accessibility, and packaging behavior still requires target-platform execution; source presence is not treated as proof that a platform guarantee works.

A release is blocked by failing tests, unresolved high-severity dependency findings, known secret leakage, a broken migration/restore compatibility path, or an unreviewed cryptographic format change.
