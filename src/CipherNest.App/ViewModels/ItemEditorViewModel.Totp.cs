using System.Globalization;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Models;
using CipherNest.App.Services;
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

    [ObservableProperty]
    public partial string TotpUriImportText { get; set; } = string.Empty;

    public string TotpPeriodText => string.Format(
        CultureInfo.CurrentUICulture,
        TotpText("TotpPeriodFormat"),
        TotpPeriodSeconds);

    public string TotpValidityText => TotpSecondsRemaining <= 0
        ? string.Empty
        : string.Format(
            CultureInfo.CurrentUICulture,
            TotpText("TotpValidityFormat"),
            TotpSecondsRemaining);

    partial void OnSelectedTypeChanged(VaultItemType value)
    {
        IsTotpItem = value == VaultItemType.OneTimePassword;
        TotpUriImportText = string.Empty;
        ClearTotpPresentation();
    }

    partial void OnSecretChanged(string value) => ClearTotpPresentation();
    partial void OnSelectedTotpAlgorithmChanged(TotpAlgorithm value) => ClearTotpPresentation();
    partial void OnTotpDigitsChanged(int value) => ClearTotpPresentation();

    partial void OnTotpPeriodSecondsChanged(int value)
    {
        ClearTotpPresentation();
        OnPropertyChanged(nameof(TotpPeriodText));
    }

    partial void OnTotpSecondsRemainingChanged(int value) => OnPropertyChanged(nameof(TotpValidityText));

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
            ErrorMessage = TotpText("TotpInvalidSeedSettingsError");
        }
        catch (Exception ex)
        {
            ClearTotpPresentation();
            _exceptions.Report("ItemEditor.RefreshTotp", ex);
            ErrorMessage = TotpText("TotpGenerateError");
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
            ErrorMessage = TotpText("TotpCopyCodeError");
        }
    }

    [RelayCommand]
    private void ImportTotpUri()
    {
        var uriText = TotpUriImportText;
        TotpUriImportText = string.Empty;
        if (!IsTotpItem || IsReauthenticationRequired) return;
        if (string.IsNullOrWhiteSpace(uriText))
        {
            ErrorMessage = TotpText("TotpImportMissingUriError");
            return;
        }

        try
        {
            var profile = ServiceProviderHelper.GetRequiredService<ITotpUriCodec>().Parse(uriText);
            Secret = profile.Secret;
            SelectedTotpAlgorithm = profile.Algorithm;
            TotpDigits = profile.Digits;
            TotpPeriodSeconds = profile.PeriodSeconds;
            Username = profile.AccountName;
            Title = string.IsNullOrWhiteSpace(profile.Issuer) ? profile.AccountName : profile.Issuer;
            ErrorMessage = TotpText("TotpImportSuccess");
        }
        catch (ArgumentException)
        {
            ErrorMessage = TotpText("TotpImportInvalidError");
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.ImportTotpUri", ex);
            ErrorMessage = TotpText("TotpImportFailureError");
        }
        finally
        {
            uriText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CopyTotpUriAsync()
    {
        if (!IsTotpItem || IsReauthenticationRequired) return;
        string uriText = string.Empty;
        try
        {
            var accountName = Username.Trim();
            var issuer = Title.Trim();
            if (string.IsNullOrWhiteSpace(accountName))
            {
                accountName = issuer;
                issuer = string.Empty;
            }

            var profile = new TotpUriProfile(accountName, issuer, Secret, SelectedTotpAlgorithm, TotpDigits, TotpPeriodSeconds);
            uriText = ServiceProviderHelper.GetRequiredService<ITotpUriCodec>().Format(profile);
            var preferences = await _settings.LoadAsync();
            await _clipboard.CopySecretAsync(uriText, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
            ErrorMessage = TotpText("TotpCopyUriSuccess");
        }
        catch (ArgumentException)
        {
            ErrorMessage = TotpText("TotpCopyUriInvalidError");
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.CopyTotpUri", ex);
            ErrorMessage = TotpText("TotpCopyUriFailureError");
        }
        finally
        {
            uriText = string.Empty;
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
        TotpUriImportText = string.Empty;
        ClearTotpPresentation();
    }

    private void ClearTotpPresentation()
    {
        CurrentTotpCode = string.Empty;
        TotpSecondsRemaining = 0;
    }

    private static string TotpText(string key) =>
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(key);
}
