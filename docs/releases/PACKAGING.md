# CipherNest Executable and Distribution Artifact Build Guide

This is the canonical end-to-end guide for turning an exact CipherNest source commit into runnable or distributable platform artifacts. It covers Windows `.exe` and `.msix`, Android `.apk` and `.aab`, iOS `.ipa`, and Mac Catalyst `.app` and `.pkg` outputs, including prerequisites, build inputs, signing, versioning, output discovery, verification, checksums, release evidence, and troubleshooting.

Compilation, packaging, signing, notarization, store submission, and device validation are different release gates. A successful `dotnet build` does not by itself prove that a signed installer or store package is ready to ship.

For the release-candidate/evidence process, use [`RELEASE_PROCESS.md`](RELEASE_PROCESS.md). For compilation prerequisites and verified target shapes, use [`../setup/BUILD.md`](../setup/BUILD.md). For reproducibility requirements, use [`REPRODUCIBLE_BUILDS.md`](REPRODUCIBLE_BUILDS.md).

> **Security rule:** never commit signing certificates, private keys, keystores, provisioning profiles containing private material, certificate passwords, notarization credentials, store API tokens, password files, or base64-encoded signing secrets. Keep release credentials outside the repository in protected local/CI secret storage.

## 1. Current application/package identity

The executable project is:

```text
src/CipherNest.App/CipherNest.App.csproj
```

Current project metadata:

| Setting | Current value/source |
|---|---|
| Product title | `CipherNest` |
| Application ID | `in.sanskar.ciphernest` |
| Display version | `0.1.0` |
| Build/application version | `1` |
| Android minimum | API 26 |
| iOS minimum | 15.0 |
| Mac Catalyst minimum | 15.0 |
| Windows minimum | 10.0.19041.0 |
| Default Android RID | `android-arm64` |
| Optional funding build property | `CipherNestEnableFundingLink` |

Before a public release, deliberately choose the version/build number and reconcile every platform-specific manifest with the project metadata. Do not assume a store accepts stale or mismatched versions.

## 2. Artifact matrix

| Target | TFM | Primary host | Runnable/distribution outputs | Current verified compile shape |
|---|---|---|---|---|
| Windows | `net10.0-windows10.0.19041.0` | Windows | unpackaged `.exe` folder, packaged `.msix` | `win-x64` |
| Android | `net10.0-android` | Windows/macOS/Linux with Android toolchain | signed `.apk`, signed `.aab` | `android-arm64` |
| iOS | `net10.0-ios` | macOS/Xcode, or Windows paired to a Mac | signed `.ipa` | device: `ios-arm64`; CI compile: `iossimulator-arm64` |
| Mac Catalyst | `net10.0-maccatalyst` | macOS/Xcode | `.app`, signed `.pkg` | `maccatalyst-arm64` |

Other architectures must be validated independently before release. The presence of an RID in a project property does not mean that exact architecture has completed real-device or store validation.

## 3. Complete build-input inventory

Executable creation is not driven by one file. The following repository inputs participate directly or indirectly in restore, compile, package, verification, or release provenance.

### 3.1 Repository/toolchain inputs

```text
global.json
Directory.Build.props
Directory.Packages.props
CipherNest.slnx
.github/workflows/dotnet-desktop.yml
build/scripts/verify.ps1
build/scripts/verify.sh
scripts/verify-core.ps1
scripts/verify-core.sh
scripts/verify-windows.ps1
scripts/verify-android.sh
scripts/verify-apple.sh
```

- `global.json` selects the .NET 10 SDK family.
- `Directory.Build.props` applies shared analyzer, warning, nullable, code-style, and deterministic-build policy.
- `Directory.Packages.props` centrally controls package versions.
- `CipherNest.slnx` defines the solution graph used for solution/core work.
- CI and verification scripts define the repository's canonical compile/test gates.

### 3.2 Executable project and common app inputs

```text
src/CipherNest.App/CipherNest.App.csproj
src/CipherNest.App/App.xaml
src/CipherNest.App/App.xaml.cs
src/CipherNest.App/AppShell.xaml
src/CipherNest.App/AppShell.xaml.cs
src/CipherNest.App/MauiProgram.cs
src/CipherNest.App/**/*.cs
src/CipherNest.App/**/*.xaml
```

