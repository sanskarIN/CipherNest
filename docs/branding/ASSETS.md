# CipherNest Branding Assets

CipherNest uses an original geometric nest/shield/safe-door motif designed for clarity at small sizes and a calm privacy-oriented tone. Do not replace it with copied lock-brand imagery.

## Source assets

The editable/vector-friendly sources live under the MAUI project resources:

- `src/CipherNest.App/Resources/AppIcon/appicon.svg` — primary icon/base mark;
- `src/CipherNest.App/Resources/AppIcon/appiconfg.svg` — adaptive foreground mark;
- `src/CipherNest.App/Resources/AppIcon/appicon-mono.svg` — monochrome system-surface source derived from the same geometry;
- `src/CipherNest.App/Resources/Splash/splash.svg` — splash vector with the CipherNest wordmark and `Made by the Sanskar` creator credit;
- `src/CipherNest.App/Resources/Images/ciphernest_logo.svg` — primary in-app logo source;
- `src/CipherNest.App/Resources/Images/ciphernest_logo_dark.svg` — higher-contrast dark-surface logo source.

.NET MAUI generates configured platform-specific icon/splash assets from the project resources during target builds. Store-delivery icon sets still require inspection against the current Android, Apple, and Windows packaging rules before release.

## Rules

- Keep the mark recognizable without text at favicon/small-icon scale.
- Keep critical geometry inside adaptive-icon safe areas.
- Never put passwords, recovery keys, payment-card data, or real user content into brand/store imagery.
- `Made by the Sanskar` belongs on splash/About/branding surfaces and must not overlay user vault content.
- The splash may carry the product wordmark/creator credit, while the launcher icon itself should remain a simple text-free mark.
- Preserve light/dark contrast and test system surfaces that mask/crop icons.
- Do not add unverified security claims such as “unhackable”, “military-grade”, “100% secure”, or “audited”.

## Platform generation and inspection

1. Edit the committed SVG sources rather than generated raster outputs.
2. Build each MAUI target so its normal asset pipeline regenerates configured sizes.
3. Inspect Android adaptive foreground/background safe-zone behavior and monochrome/themed-icon behavior on supported Android versions.
4. Inspect iOS/Mac icon rendering, opaque-background requirements, small-size legibility, and store asset acceptance.
5. Inspect Windows app-list/taskbar/tile sizes and high-DPI scaling.
6. Inspect the splash on small/large phones, tablets/foldables, desktop windows, light/dark system states, and localized startup environments where applicable.
7. Generate store listing/feature graphics separately according to current store requirements; see `docs/releases/STORE_LISTING_GUIDE.md`.
8. Keep signing/store credentials unrelated to asset generation and outside the repository.

## Monochrome use

`appicon-mono.svg` is the committed single-color source for system surfaces that request a monochrome mark. Keep it derived from the same nest/shield silhouette; do not embed tiny text, gradients, or user data. Platform packaging may require converting or wiring this source into target-specific metadata, which must be validated during the release build rather than assumed from source presence.

## Light and dark variants

The primary logo uses the normal CipherNest teal/light motif. `ciphernest_logo_dark.svg` provides a dark-surface variant with increased contrast. These are source assets, not proof that every target automatically chooses the right variant; target pages/store graphics must be visually checked during release testing.

## Web/favicons

The current project has no web/admin companion, so a favicon package is not shipped. If a web surface is added later, derive the favicon from the same vector mark and document the generated sizes here.
