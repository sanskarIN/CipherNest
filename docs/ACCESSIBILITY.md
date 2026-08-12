# CipherNest Accessibility Guide

CipherNest is intended to remain usable with larger text, screen readers, keyboard navigation, narrow windows, and reduced-motion preferences while preserving the meaning of security warnings and secret-masking behavior.

This document records current source support and required target-platform validation. Source structure alone is not a claim that every assistive-technology combination has been fully tested.

## 1. Current dynamic typography resources

`Resources/Styles/Styles.xaml` defines dynamic font resources:

```text
BodyFontSize    = 15
CaptionFontSize = 14
TitleFontSize   = 30
ControlFontSize = 15
```

`AccessibilityPreferenceApplicator` changes them when Larger Interface is enabled:

```text
BodyFontSize    = 18
CaptionFontSize = 17
TitleFontSize   = 34
ControlFontSize = 18
```

Labels, entries, editors, pickers, search bars, and buttons use these resources where configured.

## 2. Larger Interface preference

`AppPreferences.LargerInterface` defaults to `false`.

At startup/resume/settings application, `AccessibilityPreferenceApplicator.Apply` updates dynamic resources rather than changing encrypted vault formats or item data.

Larger Interface must be tested together with OS-level text scaling. The app preference is not intended to disable/resist system accessibility scaling.

## 3. Reduced Motion preference

`AppPreferences.ReducedMotion` defaults to `false`.

The applicator publishes:

```text
ReducedMotionEnabled
```

as an application resource for motion-consuming UI.

Current/future animations should consult this preference and avoid introducing essential information that is conveyed only by motion.

## 4. Minimum button height

The default Button style uses:

```text
MinimumHeightRequest = 44 DIP
```

This provides a baseline touch-target intent. Actual effective target size still depends on layout, platform rendering, screen density, neighboring controls, and future custom styles.

New controls should preserve at least equivalent practical target usability and spacing.

## 5. Themes and contrast

The app supports:

- System theme;
- Light theme;
- Dark theme.

Core styles use `AppThemeBinding` for surfaces/text/card colors. Every new view should be checked in both light and dark modes rather than assuming a hard-coded foreground/background works everywhere.

Do not communicate critical state using color alone. Security/error status should have readable text and, where appropriate, semantic announcements.

## 6. Semantic metadata

Views should provide MAUI semantic descriptions/hints/live-region behavior for controls/state where visible text alone is insufficient.

Security-sensitive rule:

- semantic descriptions must not reveal a masked secret merely to make the UI more descriptive;
- screen-reader text for password/secret fields should preserve masking expectations;
- error/state announcements should avoid reading secret values back to the user unexpectedly.

## 7. Live regions and changing security state

Where important status changes dynamically, such as validation errors or security actions, use accessible announcement/live-region behavior where practical.

Examples of state that may merit announcement:

- vault locked/unlocked;
- authentication failed;
- destructive action rejected/cancelled;
- backup/restore status;
- plaintext cleanup warning;
- item save/validation failure.

Do not announce passphrases, recovery keys, secrets, decrypted attachment contents, or clipboard values automatically.

## 8. Keyboard navigation

Desktop targets (Windows/Mac Catalyst) require keyboard-only testing.

Check:

- logical focus order;
- all actions reachable without pointer/touch;
- visible focus indication;
- Enter/Space behavior for buttons;
- text-field tab order;
- dialogs/alerts returning focus sensibly;
- no keyboard trap inside scrollable/filter/editor surfaces;
- protected secret reveal/copy remains deliberate.

Source presence of controls is not proof of good keyboard runtime behavior.

## 9. Screen-reader matrix

Before a release candidate, test relevant current platforms with:

- Android TalkBack;
- iOS VoiceOver;
- macOS VoiceOver for Mac Catalyst where applicable;
- Windows Narrator.

At minimum walk through:

1. onboarding;
2. unlock;
3. vault list/search/filter;
4. create/edit item;
5. protected-item re-authentication;
6. generator;
7. security audit;
8. trash/destructive confirmations;
9. settings;
10. backup/restore;
11. transfer/plaintext warnings;
12. security/About/legal surfaces.

## 10. Secret fields

Password/secret values should remain masked by default.

Accessibility must not become a bypass that automatically exposes a secret in:

- accessibility labels;
- hints;
- automation properties;
- quick-action descriptions;
- validation messages.

Explicit reveal/copy remains a deliberate user action.

## 11. Protected items

Items with `RequiresReauthentication` display a protected state until the current master passphrase is re-authenticated.

