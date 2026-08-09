# Reproducible Build Guidance

CipherNest enables deterministic compilation for managed projects. Exact platform packages can still vary with SDK/workload, OS image, native toolchain, and signing environment.

For a repeatable comparison:

1. Use the SDK family pinned by `global.json` and record `dotnet --info` plus `dotnet workload list`.
2. Restore from the committed central package file and a clean package cache when investigating discrepancies.
3. Build Release with signing disabled for comparison artifacts.
4. Compare managed assemblies before platform signing/packaging.
5. Record Xcode/Android SDK/Windows SDK versions for native packages.
6. Keep signing outside the reproducibility comparison; signatures commonly embed non-deterministic data.

Do not claim bit-for-bit reproducibility for a target until the release pipeline demonstrates it.
