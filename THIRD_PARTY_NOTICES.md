# Third-Party Notices

CipherNest is licensed under GPL-3.0-or-later. It also depends on third-party packages that retain their own copyrights and licenses.

## Runtime dependencies

| Dependency | Role | License family |
|---|---|---|
| CommunityToolkit.Mvvm | MVVM source generators and observable/command infrastructure | MIT |
| Konscious.Security.Cryptography.Argon2 | Argon2id password-based key derivation | MIT |
| Microsoft.Data.Sqlite | SQLite ADO.NET provider | MIT |
| Microsoft.Extensions.Logging.Debug | Debug logging provider | MIT |
| Microsoft.Maui.Controls | .NET MAUI UI framework | MIT |

## Test/build dependencies

| Dependency | Role | License family |
|---|---|---|
| Microsoft.NET.Test.Sdk | .NET test host integration | MIT |
| xunit | Unit/integration test framework | Apache-2.0 |
| xunit.runner.visualstudio | Visual Studio / VSTest xUnit runner | Apache-2.0 |

The package versions used by a particular build are pinned in `Directory.Packages.props` and may change over time. Before distributing a release, the release checklist requires reviewing the restored package metadata and bundled license texts for the exact resolved versions. This file is a human-readable notice and does not replace the original third-party license terms.

No third-party signing key, API key, certificate, or proprietary SDK secret belongs in this repository.
