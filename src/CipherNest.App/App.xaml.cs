using CipherNest.Application.Abstractions;
using CipherNest.Application.Services;
using CipherNest.App.Services;
using CipherNest.Domain.Models;

namespace CipherNest.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;
    private readonly IScreenshotProtectionService _screenshots;
    private readonly IPrivacySafeExceptionReporter _exceptions;
    private readonly ILocalizationService _localization;
    private readonly SessionLockPolicy _lockPolicy;
    private DateTimeOffset? _inactiveUtc;

    public App(
        IVaultService vault,
        ISettingsStore settings,
        IScreenshotProtectionService screenshots,
        IPrivacySafeExceptionReporter exceptions,
        ILocalizationService localization,
        SessionLockPolicy lockPolicy)
    {
        InitializeComponent();
        _vault = vault;
        _settings = settings;
        _screenshots = screenshots;
        _exceptions = exceptions;
        _localization = localization;
        _lockPolicy = lockPolicy;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Deactivated += OnWindowDeactivated;
        window.Stopped += OnWindowStopped;
        window.Resumed += OnWindowResumed;
        window.Activated += OnWindowActivated;
        _ = ApplyInitialPreferencesAsync();
        return window;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception) _exceptions.Report("AppDomain.UnhandledException", exception, fatal: e.IsTerminating);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _exceptions.Report("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private async void OnWindowDeactivated(object? sender, EventArgs e) => await HandleInactiveAsync();
    private async void OnWindowStopped(object? sender, EventArgs e) => await HandleInactiveAsync();
    private async void OnWindowResumed(object? sender, EventArgs e) => await HandleActiveAsync();
    private async void OnWindowActivated(object? sender, EventArgs e) => await HandleActiveAsync();

    private async Task HandleInactiveAsync()
    {
        _inactiveUtc ??= DateTimeOffset.UtcNow;
        try
        {
            var preferences = await _settings.LoadAsync();
            if (_lockPolicy.ShouldLockWhenBackgrounded(preferences, _vault.IsUnlocked)) await _vault.LockAsync();
        }
        catch (Exception exception)
        {
            _exceptions.Report("Lifecycle.Inactive", exception);
            if (_vault.IsUnlocked) await _vault.LockAsync();
        }
    }

    private async Task HandleActiveAsync()
    {
        try
        {
            var preferences = await _settings.LoadAsync();
            ApplyTheme(preferences.Theme);
            _localization.Apply(preferences.Language);
            AccessibilityPreferenceApplicator.Apply(preferences.LargerInterface, preferences.ReducedMotion);
            await _screenshots.ApplyAsync(preferences.ScreenshotProtection);
            if (_lockPolicy.ShouldLockAfterInactivity(preferences, _vault.IsUnlocked, _inactiveUtc, DateTimeOffset.UtcNow))
            {
                await _vault.LockAsync();
            }
            if (!_vault.IsUnlocked && Shell.Current is not null && await _vault.HasVaultAsync())
            {
                await Shell.Current.GoToAsync("//unlock");
            }
        }
        catch (Exception exception)
        {
            _exceptions.Report("Lifecycle.Active", exception);
            if (_vault.IsUnlocked) await _vault.LockAsync();
        }
        finally
        {
            _inactiveUtc = null;
        }
    }

    private async Task ApplyInitialPreferencesAsync()
    {
        try
        {
            var preferences = await _settings.LoadAsync();
            ApplyTheme(preferences.Theme);
            _localization.Apply(preferences.Language);
            AccessibilityPreferenceApplicator.Apply(preferences.LargerInterface, preferences.ReducedMotion);
            await _screenshots.ApplyAsync(preferences.ScreenshotProtection);
        }
        catch (Exception exception)
        {
            _exceptions.Report("Startup.Preferences", exception);
            ApplyTheme(AppThemePreference.System);
            _localization.Apply(AppLanguagePreference.System);
            AccessibilityPreferenceApplicator.Apply(largerInterface: false, reducedMotion: true);
        }
    }

    private static void ApplyTheme(AppThemePreference theme)
    {
        if (Current is null) return;
        Current.UserAppTheme = theme switch
        {
            AppThemePreference.Light => AppTheme.Light,
            AppThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
