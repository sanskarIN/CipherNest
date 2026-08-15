using System.Text.Json;

namespace CipherNest.Infrastructure.Services;

public static class BackupHeaderJsonPolicy
{
    private const int RequiredHeaderPropertyCount = 5;
    private const int RequiredKdfPropertyCount = 3;

    public static void Validate(ReadOnlyMemory<byte> headerJson)
    {
        BackupFormatPolicy.ValidateHeaderLength(headerJson.Length);

        using var document = JsonDocument.Parse(
            headerJson,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = BackupFormatPolicy.MaximumHeaderJsonDepth
            });

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Backup header must be a JSON object.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            if (!IsKnownHeaderProperty(property.Name))
                throw new InvalidDataException("Backup header contains unexpected metadata.");
            if (!seen.Add(property.Name))
                throw new InvalidDataException("Backup header contains duplicate metadata.");
        }

        if (seen.Count != RequiredHeaderPropertyCount)
            throw new InvalidDataException("Backup header is missing required metadata.");

        RequireKind(root.GetProperty("Version"), JsonValueKind.Number, "Backup version metadata has an invalid JSON type.");
        RequireKind(root.GetProperty("Salt"), JsonValueKind.String, "Backup salt metadata has an invalid JSON type.");
        RequireKind(root.GetProperty("Kdf"), JsonValueKind.Object, "Backup key-derivation metadata has an invalid JSON type.");
        RequireKind(root.GetProperty("ChunkSize"), JsonValueKind.Number, "Backup chunk-size metadata has an invalid JSON type.");
        RequireKind(root.GetProperty("CreatedUtc"), JsonValueKind.String, "Backup creation-time metadata has an invalid JSON type.");

        ValidateKdfObject(root.GetProperty("Kdf"));
    }

    private static void ValidateKdfObject(JsonElement kdf)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in kdf.EnumerateObject())
        {
            if (!IsKnownKdfProperty(property.Name))
                throw new InvalidDataException("Backup key-derivation metadata contains an unexpected property.");
            if (!seen.Add(property.Name))
                throw new InvalidDataException("Backup key-derivation metadata contains a duplicate property.");
            RequireKind(property.Value, JsonValueKind.Number, "Backup key-derivation metadata contains a non-numeric value.");
        }

        if (seen.Count != RequiredKdfPropertyCount)
            throw new InvalidDataException("Backup key-derivation metadata is incomplete.");
    }

    private static bool IsKnownHeaderProperty(string name) =>
        name is "Version" or "Salt" or "Kdf" or "ChunkSize" or "CreatedUtc";

    private static bool IsKnownKdfProperty(string name) =>
        name is "MemoryKiB" or "Iterations" or "Parallelism";

    private static void RequireKind(JsonElement element, JsonValueKind expected, string message)
    {
        if (element.ValueKind != expected)
            throw new InvalidDataException(message);
    }
}
