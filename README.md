# CipherNest

CipherNest is a local-first, open-source password, secure-note, identity, credential, and encrypted-document vault built with .NET MAUI and C#.

> **Security status:** CipherNest has not yet undergone an independent professional security audit. It uses established primitives and a deliberately small security-sensitive core, but must not be described as “unhackable”, “military-grade”, or “100% secure”.

## Current release

- No account, email, phone number, server, or cloud service is required.
- Vault records and attachments are encrypted locally.
- The master passphrase is never stored.
- A random vault data-encryption key is wrapped by a key derived with Argon2id.
- AES-256-GCM is used for authenticated encryption with unique nonces.
- Search and password audits run only over decrypted in-memory data while unlocked.
- Encrypted backup/restore is supported; plaintext export is deliberately excluded from the first hardened release.
- No analytics or telemetry are enabled.

## Build

Requirements: a current .NET 10 SDK with the .NET MAUI workload and platform SDKs for the desired target.

```bash
dotnet workload restore
dotnet restore CipherNest.slnx
dotnet build CipherNest.slnx -c Release
dotnet test CipherNest.slnx -c Release
```

See `docs/setup/BUILD.md` and `docs/TROUBLESHOOTING.md` for platform details.

## Repository

Source: https://github.com/sanskarIN/CipherNest  
Creator: https://www.github.com/sanskarIN  
Business: sanskarin@outlook.in  
Support: supportramsandesh@gmail.com

Made by the Sanskar

## License

GPL-3.0-or-later. See `LICENSE`.