The SDK-style project automatically discovers normal compile/XAML inputs according to .NET/MAUI item rules. Do not create a hand-maintained list of every page/ViewModel source file for publishing and do not manually copy those source files into the output folder.

### 3.3 Android platform inputs

```text
src/CipherNest.App/Platforms/Android/AndroidManifest.xml
src/CipherNest.App/Platforms/Android/MainActivity.cs
src/CipherNest.App/Platforms/Android/MainApplication.cs
```

`AndroidManifest.xml` affects package behavior and permissions. Current source disables Android backup, disables cleartext traffic, labels the application `CipherNest`, enables RTL support, and declares biometric permission. Review the final merged manifest for every release.

### 3.4 iOS platform inputs

```text
src/CipherNest.App/Platforms/iOS/AppDelegate.cs
src/CipherNest.App/Platforms/iOS/Info.plist
src/CipherNest.App/Platforms/iOS/Program.cs
```

The current iOS `Info.plist` includes iPhone/iPad device-family settings, supported orientations, arm64 requirement, and the Face ID usage description.

### 3.5 Mac Catalyst platform inputs

```text
src/CipherNest.App/Platforms/MacCatalyst/AppDelegate.cs
src/CipherNest.App/Platforms/MacCatalyst/Info.plist
src/CipherNest.App/Platforms/MacCatalyst/Program.cs
```

The current repository does **not** contain a committed `Platforms/MacCatalyst/Entitlements.plist`. Do not copy an example `CodesignEntitlements` argument into a release command unless a reviewed entitlements file is actually added and required by the selected distribution channel.

### 3.6 Windows platform inputs

```text
src/CipherNest.App/Platforms/Windows/App.xaml
src/CipherNest.App/Platforms/Windows/App.xaml.cs
src/CipherNest.App/Platforms/Windows/Package.appxmanifest
```

`Package.appxmanifest` controls the packaged Windows identity/capabilities and currently declares publisher `CN=Sanskar`, package version `0.1.0.0`, minimum Windows `10.0.19041.0`, and `runFullTrust`. A certificate used to sign an MSIX must match the package publisher identity required by the release channel.

### 3.7 MAUI packaging resources

The project explicitly declares MAUI resources through `CipherNest.App.csproj`. Current packaging-relevant resource paths include:

```text
src/CipherNest.App/Resources/AppIcon/appicon.svg
src/CipherNest.App/Resources/AppIcon/appiconfg.svg
src/CipherNest.App/Resources/AppIcon/appicon-mono.svg
src/CipherNest.App/Resources/Splash/splash.svg
src/CipherNest.App/Resources/Images/bmc_support.svg
src/CipherNest.App/Resources/Images/ciphernest_logo.svg
src/CipherNest.App/Resources/Images/ciphernest_logo_dark.svg
src/CipherNest.App/Resources/Localization/AppStrings.resx
src/CipherNest.App/Resources/Localization/AppStrings.hi-IN.resx
src/CipherNest.App/Resources/Raw/wordlist_notice.txt
src/CipherNest.App/Resources/Strings/AppResources.resx
src/CipherNest.App/Resources/Styles/Colors.xaml
src/CipherNest.App/Resources/Styles/Styles.xaml
```

In addition, the `.csproj` includes the normal `Resources/Images/*`, `Resources/Fonts/*`, and `Resources/Raw/**` MAUI item globs. Any file that matches those declarations is part of the applicable app resource pipeline; it does not need to be copied manually.

### 3.8 Referenced source projects

The executable project references all of these class libraries:

```text
src/CipherNest.Application/CipherNest.Application.csproj
src/CipherNest.Domain/CipherNest.Domain.csproj
src/CipherNest.Infrastructure/CipherNest.Infrastructure.csproj
src/CipherNest.Shared/CipherNest.Shared.csproj
```

All compile inputs included by those SDK-style projects are built transitively when `CipherNest.App.csproj` is published. Do not separately publish the class-library projects as executables.

### 3.9 Tests and release documents

Tests are not packaged into the app, but they are release gates:

```text
tests/CipherNest.UnitTests/CipherNest.UnitTests.csproj
tests/CipherNest.IntegrationTests/CipherNest.IntegrationTests.csproj
tests/CipherNest.UiTests/CipherNest.UiTests.csproj
tests/Directory.Build.props
```

Release documentation/evidence inputs include:

