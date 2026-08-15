using System.Text;
using System.Text.Json;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class BackupHeaderJsonPolicyTests
{
    private const string ValidHeader = "{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}";

    [Fact]
    public void CurrentStrictHeader_IsAccepted()
    {
        BackupHeaderJsonPolicy.Validate(Encoding.UTF8.GetBytes(ValidHeader));
    }

    [Theory]
    [InlineData("{\"Version\":2,\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    [InlineData("{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    public void DuplicateMetadata_IsRejected(string json)
    {
        Assert.Throws<InvalidDataException>(() => BackupHeaderJsonPolicy.Validate(Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData("{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\",\"Unexpected\":true}")]
    [InlineData("{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1,\"Unexpected\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    [InlineData("{\"Version\":2,\"version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    public void UnexpectedMetadata_IsRejected(string json)
    {
        Assert.Throws<InvalidDataException>(() => BackupHeaderJsonPolicy.Validate(Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData("{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576}")]
    [InlineData("{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    public void MissingRequiredMetadata_IsRejected(string json)
    {
        Assert.Throws<InvalidDataException>(() => BackupHeaderJsonPolicy.Validate(Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData("{\"Version\":\"2\",\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    [InlineData("{\"Version\":2,\"Salt\":[],\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    [InlineData("{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":null,\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    [InlineData("{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":\"65536\",\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")]
    public void WrongJsonTypes_AreRejected(string json)
    {
        Assert.Throws<InvalidDataException>(() => BackupHeaderJsonPolicy.Validate(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void OverDepthJson_IsRejectedByParserBoundary()
    {
        var nested = string.Concat(Enumerable.Repeat("{\"x\":", BackupFormatPolicy.MaximumHeaderJsonDepth + 1)) +
                     "0" +
                     new string('}', BackupFormatPolicy.MaximumHeaderJsonDepth + 1);
        var json = ValidHeader[..^1] + ",\"Unexpected\":" + nested + "}";

        Assert.Throws<JsonException>(() => BackupHeaderJsonPolicy.Validate(Encoding.UTF8.GetBytes(json)));
    }
}
