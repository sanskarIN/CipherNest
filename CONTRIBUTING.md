# Contributing to CipherNest

Thank you for improving CipherNest. Security-sensitive changes require tests and focused review.

## Read before changing source

Start with:

- `docs/README.md` — complete documentation index;
- `docs/DEVELOPER_GUIDE.md` — architecture/development rules;
- `docs/MAINTAINER_GUIDE.md` — repository/security/release ownership expectations;
- `docs/TESTING_GUIDE.md` and `docs/TEST_PLAN.md` — test-layer and release-test requirements;
- `docs/security/THREAT_MODEL.md` and `docs/security/CRYPTOGRAPHIC_DESIGN.md` — security assumptions and implemented cryptographic design;
- `docs/DOCUMENTATION_MAINTENANCE.md` — required documentation synchronization.

## Workflow

1. Create a focused branch unless the current repository workflow explicitly uses direct main changes.
2. Keep secrets, signing material, credentials, private test vaults, real backups, and production/user data out of commits.
3. Prefer the committed verification scripts from `docs/setup/BUILD.md` rather than hand-assembling incomplete test commands.
4. Add tests for behavior changes at the appropriate unit/integration/source/device layer, especially crypto envelopes, migrations, import parsers, session/lock lifecycle, attachment framing, backup restore, resource limits, and privacy-sensitive UI.
5. Update user/developer/security/format/release documentation when behavior or assumptions change.
6. Open a focused pull request using the repository template when contributing through review branches.

## Quality gates

Repository build policy enables nullable analysis, warnings-as-errors, latest analyzers, deterministic managed builds, and code-style enforcement.

Do not solve a new warning/test failure by globally disabling those gates.

Core verification entry points:

```text
scripts/verify-core.ps1
scripts/verify-core.sh
```

Platform compile verification:

```text
scripts/verify-windows.ps1
scripts/verify-android.sh
scripts/verify-apple.sh
```

See `docs/verification/CI_GATES.md`. Configured CI is not considered passing evidence until the exact candidate commit actually executes successfully.

## Security-sensitive changes

Cryptography changes must not introduce custom primitives. Changes to KDF parameters, envelope formats, nonce/AAD handling, key/session lifecycles, backup/attachment formats, database replacement/migration, destructive authorization, or plaintext-export boundaries require focused design/compatibility/regression tests and documentation updates.

Use the existing Application/Infrastructure boundaries rather than creating UI paths that directly access SQLite or ad-hoc cryptography.

## Test data

Use synthetic/disposable data only. Never commit or request:

- real vault databases;
- master/backup passphrases;
- recovery keys;
- biometric secondary secrets;
- decrypted attachments/private documents;
- payment credentials;
- signing keys/certificates/passwords;
- store/API tokens.

## Documentation

A behavior change that makes documentation inaccurate is incomplete. Update the applicable canonical documents listed in `docs/DOCUMENTATION_MAINTENANCE.md` in the same change series.

## Vulnerabilities

Report vulnerabilities privately as described in `SECURITY.md`; do not open public issues for exploitable findings. Maintainers follow `docs/operations/SECURITY_RESPONSE.md` for coordinated investigation/response.
