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

    partial void OnSelectedTypeChanged(VaultItemType value)
    {
        IsTotpItem = value == VaultItemType.OneTimePassword;
        TotpUriImportText = string.Empty;
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

    [RelayCommand]
    private void ImportTotpUri()
    {
        if (!IsTotpItem || IsReauthenticationRequired) return;
        var uriText = TotpUriImportText;
        TotpUriImportText = string.Empty;
        if (string.IsNullOrWhiteSpace(uriText))
        {
            ErrorMessage = "Paste an otpauth://totp/... setup URI before importing.";
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
            ErrorMessage = "TOTP setup URI imported locally. Review the account, issuer, algorithm, digits, and period before saving.";
        }
        catch (ArgumentException)
        {
            ErrorMessage = "The TOTP setup URI is invalid or unsupported. CipherNest accepts bounded otpauth://totp/... URIs only; HOTP and ambiguous duplicate parameters are rejected.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.ImportTotpUri", ex);
            ErrorMessage = "The TOTP setup URI could not be imported safely. Existing item fields were not intentionally changed after the failure.";
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
            ErrorMessage = "TOTP setup URI copied with timed clipboard cleanup where supported. The URI contains the seed and must be protected like the seed itself.";
        }
        catch (ArgumentException)
        {
            ErrorMessage = "A TOTP setup URI could not be created. Enter a valid Base32 seed and account name, then review the algorithm, digits, and period.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.CopyTotpUri", ex);
            ErrorMessage = "The TOTP setup URI could not be copied safely. The encrypted vault item remains unchanged.";
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
}
