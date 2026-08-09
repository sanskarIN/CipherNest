# Terms and Important Notices

CipherNest is provided under GPL-3.0-or-later without warranty. It is security-sensitive software that has not yet completed an independent professional security audit.

## Local-only recovery

You are responsible for maintaining your master passphrase, any optional recovery key, and tested encrypted backups. CipherNest does not operate a server-side password reset or recovery copy in the current release. If the master passphrase and all usable recovery material are lost, the project cannot decrypt the local vault for you.

Do not rely on CipherNest as the sole copy of critical information until you have created and successfully tested encrypted backup/restore using disposable or safely duplicated data.

## Security limitations

Open source does not itself guarantee security. CipherNest cannot guarantee protection on a rooted, jailbroken, malware-compromised, or otherwise privileged-controlled device. It cannot guarantee deterministic erasure of managed-runtime strings, flash-storage remnants, filesystem snapshots, clipboard history, operating-system share caches, or copies retained by another application.

Do not describe or rely on the current release as unhackable, military-grade, 100% secure, or independently audited.

## Plaintext export

Plaintext CSV or attachment export leaves CipherNest's encrypted storage boundary. Once plaintext is shared, saved, indexed, backed up, photographed, copied, or opened by another application, CipherNest cannot control later retention or disclosure. Use encrypted backup when interoperability does not require plaintext.

## Platform services

Biometric authentication, secure storage, file pickers, clipboard behavior, screenshot controls, app lifecycle delivery, and sharing are provided partly by the operating system and can vary by platform/version/device. Unsupported behavior uses documented fallback paths, but final platform validation remains a release requirement.

## Updates

Security-sensitive formats, dependencies, platform APIs, and threat assumptions can change. Review `CHANGELOG.md`, `SECURITY.md`, `PRIVACY.md`, the threat model, and release notes before upgrading a high-value vault.
