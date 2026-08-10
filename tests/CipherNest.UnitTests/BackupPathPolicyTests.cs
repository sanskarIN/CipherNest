using CipherNest.Infrastructure.Services;
using CipherNest.Shared;

namespace CipherNest.UnitTests;

public sealed class BackupPathPolicyTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestBackupPathPolicy", Guid.NewGuid().ToString("N"));

    public BackupPathPolicyTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void AcceptsDedicatedBackupDirectory()
    {
        var database = Path.Combine(_directory, "vault.db");
        var backups = Path.Combine(_directory, "Backups");
        Directory.CreateDirectory(backups);
        var destination = Path.Combine(backups, "vault.cnbak");

        Assert.Equal(Path.GetFullPath(destination), BackupPathPolicy.ValidateExportDestination(destination, database));
    }

    [Theory]
    [InlineData("")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData(".previous")]
    [InlineData(".previous.0123456789abcdef")]
    public void RejectsDatabaseAndRecoveryFileCollisions(string suffix)
    {
        var database = Path.Combine(_directory, "vault.db");
        var destination = database + suffix;

        Assert.Throws<InvalidOperationException>(() => BackupPathPolicy.ValidateExportDestination(destination, database));
    }

    [Fact]
    public void RejectsAttachmentStoreDestination()
    {
        var database = Path.Combine(_directory, "vault.db");
        var attachmentDirectory = Path.Combine(_directory, AppConstants.AttachmentDirectoryName);
        var destination = Path.Combine(attachmentDirectory, $"{Guid.NewGuid():N}.cna");

        Assert.Throws<InvalidOperationException>(() => BackupPathPolicy.ValidateExportDestination(destination, database));
    }

    [Fact]
    public void TemporarySiblingPath_IsUniqueAndStaysBesideDestination()
    {
        var destination = Path.Combine(_directory, "Backups", "CipherNest.cnbak");
        var first = BackupPathPolicy.CreateTemporarySiblingPath(destination);
        var second = BackupPathPolicy.CreateTemporarySiblingPath(destination);

        Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(destination)), Path.GetDirectoryName(first));
        Assert.NotEqual(first, second);
        Assert.True(first.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }
}
