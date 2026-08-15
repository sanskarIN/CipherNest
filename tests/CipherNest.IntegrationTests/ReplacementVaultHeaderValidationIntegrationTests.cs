using CipherNest.Infrastructure.Persistence;

namespace CipherNest.IntegrationTests;

public sealed class ReplacementVaultHeaderValidationIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestReplacementHeader", Guid.NewGuid().ToString("N"));
    private string ActivePath => Path.Combine(_directory, "active.db");
    private string CandidatePath => Path.Combine(_directory, "candidate.db");

    public ReplacementVaultHeaderValidationIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task MalformedButBoundedCandidateHeader_IsRejectedBeforeActiveDatabaseMutation()
    {
        var active = new SqliteVaultStore(ActivePath);
        await active.InitializeAsync();
        var activeHeader = BuildVersion2Header();
        await active.WriteHeaderAsync(activeHeader);

        var candidate = new SqliteVaultStore(CandidatePath);
        await candidate.InitializeAsync();
        var malformed = BuildVersion2Header()[..^1] + ",\"unexpected\":true}";
        await candidate.WriteHeaderAsync(malformed);

        await Assert.ThrowsAsync<InvalidDataException>(() => active.ReplaceDatabaseAsync(CandidatePath));

        Assert.Equal(activeHeader, await active.ReadHeaderAsync());
        Assert.True(File.Exists(CandidatePath));
    }

    [Fact]
    public async Task LegacyVersion1CandidateHeader_RemainsReplacementCompatible()
    {
        var active = new SqliteVaultStore(ActivePath);
        await active.InitializeAsync();
        await active.WriteHeaderAsync(BuildVersion2Header());

        var candidate = new SqliteVaultStore(CandidatePath);
        await candidate.InitializeAsync();
        var legacy = BuildVersion1Header();
        await candidate.WriteHeaderAsync(legacy);

        await active.ReplaceDatabaseAsync(CandidatePath);

        Assert.Equal(legacy, await active.ReadHeaderAsync());
    }

    private static string BuildVersion2Header()
    {
        var wrapper = BuildWrapper();
        return $"{{\"version\":2,\"master\":{wrapper},\"recovery\":null,\"secondary\":null}}";
    }

    private static string BuildVersion1Header()
    {
        var wrapper = BuildWrapper();
        return $"{{\"version\":1,\"master\":{wrapper},\"recovery\":null}}";
    }

    private static string BuildWrapper() =>
        "{\"version\":1,\"salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"kdf\":{\"memoryKiB\":65536,\"iterations\":3,\"parallelism\":1},\"nonce\":\"AAAAAAAAAAAAAAAA\",\"ciphertext\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\",\"tag\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}";

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
