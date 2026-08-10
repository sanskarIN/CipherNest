using System.Security.Cryptography;
using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Exceptions;
using CipherNest.Shared;
using Konscious.Security.Cryptography;

namespace CipherNest.Infrastructure.Crypto;

public sealed class CryptoService : ICryptoService
{
    public static readonly KdfParameters DefaultKdf = new(64 * 1024, 3, 1);
    public const int MinimumKdfMemoryKiB = 16 * 1024;
    public const int MaximumKdfMemoryKiB = 512 * 1024;
    public const int MaximumKdfIterations = 10;
    public const int MaximumKdfParallelism = 16;
    public const int MinimumPassphraseCharacters = 12;
    public const int MaximumPassphraseCharacters = 4_096;
    private const int KeySize = 32;
    private const int SaltSize = 16;
    private const int MaximumSaltSize = 64;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public WrappedKeyEnvelope CreateWrappedKey(ReadOnlySpan<char> passphrase)
    {
        var dataKey = RandomNumberGenerator.GetBytes(KeySize);
        try
        {
            return WrapKey(dataKey, passphrase);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    public WrappedKeyEnvelope WrapKey(ReadOnlySpan<byte> dataKey, ReadOnlySpan<char> passphrase)
    {
        ValidatePassphrase(passphrase);
        ValidateKey(dataKey);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[]? kek = null;
        try
        {
            kek = DeriveKey(passphrase, salt, DefaultKdf);
            var aad = BuildKeyWrapAssociatedData(AppConstants.CryptoFormatVersion, DefaultKdf);
            var encrypted = Encrypt(dataKey, kek, aad);
            return new WrappedKeyEnvelope(
                AppConstants.CryptoFormatVersion,
                salt,
                DefaultKdf,
                encrypted.Nonce,
                encrypted.Ciphertext,
                encrypted.Tag);
        }
        finally
        {
            if (kek is not null) CryptographicOperations.ZeroMemory(kek);
        }
    }

    public byte[] UnwrapKey(ReadOnlySpan<char> passphrase, WrappedKeyEnvelope envelope)
    {
        ValidatePassphrase(passphrase);
        ValidateWrappedKey(envelope);
        byte[]? kek = null;
        try
        {
            kek = DeriveKey(passphrase, envelope.Salt, envelope.Kdf);
            var aad = BuildKeyWrapAssociatedData(envelope.Version, envelope.Kdf);
            return Decrypt(new EncryptedEnvelope(envelope.Version, envelope.Nonce, envelope.Ciphertext, envelope.Tag), kek, aad);
        }
        catch (CryptographicException ex)
        {
            throw new VaultAuthenticationException(ex);
        }
        finally
        {
            if (kek is not null) CryptographicOperations.ZeroMemory(kek);
        }
    }

    public EncryptedEnvelope Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData)
    {
        ValidateKey(key);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        return new EncryptedEnvelope(AppConstants.CryptoFormatVersion, nonce, ciphertext, tag);
    }

    public byte[] Decrypt(EncryptedEnvelope envelope, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData)
    {
        ValidateKey(key);
        if (envelope.Version != AppConstants.CryptoFormatVersion || envelope.Nonce.Length != NonceSize || envelope.Tag.Length != TagSize)
        {
            throw new CryptographicException("Unsupported or invalid encrypted envelope.");
        }

        var plaintext = new byte[envelope.Ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(envelope.Nonce, envelope.Ciphertext, envelope.Tag, plaintext, associatedData);
            return plaintext;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw;
        }
    }

    public byte[] DeriveKey(ReadOnlySpan<char> passphrase, ReadOnlySpan<byte> salt, KdfParameters parameters)
    {
        ValidatePassphrase(passphrase);
        ValidateKdfParameters(salt, parameters);

        var utf8 = new byte[Encoding.UTF8.GetByteCount(passphrase)];
        try
        {
            Encoding.UTF8.GetBytes(passphrase, utf8);
            using var argon2 = new Argon2id(utf8)
            {
                Salt = salt.ToArray(),
                MemorySize = parameters.MemoryKiB,
                Iterations = parameters.Iterations,
                DegreeOfParallelism = parameters.Parallelism
            };
            return argon2.GetBytes(KeySize);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8);
        }
    }

    private static byte[] BuildKeyWrapAssociatedData(int version, KdfParameters parameters) =>
        Encoding.UTF8.GetBytes($"CipherNest|VaultKey|v{version}|m={parameters.MemoryKiB}|t={parameters.Iterations}|p={parameters.Parallelism}");

    private static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize) throw new ArgumentException("A 256-bit key is required.", nameof(key));
    }

    private static void ValidatePassphrase(ReadOnlySpan<char> passphrase)
    {
        if (passphrase.Length is < MinimumPassphraseCharacters or > MaximumPassphraseCharacters)
            throw new ArgumentException($"A passphrase or recovery key must contain between {MinimumPassphraseCharacters:N0} and {MaximumPassphraseCharacters:N0} characters.", nameof(passphrase));
    }

    private static void ValidateKdfParameters(ReadOnlySpan<byte> salt, KdfParameters parameters)
    {
        if (salt.Length is < SaltSize or > MaximumSaltSize ||
            parameters.MemoryKiB is < MinimumKdfMemoryKiB or > MaximumKdfMemoryKiB ||
            parameters.Iterations is < 1 or > MaximumKdfIterations ||
            parameters.Parallelism is < 1 or > MaximumKdfParallelism)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "KDF parameters are outside the supported security/resource bounds.");
        }
    }

    private static void ValidateWrappedKey(WrappedKeyEnvelope envelope)
    {
        if (envelope.Version != AppConstants.CryptoFormatVersion || envelope.Salt.Length is < SaltSize or > MaximumSaltSize || envelope.Nonce.Length != NonceSize || envelope.Tag.Length != TagSize)
        {
            throw new VaultAuthenticationException();
        }
        try { ValidateKdfParameters(envelope.Salt, envelope.Kdf); }
        catch (ArgumentOutOfRangeException ex) { throw new VaultAuthenticationException(ex); }
    }
}