Screen readers/automation should not receive the hidden decrypted values before re-authentication merely because the visual control is obscured.

## 12. Responsive layout

CipherNest supports phone/tablet/resizable desktop contexts through MAUI layouts. Important current work includes wrapping vault actions/filter surfaces and avoiding assumptions about fixed window width.

Test:

- narrow phone portrait;
- phone landscape;
- tablet-like width;
- narrow resizable desktop window;
- wide desktop window;
- Larger Interface enabled;
- OS large text.

Look for clipped buttons, unreachable controls, overlapping labels, horizontal overflow, and dialogs whose critical warning text is truncated.

## 13. Scroll behavior

Long settings/forms/editor content should remain reachable when text grows. Do not solve clipping by shrinking security text below readable sizes.

Test keyboard focus inside scroll containers and ensure focused controls can be brought into view.

## 14. Generator accessibility

Generator controls must communicate:

- password/passphrase mode;
- length/word count;
- included character groups;
- ambiguous-character exclusion;
- generated result state;
- copy action.

Do not automatically speak a generated secret merely because the result changed. A screen-reader user should retain control over when a sensitive generated value is exposed/copyable.

## 15. Secure-note preview accessibility

Safe note preview has an accessible text representation generated from the bounded markup model. Headings/lists/checklists/code should remain understandable without relying purely on visual formatting.

Raw HTML is not executed.

## 16. Attachment preview accessibility

Attachment preview is intentionally a bounded text alert/display path. Ensure:

- filename/context is announced without leaking filesystem storage paths;
- truncation notice is accessible;
- invalid UTF-8/unsupported-type errors are understandable;
- opening preview does not create an inaccessible hidden modal trap.

## 17. Destructive confirmation accessibility

Trash deletion/full-vault deletion/plaintext export warnings must remain fully readable and navigable with assistive technologies.

Authentication and confirmation are distinct. Do not remove confirmation text because a screen-reader flow feels longer; simplify wording only without weakening meaning.

## 18. Screenshot protection and assistive technology

Screenshot protection is platform-specific. Do not assume a screenshot/security flag is equivalent to accessibility privacy.

Test whether supported screenshot protection has unintended consequences for accessibility services or app switching. If the platform cannot reliably protect screenshots, keep the UI wording honest.

## 19. Clipboard behavior

Screen-reader users may rely heavily on copy/paste. CipherNest's delayed conditional cleanup must preserve unrelated newer clipboard content just as it does for other users.

Documentation/help text should explain that clipboard history/sync may retain secrets.

## 20. Localization and accessibility

Future translations can expand substantially compared with English.

Test long translated labels with:

- large text;
- narrow windows;
- screen-reader semantic text;
- destructive/security warnings.

A translation that shortens a security warning must preserve its meaning, not merely fit the layout.

## 21. Error messages

User-facing errors should be:

- fixed/safe (no raw filesystem exception text);
- actionable where possible;
- concise enough to announce clearly;
- explicit about whether data was changed or remained protected.

Avoid messages that communicate failure only through icon/color.

## 22. Accessibility source tests

`CipherNest.UiTests` includes source/repository checks for accessibility/navigation/layout structure. Keep these as regression guards for:

- semantic properties;
- responsive/wrapping layout intent;
- route/page structure;
- touch-target/font-resource patterns where tested.

They do not replace TalkBack/VoiceOver/Narrator/runtime keyboard tests.

## 23. Release checklist

For every target release, record:

- OS/device/emulator version;
- screen reader used;
- text scaling level(s);
- Larger Interface on/off;
- Reduced Motion on/off;
- Light/Dark/System themes;
- keyboard-only results on desktop;
- narrow/wide layout results;
- any known limitation and whether it is release-blocking.

## 24. New UI review checklist

Before merging a new view/control:

- Is every action reachable without relying on color/motion alone?
- Are secret values masked in semantics as well as visuals?
- Are labels/descriptions meaningful?
- Does it work with large text?
- Does it work in light/dark theme?
- Does it work at narrow width?
- Is touch target practical?
- Is keyboard focus logical?
- Are changing errors/status announced appropriately?
- Does reduced-motion state apply if animation is added?
- Has a source regression test been added when practical?
- Has real target-device assistive-technology validation been scheduled/performed for release?

## 25. Current claim boundary

CipherNest contains accessibility-oriented source architecture and regression checks. It must not be described as fully WCAG/platform-accessibility certified solely from source presence. Release claims should reflect actual target-device testing evidence.
