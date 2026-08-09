using CipherNest.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class GeneratorDefaultsViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;

    [ObservableProperty] private bool passphraseMode;
    [ObservableProperty] private int passwordLength = 20;
    [ObservableProperty] private int passphraseWordCount = 8;
    [ObservableProperty] private bool uppercase = true;
    [ObservableProperty] private bool lowercase = true;
    [ObservableProperty] private bool digits = true;
    [ObservableProperty] private bool symbols = true;
    [ObservableProperty] private bool excludeAmbiguous = true;
    [ObservableProperty] private string statusMessage = string.Empty;

    public GeneratorDefaultsViewModel(ISettingsStore settings) => _settings = settings;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var preferences = await _settings.LoadAsync();
        PassphraseMode = preferences.GeneratorPassphraseMode;
        PasswordLength = preferences.GeneratorPasswordLength;
        PassphraseWordCount = preferences.GeneratorPassphraseWordCount;
        Uppercase = preferences.GeneratorUppercase;
        Lowercase = preferences.GeneratorLowercase;
        Digits = preferences.GeneratorDigits;
        Symbols = preferences.GeneratorSymbols;
        ExcludeAmbiguous = preferences.GeneratorExcludeAmbiguous;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        PasswordLength = Math.Clamp(PasswordLength, 8, 256);
        PassphraseWordCount = Math.Clamp(PassphraseWordCount, 6, 16);
        if (!PassphraseMode && !Uppercase && !Lowercase && !Digits && !Symbols)
        {
            StatusMessage = "Select at least one character group for password mode.";
            return;
        }

        var preferences = await _settings.LoadAsync();
        await _settings.SaveAsync(preferences with
        {
            GeneratorPassphraseMode = PassphraseMode,
            GeneratorPasswordLength = PasswordLength,
            GeneratorPassphraseWordCount = PassphraseWordCount,
            GeneratorUppercase = Uppercase,
            GeneratorLowercase = Lowercase,
            GeneratorDigits = Digits,
            GeneratorSymbols = Symbols,
            GeneratorExcludeAmbiguous = ExcludeAmbiguous
        });
        StatusMessage = "Generator defaults saved locally.";
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//settings");
}
