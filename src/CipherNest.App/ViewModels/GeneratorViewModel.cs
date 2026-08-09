using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class GeneratorViewModel : ObservableObject
{
    private readonly IPasswordGenerator _generator;
    private readonly IClipboardSecurityService _clipboard;
    private readonly ISettingsStore _settings;

    [ObservableProperty] private bool passphraseMode;
    [ObservableProperty] private int length = 20;
    [ObservableProperty] private int wordCount = 8;
    [ObservableProperty] private bool uppercase = true;
    [ObservableProperty] private bool lowercase = true;
    [ObservableProperty] private bool digits = true;
    [ObservableProperty] private bool symbols = true;
    [ObservableProperty] private bool excludeAmbiguous = true;
    [ObservableProperty] private string generatedValue = string.Empty;
    [ObservableProperty] private string strengthLabel = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public GeneratorViewModel(IPasswordGenerator generator, IClipboardSecurityService clipboard, ISettingsStore settings)
    {
        _generator = generator;
        _clipboard = clipboard;
        _settings = settings;
    }

    [RelayCommand]
    public async Task LoadDefaultsAsync()
    {
        var preferences = await _settings.LoadAsync();
        PassphraseMode = preferences.GeneratorPassphraseMode;
        Length = Math.Clamp(preferences.GeneratorPasswordLength, 8, 256);
        WordCount = Math.Clamp(preferences.GeneratorPassphraseWordCount, 6, 16);
        Uppercase = preferences.GeneratorUppercase;
        Lowercase = preferences.GeneratorLowercase;
        Digits = preferences.GeneratorDigits;
        Symbols = preferences.GeneratorSymbols;
        ExcludeAmbiguous = preferences.GeneratorExcludeAmbiguous;
        if (!PassphraseMode && !Uppercase && !Lowercase && !Digits && !Symbols) Lowercase = true;
        Generate();
    }

    [RelayCommand]
    private void Generate()
    {
        try
        {
            var options = new GeneratorOptions
            {
                Mode = PassphraseMode ? GeneratorMode.Passphrase : GeneratorMode.Password,
                Length = Length,
                WordCount = WordCount,
                Uppercase = Uppercase,
                Lowercase = Lowercase,
                Digits = Digits,
                Symbols = Symbols,
                ExcludeAmbiguous = ExcludeAmbiguous
            };
            GeneratedValue = _generator.Generate(options);
            StrengthLabel = PassphraseMode
                ? $"Generated from 256 local words: approximately {checked(WordCount * 8)} bits of random-selection entropy if kept exactly as generated."
                : _generator.Evaluate(GeneratedValue).Label;
            ErrorMessage = string.Empty;
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (GeneratedValue.Length == 0) return;
        var preferences = await _settings.LoadAsync();
        await _clipboard.CopySecretAsync(GeneratedValue, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");
}
