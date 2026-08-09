# Privacy-Safe Diagnostics

CipherNest treats logs and diagnostics as a separate security boundary.

## Never log

- master passphrases;
- recovery keys;
- biometric secondary secrets;
- vault data-encryption keys or derived keys;
- decrypted titles, usernames, passwords, notes, custom fields, tags, or attachment content;
- raw imported CSV rows;
- clipboard values;
- encrypted payload bytes where they are not required for a diagnostic operation.

## Central exception reporting

The application-wide reporter records only a sanitized operation identifier, exception type, HResult, severity, and a fixed statement that exception messages/stacks were omitted. It intentionally does not pass the `Exception` object to the logger because messages, file paths, or stack data can unexpectedly contain sensitive context.

Unhandled AppDomain and unobserved TaskScheduler exceptions are routed through this reporter. Lifecycle preference failures are also reported through the same redacted path before the app falls back to a safe lock/default state.

## Developer diagnostics

Developer options may expose format versions, migration versions, sanitized dependency information, and redacted environment facts. They must never expose decrypted records, passphrases, recovery material, vault keys, clipboard contents, or plaintext attachment buffers.

## Crash services

No third-party crash-reporting or analytics service is enabled in the current release. Adding one requires explicit privacy review, opt-in design where appropriate, a data-flow update to the threat model/privacy notice, and proof that vault secrets cannot be attached to reports.
