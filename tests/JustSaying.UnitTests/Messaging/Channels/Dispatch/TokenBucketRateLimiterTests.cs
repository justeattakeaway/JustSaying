using JustSaying.Messaging.Channels.Dispatch;

namespace JustSaying.UnitTests.Messaging.Channels.Dispatch;

public class TokenBucketRateLimiterTests
{
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(-100)]
    public void Constructor_ThrowsForInvalidMaxPerSecond(int maxPerSecond)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new TokenBucketRateLimiter(maxPerSecond));
    }

    [Test]
    public void Constructor_AcceptsPositiveValue()
    {
        using var limiter = new TokenBucketRateLimiter(1);
        // No exception
    }

    [Test]
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

    [Test]
    public async Task WaitAsync_BlocksWhenTokensExhausted()
    {
        const int max = 1;
        using var limiter = CreateWithoutAutoReplenish(max);

        // Consume the only token
        await limiter.WaitAsync(CancellationToken.None);

        // Next call should block
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Should.ThrowAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts.Token));
    }

    [Test]
    public async Task WaitAsync_TokensReplenishAfterOneSecond()
    {
        const int max = 1;
        using var limiter = new TokenBucketRateLimiter(max);

        // Consume the token
        await limiter.WaitAsync(CancellationToken.None);

        // The next token only becomes available once the timer replenishes the bucket.
        // Use a generous timeout so a slow CI agent can't make this fail.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await limiter.WaitAsync(cts.Token);

        stopwatch.Elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(500));
    }

    [Test]
    public async Task Replenish_RestoresConsumedTokensUpToMax()
    {
        const int max = 3;
        using var limiter = CreateWithoutAutoReplenish(max);

        for (int i = 0; i < max; i++)
        {
            await limiter.WaitAsync(CancellationToken.None);
        }

        limiter.Replenish();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        for (int i = 0; i < max; i++)
        {
            await limiter.WaitAsync(cts.Token);
        }

        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Should.ThrowAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts2.Token));
    }

    [Test]
    public async Task WaitAsync_ThrowsWhenCancelled()
    {
        const int max = 1;
        using var limiter = new TokenBucketRateLimiter(max);

        // Consume the only token
        await limiter.WaitAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts.Token));
    }

    [Test]
    public async Task WaitAsync_ThrowsAfterDisposal()
    {
        var limiter = new TokenBucketRateLimiter(1);
        limiter.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(
            () => limiter.WaitAsync(CancellationToken.None));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var limiter = new TokenBucketRateLimiter(1);
        limiter.Dispose();
        limiter.Dispose(); // Should not throw
    }

    [Test]
    public async Task Replenish_DoesNotThrowWhenDisposedDuringReplenishment()
    {
        // Consume a token so replenishment has work to do, then dispose
        // before replenishing — exercises the disposed check in Replenish.
        var limiter = CreateWithoutAutoReplenish(1);
        await limiter.WaitAsync(CancellationToken.None);
        limiter.Dispose();

        limiter.Replenish();
    }

    [Test]
    public async Task Replenish_DoesNotReleaseWhenNoTokensConsumed()
    {
        // All tokens are still available so replenishment should be a no-op
        using var limiter = CreateWithoutAutoReplenish(5);

        limiter.Replenish();

        // All 5 tokens should still be available (not over-released)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        for (int i = 0; i < 5; i++)
        {
            await limiter.WaitAsync(cts.Token);
        }

        // 6th should block — proves we didn't exceed max
        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Should.ThrowAsync<OperationCanceledException>(
            () => limiter.WaitAsync(cts2.Token));
    }

    // The timer never fires, so tests control replenishment by calling Replenish() and
    // can't be affected by a timer tick landing mid-test on a slow CI agent.
    private static TokenBucketRateLimiter CreateWithoutAutoReplenish(int maxPerSecond)
        => new(maxPerSecond, Timeout.InfiniteTimeSpan);
}
