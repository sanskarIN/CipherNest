# Architecture

CipherNest uses a dependency-inverted multi-project structure:

- `CipherNest.Domain`: entities and domain rules without MAUI/SQLite dependencies.
- `CipherNest.Application`: use-case abstractions, DTOs, validation, and orchestration.
- `CipherNest.Infrastructure`: cryptography, SQLite persistence, files, backups, clocks and concrete services.
- `CipherNest.Shared`: constants and small cross-layer primitives.
- `CipherNest.App`: MAUI views, ViewModels, resources, lifecycle integration, clipboard and platform surfaces.
- `tests`: security-critical unit/integration coverage.

The UI never receives a database connection or KDF implementation directly. It interacts with `IVaultService`, `IPasswordGenerator`, `ISecurityAuditService`, `IBackupService`, and settings/lifecycle abstractions.
