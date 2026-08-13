# Third-Party Notices

CipherNest is licensed under GPL-3.0-or-later. It also depends on third-party packages that retain their own copyrights and licenses.

## Runtime dependencies

| Dependency | Current central pin | Role | License family |
|---|---:|---|---|
| CommunityToolkit.Mvvm | 8.4.0 | MVVM source generators and observable/command infrastructure | MIT |
| Konscious.Security.Cryptography.Argon2 | 1.3.1 | Argon2id password-based key derivation | MIT |
| Microsoft.Data.Sqlite | 10.0.10 | SQLite ADO.NET provider | MIT |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.12 | Native SQLite bundle used by the local encrypted-record persistence layer | zlib/public-domain-style components; verify exact restored notices before distribution |
| Microsoft.Extensions.Logging.Debug | 10.0.0 | Debug logging provider | MIT |
| Microsoft.Maui.Controls | 10.0.0 | .NET MAUI UI framework | MIT |

The explicit SQLitePCLRaw bundle pin was added after hosted restore surfaced a high-severity advisory for the older transitive native package. The current hosted candidate restored and built without that earlier `NU1903` blocker. This does not replace dependency review for later release candidates.

## Test/build dependencies

| Dependency | Current central pin | Role | License family |
|---|---:|---|---|
| Microsoft.NET.Test.Sdk | 18.0.0 | .NET test host integration | MIT |
| xunit | 2.9.3 | Unit/integration test framework | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Visual Studio / VSTest xUnit runner | Apache-2.0 |

The package versions used by a particular build are pinned in `Directory.Packages.props` and may change over time. Before distributing a release, the release checklist requires reviewing the restored package metadata, transitive dependency graph, advisory status, and bundled license texts for the exact resolved versions. This file is a human-readable notice and does not replace the original third-party license terms.

No third-party signing key, API key, certificate, or proprietary SDK secret belongs in this repository.
