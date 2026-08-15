using System.Text;
using System.Text.Json;
using CipherNest.Shared;

namespace CipherNest.Infrastructure.Services;

public static class VaultHeaderJsonPolicy
{
    public const int MinimumSupportedVersion = 1;
    public const int CurrentVersion = 2;

    private const int Version1RootPropertyCount = 3;
    private const int Version2RootPropertyCount = 4;
    private const int WrappedKeyPropertyCount = 6;
    private const int KdfPropertyCount = 3;

    public static void Validate(string headerJson)
    {
        ArgumentNullException.ThrowIfNull(headerJson);
        var utf8Bytes = Encoding.UTF8.GetByteCount(headerJson);
        if (utf8Bytes is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes)
            throw new InvalidDataException("Vault header exceeds the supported size limit.");

        using var document = JsonDocument.Parse(
            headerJson,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = VaultStorageLimits.MaximumVaultHeaderJsonDepth
            });

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Vault header must be a JSON object.");

        var rootProperties = ReadUniqueProperties(root, IsKnownRootProperty, "Vault header contains unexpected metadata.", "Vault header contains duplicate metadata.");
        if (!rootProperties.Contains("version") || !rootProperties.Contains("master") || !rootProperties.Contains("recovery"))
            throw new InvalidDataException("Vault header is missing required metadata.");

        var versionElement = root.GetProperty("version");
        RequireInteger(versionElement, "Vault header version metadata is invalid.");
        if (!versionElement.TryGetInt32(out var version) || version is < MinimumSupportedVersion or > CurrentVersion)
            throw new InvalidDataException("Vault header version is unsupported.");

        if (version == 1)
        {
            if (rootProperties.Count != Version1RootPropertyCount || rootProperties.Contains("secondary"))
                throw new InvalidDataException("Vault header version 1 contains incompatible metadata.");
        }
        else
        {
            if (rootProperties.Count != Version2RootPropertyCount || !rootProperties.Contains("secondary"))
                throw new InvalidDataException("Vault header version 2 is missing required metadata.");
        }

        ValidateWrappedKey(root.GetProperty("master"), allowNull: false, "master");
        ValidateWrappedKey(root.GetProperty("recovery"), allowNull: true, "recovery");
        if (version == 2)
            ValidateWrappedKey(root.GetProperty("secondary"), allowNull: true, "secondary");
    }

    private static void ValidateWrappedKey(JsonElement element, bool allowNull, string label)
    {
        if (allowNull && element.ValueKind == JsonValueKind.Null) return;
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Vault header {label} wrapper must be a JSON object or null where permitted.");

        var properties = ReadUniqueProperties(
            element,
            IsKnownWrappedKeyProperty,
            $"Vault header {label} wrapper contains unexpected metadata.",
            $"Vault header {label} wrapper contains duplicate metadata.");

        if (properties.Count != WrappedKeyPropertyCount ||
            !properties.Contains("version") ||
            !properties.Contains("salt") ||
            !properties.Contains("kdf") ||
            !properties.Contains("nonce") ||
            !properties.Contains("ciphertext") ||
            !properties.Contains("tag"))
        {
            throw new InvalidDataException($"Vault header {label} wrapper is missing required metadata.");
        }

        RequireInteger(element.GetProperty("version"), $"Vault header {label} wrapper version metadata is invalid.");
        RequireKind(element.GetProperty("salt"), JsonValueKind.String, $"Vault header {label} wrapper salt metadata is invalid.");
        RequireKind(element.GetProperty("kdf"), JsonValueKind.Object, $"Vault header {label} wrapper KDF metadata is invalid.");
        RequireKind(element.GetProperty("nonce"), JsonValueKind.String, $"Vault header {label} wrapper nonce metadata is invalid.");
        RequireKind(element.GetProperty("ciphertext"), JsonValueKind.String, $"Vault header {label} wrapper ciphertext metadata is invalid.");
        RequireKind(element.GetProperty("tag"), JsonValueKind.String, $"Vault header {label} wrapper tag metadata is invalid.");
        ValidateKdf(element.GetProperty("kdf"), label);
    }

    private static void ValidateKdf(JsonElement kdf, string label)
    {
        var properties = ReadUniqueProperties(
            kdf,
            IsKnownKdfProperty,
            $"Vault header {label} wrapper KDF contains unexpected metadata.",
            $"Vault header {label} wrapper KDF contains duplicate metadata.");

        if (properties.Count != KdfPropertyCount ||
            !properties.Contains("memoryKiB") ||
            !properties.Contains("iterations") ||
            !properties.Contains("parallelism"))
        {
            throw new InvalidDataException($"Vault header {label} wrapper KDF is missing required metadata.");
        }

        RequireInteger(kdf.GetProperty("memoryKiB"), $"Vault header {label} wrapper KDF memory metadata is invalid.");
        RequireInteger(kdf.GetProperty("iterations"), $"Vault header {label} wrapper KDF iteration metadata is invalid.");
        RequireInteger(kdf.GetProperty("parallelism"), $"Vault header {label} wrapper KDF parallelism metadata is invalid.");
    }

    private static HashSet<string> ReadUniqueProperties(
        JsonElement element,
        Func<string, bool> isKnown,
        string unexpectedMessage,
        string duplicateMessage)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!isKnown(property.Name)) throw new InvalidDataException(unexpectedMessage);
            if (!seen.Add(property.Name)) throw new InvalidDataException(duplicateMessage);
        }
        return seen;
    }

    private static bool IsKnownRootProperty(string name) =>
        name is "version" or "master" or "recovery" or "secondary";

    private static bool IsKnownWrappedKeyProperty(string name) =>
        name is "version" or "salt" or "kdf" or "nonce" or "ciphertext" or "tag";

    private static bool IsKnownKdfProperty(string name) =>
        name is "memoryKiB" or "iterations" or "parallelism";

    private static void RequireInteger(JsonElement element, string message)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out _))
            throw new InvalidDataException(message);
    }

    private static void RequireKind(JsonElement element, JsonValueKind expected, string message)
    {
        if (element.ValueKind != expected) throw new InvalidDataException(message);
    }
}
