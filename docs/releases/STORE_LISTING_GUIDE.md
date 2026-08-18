# Store Listing and Branding Guide

Store metadata must be reviewed against the exact packaged candidate. Use `RELEASE_PROCESS.md`, `../verification/DOCUMENTATION_SUITE_2026_08_12.md`, the August 18 TOTP interoperability verification record, and the canonical documentation hub `../README.md` before publishing claims/screenshots.

## Positioning

Describe CipherNest as a local-first password, secure-note, credential, TOTP, and encrypted-document vault. Do not claim that it is unhackable, military-grade, 100% secure, independently audited, or appropriate for high-risk use until evidence supports those statements.

Recommended short description:

> Local-first encrypted password, note, credential, TOTP, and document vault with no account required.

## Required disclosure points

Store copy and screenshots should accurately state:

- current release works locally and requires no CipherNest account or cloud service;
- forgotten master passphrases cannot be server-recovered;
- optional recovery material must be stored separately;
- biometrics are a convenience unlock and do not replace the master passphrase;
- clipboard clearing and screenshot blocking have platform limitations;
- plaintext export leaves the protected vault boundary;
- TOTP setup URIs contain the long-lived seed and therefore require the same secret-handling care as the seed itself;
- independent professional security audit is still outstanding.

Store wording should remain consistent with `../USER_GUIDE.md`, `../security/THREAT_MODEL.md`, `../security/SESSION_SECURITY.md`, `../security/DATA_LIFECYCLE.md`, `../security/TOTP.md`, and `../../PRIVACY.md` for the exact candidate.

## Project support link

The open-source project support URL is `https://buymeacoffee.com/sanskarIN`. Repository surfaces may present it as optional voluntary support for continued development.

Before a store-distributed build exposes an external funding/payment call to action, verify the current policy for that exact store, distribution method, country/region, and app category. Store policies change independently of the source tree and are not treated as verified by this repository. If the current policy does not permit the in-app link or wording, omit/disable that call to action in the affected store build while retaining the repository/GitHub funding metadata. Never imply that financial support changes feature access, privacy/security handling, support priority, GPL rights, or recovery capability.

The MAUI app supports a build-time switch so a store package can omit the in-app funding frame and funding metadata label without editing source:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

`CipherNestEnableFundingLink` defaults to `true`. Setting it explicitly to `false` defines `CIPHERNEST_DISABLE_FUNDING_LINK`; `BuildFeatureFlags.IsFundingLinkEnabled` becomes `false`, and the guarded in-app funding surfaces are hidden. The repository README, SUPPORT file, and `.github/FUNDING.yml` are source-repository metadata and remain unchanged by that app-build switch.

Record the chosen build value in release provenance.

## Visual assets

Use the original CipherNest nest/shield geometry from the repository. The icon must remain legible without text at small sizes. See `../branding/ASSETS.md` for canonical editable sources and generation rules.

- App/store icon: square source, preserve clear space around the mark, no tiny words.
- Android adaptive foreground: use the standalone foreground mark; keep critical geometry inside the safe zone.
- Android adaptive background: use the documented solid brand background rather than baking a shadow into the foreground.
- Splash: use the mark and product name with `Made by the Sanskar` as secondary branding, never overlaying user content.
- Feature graphic: show the mark, `CipherNest`, and a calm local-first security message. Do not display real passwords, recovery keys, payment-card data, or screenshots containing real personal vaults.
- Light/dark store screenshots: demonstrate onboarding, masked vault items, generator, local audit, encrypted backup, Settings, the TOTP text-URI workflow where appropriate, and the honest security-status surface.

## Privacy screenshots

Use synthetic sample records only. Avoid real email addresses, URLs, server names, card references, Wi-Fi credentials, recovery keys, TOTP seeds/codes/setup URIs, imported documents, private attachments, or diagnostic paths.

If a screenshot shows a plaintext-export, setup-URI warning, or destructive warning, keep the warning meaningful rather than cropping it to make the listing more visually attractive.

## Release asset and claims gate

Before publishing:

- verify each generated platform asset on a real target or official emulator/simulator;
- confirm clipping, safe-zone placement, contrast, transparency, scaling, and store-specific size requirements against current platform documentation;
- verify every feature shown in screenshots exists in the packaged candidate;
- verify every privacy/security claim matches current docs/source and actual device behavior;
- verify the independent-audit status remains accurate;
- verify bounded text-only TOTP setup-URI import/copy with synthetic seeds on representative compatible authenticator applications before making interoperability claims beyond repository behavior;
- verify no deferred feature (cloud sync, browser/app autofill, TOTP QR/camera enrollment, HOTP interoperability, automatic provider enrollment, Windows Hello, rich PDF scanning, complete migration of all UI literals into additional-language catalogs, etc.) is implied to be complete;
- complete the relevant `../RELEASE_CHECKLIST.md` and `RELEASE_PROCESS.md` evidence.

## TOTP and language listing claims

Accurate current wording may state that CipherNest can:

- store a TOTP seed inside the encrypted local vault;
- generate RFC 6238-style time-based one-time codes locally while unlocked;
- import bounded TOTP-only `otpauth://totp/...` text setup URIs locally;
- format/copy the current TOTP item as a canonical setup URI through the existing timed secret-clipboard policy.

Do not imply that CipherNest:

- universally interoperates with every authenticator/provider without representative target validation;
- scans or renders TOTP QR codes;
- supports HOTP/counter enrollment;
- automatically enrolls with providers;
- autofills provider codes;
- erases operating-system clipboard history/synchronization copies;
- isolates a TOTP seed from compromise of the same unlocked vault;
- has independently audited TOTP security.

Language/store metadata may state that reviewed Hindi resources are available for the resource-backed interface. Do not market the whole UI as fully translated while not-yet-migrated literals can still fall back/remain in English.