```text
docs/setup/BUILD.md
docs/RELEASE_CHECKLIST.md
docs/releases/PACKAGING.md
docs/releases/RELEASE_PROCESS.md
docs/releases/REPRODUCIBLE_BUILDS.md
docs/releases/STORE_LISTING_GUIDE.md
docs/verification/CI_GATES.md
THIRD_PARTY_NOTICES.md
CHANGELOG.md
PROJECT_STATUS.md
what_changed.md
```

This inventory is intentionally expressed both as exact files and SDK/glob rules. That avoids the common mistake of listing a few hand-picked source files while silently omitting files that MSBuild/MAUI actually discovers automatically.

## 4. Never publish the whole solution

For distribution artifacts, publish the **MAUI app project directly**:

```bash
dotnet publish src/CipherNest.App/CipherNest.App.csproj ...
```

Do not use:

```bash
dotnet publish CipherNest.slnx
```

The solution contains class libraries and test projects. Publishing the solution can attempt to publish projects that are not executable distribution targets.

## 5. Prerequisites and host setup

### 5.1 Every host

From the repository root:

```bash
dotnet --info
dotnet workload list
dotnet nuget list source
```

The repository currently selects .NET SDK `10.0.100` as the baseline with `latestFeature` roll-forward. Record the **actual** resolved SDK/workload versions in release provenance.

Restore the app project:

```bash
dotnet restore src/CipherNest.App/CipherNest.App.csproj
```

Where supported, workload restore can help install/repair workloads required by the project:

```bash
dotnet workload restore src/CipherNest.App/CipherNest.App.csproj
```

### 5.2 Windows host

Required for Windows packaging:

- .NET 10 SDK and MAUI workload;
- supported Visual Studio/Build Tools components for .NET MAUI/Windows App SDK;
- Windows SDK compatible with the target;
- a code-signing certificate for signed MSIX production/testing as applicable.

### 5.3 Android host

Required:

- .NET 10 SDK and MAUI Android workload;
- supported JDK;
- Android SDK/build tools;
- release keystore for signed distribution packages.

Check tools without exposing secrets:

```bash
java -version
keytool -help
```

### 5.4 Apple host

Native iOS and Mac Catalyst packaging requires Apple's build tools on macOS:

- .NET 10 SDK;
- compatible `maui-ios`/`maui-maccatalyst` workload versions;
- compatible Xcode and command-line tools;
- Apple signing certificates/provisioning profiles for signed distribution;
- Apple Developer Program access for store/ad-hoc distribution as required.

Check:

```bash
xcodebuild -version
xcode-select -p
dotnet --info
dotnet workload list
security find-identity -v -p codesigning
```

The repository's recorded hosted Apple compile pairing is documented in `../setup/BUILD.md`. Do not suppress Xcode compatibility validation merely to force an incompatible toolchain to build.

## 6. Clean release preparation

Start from a clean checkout of the exact candidate commit/tag. Record:

```bash
git rev-parse HEAD
git status --short
dotnet --info
dotnet workload list
```

Do not use broad cleanup commands that could delete local signing material. Signing secrets should not be stored under the repository in the first place.

Run the repository gates before packaging:

PowerShell/core:

```powershell
./scripts/verify-core.ps1
./scripts/verify-windows.ps1
```

POSIX/core/platform:

```bash
sh scripts/verify-core.sh
sh scripts/verify-android.sh
# On a compatible Mac:
sh scripts/verify-apple.sh
```

A release artifact must be rebuilt from the same exact source candidate after any source/build-manifest change.

## 7. Funding/BMC distribution switch

The optional in-app Buy Me a Coffee surface is enabled by default. If the applicable store/distribution policy requires it to be absent, add:

```text
-p:CipherNestEnableFundingLink=false
```

to the **publish** command for that exact artifact. Record the selected value in provenance.

This build property changes the compiled app surface. It does not remove repository funding metadata, README content, or support documentation.

## 8. Windows — unpackaged `.exe`

The current repository already verifies the Windows target with `win-x64` and `WindowsPackageType=None`.

### 8.1 Framework-dependent unpackaged executable

From a Developer PowerShell/command prompt on Windows:

```powershell
dotnet publish src/CipherNest.App/CipherNest.App.csproj `
  -c Release `
  -p:CipherNestTargetFrameworks=net10.0-windows10.0.19041.0 `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:WindowsPackageType=None
