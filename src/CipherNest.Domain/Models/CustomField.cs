namespace CipherNest.Domain.Models;

public sealed record CustomField(string Name, string Value, bool IsSecret = false);
