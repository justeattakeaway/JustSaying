namespace JustSaying.Messaging.Channels.Dispatch;

/// <summary>
/// A simple token bucket rate limiter that allows a maximum number of operations per second.
/// Tokens are replenished every second back to the maximum.
/// </summary>
internal sealed class TokenBucketRateLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _replenishTimer;
    private readonly int _maxTokens;
    private int _disposed;
    private int _replenishing;

    /// <summary>
    /// Creates a new <see cref="TokenBucketRateLimiter"/>.
    /// </summary>
    /// <param name="maxPerSecond">The maximum number of tokens available per second. Must be greater than zero.</param>
    public TokenBucketRateLimiter(int maxPerSecond)
    {
        if (maxPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPerSecond), maxPerSecond, "Value must be greater than zero.");
        }

        _maxTokens = maxPerSecond;
        _semaphore = new SemaphoreSlim(maxPerSecond, maxPerSecond);
        _replenishTimer = new Timer(Replenish, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Asynchronously waits to acquire a token. Blocks if no tokens are available until
    /// a token is replenished or the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>A task that completes when a token has been acquired.</returns>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(TokenBucketRateLimiter));
        }

        return _semaphore.WaitAsync(cancellationToken);
    }

    private void Replenish(object state)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        // Prevent overlapping timer callbacks from double-releasing tokens.
        if (Interlocked.CompareExchange(ref _replenishing, 1, 0) != 0)
        {
            return;
        }

        try
        {
            int tokensToRelease = _maxTokens - _semaphore.CurrentCount;

            if (tokensToRelease > 0)
            {
                _semaphore.Release(tokensToRelease);
            }
        }
        catch (ObjectDisposedException)
        {
            // Dispose() was called concurrently — safe to ignore.
        }
        finally
        {
            Volatile.Write(ref _replenishing, 0);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _replenishTimer.Dispose();
        _semaphore.Dispose();
    }
}
