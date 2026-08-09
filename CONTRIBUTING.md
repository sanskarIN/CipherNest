# Contributing to CipherNest

Thank you for improving CipherNest. Security-sensitive changes require tests and focused review.

## Workflow

1. Create a focused branch.
2. Keep secrets, signing material, credentials, private test vaults, and production data out of commits.
3. Run `dotnet format --verify-no-changes`, `dotnet build -c Release`, and `dotnet test -c Release`.
4. Add tests for behavior changes, especially crypto envelopes, migrations, import parsers, lock lifecycle, and backup restore.
5. Open a pull request using the repository template.

Cryptography changes must not introduce custom primitives. Changes to KDF parameters, envelope formats, nonce handling, key lifecycles, or backup formats require an architecture decision record and compatibility tests.

Report vulnerabilities privately as described in `SECURITY.md`; do not open public issues for exploitable findings.
