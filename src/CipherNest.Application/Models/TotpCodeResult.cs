namespace CipherNest.Application.Models;

public sealed record TotpCodeResult(string Code, int SecondsRemaining, DateTimeOffset ValidUntilUtc);
