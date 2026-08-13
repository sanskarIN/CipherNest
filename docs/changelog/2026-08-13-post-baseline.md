# 2026-08-13 Post-Baseline Hardening Supplement

This supplement records source/test work added after the fully observed hosted baseline documented in `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md`.

## CSV input hardening

- CSV decoding is explicitly UTF-8 only.
- Malformed UTF-8 is rejected through the public parser path rather than being silently replaced.
- BOM handling remains limited to a UTF-8 BOM at the beginning of the stream.
- Parser reads are routed through the same strict decoder path, including look-ahead/pushback behavior.
- Escaped-quote parsing remains regression-covered after the decoder refactor.

## Attachment metadata hardening

- Display-name normalization handles both slash styles independently of the current host platform.
- Pseudo/path-derived attachment display names are rejected or reduced to an allowed leaf name before vault persistence/encryption.
- Existing control-character, display-name length, media-type length, and default media-type rules remain in effect.

## Secure-note checklist hardening

- Checklist item text has a centralized maximum-character policy.
- The editor rejects oversized checklist input before newline normalization and note reconstruction.
- Unit coverage exercises the exact checklist item character boundary.

## Additional regression coverage

- Preference normalization boundaries were expanded.
- Backup encrypted-chunk index boundaries were expanded.
- Runtime-null elements inside item collections are covered.
- CSV strict decoder and UTF-8-only behavior have focused regression tests.
- Attachment display-name normalization has focused regression coverage.

## Verification status

The earlier hosted green run is historical evidence for its recorded commit only. The exact current candidate must rerun the complete core/platform CI and CodeQL matrix before these post-baseline changes are represented as release-verified.
