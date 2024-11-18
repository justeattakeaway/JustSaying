namespace JustSaying.TestingFramework;

public static class InspectableHandlerExtensions
{
    public static async Task WaitForMessageCountAsync<T>(this InspectableHandler<T> handler, int count, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();
        var messageCount = 0;

        handler.OnHandle += Handler;

        await tcs.Task;

        handler.OnHandle -= Handler;
        return;

        void Handler(T _)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled(cancellationToken);
                return;
            }

            Interlocked.Increment(ref messageCount);
            if (messageCount >= count)
            {
                tcs.SetResult(true);
            }
        }
    }
}
