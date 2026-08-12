# Platform Packaging

Packaging is intentionally separated from source compilation because release signing credentials must never be committed. The canonical end-to-end candidate/evidence/signing process is `RELEASE_PROCESS.md`; build prerequisites are in `../setup/BUILD.md`.

## Android

- Build the `net10.0-android` Release target in a protected environment.
- Supply the Android signing keystore/password through protected CI or local secret storage.
- Prefer the store-required bundle format for distribution and retain mapping/symbol artifacts where applicable.
- Verify package/application ID `in.sanskar.ciphernest`, version code/name, adaptive icon, splash, minimum SDK, and requested permissions against actual behavior.
- Test biometric enrollment/cancel/failure, screenshot protection, clipboard handling, background locking, backup/file picker, export/share, and accessibility on representative real devices before promotion.

## Windows

- Build `net10.0-windows10.0.19041.0` and produce the store/package form required by the intended distribution channel.
- Keep code-signing certificates/passwords outside the repository.
- Confirm package identity, app icon assets, desktop resizing/keyboard navigation, file picker/share behavior, and lock lifecycle.
- Windows biometric convenience unlock is intentionally not advertised in the current release.

## iOS

- Use a supported Mac/Xcode environment with protected signing/provisioning material.
- Confirm application identifier, entitlements, Face ID usage description, icons, splash, minimum OS, privacy declarations, and store metadata.
- Exercise Face ID/Touch ID availability, denial, cancellation, enrollment changes, secure-storage reset, background/sleep locking, and restore behavior.

## Mac Catalyst

- Build/sign/notarize in the supported Apple environment.
- Verify resizing, keyboard/mouse navigation, secure storage, biometric availability, share/file picker behavior, icon presentation, and privacy declarations.

## Optional funding CTA per distribution

The source repository uses `https://buymeacoffee.com/sanskarIN` for optional voluntary project support. Before packaging any store build, verify whether that exact store/distribution/region/app category permits an in-app external funding CTA.

Normal builds use `CipherNestEnableFundingLink=true`. If the applicable policy requires the in-app funding CTA to be absent, build with:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

That switch hides the About funding frame and funding metadata label in the compiled app. It does not remove repository README, SUPPORT, or `.github/FUNDING.yml` metadata. Record the chosen property value in release-build provenance so the packaged artifact can be reproduced consistently.

## Before signing

- Run the complete `docs/RELEASE_CHECKLIST.md` gate.
- Follow `RELEASE_PROCESS.md` and retain the exact candidate/toolchain/test/device/provenance record.
- Review `docs/NEXT_STEPS.md` and reconcile any release-blocking action applicable to the candidate.
- Confirm `THIRD_PARTY_NOTICES.md` against the exact restored packages.
- Confirm the audit status and threat model are current.
- Execute/review the documentation-completeness gate in `../verification/DOCUMENTATION_SUITE_2026_08_12.md` so store/release wording matches the exact shipped candidate.
- Confirm all screenshots/sample vaults contain synthetic data only.
- Confirm the funding CTA build setting matches the verified policy for that exact distribution target.
- Preserve the exact source commit/tag and environment metadata used for the candidate.

## Signing material

Never commit keystores, PFX/P12 files, provisioning profiles containing private material, certificate passwords, notarization credentials, store tokens, or base64-encoded signing blobs. Use protected secret stores with least privilege and rotation procedures.
