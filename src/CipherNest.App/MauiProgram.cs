using CipherNest.Application.Abstractions;
using CipherNest.Application.Services;
using CipherNest.App.Services;
using CipherNest.App.ViewModels;
using CipherNest.App.Views;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;
using CipherNest.Shared;
using Microsoft.Extensions.Logging;

namespace CipherNest.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        var dataDirectory = FileSystem.Current.AppDataDirectory;
        var dbPath = Path.Combine(dataDirectory, AppConstants.DatabaseFileName);
        var settingsPath = Path.Combine(dataDirectory, "settings.json");

        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton<ICryptoService, CryptoService>();
        builder.Services.AddSingleton<IVaultStore>(_ => new SqliteVaultStore(dbPath));
        builder.Services.AddSingleton<IVaultService, VaultService>();
        builder.Services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
        builder.Services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        builder.Services.AddSingleton<ISafeNoteMarkupService, SafeNoteMarkupService>();
        builder.Services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore(settingsPath));
        builder.Services.AddSingleton<IBackupService, EncryptedBackupService>();
        builder.Services.AddSingleton<IPlaintextTransferService, CsvTransferService>();
        builder.Services.AddSingleton<IClipboardSecurityService, ClipboardSecurityService>();
        builder.Services.AddSingleton<IScreenshotProtectionService, ScreenshotProtectionService>();
        builder.Services.AddSingleton<IBiometricUnlockService, BiometricUnlockService>();
        builder.Services.AddSingleton<IStorageMaintenanceService, StorageMaintenanceService>();
        builder.Services.AddSingleton<IPrivacySafeExceptionReporter, PrivacySafeExceptionReporter>();
        builder.Services.AddSingleton<UnlockRateLimiter>();
        builder.Services.AddSingleton<SessionSecurityState>();

        builder.Services.AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<UnlockViewModel>();
        builder.Services.AddTransient<VaultViewModel>();
        builder.Services.AddTransient<ItemEditorViewModel>();
        builder.Services.AddTransient<GeneratorViewModel>();
        builder.Services.AddTransient<GeneratorDefaultsViewModel>();
        builder.Services.AddTransient<AuditViewModel>();
        builder.Services.AddTransient<TrashViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<TransferViewModel>();
        builder.Services.AddTransient<DeveloperViewModel>();

        builder.Services.AddTransient<StartupPage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<UnlockPage>();
        builder.Services.AddTransient<VaultPage>();
        builder.Services.AddTransient<ItemEditorPage>();
        builder.Services.AddTransient<GeneratorPage>();
        builder.Services.AddTransient<GeneratorDefaultsPage>();
        builder.Services.AddTransient<AuditPage>();
        builder.Services.AddTransient<TrashPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<TransferPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<DeveloperPage>();

        return builder.Build();
    }
}
