# Post-Baseline Verification Checklist — 2026-08-13

Use this checklist for every commit after the last fully observed hosted baseline.

## Source and test gate

- Restore all non-MAUI test projects from a clean checkout.
- Build UnitTests, IntegrationTests, and UiTests with analyzers and warnings-as-errors.
- Run all three test projects.
- Run the configured formatting verification for Shared, Domain, Application, Infrastructure, and every test project.
- Treat a changed source-regression test as evidence only after the matching runtime behavior remains covered where practical.

## Platform build gate

- Build Android Release with the repository's isolated Android target and supported Android RID.
- Build Windows Release in both default and funding-disabled variants.
- Build iOS simulator Release with the documented compatible macOS/Xcode/.NET workload pairing.
- Build Mac Catalyst Release with the same documented Apple toolchain pairing.
- Do not inherit a platform-build conclusion from an earlier commit after source, project, dependency, workflow, resource, or platform-binding changes.

## Security and dependency gate

- Run CodeQL on the exact candidate.
- Review NuGet restore output for vulnerability warnings.
- Review direct and transitive dependency changes separately from successful compilation.
- Preserve the independent-audit disclaimer even when automated static analysis succeeds.

## Current post-baseline areas requiring rerun

The current branch contains changes after the recorded successful baseline in these areas:

- strict UTF-8 CSV decoding and malformed-input rejection;
- attachment display-name normalization across path separator styles;
- secure-note checklist input bounds and boundary tests;
- additional preference, backup-framing, validation, and attachment regression coverage.

These changes are individually bounded, but release evidence must be produced from the exact final commit rather than inferred from their size.

## Device and packaging gates remain external

Hosted source verification does not replace physical-device biometric, clipboard, screenshot, lifecycle, secure-storage, share-sheet, accessibility, signing, notarization, store-policy, or independent security-review work.
