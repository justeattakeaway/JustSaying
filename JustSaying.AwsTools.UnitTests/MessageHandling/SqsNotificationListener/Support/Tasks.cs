using System;
using System.Threading.Tasks;
using NLog;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public static class Tasks
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const int DefaultTimeoutMillis = 5000;
        private const int DelaySendMillis = 200;

        public static async Task<bool> WaitWithTimeoutAsync(Task task)
        {
            return await WaitWithTimeoutAsync(task, TimeSpan.FromMilliseconds(DefaultTimeoutMillis));
        }

        public static async Task<bool> WaitWithTimeoutAsync(Task task, TimeSpan timeoutDuration)
        {
            var timeoutTask = Task.Delay(timeoutDuration);
            var firstToComplete = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);

            if (firstToComplete == timeoutTask)
            {
                Log.Error("Task did not complete before timeout of " + timeoutDuration);
                return false;
            }

            return true;
        }
        public static void DelaySendDone(TaskCompletionSource<object> doneSignal)
        {
            Task.Run(async () =>
            {
                await Task.Yield();
                await Task.Delay(DelaySendMillis);
                doneSignal.SetResult(null);
            });
        }
    }
}
