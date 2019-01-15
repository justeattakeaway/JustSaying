using System;
using System.Threading.Tasks;

namespace JustSaying.TestingFramework
{
    public static class TaskHelpers
    {
        private const int DefaultTimeoutMillis = 10000;
        private const int DelaySendMillis = 200;

        public static async Task<bool> WaitWithTimeoutAsync(Task task)
            => await WaitWithTimeoutAsync(task, TimeSpan.FromMilliseconds(DefaultTimeoutMillis))
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
                await Task.Delay(DelaySendMillis).ConfigureAwait(false);
                doneSignal.SetResult(null);
            });
        }
    }
}