```

Expected output tree is under:

```text
src/CipherNest.App/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/
```

The runnable file is `CipherNest.exe`, but **the executable must stay with its required published companion files**. Do not distribute only the `.exe` unless the exact publish mode has been proven to produce a true standalone single-file app; this project does not currently define such a single-file release contract.

### 8.2 Self-contained Windows App SDK variant

To include Windows App SDK runtime components in the published folder, use:

```powershell
dotnet publish src/CipherNest.App/CipherNest.App.csproj `
  -c Release `
  -p:CipherNestTargetFrameworks=net10.0-windows10.0.19041.0 `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:WindowsPackageType=None `
  -p:WindowsAppSDKSelfContained=true
```

Compare size/startup/install assumptions and test on a clean representative machine before deciding which mode to ship.

### 8.3 Funding-disabled `.exe` variant

Append:

```text
-p:CipherNestEnableFundingLink=false
```

Do not edit source to create a one-off store variant when the build switch is sufficient.

## 9. Windows — signed `.msix`

The packaged Windows identity is defined by:

```text
src/CipherNest.App/Platforms/Windows/Package.appxmanifest
```

Current publisher is `CN=Sanskar`. The signing certificate and package identity must be compatible with the selected distribution channel.

### 9.1 Inspect available signing certificates

PowerShell:

```powershell
Get-ChildItem Cert:\CurrentUser\My | Format-Table Thumbprint, Subject, FriendlyName
```

Store only the selected thumbprint/reference in non-secret build metadata. Do not export a PFX into the repository.

### 9.2 Package command using the repository's current RID shape

```powershell
dotnet publish src/CipherNest.App/CipherNest.App.csproj `
  -c Release `
  -p:CipherNestTargetFrameworks=net10.0-windows10.0.19041.0 `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:WindowsPackageType=Package `
  -p:AppxPackageSigningEnabled=true `
  -p:PackageCertificateThumbprint="$env:CIPHERNEST_WINDOWS_CERT_THUMBPRINT"
```

Expected package output is under the Windows Release tree, normally an `AppPackages/<app-and-version>/` directory containing the `.msix` and associated install metadata.

### 9.3 .NET 10 RID note

For .NET 10 use portable Windows RIDs such as `win-x64`, `win-x86`, and `win-arm64`; do not copy old `win10-x64` examples from older .NET guidance.

Microsoft's current MAUI Windows publishing guidance also documents a `RuntimeIdentifierOverride` workaround for Windows App SDK RID handling. This repository does not currently persist the mapping property used by that workaround, while its verified build path uses `-r win-x64`. If Windows packaging fails specifically because of RID resolution, compare the current Microsoft guidance with the exact SDK/Windows App SDK versions before changing `CipherNest.App.csproj`; any such build-configuration change creates a new release candidate that must be re-verified.

### 9.4 Verify the MSIX signature

```powershell
Get-AuthenticodeSignature path\to\CipherNest.msix | Format-List
```

A locally self-signed test certificate is not equivalent to a publicly trusted production signing identity or Microsoft Store signing.

## 10. Android — release signing material

Android distribution packages must be signed with a release key. Keep the keystore and password material outside the repository and back them up securely; losing the update key can prevent future updates depending on the selected store/key-management model.

A keystore is normally created once with the JDK `keytool`. Example **placeholder** command:

```bash
keytool -genkeypair -v \
  -keystore /secure/outside-repo/ciphernest-release.keystore \
  -alias ciphernest \
  -keyalg RSA -keysize 2048 -validity 10000
```

Choose the key parameters according to current Android/store requirements and organizational policy. Never paste the real password into documentation or commit history.

## 11. Android — signed `.apk` and `.aab`

### 11.1 Produce both package formats

Use password files/CI secret injection rather than plain-text passwords in shell history. Example using files outside the repository:

```bash
dotnet publish src/CipherNest.App/CipherNest.App.csproj \
  -c Release \
  -p:CipherNestTargetFrameworks=net10.0-android \
  -f net10.0-android \
  -r android-arm64 \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=/secure/outside-repo/ciphernest-release.keystore \
  -p:AndroidSigningKeyAlias=ciphernest \
  -p:AndroidSigningKeyPass=file:/secure/outside-repo/android-key-pass.txt \
  -p:AndroidSigningStorePass=file:/secure/outside-repo/android-store-pass.txt \
  '-p:AndroidPackageFormats=aab;apk'
```

