# Troubleshooting

## MAUI workload missing
Run `dotnet workload restore`. Confirm the selected SDK matches `global.json` or change the roll-forward policy only after validating the build.

## Android SDK/JDK errors
Use the Android SDK/JDK versions supported by the installed MAUI workload and verify environment paths in the IDE.

## Apple targets fail on Windows/Linux
Build iOS/MacCatalyst on a supported Apple build host with Xcode.

## Vault does not unlock
Confirm the exact master passphrase. Do not repeatedly guess if you have recovery material available. The application cannot recover a forgotten passphrase in the local-only release.

## Backup restore fails
Do not modify the backup. Authentication failure, unsupported format, corruption, or a wrong backup passphrase intentionally aborts restore.

Never send real vaults, passphrases, or keys to support.
