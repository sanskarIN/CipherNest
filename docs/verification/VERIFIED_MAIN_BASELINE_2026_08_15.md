# Verified `main` Baseline — 2026-08-15

This record captures the last fully observed immutable `main` baseline before the 2026-08-16 presentation/support-surface pass.

## Candidate

- Commit: `d405bb3ae0a88f4abfcdcb574227c372683dd790`
- Commit message: `docs(verification): freeze final repository completion candidate`
- Author/committer identity observed by GitHub Actions: `Sanskar <sanskarin@outlook.in>`

## CipherNest CI

GitHub Actions run `31879581456` completed successfully for the exact candidate above.

The `test-core` job built all three test projects with analyzers and completed with zero build warnings/errors:

- Unit tests: **346 passed, 0 failed, 0 skipped**
- Integration tests: **98 passed, 0 failed, 0 skipped**
- UI/source tests: **110 passed, 0 failed, 0 skipped**
- Total: **554 passed, 0 failed, 0 skipped**
- Configured core formatting checks: passed

Platform build jobs in the same run also completed successfully:

- Windows Release with analyzers
- Windows Release with `CipherNestEnableFundingLink=false`
- Android Release with analyzers
- iOS simulator Release with analyzers
- Mac Catalyst Release with analyzers

## CodeQL

GitHub Actions run `31879581401` completed successfully for the same candidate. Its analysis job completed:

- CodeQL initialization
- analyzable core build
- .NET MAUI Android workload installation
- analyzable MAUI application build
- CodeQL analysis

## Interpretation

This is exact-head evidence only for `d405bb3ae0a88f4abfcdcb574227c372683dd790`. Any later commit becomes a new candidate and must rerun the configured gates before it can inherit exact-head status.

The successful repository-side gates do not replace physical-device validation, accessibility validation on assistive technologies, signing/notarization, store-policy review, store submission, or an independent professional security review.
