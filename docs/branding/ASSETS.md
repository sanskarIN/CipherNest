# CipherNest Branding Assets

CipherNest uses an original geometric nest/shield/safe-door motif designed for clarity at small sizes and a calm privacy-oriented tone. Do not replace it with copied lock-brand imagery.

## Source assets

The editable/vector-friendly sources live under the MAUI project resources:

- `src/CipherNest.App/Resources/AppIcon/appicon.svg` — primary icon/base mark;
- `src/CipherNest.App/Resources/AppIcon/appiconfg.svg` — adaptive foreground mark;
- `src/CipherNest.App/Resources/Splash/splash.svg` — splash vector;
- `src/CipherNest.App/Resources/Images/ciphernest_logo.svg` — in-app logo/wordmark surface.

.NET MAUI generates platform-specific raster/icon assets from these sources during target builds where configured by the project file.

## Rules

- Keep the mark recognizable without text at favicon/small-icon scale.
- Keep critical geometry inside adaptive-icon safe areas.
- Never put passwords, recovery keys, payment-card data, or real user content into brand/store imagery.
- `Made by the Sanskar` may appear on the splash/About/branding surfaces but must not overlay user vault content.
- Preserve light/dark contrast and test system surfaces that mask/crop icons.
- Do not add unverified security claims such as “unhackable”, “military-grade”, “100% secure”, or “audited”.

## Platform generation

1. Edit the SVG source rather than generated raster outputs.
2. Build each MAUI target so its normal asset pipeline regenerates sizes.
3. Inspect Android adaptive foreground/background safe-zone behavior.
4. Inspect iOS/Mac icon rendering and required opaque-background behavior for store assets.
5. Inspect Windows tile/app-list/taskbar sizes.
6. Generate store listing/feature graphics separately according to current store requirements; see `docs/releases/STORE_LISTING_GUIDE.md`.
7. Keep signing/store credentials unrelated to asset generation and outside the repository.

## Monochrome use

When a system surface requires a monochrome mark, derive it from the same nest/shield geometry using a single filled silhouette. Do not embed tiny text or rely on gradients for recognizability.

## Web/favicons

The current project has no web/admin companion, so a favicon package is not shipped. If a web surface is added later, derive the favicon from the same vector mark and document the generated sizes here.