On PowerShell quote the semicolon-containing MSBuild property so the shell does not treat `;` as a command separator.

To generate only one format, set:

```text
-p:AndroidPackageFormats=aab
```

or:

```text
-p:AndroidPackageFormats=apk
```

### 11.2 Output discovery

Depending on SDK/RID layout, signed packages are emitted under the Android Release publish tree, for example:

```text
src/CipherNest.App/bin/Release/net10.0-android/publish/
src/CipherNest.App/bin/Release/net10.0-android/android-arm64/publish/
```

Do not guess which similarly named package is signed. Inspect the publish output and signing verification result.

PowerShell discovery:

```powershell
Get-ChildItem src/CipherNest.App/bin/Release/net10.0-android -Recurse -File -Include *.apk,*.aab
```

POSIX discovery:

```bash
find src/CipherNest.App/bin/Release/net10.0-android -type f \( -name '*.apk' -o -name '*.aab' \) -print
```

### 11.3 ABI warning

`CipherNest.App.csproj` currently defaults Android publishing to `android-arm64` while also declaring Android ARM, ARM64, x86, and x64 runtime identifiers. A store AAB built with only `android-arm64` must not be advertised as covering every ABI. If a release must support additional ABIs, deliberately configure the required runtime identifiers, inspect the resulting package, and test representative devices before promotion.

### 11.4 Verify Android signing

For APKs, use the Android SDK build-tools `apksigner`:

```bash
apksigner verify --verbose --print-certs path/to/CipherNest.apk
```

For an AAB, which uses JAR signing semantics:

```bash
jarsigner -verify -verbose -certs path/to/CipherNest.aab
```

Do not publish an unsigned/intermediate artifact simply because its filename resembles the signed package.

## 12. iOS — signed `.ipa` on a Mac

An iOS distribution package requires a compatible Mac/Xcode toolchain, an Apple signing identity, and an appropriate provisioning profile.

List signing identities:

```bash
security find-identity -v -p codesigning
```

Publish a physical-device/App Store style archive with placeholders replaced by the protected build environment:

```bash
dotnet publish src/CipherNest.App/CipherNest.App.csproj \
  -c Release \
  -p:CipherNestTargetFrameworks=net10.0-ios \
  -f net10.0-ios \
  -p:ArchiveOnBuild=true \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey='Apple Distribution: <Name> (<TEAMID>)' \
  -p:CodesignProvision='<Provisioning Profile Name>'
```

Expected `.ipa` location:

```text
src/CipherNest.App/bin/Release/net10.0-ios/ios-arm64/publish/
```

The provisioning profile/certificate determine the allowed distribution channel. An iOS simulator build is compile/test evidence, **not** an `.ipa` distribution artifact.

The current repository does not require a committed iOS entitlements file for its documented publish command. If new capabilities require entitlements later, add/review the actual entitlements file and then pass/configure `CodesignEntitlements` deliberately.

## 13. iOS publish from Windows through a paired Mac

Native Apple tools still execute on a Mac. A Windows developer can publish iOS through a correctly paired Mac build host.

Use the same iOS signing/archive properties plus the remote build properties required by the installed MAUI toolchain, for example:

```powershell
dotnet publish src/CipherNest.App/CipherNest.App.csproj `
  -c Release `
  -p:CipherNestTargetFrameworks=net10.0-ios `
  -f net10.0-ios `
  -p:ArchiveOnBuild=true `
  -p:RuntimeIdentifier=ios-arm64 `
  -p:CodesignKey="Apple Distribution: <Name> (<TEAMID>)" `
  -p:CodesignProvision="<Provisioning Profile Name>" `
  -p:ServerAddress=<MAC-IP> `
  -p:ServerUser=<MAC-USER> `
  -p:TcpPort=58181 `
  -p:_DotNetRootRemoteDirectory=/Users/<MAC-USER>/Library/Caches/Xamarin/XMA/SDKs/dotnet/
```

Prefer saved SSH-key pairing over putting a Mac password on the command line. Never commit `ServerPassword` or remote credentials.

## 14. Mac Catalyst — unsigned/local `.app`

For a local/unsigned app bundle on a compatible Mac:

```bash
dotnet publish src/CipherNest.App/CipherNest.App.csproj \
  -c Release \
  -p:CipherNestTargetFrameworks=net10.0-maccatalyst \
  -f net10.0-maccatalyst \
  -p:RuntimeIdentifier=maccatalyst-arm64 \
  -p:CreatePackage=false
