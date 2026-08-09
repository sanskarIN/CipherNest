# Test Plan

Release candidates must cover:

- Argon2id known-answer vectors using fixed passphrase/salt/parameters.
- Hostile/untrusted KDF metadata bounds before Argon2 work: salts shorter than 16 or longer than 64 bytes, memory outside 16–512 MiB, iterations outside 1–10, and parallelism outside 1–16 must be rejected without honoring the excessive resource request.
- AES-GCM round-trip, nonce-size validation, associated-data binding, record tamper rejection, and wrong-key rejection.
- Wrong master passphrase, recovery-key behavior, secondary biometric-wrapper behavior, and per-item re-authentication rules.
- Master-passphrase changes must end the current security session, lock the vault, clear the remembered master-authentication timestamp, and require the new master passphrase before biometric convenience unlock can resume.
- Failed interactive unlocks must follow the bounded backoff schedule, reset after success, and never be represented as protection against offline attacks on a copied database.
- Backup corruption, wrong backup passphrase, authenticated attachment restore, and restore atomicity/preservation of the active vault after failed restore.
- Schema creation, migration idempotence, ordered migration history, and rejection of unsupported future schemas.
- Multi-megabyte encrypted attachment streaming round trips across multiple 256 KiB chunks, truncation/tamper rejection, and 100 MB bounds.
- Safe text-attachment preview type/size/UTF-8 limits and no-temp-file behavior where practical to automate.
- CRUD/search/filter/sort/audit while unlocked and denial while locked.
- Large-vault UI behavior must page rendered results incrementally while preserving local search/filter/sort result counts.
- Weak, reused, exact-duplicate, missing-title, and overdue-review audit findings.
- Safe secure-note Markdown subset, checklist editing, HTML neutralization, line/character bounds, and fenced-code behavior.
- Password generator selected-group guarantees, ambiguous-character exclusion, 256-entry passphrase-list invariants, requested word counts, and passphrase lower bounds.
- CSV valid quoting, duplicate/empty headers, unterminated quotes, characters after closing quotes, excessive columns/fields/rows, and malformed-parser corpus coverage.
- Plaintext export confirmation phrase, master-passphrase re-authentication, explicit warning, temporary-cache cleanup path, and no attachment inclusion.
- Clipboard username/password/custom-secret copy must remain explicit. Timed clearing must be bounded, preserve a newer unrelated clipboard value, and be attempted immediately when manual/background/timeout locking occurs.
- Manual lock, lock on background, lock after inactivity/resume, clock-rollback fail-closed behavior, and lifecycle error handling.
- Sensitive ViewModel state must be cleared when Unlock, Settings, Transfer, Trash, Item Editor, and Onboarding pages disappear; managed-memory limitations remain documented.
- Trash retention cutoffs must be deterministic and bounded. Automatic configured-retention cleanup must run during routine vault maintenance, while manual permanent deletion/empty-trash actions require current master-passphrase re-authentication and explicit confirmation.
- Biometric availability/enrollment/change/cancel/failure flows on real Android and Apple targets; fallback to master passphrase on unsupported platforms.
- Screenshot-protection behavior where supported, with explicit fallback verification elsewhere.
- Theme/accessibility checks: larger-interface setting, OS large text, keyboard focus, screen-reader labels/live regions, contrast, touch target size, responsive narrow/desktop layouts, and reduced-motion behavior.
- English resource fallback, saved System/English language preference, and layout resilience when future localized strings expand.
- Privacy-safe diagnostics: operation identifiers may be logged, while exception message, stack, vault fields, passphrases, keys, and decrypted attachments are absent; temporary redacted diagnostic share files should be deleted where permitted. Capability-probe and external-link launcher failures must not reintroduce raw exception-message logging.
- Project-support metadata consistency: `AppConstants.BuyMeACoffeeUrl`, About, README, SUPPORT, and `.github/FUNDING.yml` must resolve to `https://buymeacoffee.com/sanskarIN`; About public URLs/emails should continue to bind from shared constants rather than duplicate values.
- Funding CTA build gating: default app builds expose the optional support surface; builds with `CipherNestEnableFundingLink=false` must compile the disable symbol and hide the in-app support frame/metadata while leaving repository funding metadata unchanged.
- Branding checks must verify the original vector mark, splash wordmark, `Made by the Sanskar` credit, monochrome source, and dark-surface logo source without placing branding over user content.
- Android/Windows smoke tests plus iOS/MacCatalyst builds and smoke tests on an appropriate Apple host/device or simulator.
- Dependency vulnerability scanning, dependency review, CodeQL, and secret scanning.

## Current automated source coverage

The repository includes unit/integration tests for cryptographic behavior, Argon2id known-answer and hostile-resource bounds, vault lifecycle, backup/restore corruption and wrong-passphrase preservation, passphrase rotation, secondary unlock wrappers, generator behavior, safe-note parsing, local audit findings, schema migration, malformed CSV parsing, multi-megabyte attachment streaming, and attachment tamper/truncation rejection.

Pure unit-test policies now cover session lock timing/background behavior, clock rollback, clipboard clear-delay bounds/replacement preservation, trash-retention cutoffs, and failed-unlock exponential backoff. UI structure tests are part of the core CI job and check core routes, semantic metadata, localization structure, responsive/incremental vault layout, explicit clipboard actions, sensitive-page cleanup hooks, master-passphrase session reset, guarded trash deletion, privacy-safe unlock/external-link diagnostics, legal surfaces, branding-source requirements, project-support metadata consistency, centralized About metadata bindings, and the store-toggleable funding CTA source path.

Device-specific biometric, screenshot, clipboard API behavior, lifecycle callbacks, share-sheet/cache deletion, in-memory preview presentation, browser/launcher behavior, accessibility, localization rendering, store-policy interpretation, and packaging behavior still requires target-platform execution; source presence is not treated as proof that a platform guarantee works.

A release is blocked by failing tests, unresolved high-severity dependency findings, known secret leakage, a broken migration/restore compatibility path, an unbounded untrusted resource parameter, or an unreviewed cryptographic format change.
