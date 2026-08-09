# Biometric Unlock Design

CipherNest treats biometric unlock as an optional convenience path. It does not replace the master passphrase, recovery key, or the requirement to re-enter the master passphrase for security-sensitive changes.

## Design

1. The vault data-encryption key (DEK) remains random and unchanged.
2. Enabling biometrics requires an unlocked vault and successful master-passphrase re-authentication.
3. CipherNest asks the operating system to perform biometric authentication.
4. CipherNest generates a random 384-bit secondary secret using `RandomNumberGenerator`.
5. The secondary secret is stored using MAUI `SecureStorage` and is never written to the SQLite vault database.
6. A new authenticated wrapped-key envelope is produced by wrapping the same DEK with a key derived from the secondary secret.
7. The wrapped envelope is stored in the versioned vault header.
8. During biometric unlock, CipherNest first asks the operating system to authenticate the user, then retrieves the secondary secret from OS secure storage and unwraps the DEK.
9. Disabling biometric unlock requires the current master passphrase, removes the secondary wrapper, then removes the secure-storage value.

The master passphrase itself is never stored for biometric unlock.

## Platform support

- Android: native `BiometricPrompt` is used on Android 9 / API 28 and newer when the device reports biometric authentication availability. Android 8.x falls back to master-passphrase unlock.
- iOS and Mac Catalyst: `LocalAuthentication.LAContext` with `DeviceOwnerAuthenticationWithBiometrics` is used when available.
- Windows: biometric unlock is currently disabled until a Windows Hello implementation can be tested and reviewed. Master-passphrase unlock remains available.

## Important limitations

- `SecureStorage` protects the secondary secret using platform facilities, but the present design does not cryptographically bind every retrieval of that secret to a hardware-backed biometric key operation. The application performs an OS biometric prompt immediately before retrieval. This distinction must be preserved in security claims.
- Rooted/jailbroken devices, compromised operating systems, injected processes, debuggers with sufficient privilege, and memory scraping can bypass assumptions made by the application.
- Managed-runtime memory cannot be guaranteed to be wiped perfectly. CipherNest clears mutable byte buffers where practical and avoids intentionally persisting decrypted keys.
- Changing or removing device biometrics may invalidate or change platform behavior. If the stored secondary secret becomes unavailable or does not authenticate the current vault, CipherNest deletes the stale secure-storage entry and requires the master passphrase.
- A restored vault backup can contain an older secondary wrapped-key envelope while the OS secure-storage entry belongs to another installation. The mismatch is expected to fail authentication; the master passphrase remains the recovery path.
- Recovery keys do not authorize enabling/disabling biometrics or other master-passphrase-only security settings.

## Review requirements

Before describing biometric unlock as production-hardened, validate on physical Android, iPhone/iPad, and Mac hardware; test biometric enrollment changes; test app reinstall/restore behavior; verify platform backup exclusions/secure-storage migration semantics; and obtain independent review of the unlock architecture.