```

Expected `.app` location is under:

```text
src/CipherNest.App/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/
```

An unsigned local `.app` is not a substitute for signed/notarized public distribution.

## 15. Mac Catalyst — signed `.pkg`

### 15.1 Outside the Mac App Store / Developer ID pattern

Use protected certificate/profile names from the build machine:

```bash
dotnet publish src/CipherNest.App/CipherNest.App.csproj \
  -c Release \
  -p:CipherNestTargetFrameworks=net10.0-maccatalyst \
  -f net10.0-maccatalyst \
  -p:RuntimeIdentifier=maccatalyst-arm64 \
  -p:CreatePackage=true \
  -p:EnableCodeSigning=true \
  -p:EnablePackageSigning=true \
  -p:CodesignKey='Developer ID Application: <Name> (<TEAMID>)' \
  -p:CodesignProvision='<Non-App-Store Provisioning Profile>' \
  -p:PackageSigningKey='Developer ID Installer: <Name> (<TEAMID>)' \
  -p:UseHardenedRuntime=true
```

If the selected signing model does not require a provisioning profile, follow current Apple/.NET MAUI guidance for that exact channel rather than inventing one.

### 15.2 Mac App Store pattern

Use the appropriate App Store distribution and installer identities, for example:

```bash
dotnet publish src/CipherNest.App/CipherNest.App.csproj \
  -c Release \
  -p:CipherNestTargetFrameworks=net10.0-maccatalyst \
  -f net10.0-maccatalyst \
  -p:RuntimeIdentifier=maccatalyst-arm64 \
  -p:CreatePackage=true \
  -p:EnableCodeSigning=true \
  -p:EnablePackageSigning=true \
  -p:CodesignKey='Apple Distribution: <Name> (<TEAMID>)' \
  -p:CodesignProvision='<Mac App Store Provisioning Profile>' \
  -p:PackageSigningKey='3rd Party Mac Developer Installer: <Name> (<TEAMID>)'
```

Do **not** add `-p:CodesignEntitlements=Platforms/MacCatalyst/Entitlements.plist` to these commands unless that file actually exists and has been reviewed for the app's capabilities.

Expected package output is under:

```text
src/CipherNest.App/bin/Release/net10.0-maccatalyst/maccatalyst-arm64/publish/
```

or the non-RID-specific publish folder when a universal/default architecture layout is used.

## 16. Mac Catalyst notarization for outside-store distribution

For public distribution outside the Mac App Store, signing is not the end of the process. Follow current Apple notarization requirements for the final signed artifact, normally using protected notarization credentials stored in Keychain/CI secret storage.

After notarization, validate the final distributed artifact rather than only the pre-notarization package.

Typical local verification commands:

```bash
codesign --verify --deep --strict --verbose=2 /path/to/CipherNest.app
spctl --assess --type execute --verbose=4 /path/to/CipherNest.app
```

Use current Apple notarization tooling/policy at release time because requirements can change independently of repository source.

## 17. Versioning checklist before generating final artifacts

Before each release candidate:

1. choose the semantic/display version;
2. choose a monotonically valid platform build/version code;
3. update `ApplicationDisplayVersion` and `ApplicationVersion` in `CipherNest.App.csproj` as required;
4. reconcile Windows `Package.appxmanifest` identity version/publisher with the intended Windows package channel;
5. ensure Android package ID/version code/version name resolve as intended;
6. ensure Apple bundle identifier/version/build and provisioning profiles refer to the same app identity;
7. rebuild every final artifact from the exact tagged candidate;
8. do not reuse old output directories as evidence for a new source commit.

## 18. Release artifact discovery

After publishing, inventory candidate files rather than relying on memory.

PowerShell:

```powershell
Get-ChildItem src/CipherNest.App/bin/Release -Recurse -File |
  Where-Object { $_.Extension -in '.exe','.msix','.apk','.aab','.ipa','.pkg' } |
  Select-Object FullName, Length, LastWriteTimeUtc
```

POSIX:

```bash
find src/CipherNest.App/bin/Release -type f \
  \( -name '*.exe' -o -name '*.msix' -o -name '*.apk' -o -name '*.aab' -o -name '*.ipa' -o -name '*.pkg' \) \
  -print
