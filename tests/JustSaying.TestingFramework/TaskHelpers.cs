namespace JustSaying.TestingFramework;

public static class TaskHelpers
{
    public static async Task<bool> WaitWithTimeoutAsync(Task task)
        => await WaitWithTimeoutAsync(task, TimeSpan.FromMilliseconds(10000))
            .ConfigureAwait(false);

    public static async Task<bool> WaitWithTimeoutAsync(Task task, TimeSpan timeoutDuration)
    {
        var timeoutTask = Task.Delay(timeoutDuration);
        var firstToComplete = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);

        if (firstToComplete != timeoutTask) return true;
        return false;
    }

    public static void DelaySendDone(TaskCompletionSource<object> doneSignal)
    {
        Task.Run(async () =>
        {
            await Task.Delay(200).ConfigureAwait(false);
            doneSignal.SetResult(null);
        });
    }

    /// <summary>
    /// Swallows any <see cref="OperationCanceledException"/>'s and returns true if one was swallowed, else false.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public static async Task<bool> HandleCancellation(this Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }
}
