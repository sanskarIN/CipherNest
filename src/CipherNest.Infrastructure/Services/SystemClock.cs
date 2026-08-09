using CipherNest.Application.Abstractions;

namespace CipherNest.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