```

Remember that `.app` is a directory bundle, not a single ordinary file:

```bash
find src/CipherNest.App/bin/Release -type d -name '*.app' -print
```

## 19. SHA-256 checksums

Generate checksums **after** the final signing/notarization step because those steps can change bytes.

Windows PowerShell:

```powershell
Get-FileHash path\to\artifact -Algorithm SHA256
```

Linux:

```bash
sha256sum path/to/artifact
```

macOS:

```bash
shasum -a 256 path/to/artifact
```

For a directory bundle such as `.app`, checksum the exact archive/package that will actually be distributed, or create a deterministic release archive under the release process and checksum that file. Do not publish a checksum of a different intermediate representation.

## 20. Artifact validation checklist

For every artifact, record pass/fail evidence for all applicable items:

- exact source commit/tag;
- clean working tree at candidate selection;
- exact .NET SDK/workload/toolchain versions;
- correct application/package ID;
- correct version/build number;
- expected icon/splash/branding;
- expected `CipherNestEnableFundingLink` value;
- expected architecture(s);
- expected permissions/capabilities/entitlements;
- expected signing identity;
- signature verification passed;
- package installs/launches on a representative clean target;
- startup/onboarding/unlock/lock smoke test;
- secure storage/biometric behavior on applicable physical devices;
- clipboard/screenshot/background lifecycle checks;
- backup/restore/file-picker/share checks;
- accessibility/responsive UI checks;
- release tests/CI evidence reviewed;
- third-party license/notices reviewed;
- artifact SHA-256 recorded;
- final filename recorded;
- store/notarization result recorded where applicable.

Do not mark a package ready merely because it exists on disk.

## 21. Suggested artifact naming

Keep generated SDK filenames if a store/tool requires them. For separately archived release copies, use an unambiguous naming scheme such as:

```text
CipherNest-0.1.0-win-x64-unpackaged.zip
CipherNest-0.1.0-win-x64.msix
CipherNest-0.1.0-android-arm64.apk
CipherNest-0.1.0-android.aab
CipherNest-0.1.0-ios-arm64.ipa
CipherNest-0.1.0-maccatalyst-arm64.pkg
```

Do not rename a signed/store artifact if the distribution tool expects its generated name. The provenance record should map the original generated path to any archived release filename.

## 22. Do not commit generated artifacts or secrets by default

Build outputs belong under `bin/` and `obj/` and should remain ignored unless the repository has an explicit, reviewed release-artifact policy.

Never commit:

```text
*.keystore
*.jks
*.pfx
*.p12
private signing keys
certificate passwords
Android signing password files
Apple provisioning/signing secrets
notarization credentials
store API keys/tokens
remote Mac passwords
base64-encoded secret copies
```

If a release artifact is attached to a GitHub Release or store submission, generate it from the exact immutable tag/candidate and keep the release provenance/checksum separately from secrets.

## 23. Troubleshooting

### `NETSDK`/SDK version mismatch

Check:

```bash
dotnet --info
cat global.json
```

Install a compatible .NET 10 SDK or correct the intended SDK policy in a reviewed source change. Do not silently build a release with an unknown toolchain.

### MAUI workload missing

Check:

```bash
dotnet workload list
```

Then install/restore the required workload for the host/target. Record the resolved workload versions.

### Windows package certificate mismatch

Compare:

- `Package.appxmanifest` publisher;
- certificate subject;
- certificate thumbprint selected by the build;
- store-associated identity if publishing to Microsoft Store.

Do not weaken signature validation to make a mismatched package install.

### Windows RID errors

Use .NET 10 portable RIDs (`win-x64`, `win-x86`, `win-arm64`). If the error matches the Windows App SDK runtime-identifier issue, review the current MAUI `RuntimeIdentifierOverride` guidance before changing project configuration.

### Android package is unsigned or wrong key

Inspect the exact `.apk`/`.aab` with `apksigner`/`jarsigner`, verify the key alias/keystore path, and confirm the release did not accidentally use the development debug keystore.

### Android AAB does not contain intended ABIs

Review the project's default Android RID and deliberately configure/test required architectures. Do not infer ABI coverage from the `.aab` extension alone.

### Apple signing identity not found

Check:

```bash
security find-identity -v -p codesigning
```

Then verify certificate installation, expiration, team, provisioning profile, bundle identifier, and selected Xcode/toolchain.

### Apple Xcode/workload incompatibility

Do not disable compatibility checks. Align Xcode with the installed .NET Apple workload/SDK combination and rerun the compile gate.

### `.ipa` not produced

Confirm:

- physical-device RID `ios-arm64`;
- `ArchiveOnBuild=true`;
- valid signing identity and provisioning profile;
- packaging is being run on/through a Mac rather than only building a simulator target.

### Mac `.pkg` not produced

Confirm `CreatePackage=true`, selected architecture, signing/package-signing properties, and the expected publish folder. For a plain `.app`, use `CreatePackage=false`.

### Funding CTA appears in a package where policy requires it hidden

Rebuild the exact artifact with:

```text
-p:CipherNestEnableFundingLink=false
```

Do not patch the already-built package manually.

## 24. Release provenance template

Store a non-secret record for each final artifact:

```text
CipherNest release artifact provenance
--------------------------------------
Source repository: https://github.com/sanskarIN/CipherNest
Source commit/tag:
Build UTC timestamp:
Target platform:
Target framework:
Runtime identifier(s):
Configuration: Release
Application display version:
Application/build version:
CipherNestEnableFundingLink:
Host OS/build:
.NET SDK:
.NET workloads:
Android SDK/JDK OR Xcode/Windows SDK:
Signing identity reference (NO private key/password):
Provisioning profile reference if applicable:
Artifact generated path:
Archived/distributed filename:
Artifact SHA-256:
Signature verification result:
CI/test evidence references:
Device/install smoke-test evidence:
Notarization/store result if applicable:
Approved exceptions/known limitations:
```

## 25. Canonical end-to-end release order

For a real release, follow this order:

1. freeze/select exact source candidate;
2. capture environment/toolchain metadata;
3. run core and relevant platform verification;
4. choose version/build numbers;
5. confirm package IDs/manifests/resources;
6. check current store/distribution policy, including the funding CTA decision;
7. obtain signing/provisioning material from protected storage;
8. publish the **app project** for the intended platform/artifact;
9. locate the exact generated artifact;
10. verify signing and architecture/package metadata;
11. install/launch on representative target hardware;
12. run applicable security/lifecycle/accessibility/device checks;
13. notarize where required;
14. compute SHA-256 on the final bytes that will be distributed;
15. write release provenance;
16. complete [`../RELEASE_CHECKLIST.md`](../RELEASE_CHECKLIST.md);
17. publish/store-submit only after applicable gates pass;
18. retain evidence without retaining secrets in Git.

## 26. Current primary references

The commands in this guide are adapted to CipherNest's **current `net10.0-*` target frameworks and repository structure**. At release time, re-check current platform guidance because SDK/store/signing requirements change independently of source code.

Primary Microsoft .NET MAUI references:

- Android CLI publishing: <https://learn.microsoft.com/dotnet/maui/android/deployment/publish-cli?view=net-maui-10.0>
- Windows deployment overview: <https://learn.microsoft.com/dotnet/maui/windows/deployment/overview?view=net-maui-10.0>
- Windows packaged CLI publishing: <https://learn.microsoft.com/dotnet/maui/windows/deployment/publish-cli?view=net-maui-10.0>
- Windows unpackaged CLI publishing: <https://learn.microsoft.com/dotnet/maui/windows/deployment/publish-unpackaged-cli?view=net-maui-10.0>
- iOS CLI publishing: <https://learn.microsoft.com/dotnet/maui/ios/deployment/publish-cli?view=net-maui-10.0>
- Mac Catalyst unsigned publishing: <https://learn.microsoft.com/dotnet/maui/mac-catalyst/deployment/publish-unsigned?view=net-maui-10.0>
- Mac Catalyst App Store publishing: <https://learn.microsoft.com/dotnet/maui/mac-catalyst/deployment/publish-app-store?view=net-maui-10.0>
- Mac Catalyst outside-store publishing: <https://learn.microsoft.com/dotnet/maui/mac-catalyst/deployment/publish-outside-app-store?view=net-maui-10.0>

Also follow current Apple, Android/Google Play, Microsoft Store, and certificate-authority requirements for the exact release channel.

## 27. Final rule

A generated file is not automatically a release. Treat `build -> package -> sign -> verify -> device test -> notarize/store review -> checksum -> provenance` as one evidence chain tied to one immutable source commit. If any build input, signing configuration, manifest, resource, or source file changes, create a new candidate and repeat the applicable gates.
