# Release Checklist

- [ ] `dotnet format --verify-no-changes` succeeds in the release environment.
- [ ] Release build succeeds for every supported target available to the release environment.
- [ ] Unit, integration, and UI-structure tests pass with warnings-as-errors/analyzers enabled.
- [ ] Android and Windows smoke tests pass; iOS/MacCatalyst build and smoke tests pass on an appropriate Apple environment.
- [ ] Manual lifecycle tests cover background, sleep/resume, timeout, manual lock, and fail-closed behavior.
- [ ] Biometric enrollment/availability/cancel/failure/device-change behavior is validated on supported real devices before marketing it as supported.
- [ ] Screenshot and clipboard controls are tested on each target and platform limitations remain visible in product/docs.
- [ ] Dependency vulnerability, dependency-review, CodeQL, and secret scans pass or have documented reviewed exceptions.
- [ ] No signing keys, certificates, passwords, API keys, crash tokens, store credentials, or other production secrets exist in repository/history/artifacts.
- [ ] Restored package metadata and license texts are checked against `THIRD_PARTY_NOTICES.md` for the exact resolved versions.
- [ ] Database migration tests pass, including future-schema rejection and compatibility with every supported prior schema.
- [ ] Crypto known-answer, tamper, wrong-key, and format-version tests pass; every cryptographic-format change has focused review.
- [ ] Backup/restore is tested on real target devices with disposable data, including encrypted attachments and corrupted-container rejection.
- [ ] Large attachment streaming, safe text preview, plaintext export warning, and temporary-cache cleanup are exercised.
- [ ] Threat model, privacy notice, security design, diagnostics policy, third-party notices, changelog, support instructions, and audit status are current.
- [ ] Store permissions/descriptions match actual app behavior and the store copy makes no unverified security claim.
- [ ] Store icons/splash/feature-graphic screenshots are checked for safe-zone clipping, contrast, scaling, and synthetic-only demo data.
- [ ] Localization fallback and large-interface layout are smoke-tested; future translations must preserve the meaning of security warnings.
- [ ] Reproducible-build guidance is checked where practical and build dependencies are pinned/reviewed.
- [ ] Signed release artifacts are generated only from a protected release environment; signing material never enters the repository.
- [ ] Independent professional security-audit status is stated exactly as it exists at release time.

A checklist item may be marked not applicable only with a written reason in the release notes. CipherNest must not be called bug-free or independently audited unless that statement is actually supported.
