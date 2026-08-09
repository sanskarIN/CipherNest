# Release Process

1. Freeze security-sensitive format changes and complete compatibility tests.
2. Run the full quality gate from `docs/RELEASE_CHECKLIST.md`.
3. Review dependency and CodeQL findings; high-severity unresolved findings block release.
4. Update version/build, changelog, threat model, audit status, and third-party notices.
5. Produce unsigned verification builds from a clean commit.
6. Produce signed platform artifacts only in protected release environments with signing credentials stored as secrets.
7. Verify installed artifact version/hash and run smoke tests on supported target devices.
8. Tag the exact release commit using semantic versioning.
9. Publish checksums and release notes without including internal secrets or private vulnerability details before coordinated disclosure.
