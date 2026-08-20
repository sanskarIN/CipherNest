# CipherNest Web / Linux Host

`CipherNest.Web` is the cross-platform local browser host for CipherNest. It exists to close the Linux and browser UI gap without pretending that .NET MAUI itself ships a Linux target.

## Platform model

The host targets `net10.0` with ASP.NET Core Blazor Interactive Server. It is intended to run locally on:

- Linux (x64/arm64 where the .NET 10 runtime and native SQLite package are supported);
- Windows 10/11;
- macOS;
- development machines that need a browser-accessible CipherNest UI.

Android, iOS, Windows, and Mac Catalyst continue to use `CipherNest.App` for the native MAUI experience. Linux uses this local host plus the system browser.

This is **not** a remotely hosted password-manager service. `Program.cs` binds Kestrel to loopback only (`127.0.0.1`/`localhost`), uses the existing encrypted SQLite/infrastructure layer, and stores vault data in a local application-data directory. Do not change the listener to a public interface without a separate authentication, transport-security, threat-model, deployment, and review design.

## Current cross-platform web workflows

The first host surface intentionally covers the common core required to make the vault usable on Linux and in a local browser:

- detect/create a local encrypted vault;
- create optional recovery material;
- unlock with master passphrase or recovery material;
- lock and clear page-owned sensitive editor state;
- list encrypted vault items after unlock;
- local unlocked search;
- add encrypted items through the existing `IVaultService`;
- move items to Trash;
- loopback health probe for packaging/CI;
- responsive keyboard-friendly browser layout.

The host reuses the same `CryptoService`, `SqliteVaultStore`, `VaultService`, password generator, TOTP service/URI codec, audit service, and safe-note service implementations as the shared implementation. `IVaultService` is registered per Interactive Server circuit because it owns decrypted session-key state; a separate browser circuit therefore does not inherit another circuit's unlocked key. Native-only convenience integrations such as platform biometric prompts, MAUI secure storage, screenshot APIs, and OS share/picker surfaces are not falsely claimed for the browser host.

## Security boundaries

- Listener: loopback only.
- Vault session: per Interactive Server circuit; decrypted session-key state is not application-global.
- Cache policy: `no-store` and `no-cache` response headers.
- Framing: denied with CSP `frame-ancestors 'none'` and `X-Frame-Options: DENY`.
- Referrer policy: `no-referrer`.
- Browser permissions: camera, microphone, geolocation, payment, and USB disabled by response policy for this host.
- CSP limits content to the local application origin plus the WebSocket connection required by Interactive Server.
- UI exception handling uses fixed user-facing failures instead of surfacing raw exception messages.
- Secrets remain subject to .NET managed-memory and browser/OS limitations; deterministic erasure is not claimed.

## Data directory and port

Default port: `5187`.

Override with command-line configuration:

```bash
dotnet run --project src/CipherNest.Web/CipherNest.Web.csproj -- --CipherNest:Port=5190
```

Override the local data directory with:

```bash
export CIPHERNEST_DATA_DIR="$HOME/.local/share/CipherNest"
```

On Windows, the equivalent environment variable can point at a private per-user directory. If no override is supplied, the host uses the platform `LocalApplicationData` location and a `CipherNest` child directory.

## Linux launch

From the repository root:

```bash
bash scripts/run-linux.sh
```

The launcher starts the local host, waits for the loopback health endpoint when `curl` is available, and opens the local URL with `xdg-open` when available.

## Verification

```bash
bash scripts/verify-web.sh
```

The verification script restores, builds, checks formatting, launches against a temporary data directory, and requires the loopback `/healthz` probe to succeed.

GitHub Actions additionally builds this host on Linux, Windows, and macOS and publishes a Linux x64 framework-dependent output.

## Complete source inventory for this host

This section is the delegated exhaustive inventory for `src/CipherNest.Web/`. `tests/CipherNest.UiTests/RepositoryDocumentationInventorySourceTests.cs` combines it with the main `docs/SOURCE_CODE_REFERENCE.md` inventory.

- `src/CipherNest.Web/CipherNest.Web.csproj` — .NET 10 ASP.NET Core Web project referencing the existing Application, Domain, Infrastructure, and Shared layers.
- `src/CipherNest.Web/Program.cs` — loopback-only host composition, local data-directory resolution, existing encrypted-core DI wiring, per-circuit vault session isolation, response hardening, health probe, and Razor component endpoint mapping.
- `src/CipherNest.Web/Components/_Imports.razor` — shared Razor imports for domain/application types and Blazor primitives.
- `src/CipherNest.Web/Components/App.razor` — HTML document shell, local stylesheet, global Interactive Server routing, and Blazor bootstrap script.
- `src/CipherNest.Web/Components/Routes.razor` — application router, not-found surface, and default layout selection.
- `src/CipherNest.Web/Components/Layout/MainLayout.razor` — responsive shell identifying the local-only security model and repository source link.
- `src/CipherNest.Web/Components/Pages/Home.razor` — vault onboarding/unlock/lock/list/search/add/Trash browser workflows with fixed failure messaging and sensitive-field cleanup.
- `src/CipherNest.Web/wwwroot/app.css` — responsive dark UI, focus visibility, reduced-motion handling, mobile breakpoints, and form/card styling.
- `src/CipherNest.Web/README.md` — platform model, security boundary, run/verify instructions, and this exhaustive delegated source inventory.
