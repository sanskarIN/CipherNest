# Authentication and Recovery Localization Verification — 2026-08-19

## Scope

This record covers the August 19, 2026 repository-side migration of CipherNest's initial authentication and vault-onboarding security surfaces into the reviewed neutral-English/Hindi (`hi-IN`) resource system.

The migrated scope includes:

- Unlock page fixed text and accessibility descriptions;
- periodic master-passphrase and biometric status messages;
- biometric operating-system prompt text;
- unlock rate-limit and authentication-failure messages;
- onboarding local-only/recovery warnings;
- one-time recovery-key acknowledgement text;
- master-passphrase setup labels/placeholders;
- password-strength labels used by onboarding;
- vault-creation validation/failure messages.

It is a source/review contract. It is **not** a substitute for exact-head CI, physical biometric testing, assistive-technology validation, or independent professional security review.

## Security meaning translations must preserve

Authentication/recovery translations must continue to communicate all of these boundaries:

1. the current CipherNest release is local-only and does not require an account or cloud service;
2. a recovery key, when enabled, is shown during setup and must be saved separately;
3. CipherNest cannot later retrieve a lost recovery key for the user;
4. losing both the master passphrase and usable recovery material can make the local vault unrecoverable;
5. biometric unlock is convenience authentication and does not replace recovery limitations;
6. configured periodic master-passphrase authentication can disable biometric convenience until the master passphrase is entered again;
7. biometric cancellation/failure must fall back to the master passphrase rather than imply vault corruption;
8. missing or mismatched protected biometric material requires master-passphrase unlock and reconfiguration;
9. repeated failed interactive unlock attempts remain subject to the existing bounded rate limiter;
10. a failed vault creation attempt must never be presented as a successful setup.

A fluent translation that weakens or changes one of these meanings is a security/documentation defect.

## Resource catalogs

The authentication/onboarding resources live in:

- `src/CipherNest.App/Resources/Localization/AppStrings.resx`
- `src/CipherNest.App/Resources/Localization/AppStrings.hi-IN.resx`

The existing global catalog-parity test requires the neutral and Hindi resource sets to remain synchronized.

Dynamic numeric formats must preserve their required placeholders:

- `UnlockRateLimitFormat` → `{0}`;
- `OnboardingMasterTooLongFormat` → `{0:N0}`;
- `OnboardingMasterRequirementsErrorFormat` → `{0:N0}` and `{1:N0}`.

## Unlock integration

`src/CipherNest.App/Views/UnlockPage.xaml` uses the reusable `l10n:Translate` path for:

- logo semantics;
- title/local-only statement;
- biometric action and semantic description;
- master-passphrase/recovery-key fallback text;
- credential placeholder/semantics;
- error semantic description;
- Unlock action;
- recovery/biometric limitation warning.

`src/CipherNest.App/ViewModels/UnlockViewModel.cs` resolves reviewed resources for:

- security-session master requirement;
- rate-limit remaining-time status;
- authentication failure;
- periodic master check due;
- native biometric prompt;
- biometric cancellation/failure;
- protected secondary-secret unavailability;
- biometric/vault data mismatch.

The rate-limit value is formatted with `CultureInfo.CurrentUICulture`.

## Onboarding integration

`src/CipherNest.App/Views/OnboardingPage.xaml` uses reviewed resources for:

- local-vault setup title and local-only statement;
- one-time recovery-key warning and semantics;
- separate-storage acknowledgement;
- recovery-limitation warning;
- master/confirmation labels and placeholders;
- optional recovery-key choice;
- explicit recovery-limit acknowledgement;
- vault-creation error semantics and create action.

`src/CipherNest.App/ViewModels/OnboardingViewModel.cs` resolves reviewed resources for:

- initial password-strength guidance;
- oversized master-passphrase feedback;
- strength labels mapped from the authoritative generator score;
- master-passphrase requirements error;
- existing/unavailable local-vault error;
- unexpected vault-creation failure text.

The authoritative `IPasswordGenerator.Evaluate(...).Score` remains responsible for strength scoring and `CanCreate`; localization changes presentation labels only.

## Automated source guards

### `UnlockLocalizationSourceTests.cs`

Guards:

- Unlock page resource use;
- removal of selected hard-coded English security copy;
- resource-backed ViewModel statuses and biometric prompt;
- current-culture lockout formatting.

### `OnboardingLocalizationSourceTests.cs`

Guards:

- onboarding/recovery XAML resource use;
- resource-backed password-strength and setup-failure text;
- removal of selected previous hard-coded English copy.

### `AuthenticationLocalizationCatalogSourceTests.cs`

Guards:

- neutral/Hindi presence and nonblank values for the authentication/onboarding resource set;
- reviewed Hindi differences from neutral English;
- required numeric formatting placeholders.

### Updated `OnboardingFailureContainmentSourceTests.cs`

The pre-existing fail-safe containment test now verifies the canonical English failure guarantee through the resource catalog while still requiring privacy-safe exception reporting. This avoids forcing a security message to remain hard-coded in the ViewModel merely to satisfy a source test.

## Manual target validation still required

On supported targets, using disposable synthetic vault data:

- exercise English, Hindi, and System language modes;
- reconstruct/navigate pages after language changes and verify the selected catalog is reflected;
- test a new-vault flow with recovery enabled and disabled;
- verify the recovery key is shown once and cleared from owned UI state after continuing;
- test incorrect master/recovery credentials and rate limiting;
- test periodic master-passphrase enforcement;
- on supported Android/iOS/Mac Catalyst devices, test biometric available/unavailable, success, denial, cancellation, lockout, enrollment changes, and protected-secret loss/mismatch;
- confirm master-passphrase fallback remains usable after biometric failure;
- test long Hindi strings with normal/large text and narrow/resizable layouts;
- verify TalkBack, VoiceOver, Narrator, keyboard focus/order, and semantic descriptions where applicable.

Do not place real master passphrases, recovery keys, biometric secrets, or vault content in screenshots, logs, support artifacts, or test evidence.

## Exact-head automated gates

Before calling the resulting release candidate exact-head verified, observe successful configured gates for the frozen SHA:

- core restore/build/test/format;
- Windows default Release;
- Windows funding-disabled Release;
- Android Release;
- iOS simulator Release;
- Mac Catalyst Release;
- CodeQL;
- applicable dependency/security review.

No success result is inferred from source inspection alone.

## Accurate release wording

It is accurate to say the reviewed resource-backed Hindi scope now includes the migrated TOTP workflow plus initial Unlock and vault-onboarding/recovery security surfaces.

Do **not** claim:

- every CipherNest screen is fully translated;
- every already-constructed page live-updates in place after a language change;
- biometrics replace master-passphrase/recovery requirements;
- physical biometric behavior has been validated merely because source tests exist;
- unrecoverable local vaults can be remotely restored;
- CipherNest has completed an independent professional security audit.
