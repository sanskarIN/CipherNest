using CipherNest.Domain.Models;

namespace CipherNest.Application.Models;

public sealed record TotpUriProfile(
    string AccountName,
    string Issuer,
    string Secret,
    TotpAlgorithm Algorithm,
    int Digits,
    int PeriodSeconds);
