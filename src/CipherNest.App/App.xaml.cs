using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;

namespace CipherNest.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;
    private DateTimeOffset? _backgroundedUtc;

    public App(IVaultService vault, ISettingsStore settings)
    {
        InitializeComponent();
        _vault = vault;
        _settings = settings;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override async void OnSleep()
    {
        base.OnSleep();
        _backgroundedUtc = DateTimeOffset.UtcNow;
        try
        {
            var preferences = await _settings.LoadAsync();
            if (preferences.LockOnBackground && _vault.IsUnlocked)
            {
                await _vault.LockAsync();
            }
        }
        catch
        {
            if (_vault.IsUnlocked)
            {
                await _vault.LockAsync();
            }
        }
    }

    protected override async void OnResume()
    {
        base.OnResume();
        try
        {
            if (!_vault.IsUnlocked || _backgroundedUtc is null)
            {
                return;
            }
            var preferences = await _settings.LoadAsync();
            if ((DateTimeOffset.UtcNow - _backgroundedUtc.Value).TotalSeconds >= preferences.LockTimeoutSeconds)
            {
                await _vault.LockAsync();
                await Shell.Current.GoToAsync("//unlock");
            }
        }
        catch
        {
            await _vault.LockAsync();
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//unlock");
            }
        }
        finally
        {
            _backgroundedUtc = null;
        }
    }
}
