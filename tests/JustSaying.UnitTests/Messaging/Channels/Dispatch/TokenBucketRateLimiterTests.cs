using JustSaying.Messaging.Channels.Dispatch;

namespace JustSaying.UnitTests.Messaging.Channels.Dispatch;

public class TokenBucketRateLimiterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_ThrowsForInvalidMaxPerSecond(int maxPerSecond)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenBucketRateLimiter(maxPerSecond));
    }

    [Fact]
    public void Constructor_AcceptsPositiveValue()
    {
        using var limiter = new TokenBucketRateLimiter(1);
        // No exception
    }

    [Fact]
    public async Task WaitAsync_AllowsUpToMaxTokensImmediately()
    {
        const int max = 5;
        using var limiter = new TokenBucketRateLimiter(max);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        for (int i = 0; i < max; i++)
        {
            await limiter.WaitAsync(cts.Token);
        }
    }

    [Fact]
    public async Task WaitAsync_BlocksWhenTokensExhausted()
    {
        const int max = 1;
        using var limiter = new TokenBucketRateLimiter(max);

        // Consume the only token
        await limiter.WaitAsync(CancellationToken.None);

        // Next call should block
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task WaitAsync_TokensReplenishAfterOneSecond()
    {
        const int max = 1;
        using var limiter = new TokenBucketRateLimiter(max);

        // Consume the token
        await limiter.WaitAsync(CancellationToken.None);

        // Wait for replenishment (slightly over 1 second to allow for timer imprecision)
        await Task.Delay(TimeSpan.FromMilliseconds(1200));

        // Should succeed now
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await limiter.WaitAsync(cts.Token);
    }

    [Fact]
    public async Task WaitAsync_ThrowsWhenCancelled()
    {
        const int max = 1;
        using var limiter = new TokenBucketRateLimiter(max);

        // Consume the only token
        await limiter.WaitAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts.Token));
    }

    [Fact]
    public async Task WaitAsync_ThrowsAfterDisposal()
    {
        var limiter = new TokenBucketRateLimiter(1);
        limiter.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => limiter.WaitAsync(CancellationToken.None));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var limiter = new TokenBucketRateLimiter(1);
        limiter.Dispose();
        limiter.Dispose(); // Should not throw
    }
}
