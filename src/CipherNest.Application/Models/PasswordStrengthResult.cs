namespace CipherNest.Application.Models;

public sealed record PasswordStrengthResult(int Score, string Label, IReadOnlyList<string> Suggestions);
