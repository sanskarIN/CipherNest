using CipherNest.Domain.Models;

namespace CipherNest.Application.Validation;

public static class TotpPolicy
{
    public const int MinimumSecretCharacters = 16;
    public const int MaximumSecretCharacters = 1024;
    public const int MaximumFormattedInputCharacters = 4096;
    public const int MinimumPeriodSeconds = 15;
    public const int MaximumPeriodSeconds = 120;

    public static string NormalizeSecret(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        if (secret.Length > MaximumFormattedInputCharacters)
            throw new ArgumentException($"Formatted TOTP secret exceeds the {MaximumFormattedInputCharacters:N0}-character input safety limit.", nameof(secret));

        var normalized = new char[Math.Min(secret.Length, MaximumSecretCharacters + 1)];
        var length = 0;
        var paddingStarted = false;

        foreach (var raw in secret)
        {
            if (char.IsWhiteSpace(raw) || raw == '-') continue;
            if (length >= normalized.Length)
                throw new ArgumentException($"TOTP secret exceeds the {MaximumSecretCharacters:N0}-character safety limit.", nameof(secret));

            var value = char.ToUpperInvariant(raw);
            if (value == '=')
            {
                paddingStarted = true;
                normalized[length++] = value;
                continue;
            }

            if (paddingStarted)
                throw new ArgumentException("TOTP Base32 padding must appear only at the end of the secret.", nameof(secret));
            if (value is not (>= 'A' and <= 'Z') && value is not (>= '2' and <= '7'))
                throw new ArgumentException("TOTP secret must use Base32 characters A-Z and 2-7.", nameof(secret));

            normalized[length++] = value;
        }

        while (length > 0 && normalized[length - 1] == '=') length--;
        if (length < MinimumSecretCharacters)
            throw new ArgumentException($"TOTP secret must contain at least {MinimumSecretCharacters} Base32 characters.", nameof(secret));
        if (length > MaximumSecretCharacters)
            throw new ArgumentException($"TOTP secret exceeds the {MaximumSecretCharacters:N0}-character safety limit.", nameof(secret));

        var remainder = length % 8;
        if (remainder is 1 or 3 or 6)
            throw new ArgumentException("TOTP secret has an invalid Base32 length.", nameof(secret));

        return new string(normalized, 0, length);
    }

    public static void ValidateSettings(TotpAlgorithm algorithm, int digits, int periodSeconds)
    {
        if (!Enum.IsDefined(algorithm)) throw new ArgumentOutOfRangeException(nameof(algorithm), "Unsupported TOTP algorithm.");
        if (digits is not (6 or 8)) throw new ArgumentOutOfRangeException(nameof(digits), "TOTP codes must use 6 or 8 digits.");
        if (periodSeconds is < MinimumPeriodSeconds or > MaximumPeriodSeconds)
            throw new ArgumentOutOfRangeException(nameof(periodSeconds), $"TOTP period must be between {MinimumPeriodSeconds} and {MaximumPeriodSeconds} seconds.");
    }
}
