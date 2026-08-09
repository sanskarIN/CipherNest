namespace CipherNest.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
