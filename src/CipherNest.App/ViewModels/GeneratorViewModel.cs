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
    private readonly IPrivacySafeExceptionReporter _exceptions;

    [ObservableProperty]
    public partial bool PassphraseMode { get; set; }

    [ObservableProperty]
    public partial int Length { get; set; } = 20;

    [ObservableProperty]
    public partial int WordCount { get; set; } = 8;

    [ObservableProperty]
    public partial bool Uppercase { get; set; } = true;

    [ObservableProperty]
    public partial bool Lowercase { get; set; } = true;

    [ObservableProperty]
    public partial bool Digits { get; set; } = true;

    [ObservableProperty]
    public partial bool Symbols { get; set; } = true;

    [ObservableProperty]
    public partial bool ExcludeAmbiguous { get; set; } = true;

    [ObservableProperty]
    public partial string GeneratedValue { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StrengthLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public GeneratorViewModel(IPasswordGenerator generator, IClipboardSecurityService clipboard, ISettingsStore settings, IPrivacySafeExceptionReporter exceptions)
    {
        _generator = generator;
        _clipboard = clipboard;
        _settings = settings;
        _exceptions = exceptions;
    }

    [RelayCommand]
    public async Task LoadDefaultsAsync()
    {
        try
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
        catch (Exception ex)
        {
            _exceptions.Report("Generator.LoadDefaults", ex);
            ClearGeneratedState();
            ErrorMessage = "Generator defaults could not be loaded safely. Review settings access and try again.";
        }
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
        catch (ArgumentException)
        {
            ClearGeneratedState();
            ErrorMessage = "The generator settings are invalid. Review the length, word count, and enabled character groups.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("Generator.Generate", ex);
            ClearGeneratedState();
            ErrorMessage = "A generated value could not be created safely. Review the generator settings and try again.";
        }
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (GeneratedValue.Length == 0) return;
        try
        {
            var preferences = await _settings.LoadAsync();
            await _clipboard.CopySecretAsync(GeneratedValue, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
        }
        catch (Exception ex)
        {
            _exceptions.Report("Generator.Copy", ex);
            ErrorMessage = "The generated value could not be copied safely. Generate a new value or retry after checking clipboard access.";
        }
    }

    private void ClearGeneratedState()
    {
        GeneratedValue = string.Empty;
        StrengthLabel = string.Empty;
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");
}
