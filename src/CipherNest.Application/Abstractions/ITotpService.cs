using CipherNest.Application.Models;
using CipherNest.Domain.Models;

namespace CipherNest.Application.Abstractions;

public interface ITotpService
{
    TotpCodeResult Generate(string base32Secret, TotpAlgorithm algorithm, int digits, int periodSeconds, DateTimeOffset utcNow);
}
