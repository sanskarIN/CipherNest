# About Security/Privacy Localization Verification — 2026-08-19

## Scope

This record covers the reviewed localization migration of the security-status and privacy/terms claims on CipherNest's About page.

The purpose is deliberately narrow: security and privacy claims that can affect user understanding are resource-backed in neutral English and Hindi (`hi-IN`) without changing the support, licensing, dependency, repository, creator, or funding metadata around them.

## Migrated claims

`src/CipherNest.App/Views/AboutPage.xaml` now resolves reviewed resources for:

- CipherNest logo semantic description;
- Security status heading;
- the statement that established cryptographic primitives/local-only storage do not equal an independent professional security audit;
- the explicit reminder that open source does not itself guarantee security;
- the security/privacy information action label;
- Privacy & terms heading;
- the local-only/no-intentional-telemetry statement;
- the warning that user-initiated plaintext export leaves the protected vault boundary.

## Security meaning translations must preserve

Translations must not weaken these statements:

1. the current release has **not** completed an independent professional security audit;
2. open-source availability does not by itself guarantee security;
3. the current release is local-only in ordinary operation;
4. CipherNest does not intentionally send vault contents, analytics, or telemetry to a CipherNest service in this release;
5. a user-initiated plaintext export leaves the encrypted/protected vault boundary;
6. full privacy/terms notices remain authoritative repository documents.

Do not rewrite those claims into absolute guarantees such as “unhackable,” “100% secure,” “zero risk,” “audited,” or “no data can ever leave the device.”

## Automated source guard

`tests/CipherNest.UiTests/AboutSecurityLocalizationSourceTests.cs` verifies:

- the About page uses the localization extension for the selected security/privacy claims;
- selected prior hard-coded English claims are absent from the XAML;
- neutral/Hindi resource values exist and differ;
- the canonical English audit-status and plaintext-export meanings remain present.

The global neutral/Hindi catalog-parity test remains an additional guard.

## Manual validation still required

On supported targets:

- verify English, Hindi, and System modes;
- navigate away/back after a language change so construction-time XAML resources are rebuilt;
- test normal and large text sizes;
- inspect wrapping on narrow phone and resizable desktop layouts;
- verify screen-reader order and pronunciation;
- verify the security/privacy action opens the intended local documentation surface;
- verify the visible Buy Me a Coffee support area and URL remain unchanged by this migration;
- confirm support language remains voluntary and does not imply paid security, recovery, feature, or support-priority benefits.

## Release claims

This migration improves consistency of reviewed security/privacy wording. It does **not** constitute a security audit, privacy certification, external penetration test, store review, or physical-device validation.
