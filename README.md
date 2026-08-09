# CipherNest

CipherNest is a local-first, open-source password, secure-note, identity, credential, and encrypted-document vault built with .NET MAUI and C#.

> **Security status:** CipherNest has not yet undergone an independent professional security audit. It uses established primitives and a deliberately small security-sensitive core, but must not be described as “unhackable”, “military-grade”, or “100% secure”.

## Current release

- No account, email, phone number, application server, or cloud synchronization is required.
- Vault records and attachments are encrypted locally; searchable item fields are not stored in plaintext SQL indexes.
- The master passphrase is never stored. A random 256-bit vault data-encryption key is wrapped with an Argon2id-derived key.
- AES-256-GCM authenticates encrypted records, wrapped keys, backups, and attachment chunks using unique nonces and contextual associated data.
- Optional one-time recovery material provides an independent wrapped-key path and must be retained separately by the user.
- Optional Android/iOS/Mac Catalyst biometric convenience unlock uses a separately generated random secondary secret protected by platform secure storage; Windows currently falls back to the master passphrase.
- Master-passphrase re-authentication is required periodically after biometric use and for security-sensitive actions such as plaintext export, biometric configuration, manual permanent deletion, and vault deletion. Changing the master passphrase ends the current security session and requires the new passphrase before biometric convenience unlock can resume.
- Repeated interactive unlock failures use a bounded exponential delay. This is a client-side control and is not claimed to stop offline guessing against a copied database.
- Local search, favorites, collections, item-type filters, review reminders, recent-use sorting, and weak/reused/duplicate-secret audit operate only over decrypted data while unlocked. Large matching result sets render incrementally in 50-item pages.
- Trash has configurable retention; routine vault maintenance removes expired encrypted trash records. Manual permanent deletion and empty-trash actions require current-master re-authentication plus explicit confirmation.
- Password generation uses `RandomNumberGenerator`; memorable passphrases use a validated 256-word local list with explicit random-selection entropy guidance and configurable defaults.
- Secure notes support a deliberately small Markdown-like subset plus checklists; raw HTML is not rendered.
- Attachments are encrypted in bounded streaming chunks. Small UTF-8 text-family attachments can be previewed in memory; other formats require explicit plaintext export.
- Encrypted backup/restore includes encrypted attachments and is the recommended transfer path.
- Generic CSV import and deliberately guarded plaintext CSV/attachment export are available for interoperability; warnings explain that operating systems and destination apps can retain plaintext copies.
- Username, primary-secret, and secret custom-field clipboard writes require explicit copy actions. Timed clearing preserves unrelated newer clipboard content, and manual/background/timeout vault locks also attempt immediate clipboard cleanup where the platform permits it.
- Sensitive credential/decrypted ViewModel fields are cleared when sensitive pages disappear, while .NET managed-memory limitations are documented rather than hidden.
- Settings include theme, larger-interface/reduced-motion preferences, local reminder controls, biometric configuration, generator defaults, storage/cache inspection, security information, backup/restore, import/export, and destructive local-vault deletion.
- English resources ship first with a persisted System/English language preference and resource-backed architecture ready for additional culture catalogs.
- Central exception reporting intentionally omits exception messages/stacks and vault content. No third-party analytics or crash-reporting service is enabled.
- Original vector branding includes launcher/adaptive sources, a splash wordmark with `Made by the Sanskar`, a monochrome source, and a dark-surface logo variant.

## Build

Requirements: a current .NET 10 SDK with the .NET MAUI workload and platform SDKs for the desired target.

```bash
dotnet workload restore
dotnet restore CipherNest.slnx
dotnet build CipherNest.slnx -c Release
dotnet test CipherNest.slnx -c Release
```

Platform packaging and signing require target SDKs/identities that are deliberately kept outside this repository. See `docs/setup/BUILD.md`, `docs/TROUBLESHOOTING.md`, `docs/TEST_PLAN.md`, `docs/RELEASE_CHECKLIST.md`, `docs/NEXT_STEPS.md`, `docs/security/THREAT_MODEL.md`, `docs/security/BIOMETRIC_UNLOCK.md`, `docs/security/SECURE_NOTES.md`, and `docs/security/PASSPHRASE_GENERATOR.md`.

## Repository

Source: https://github.com/sanskarIN/CipherNest  
Creator: https://www.github.com/sanskarIN  
Business: sanskarin@outlook.in  
Support: supportramsandesh@gmail.com  
Support development: https://buymeacoffee.com/sanskarIN

Made by the Sanskar

## License

CipherNest is licensed under GPL-3.0-or-later. See `LICENSE`. Third-party dependencies retain their own licenses; see `THIRD_PARTY_NOTICES.md`.
