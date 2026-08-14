using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class ItemEditorViewModel
{
    public IReadOnlyList<TotpAlgorithm> TotpAlgorithms { get; } = Enum.GetValues<TotpAlgorithm>();
    public IReadOnlyList<int> TotpDigitOptions { get; } = [6, 8];

    [ObservableProperty]
    public partial TotpAlgorithm SelectedTotpAlgorithm { get; set; } = TotpAlgorithm.Sha1;

    [ObservableProperty]
    public partial int TotpDigits { get; set; } = 6;

    [ObservableProperty]
    public partial int TotpPeriodSeconds { get; set; } = 30;

    [ObservableProperty]
    public partial bool IsTotpItem { get; set; }

    [ObservableProperty]
    public partial string CurrentTotpCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotpSecondsRemaining { get; set; }

    partial void OnSelectedTypeChanged(VaultItemType value)
    {
        IsTotpItem = value == VaultItemType.OneTimePassword;
        ClearTotpPresentation();
    }

    partial void OnSecretChanged(string value) => ClearTotpPresentation();
    partial void OnSelectedTotpAlgorithmChanged(TotpAlgorithm value) => ClearTotpPresentation();
    partial void OnTotpDigitsChanged(int value) => ClearTotpPresentation();
    partial void OnTotpPeriodSecondsChanged(int value) => ClearTotpPresentation();

    [RelayCommand]
    private void RefreshTotp()
    {
        if (!IsTotpItem || IsReauthenticationRequired)
        {
            ClearTotpPresentation();
            return;
        }

        try
        {
            var result = ServiceProviderHelper.GetRequiredService<ITotpService>().Generate(
                Secret,
                SelectedTotpAlgorithm,
                TotpDigits,
                TotpPeriodSeconds,
                DateTimeOffset.UtcNow);
            CurrentTotpCode = result.Code;
            TotpSecondsRemaining = result.SecondsRemaining;
            ErrorMessage = string.Empty;
        }
        catch (ArgumentException)
        {
            ClearTotpPresentation();
            ErrorMessage = "The TOTP seed or settings are invalid. Use a Base32 seed, 6 or 8 digits, and a period from 15 to 120 seconds.";
        }
        catch (Exception ex)
        {
            ClearTotpPresentation();
            _exceptions.Report("ItemEditor.RefreshTotp", ex);
            ErrorMessage = "The one-time code could not be generated safely. The encrypted seed remains unchanged.";
        }
    }

    [RelayCommand]
    private async Task CopyTotpCodeAsync()
    {
        if (!IsTotpItem || IsReauthenticationRequired) return;
        RefreshTotp();
        if (string.IsNullOrEmpty(CurrentTotpCode)) return;

        try
        {
            var preferences = await _settings.LoadAsync();
            await _clipboard.CopySecretAsync(CurrentTotpCode, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.CopyTotp", ex);
            ErrorMessage = "The one-time code could not be copied safely. You can still read it directly while the vault remains unlocked.";
        }
    }

    private VaultItem ApplyTotpSettings(VaultItem item) => item with
    {
        TotpAlgorithm = SelectedTotpAlgorithm,
        TotpDigits = TotpDigits,
        TotpPeriodSeconds = TotpPeriodSeconds
    };

    private void PopulateTotp(VaultItem item)
    {
        SelectedTotpAlgorithm = item.TotpAlgorithm;
        TotpDigits = item.TotpDigits;
        TotpPeriodSeconds = item.TotpPeriodSeconds;
        IsTotpItem = item.Type == VaultItemType.OneTimePassword;
        ClearTotpPresentation();
    }

    private void ClearTotpPresentation()
    {
        CurrentTotpCode = string.Empty;
        TotpSecondsRemaining = 0;
    }
}
