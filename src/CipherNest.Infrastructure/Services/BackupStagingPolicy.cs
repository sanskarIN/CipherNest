namespace CipherNest.Infrastructure.Services;

public static class BackupStagingPolicy
{
    private const int BufferSize = 128 * 1024;

    public static async Task CopyToNewFileAsync(Stream source, string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead) throw new ArgumentException("Backup source stream must be readable.", nameof(source));
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (source.CanSeek)
        {
            var remaining = source.Length - source.Position;
            if (remaining < 0 || remaining > BackupFormatPolicy.MaximumEncryptedContainerBytes)
                throw new InvalidDataException("Encrypted backup exceeds the supported container size limit.");
        }

        var destination = Path.GetFullPath(destinationPath);
        try
        {
            await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            var buffer = new byte[BufferSize];
            long copied = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
                if (copied > BackupFormatPolicy.MaximumEncryptedContainerBytes - read)
                    throw new InvalidDataException("Encrypted backup exceeds the supported container size limit.");
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                copied += read;
            }
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            TryDelete(destination);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
