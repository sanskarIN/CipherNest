using System.Globalization;
using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Models;
using CipherNest.Application.Validation;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class TotpUriCodec : ITotpUriCodec
{
    public const int MaximumUriCharacters = 8_192;
    public const int MaximumQueryPairs = 16;
    public const int MaximumAccountNameCharacters = 512;
    public const int MaximumIssuerCharacters = 256;

    public TotpUriProfile Parse(string uriText)
    {
        ArgumentNullException.ThrowIfNull(uriText);
        if (uriText.Length is 0 or > MaximumUriCharacters)
            throw new ArgumentException($"TOTP URI must contain between 1 and {MaximumUriCharacters:N0} characters.", nameof(uriText));
        if (uriText.Any(char.IsControl))
            throw new ArgumentException("TOTP URI must not contain control characters.", nameof(uriText));
        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
            throw new ArgumentException("TOTP URI is not a valid absolute URI.", nameof(uriText));
        if (!string.Equals(uri.Scheme, "otpauth", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only otpauth:// TOTP URIs are supported.", nameof(uriText));
        if (!string.Equals(uri.Host, "totp", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only otpauth://totp/... URIs are supported; HOTP is not supported.", nameof(uriText));
        if (!string.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("TOTP URI must not contain user-info, a custom port, or a fragment.", nameof(uriText));

        var escapedPath = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        if (string.IsNullOrEmpty(escapedPath) || escapedPath.Contains('/'))
            throw new ArgumentException("TOTP URI must contain exactly one account label path segment.", nameof(uriText));

        var label = DecodeComponent(escapedPath, plusAsSpace: false, "label").Trim();
        ValidateDisplayText(label, MaximumIssuerCharacters + 1 + MaximumAccountNameCharacters, "label");

        var query = ParseQuery(uri.Query);
        if (!query.TryGetValue("secret", out var rawSecret) || string.IsNullOrWhiteSpace(rawSecret))
            throw new ArgumentException("TOTP URI must contain a secret query parameter.", nameof(uriText));

        var secret = TotpPolicy.NormalizeSecret(DecodeComponent(rawSecret, plusAsSpace: false, "secret"));
        var algorithm = query.TryGetValue("algorithm", out var rawAlgorithm)
            ? ParseAlgorithm(DecodeComponent(rawAlgorithm, plusAsSpace: true, "algorithm"))
            : TotpAlgorithm.Sha1;
        var digits = query.TryGetValue("digits", out var rawDigits)
            ? ParseInteger(DecodeComponent(rawDigits, plusAsSpace: true, "digits"), "digits")
            : 6;
        var period = query.TryGetValue("period", out var rawPeriod)
            ? ParseInteger(DecodeComponent(rawPeriod, plusAsSpace: true, "period"), "period")
            : 30;
        if (query.ContainsKey("counter"))
            throw new ArgumentException("The counter parameter belongs to HOTP and is not accepted for TOTP URIs.", nameof(uriText));
        TotpPolicy.ValidateSettings(algorithm, digits, period);

        var separator = label.IndexOf(':');
        var labelIssuer = separator >= 0 ? label[..separator].Trim() : string.Empty;
        var accountName = separator >= 0 ? label[(separator + 1)..].Trim() : label;
        ValidateDisplayText(accountName, MaximumAccountNameCharacters, "account name");
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("TOTP URI account name must not be empty.", nameof(uriText));

        var queryIssuer = query.TryGetValue("issuer", out var rawIssuer)
            ? DecodeComponent(rawIssuer, plusAsSpace: true, "issuer").Trim()
            : string.Empty;
        if (!string.IsNullOrEmpty(queryIssuer)) ValidateDisplayText(queryIssuer, MaximumIssuerCharacters, "issuer");
        if (!string.IsNullOrEmpty(labelIssuer)) ValidateDisplayText(labelIssuer, MaximumIssuerCharacters, "issuer");
        if (!string.IsNullOrEmpty(labelIssuer) && !string.IsNullOrEmpty(queryIssuer) &&
            !string.Equals(labelIssuer, queryIssuer, StringComparison.Ordinal))
        {
            throw new ArgumentException("TOTP URI issuer in the label does not match the issuer query parameter.", nameof(uriText));
        }

        var issuer = !string.IsNullOrEmpty(queryIssuer) ? queryIssuer : labelIssuer;
        return new TotpUriProfile(accountName, issuer, secret, algorithm, digits, period);
    }

    public string Format(TotpUriProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var accountName = profile.AccountName?.Trim() ?? string.Empty;
        var issuer = profile.Issuer?.Trim() ?? string.Empty;
        ValidateDisplayText(accountName, MaximumAccountNameCharacters, "account name");
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("TOTP account name must not be empty.", nameof(profile));
        if (!string.IsNullOrEmpty(issuer)) ValidateDisplayText(issuer, MaximumIssuerCharacters, "issuer");

        var secret = TotpPolicy.NormalizeSecret(profile.Secret);
        TotpPolicy.ValidateSettings(profile.Algorithm, profile.Digits, profile.PeriodSeconds);
        var label = string.IsNullOrEmpty(issuer) ? accountName : $"{issuer}:{accountName}";
        var algorithm = profile.Algorithm switch
        {
            TotpAlgorithm.Sha1 => "SHA1",
            TotpAlgorithm.Sha256 => "SHA256",
            TotpAlgorithm.Sha512 => "SHA512",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), "Unsupported TOTP algorithm.")
        };

        var builder = new StringBuilder(256 + secret.Length + label.Length + issuer.Length);
        builder.Append("otpauth://totp/");
        builder.Append(Uri.EscapeDataString(label));
        builder.Append("?secret=");
        builder.Append(Uri.EscapeDataString(secret));
        if (!string.IsNullOrEmpty(issuer))
        {
            builder.Append("&issuer=");
            builder.Append(Uri.EscapeDataString(issuer));
        }
        builder.Append("&algorithm=");
        builder.Append(algorithm);
        builder.Append("&digits=");
        builder.Append(profile.Digits.ToString(CultureInfo.InvariantCulture));
        builder.Append("&period=");
        builder.Append(profile.PeriodSeconds.ToString(CultureInfo.InvariantCulture));

        if (builder.Length > MaximumUriCharacters)
            throw new ArgumentException($"Formatted TOTP URI exceeds the {MaximumUriCharacters:N0}-character safety limit.", nameof(profile));
        return builder.ToString();
    }

    private static Dictionary<string, string> ParseQuery(string queryText)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(queryText)) return result;

        var query = queryText[0] == '?' ? queryText[1..] : queryText;
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        if (pairs.Length > MaximumQueryPairs)
            throw new ArgumentException($"TOTP URI exceeds the {MaximumQueryPairs}-parameter safety limit.", nameof(queryText));

        foreach (var pair in pairs)
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
                throw new ArgumentException("Each TOTP URI query parameter must use name=value syntax.", nameof(queryText));
            var key = DecodeComponent(pair[..separator], plusAsSpace: true, "query parameter name");
            if (key.Length is 0 or > 64 || key.Any(static character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
                throw new ArgumentException("TOTP URI contains an invalid query parameter name.", nameof(queryText));
            if (!result.TryAdd(key, pair[(separator + 1)..]))
                throw new ArgumentException($"TOTP URI contains duplicate '{key}' parameters.", nameof(queryText));
        }

        return result;
    }

    private static TotpAlgorithm ParseAlgorithm(string value)
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal).Trim();
        return normalized.ToUpperInvariant() switch
        {
            "SHA1" => TotpAlgorithm.Sha1,
            "SHA256" => TotpAlgorithm.Sha256,
            "SHA512" => TotpAlgorithm.Sha512,
            _ => throw new ArgumentException("TOTP URI algorithm must be SHA1, SHA256, or SHA512.", nameof(value))
        };
    }

    private static int ParseInteger(string value, string fieldName)
    {
        if (!int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            throw new ArgumentException($"TOTP URI {fieldName} must be a base-10 integer.", fieldName);
        return parsed;
    }

    private static string DecodeComponent(string value, bool plusAsSpace, string fieldName)
    {
        ValidatePercentEncoding(value, fieldName);
        var encoded = plusAsSpace ? value.Replace("+", "%20", StringComparison.Ordinal) : value;
        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(encoded);
        }
        catch (UriFormatException ex)
        {
            throw new ArgumentException($"TOTP URI {fieldName} contains invalid percent encoding.", fieldName, ex);
        }
        if (decoded.Any(char.IsControl))
            throw new ArgumentException($"TOTP URI {fieldName} must not contain control characters.", fieldName);
        return decoded;
    }

    private static void ValidatePercentEncoding(string value, string fieldName)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%') continue;
            if (index + 2 >= value.Length || !IsHex(value[index + 1]) || !IsHex(value[index + 2]))
                throw new ArgumentException($"TOTP URI {fieldName} contains invalid percent encoding.", fieldName);
            index += 2;
        }
    }

    private static bool IsHex(char value) =>
        value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    private static void ValidateDisplayText(string value, int maximumCharacters, string fieldName)
    {
        if (value.Length > maximumCharacters)
            throw new ArgumentException($"TOTP {fieldName} exceeds the {maximumCharacters:N0}-character safety limit.", fieldName);
        foreach (var rune in value.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
                throw new ArgumentException($"TOTP {fieldName} must not contain control or formatting characters.", fieldName);
        }
    }
}
