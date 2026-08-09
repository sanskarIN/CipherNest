namespace CipherNest.Application.Abstractions;

public sealed record KdfParameters(int MemoryKiB, int Iterations, int Parallelism);

public sealed record WrappedKeyEnvelope(
    int Version,
    byte[] Salt,
    KdfParameters Kdf,
    byte[] Nonce,
    byte[] Ciphertext,
    byte[] Tag);

public sealed record EncryptedEnvelope(int Version, byte[] Nonce, byte[] Ciphertext, byte[] Tag);

public interface ICryptoService
{
    WrappedKeyEnvelope CreateWrappedKey(ReadOnlySpan<char> passphrase);
    byte[] UnwrapKey(ReadOnlySpan<char> passphrase, WrappedKeyEnvelope envelope);
    EncryptedEnvelope Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData);
    byte[] Decrypt(EncryptedEnvelope envelope, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData);
    byte[] DeriveKey(ReadOnlySpan<char> passphrase, ReadOnlySpan<byte> salt, KdfParameters parameters);
}
