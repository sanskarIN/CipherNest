# Support

Support email: `supportramsandesh@gmail.com`  
Business/project email: `sanskarin@outlook.in`

## ☕ Support CipherNest development

[![BMC — Support CipherNest](src/CipherNest.App/Resources/Images/bmc_support.svg)](https://buymeacoffee.com/sanskarIN)

**Buy Me a Coffee:** https://buymeacoffee.com/sanskarIN

Financial support is voluntary and does not change feature access, security handling, support priority, licensing, recovery behavior, privacy treatment, or open-source rights. The in-app funding surface can be disabled for a distribution build without removing repository funding metadata.

## Before requesting help

Review:

- `docs/USER_GUIDE.md` for normal vault/backup/restore/transfer/settings workflows;
- `docs/TROUBLESHOOTING.md` for build/runtime troubleshooting;
- `docs/operations/BACKUP_RECOVERY_RUNBOOK.md` for safe backup/restore validation and failure guidance;
- `docs/README.md` for the complete documentation index.

Include only non-sensitive information such as:

- CipherNest app version/build or source commit if known;
- platform and OS version;
- the fixed/redacted error text shown by the app;
- synthetic reproduction steps;
- whether the current vault still unlocks;
- whether a separately verified encrypted backup exists;
- approximate non-sensitive file sizes/counts when relevant.

## Never send support secrets

Do not send:

- vault contents or a real vault database;
- master passphrase;
- backup passphrase;
- recovery material;
- biometric secondary secret;
- cryptographic keys;
- decrypted backup contents;
- private attachments/documents;
- plaintext CSV containing real credentials;
- signing keys/certificates/store tokens.

Use synthetic examples when reproducing a bug.

## Security vulnerabilities

For exploitable/security-sensitive findings, follow `SECURITY.md` rather than publishing exploit details in a public issue. Maintainers use `docs/operations/SECURITY_RESPONSE.md` for response handling.

## Recovery limitation

CipherNest is local-first and has no server-held master key/password-reset mechanism. Support cannot recover a vault when all usable master/recovery key paths are lost. Encrypted backups also require their backup passphrase plus usable vault credentials for the restored snapshot.
