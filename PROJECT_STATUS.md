# Project Status

## Current release: 0.1.0

### Completed in source
- Repository and solution scaffolding.
- Domain/application/infrastructure separation.
- Versioned cryptographic envelope and key wrapping.
- Encrypted SQLite record persistence.
- Local vault create/unlock/lock.
- Item CRUD/search and local password audit primitives.
- Password/passphrase generator.
- Encrypted backup/restore service.
- MAUI navigation, theme resources, onboarding/unlock/vault/editor/generator/settings/about pages.
- Lifecycle auto-lock foundation and explicit clipboard clearing service.
- Unit/integration test suite source.
- Security/privacy/release/contribution documentation.

### Quality gate not yet independently verified
- CI must restore the exact current .NET/MAUI workloads and NuGet versions and run build/tests on hosted runners.
- Windows/Android/iOS/MacCatalyst packaging needs platform signing identities supplied outside the repository.
- iOS/MacCatalyst compilation requires an appropriate Apple build environment.
- Screenshot blocking and biometric re-authentication require final platform-by-platform validation before being advertised as guarantees.
- Independent security audit remains outstanding.

### Deliberately deferred
- Cloud sync, accounts, collaboration, autofill, TOTP, plaintext export, rich document preview, document scanning, and destructive wipe-on-failure.

No deferred feature is represented in the UI as complete.
