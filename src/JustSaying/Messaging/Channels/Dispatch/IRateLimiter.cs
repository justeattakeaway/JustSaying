namespace JustSaying.Messaging.Channels.Dispatch;

/// <summary>
/// Provides an async rate-limiting mechanism for message dispatch.
/// </summary>
internal interface IRateLimiter : IDisposable
{
    /// <summary>
    /// Asynchronously waits until the caller is permitted to proceed.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>A task that completes when the caller is allowed to proceed.</returns>
    Task WaitAsync(CancellationToken cancellationToken);
}
