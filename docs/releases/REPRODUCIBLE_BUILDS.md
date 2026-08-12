# Reproducible Build Guidance

CipherNest enables deterministic managed compilation in `Directory.Build.props` and centrally pins NuGet package versions in `Directory.Packages.props`. Reproducibility still depends on the SDK, workloads, platform toolchains, signing steps, and package feeds used by the build environment.

The end-to-end candidate/provenance process is `RELEASE_PROCESS.md`; configured/local execution gates are documented in `../verification/CI_GATES.md`.

## Record the environment

For every release candidate capture, without secrets:

```bash
dotnet --info
dotnet workload list
dotnet nuget list source
```

Also record the source commit SHA, operating-system build, Android SDK/JDK versions where relevant, Xcode version for Apple targets, selected `CipherNestEnableFundingLink` value, and exact resolved dependency graph/provenance where available.

## Restore discipline

- Build from a clean checkout of the exact release commit/tag.
- Use the repository's central package versions.
- Review restored dependency metadata and trusted package feeds in the protected release environment.
- Use a clean package cache when independently investigating discrepancies.
- Preserve package provenance/SBOM information where the release system supports it.
- Keep the exact documentation/release status tied to the same source candidate; a rebuilt artifact from changed source/docs is a different candidate for evidence purposes.

## Unsigned comparison

Compare unsigned/intermediate application binaries before platform signing whenever the platform toolchain makes that practical. Timestamps, generated platform manifests, native toolchains, and bundle metadata can still introduce differences; document known nondeterministic inputs instead of claiming byte-for-byte reproducibility without evidence.

## Signing

Signing keys, certificates, passwords, notarization credentials, store tokens, and keystores must remain outside the repository. Signing naturally changes final artifacts, so compare content before signing or compare normalized package contents when appropriate.

## Suggested comparison flow

1. Start from two clean environments that use the same documented SDK/workload/toolchain versions.
2. Restore from the same trusted feeds and central package versions.
3. Build Release with signing disabled where the platform permits it.
4. Compare managed assemblies and normalized unsigned package contents.
5. Investigate any difference and record the tool/input that caused it.
6. Execute/review the exact candidate's core/platform/documentation gates rather than assuming equivalent source.
7. Sign/notarize only in the protected release environment after the unsigned candidate passes the release checklist.
8. Record final artifact hashes and provenance according to `RELEASE_PROCESS.md`.

## Documentation provenance

`docs/verification/DOCUMENTATION_SUITE_2026_08_12.md` defines the documentation-completeness/source-to-document review gate. Release documentation, audit wording, store claims, format/version docs, and user recovery guidance must correspond to the exact source candidate whose artifacts are being reproduced.

## Verification status

The repository provides deterministic-build settings and reproducibility guidance; it does not currently claim that every Android, iOS, Mac Catalyst, or Windows store artifact is byte-for-byte reproducible across independent environments. Make a stronger claim only after an independent reproduction exercise succeeds and is documented.
