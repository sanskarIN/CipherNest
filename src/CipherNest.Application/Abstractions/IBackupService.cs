namespace CipherNest.Application.Abstractions;

public interface IBackupService
{
    Task ExportEncryptedAsync(string destinationPath, string backupPassphrase, CancellationToken cancellationToken = default);
    Task RestoreEncryptedAsync(string sourcePath, string backupPassphrase, CancellationToken cancellationToken = default);
}
