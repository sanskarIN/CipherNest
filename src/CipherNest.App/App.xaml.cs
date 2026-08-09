using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Domain.Models;

namespace CipherNest.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;
    private readonly IScreenshotProtectionService _screenshots;
    private DateTimeOffset? _inactiveUtc;

    public App(IVaultService vault, ISettingsStore settings, IScreenshotProtectionService screenshots)
    {
        InitializeComponent();
        _vault = vault;
        _settings = settings;
        _screenshots = screenshots;
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
            if (preferences.LockOnBackground && _vault.IsUnlocked) await _vault.LockAsync();
        }
        catch
        {
            if (_vault.IsUnlocked) await _vault.LockAsync();
        }
    }

    private async Task HandleActiveAsync()
    {
        try
        {
            var preferences = await _settings.LoadAsync();
            ApplyTheme(preferences.Theme);
            AccessibilityPreferenceApplicator.Apply(preferences.LargerInterface, preferences.ReducedMotion);
            await _screenshots.ApplyAsync(preferences.ScreenshotProtection);
            if (_vault.IsUnlocked && _inactiveUtc is { } inactive && (DateTimeOffset.UtcNow - inactive).TotalSeconds >= preferences.LockTimeoutSeconds)
            {
                await _vault.LockAsync();
            }
            if (!_vault.IsUnlocked && Shell.Current is not null && await _vault.HasVaultAsync())
            {
                await Shell.Current.GoToAsync("//unlock");
            }
        }
        catch
        {
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
            AccessibilityPreferenceApplicator.Apply(preferences.LargerInterface, preferences.ReducedMotion);
            await _screenshots.ApplyAsync(preferences.ScreenshotProtection);
        }
        catch
        {
            ApplyTheme(AppThemePreference.System);
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
