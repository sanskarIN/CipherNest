# Release Checklist

- [ ] `dotnet format --verify-no-changes`
- [ ] Release build succeeds for every supported target available to the release environment.
- [ ] Unit/integration tests pass; critical UI flows smoke-tested.
- [ ] Dependency vulnerability and secret scans pass.
- [ ] No signing keys or credentials are in repository/history/artifacts.
- [ ] Database and crypto compatibility tests pass against previous supported format.
- [ ] Backup/restore tested on real target devices with disposable data.
- [ ] Threat model, privacy notice, third-party licenses, changelog, and audit status are current.
- [ ] Store permissions/descriptions match actual app behavior.
- [ ] Platform screenshot/clipboard/lock limitations are documented.
- [ ] Reproducible-build instructions checked where practical.
- [ ] Signed release artifacts generated from protected CI/release environment.
