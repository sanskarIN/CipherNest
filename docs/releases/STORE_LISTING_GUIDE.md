# Store Listing and Branding Guide

## Positioning

Describe CipherNest as a local-first password, secure-note, credential, and encrypted-document vault. Do not claim that it is unhackable, military-grade, 100% secure, independently audited, or appropriate for high-risk use until evidence supports those statements.

Recommended short description:

> Local-first encrypted password, note, credential, and document vault with no account required.

## Required disclosure points

Store copy and screenshots should accurately state:

- current release works locally and requires no CipherNest account or cloud service;
- forgotten master passphrases cannot be server-recovered;
- optional recovery material must be stored separately;
- biometrics are a convenience unlock and do not replace the master passphrase;
- clipboard clearing and screenshot blocking have platform limitations;
- plaintext export leaves the protected vault boundary;
- independent professional security audit is still outstanding.

## Visual assets

Use the original CipherNest nest/shield geometry from the repository. The icon must remain legible without text at small sizes.

- App/store icon: square source, preserve clear space around the mark, no tiny words.
- Android adaptive foreground: use the standalone foreground mark; keep critical geometry inside the safe zone.
- Android adaptive background: use the documented solid brand background rather than baking a shadow into the foreground.
- Splash: use the mark and product name with `Made by the Sanskar` as secondary branding, never overlaying user content.
- Feature graphic: show the mark, `CipherNest`, and a calm local-first security message. Do not display real passwords, recovery keys, payment-card data, or screenshots containing real personal vaults.
- Light/dark store screenshots: demonstrate onboarding, masked vault items, generator, local audit, encrypted backup, Settings, and the honest security-status surface.

## Privacy screenshots

Use synthetic sample records only. Avoid real email addresses, URLs, server names, card references, Wi-Fi credentials, recovery keys, or imported documents.

## Release asset gate

Before publishing, verify each generated platform asset on a real target or official emulator/simulator. Confirm clipping, safe-zone placement, contrast, transparency, scaling, and store-specific size requirements against the current platform documentation.
