# Localization Architecture

CipherNest ships English first while keeping a resource-backed language preference model ready for additional translations.

## Current structure

- `AppLanguagePreference` persists `System` or `English`.
- `Resources/Localization/AppStrings.resx` is the neutral English catalog.
- `LocalizationService` owns UI-culture selection and `ResourceManager` lookup.
- The saved preference is applied at startup/resume and when the user changes it in Settings.
- Missing translations fall back to neutral English resources.

## Adding Hindi or another language

1. Add a culture-specific resource catalog such as `AppStrings.hi.resx` using the same keys.
2. Extend `AppLanguagePreference` with the supported language.
3. Map that preference to an explicit `CultureInfo` in `LocalizationService`.
4. Move remaining literal UI copy to resource-backed bindings as the screen is localized.
5. Test long strings, pluralization, right-to-left behavior if relevant, screen-reader pronunciation, keyboard navigation, and layout at large text sizes.
6. Keep security warnings semantically equivalent; translations must not weaken recovery, export, audit, or platform-limit wording.

The current release does not claim that all UI strings are translated. The architecture and persisted preference are intentionally prepared for staged localization without coupling language selection to vault data or cryptographic formats.
