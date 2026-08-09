using CipherNest.Application.Services;

namespace CipherNest.UnitTests;

public sealed class UnlockBackoffPolicyTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 5)]
    [InlineData(6, 10)]
    [InlineData(7, 20)]
    [InlineData(8, 40)]
    [InlineData(9, 80)]
    [InlineData(10, 160)]
    [InlineData(11, 300)]
    [InlineData(30, 300)]
    public void Backoff_IsBoundedAndIncreasesAfterFifthFailure(int failures, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), UnlockBackoffPolicy.DelayAfterFailureCount(failures));
    }
}
