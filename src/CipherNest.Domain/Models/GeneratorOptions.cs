namespace CipherNest.Domain.Models;

public enum GeneratorMode
{
    Password,
    Passphrase
}

public sealed record GeneratorOptions
{
    public GeneratorMode Mode { get; init; } = GeneratorMode.Password;
    public int Length { get; init; } = 20;
    public bool Uppercase { get; init; } = true;
    public bool Lowercase { get; init; } = true;
    public bool Digits { get; init; } = true;
    public bool Symbols { get; init; } = true;
    public bool ExcludeAmbiguous { get; init; } = true;
    public int WordCount { get; init; } = 5;
    public string Separator { get; init; } = "-";
}
