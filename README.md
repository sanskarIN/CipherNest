# CipherNest

CipherNest is a local-first, open-source password, secure-note, identity, credential, and encrypted-document vault built with .NET MAUI and C#.

> **Security status:** CipherNest has not yet undergone an independent professional security audit. It uses established primitives and a deliberately small security-sensitive core, but must not be described as “unhackable”, “military-grade”, or “100% secure”.

## Current release

- No account, email, phone number, application server, or cloud synchronization is required.
- Vault records and attachments are encrypted locally; searchable item fields are not stored in plaintext SQL indexes.
- The master passphrase is never stored. A random 256-bit vault data-encryption key is wrapped with an Argon2id-derived key.
- AES-256-GCM authenticates encrypted records, wrapped keys, backups, and attachment chunks using unique nonces and contextual associated data.
- Optional one-time recovery material provides an independent wrapped-key path and must be retained separately by the user.
- Optional Android/iOS/Mac Catalyst biometric convenience unlock uses a separately generated random secondary secret protected by platform secure storage; Windows currently falls back to the master passphrase. Android uses the API-28 `BiometricPrompt` baseline and Apple request cancellation invalidates its native authentication context.
- Master-passphrase re-authentication is required periodically after biometric use and for security-sensitive actions such as plaintext export, biometric configuration, manual permanent deletion, and vault deletion. Changing the master passphrase ends the current security session and requires the new passphrase before biometric convenience unlock can resume.
- Repeated interactive unlock failures use a bounded exponential delay. This is a client-side control and is not claimed to stop offline guessing against a copied database.
- Local search, favorites, collections, item-type filters, review reminders, recent-use sorting, and weak/reused/duplicate-secret audit operate only over decrypted data while unlocked. Large matching result sets render incrementally in 50-item pages.
- Trash has configurable retention; routine vault maintenance removes expired encrypted trash records. Manual permanent deletion and empty-trash actions require current-master re-authentication plus explicit confirmation.
- Password generation uses `RandomNumberGenerator`; memorable passphrases use a validated 256-word local list with explicit random-selection entropy guidance and configurable defaults.
- Secure notes support a deliberately small Markdown-like subset plus checklists; raw HTML is not rendered.
- Attachments are encrypted in bounded streaming chunks. Small UTF-8 text-family attachments can be previewed in memory; other formats require explicit plaintext export. Temporary decrypted export names include a random component and cleanup failures are reported without displaying the path.
- Encrypted backup/restore includes encrypted attachments and is the recommended transfer path.
- Generic CSV import and deliberately guarded plaintext CSV/attachment export are available for interoperability; warnings explain that operating systems and destination apps can retain plaintext copies.
- Username, primary-secret, and secret custom-field clipboard writes require explicit copy actions. Delayed cleanup retains only a SHA-256 fingerprint rather than the copied plaintext secret, uses fixed-time matching, and preserves unrelated newer clipboard content. Manual/background/timeout locks use the same conditional cleanup policy where the platform permits it.
- Sensitive credential/decrypted ViewModel fields are cleared when sensitive pages disappear. Bound passphrase fields are also cleared before longer authentication/file/share operations where practical, while .NET managed-memory limitations are documented rather than hidden.
- Lifecycle fallback separately contains and privacy-safe reports lock/clipboard cleanup failures so a second cleanup exception is not allowed to escape the native lifecycle callback.
- Sensitive Settings, transfer, backup, restore, item-open, and attachment file failures use fixed user-facing text plus redacted diagnostic events instead of directly surfacing exception messages that can contain paths/context.
- Settings include theme, larger-interface/reduced-motion preferences, local reminder controls, biometric configuration, generator defaults, storage/cache inspection, security information, backup/restore, import/export, and destructive local-vault deletion.
- English resources ship first with a persisted System/English language preference and resource-backed architecture ready for additional culture catalogs.
- Central exception reporting intentionally omits exception messages/stacks and vault content. No third-party analytics or crash-reporting service is enabled.
- Original vector branding includes launcher/adaptive sources, a splash wordmark with `Made by the Sanskar`, a monochrome source, and a dark-surface logo variant.

## Verification and build

Requirements: a current .NET 10 SDK with the .NET MAUI workload and platform SDKs for the desired target.

Committed verification entry points:

- `scripts/verify-core.ps1` or `scripts/verify-core.sh`
- `scripts/verify-windows.ps1`
- `scripts/verify-android.sh`
- `scripts/verify-apple.sh`

GitHub CI is configured for core restore/build/test/format gates plus Windows, Android, iOS, and Mac Catalyst Release compilation. Windows CI also compiles the funding-disabled variant. CodeQL builds the MAUI Android application path in addition to core/integration code. These configured gates are not a claim that the current head passed until the exact checks execute successfully.

The optional in-app Buy Me a Coffee surface is enabled by default. A distribution build that must omit the external funding CTA can use:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

Verify the current policy for the exact target store/region before choosing that setting. The build switch affects the app UI only; repository funding metadata remains available separately.

Platform packaging and signing require target SDKs/identities that are deliberately kept outside this repository. See `docs/setup/BUILD.md`, `docs/verification/CI_GATES.md`, `docs/TROUBLESHOOTING.md`, `docs/TEST_PLAN.md`, `docs/RELEASE_CHECKLIST.md`, `docs/NEXT_STEPS.md`, `docs/security/THREAT_MODEL.md`, `docs/security/BIOMETRIC_UNLOCK.md`, `docs/security/SECURE_NOTES.md`, and `docs/security/PASSPHRASE_GENERATOR.md`.

## Repository

Source: https://github.com/sanskarIN/CipherNest  
Creator: https://www.github.com/sanskarIN  
Business: sanskarin@outlook.in  
Support: supportramsandesh@gmail.com  
Support development: [https://buymeacoffee.com/sanskarIN](https://buymeacoffee.com/sanskarIN)

Made by the Sanskar

## License

CipherNest is licensed under GPL-3.0-or-later. See `LICENSE`. Third-party dependencies retain their own licenses; see `THIRD_PARTY_NOTICES.md`.
