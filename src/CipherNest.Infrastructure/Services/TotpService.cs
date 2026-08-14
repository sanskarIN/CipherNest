using System.Buffers.Binary;
using System.Security.Cryptography;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Models;
using CipherNest.Application.Validation;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class TotpService : ITotpService
{
    public TotpCodeResult Generate(string base32Secret, TotpAlgorithm algorithm, int digits, int periodSeconds, DateTimeOffset utcNow)
    {
        TotpPolicy.ValidateSettings(algorithm, digits, periodSeconds);
        var normalized = TotpPolicy.NormalizeSecret(base32Secret);
        var unixSeconds = utcNow.ToUniversalTime().ToUnixTimeSeconds();
        if (unixSeconds < 0) throw new ArgumentOutOfRangeException(nameof(utcNow), "TOTP timestamps before the Unix epoch are not supported.");

        var key = DecodeBase32(normalized);
        try
        {
            var counter = unixSeconds / periodSeconds;
            Span<byte> counterBytes = stackalloc byte[sizeof(long)];
            Span<byte> hash = stackalloc byte[64];
            try
            {
                BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

                using HMAC hmac = algorithm switch
                {
                    TotpAlgorithm.Sha1 => new HMACSHA1(key),
                    TotpAlgorithm.Sha256 => new HMACSHA256(key),
                    TotpAlgorithm.Sha512 => new HMACSHA512(key),
                    _ => throw new ArgumentOutOfRangeException(nameof(algorithm), "Unsupported TOTP algorithm.")
                };

                if (!hmac.TryComputeHash(counterBytes, hash, out var hashLength))
                    throw new CryptographicException("Could not compute the TOTP authentication code.");

                var offset = hash[hashLength - 1] & 0x0f;
                if (offset + 4 > hashLength) throw new CryptographicException("TOTP hash truncation offset is invalid.");

                var binary = BinaryPrimitives.ReadUInt32BigEndian(hash.Slice(offset, 4)) & 0x7fff_ffffu;
                var modulus = digits == 8 ? 100_000_000u : 1_000_000u;
                var code = (binary % modulus).ToString($"D{digits}", System.Globalization.CultureInfo.InvariantCulture);
                var elapsed = (int)(unixSeconds % periodSeconds);
                var remaining = periodSeconds - elapsed;
                var validUntil = DateTimeOffset.FromUnixTimeSeconds(unixSeconds + remaining);

                return new TotpCodeResult(code, remaining, validUntil);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(counterBytes);
                CryptographicOperations.ZeroMemory(hash);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DecodeBase32(string normalized)
    {
        var output = new byte[(normalized.Length * 5) / 8];
        var outputIndex = 0;
        var buffer = 0;
        var bits = 0;

        foreach (var character in normalized)
        {
            var value = character switch
            {
                >= 'A' and <= 'Z' => character - 'A',
                >= '2' and <= '7' => character - '2' + 26,
                _ => throw new ArgumentException("TOTP secret contains an invalid Base32 character.", nameof(normalized))
            };

            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits < 8) continue;

            bits -= 8;
            output[outputIndex++] = (byte)(buffer >> bits);
            buffer &= bits == 0 ? 0 : (1 << bits) - 1;
        }

        if (bits > 0 && buffer != 0)
        {
            CryptographicOperations.ZeroMemory(output);
            throw new ArgumentException("TOTP secret contains non-zero Base32 padding bits.", nameof(normalized));
        }
        if (outputIndex != output.Length)
        {
            CryptographicOperations.ZeroMemory(output);
            throw new ArgumentException("TOTP secret could not be decoded completely.", nameof(normalized));
        }

        return output;
    }
}
