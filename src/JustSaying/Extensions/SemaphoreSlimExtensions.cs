
internal static class SemaphoreSlimExtensions
{
    public static async Task<IDisposable> WaitScopedAsync(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken)
    {
        await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new LockLifetime(semaphoreSlim);
    }

    private sealed class LockLifetime(SemaphoreSlim lockSemaphore) : IDisposable
    {
        public void Dispose()
        {
            lockSemaphore.Release();
        }
    }
}
