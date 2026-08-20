# CipherNest Scripts

This directory contains the repository's human-invokable build, verification, and local-launch entry points.

## Complete script inventory

This section is the delegated exhaustive inventory for `scripts/`. `tests/CipherNest.UiTests/RepositoryDocumentationInventorySourceTests.cs` combines it with `docs/REPOSITORY_FILE_REFERENCE.md`.

- `scripts/README.md` — script purpose, platform mapping, and this exhaustive delegated inventory.
- `scripts/verify-core.ps1` — PowerShell core restore/build/test/format verification.
- `scripts/verify-core.sh` — POSIX-shell core restore/build/test/format verification.
- `scripts/verify-windows.ps1` — Windows MAUI verification entry point.
- `scripts/verify-android.sh` — Android MAUI verification entry point.
- `scripts/verify-apple.sh` — iOS simulator and Mac Catalyst verification entry point.
- `scripts/verify-web.sh` — .NET 10 local Web/Linux restore/build/format plus loopback runtime smoke test.
- `scripts/run-linux.sh` — Linux launcher for the loopback-only CipherNest Web host and system browser.

## Recommended verification order

For a broad contributor check, run core tests first, then the host-specific verification available on the current operating system.

Linux:

```bash
bash scripts/verify-core.sh
bash scripts/verify-web.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
./scripts/verify-windows.ps1
```

Apple build host:

```bash
bash scripts/verify-core.sh
bash scripts/verify-apple.sh
```

Android-capable build host:

```bash
bash scripts/verify-core.sh
bash scripts/verify-android.sh
```

The Web/Linux host does not replace native Android/iOS/Windows/Mac Catalyst validation. It supplies the Linux and local-browser execution path while reusing the same encrypted core.
