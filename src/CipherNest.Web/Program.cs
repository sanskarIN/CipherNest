using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;
using CipherNest.Shared;
using CipherNest.Web.Components;
using CipherNest.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var configuredPort = builder.Configuration.GetValue<int?>("CipherNest:Port") ?? 5187;
if (configuredPort is < 1024 or > 65535)
{
    throw new InvalidOperationException("CipherNest:Port must be between 1024 and 65535.");
}

// This host deliberately listens on loopback only. CipherNest Web is a local UI for
// the same encrypted on-device vault, not a remotely hosted password-manager service.
builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(configuredPort));

var dataDirectory = builder.Configuration["CipherNest:DataDirectory"];
if (string.IsNullOrWhiteSpace(dataDirectory))
{
    dataDirectory = Environment.GetEnvironmentVariable("CIPHERNEST_DATA_DIR");
}
if (string.IsNullOrWhiteSpace(dataDirectory))
{
    var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrWhiteSpace(localData))
    {
        localData = AppContext.BaseDirectory;
    }
    dataDirectory = Path.Combine(localData, AppConstants.ProductName);
}

Directory.CreateDirectory(dataDirectory);
var databasePath = Path.Combine(dataDirectory, AppConstants.DatabaseFileName);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddSingleton<IVaultStore>(_ => new SqliteVaultStore(databasePath));
// VaultService owns decrypted session key state. In an Interactive Server host it must
// be scoped to the browser circuit so another tab/client cannot inherit an unlocked key.
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddSingleton<WebVaultCreationCoordinator>();
builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddSingleton<ITotpService, TotpService>();
builder.Services.AddSingleton<ITotpUriCodec, TotpUriCodec>();
builder.Services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
builder.Services.AddSingleton<ISafeNoteMarkupService, SafeNoteMarkupService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-store";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; " +
        "img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; " +
        "connect-src 'self' ws: wss:";
    await next();
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", host = "loopback" }));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation(
        "CipherNest local web UI is listening on http://127.0.0.1:{Port}. Vault data directory: {DataDirectory}",
        configuredPort,
        dataDirectory);
});

app.Run();
