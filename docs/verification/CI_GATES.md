# CipherNest Verification Gates

This document records the source and build gates that are configured in the repository. A configured gate is not the same as a passing gate: release evidence must come from the exact candidate commit and the target environment that executed it.

## Core gate

The `test-core` job on Ubuntu restores, builds, and runs:

- `CipherNest.UnitTests`
- `CipherNest.IntegrationTests`
- `CipherNest.UiTests`

It also runs `dotnet format --verify-no-changes` against the Domain, Application, Infrastructure, Shared, and test projects. Repository build properties keep nullable analysis, analyzers, deterministic compilation, and warnings-as-errors enabled.

Local equivalents:

- PowerShell: `scripts/verify-core.ps1`
- POSIX shell: `scripts/verify-core.sh`

## Windows MAUI compile gate

The Windows job installs the MAUI workload, restores `net10.0-windows10.0.19041.0`, and compiles the Release app without producing a signed store package. It compiles both:

1. the normal default build; and
2. the store-policy variant with `CipherNestEnableFundingLink=false`.

Local equivalent: `scripts/verify-windows.ps1`.

## Android MAUI compile gate

The Android job installs `maui-android`, restores `net10.0-android`, and compiles a Release app. This catches target-specific API/binding/compiler issues but does not prove behavior on a device.

Local equivalent: `scripts/verify-android.sh`.

## Apple MAUI compile gate

The Apple job runs on a macOS host, installs the iOS and Mac Catalyst MAUI workloads, and compiles the Release targets:

- `net10.0-ios`
- `net10.0-maccatalyst`

This is a source/toolchain compile gate, not a substitute for provisioning, signing, simulator/device smoke tests, Face ID/Touch ID behavior, notarization, or App Store validation.

Local equivalent: `scripts/verify-apple.sh`.

## CodeQL

CodeQL analyzes C# source after building the integration-test/core path and the Android MAUI application target. This broadens analysis beyond the non-MAUI libraries. CodeQL results must still be reviewed for the exact release candidate.

## Dependency review

Pull requests run GitHub dependency review with `fail-on-severity: high`. Release review must also inspect the exact restored direct/transitive package graph, license obligations, and any accepted vulnerability exception.

## Workflow resource controls

Primary CI, CodeQL, and dependency review use concurrency groups that cancel superseded runs and explicit job timeouts. This prevents stale commits from consuming unlimited hosted-runner time and makes stuck workload/install failures visible.

## What these gates do not prove

The configured workflows do not replace:

- Android/iOS/Mac Catalyst/Windows physical-device or simulator smoke tests;
- biometric enrollment, denial, cancellation, lockout, and secure-storage lifecycle testing;
- screenshot/app-switcher privacy behavior;
- real clipboard history and clearing behavior;
- sleep/background/resume lifecycle validation;
- accessibility testing with TalkBack, VoiceOver, Narrator, keyboard-only navigation, and OS large text;
- file picker/share-provider plaintext-retention behavior;
- signing, notarization, store package validation, or store policy review;
- an independent cryptographic/security audit.

## Release evidence

For every candidate, record:

- immutable commit/tag;
- .NET SDK version;
- installed workload versions;
- runner/host OS and platform SDK versions;
- CI/CodeQL/dependency-review results;
- selected `CipherNestEnableFundingLink` value for each distributed package;
- device/simulator matrix and smoke-test results;
- signing/store pipeline identity outside the repository;
- known accepted exceptions with owner and expiry.

Do not mark a release gate complete from source inspection alone.
