using System;
using System.Threading.Tasks;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    public static class Tasks
    {
        private const int DefaultTimeoutMillis = 1000;

        public static async Task WaitWithTimeoutAsync(Task task)
        {
            await WaitWithTimeoutAsync(task, TimeSpan.FromMilliseconds(DefaultTimeoutMillis));
        }
        public static async Task WaitWithTimeoutAsync(Task task, TimeSpan timeoutDuration)
        {
            var timeoutTask = Task.Delay(timeoutDuration);
            var firstToComplete = await Task.WhenAny(task, timeoutTask);

            if (firstToComplete == timeoutTask)
            {
                throw new TimeoutException("Task did not complete before timeout of " + timeoutDuration);
            }
        }

        public static void DelaySendDone(TaskCompletionSource<object> doneSignal)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                doneSignal.SetResult(null);
            });
        }
    }
}
