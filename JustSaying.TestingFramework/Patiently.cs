using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace JustSaying.TestingFramework
{
    public static class Patiently
    {
        private const int DefaultTimeoutMillis = 1000;

        public static async Task WaitWithTimeoutAsync(Task task)
        {
            await WaitWithTimeoutAsync(task, TimeSpan.FromMilliseconds(DefaultTimeoutMillis));
        }

        public static void DelaySendDone(TaskCompletionSource<object> doneSignal)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                doneSignal.SetResult(null);
            });
        }

        public static async Task WaitWithTimeoutAsync(Task task, TimeSpan timeoutDuration)
        {
            var timeoutTask = Task.Delay(timeoutDuration);
            var firstToComplete = await Task.WhenAny(task, timeoutTask);

            if (firstToComplete == timeoutTask)
            {
                throw new Exception("Task did not complete before timeout of " + timeoutDuration);
            }
        }

        public static async Task VerifyExpectationAsync(Action expression)
        {
            await VerifyExpectationAsync(expression, 5.Seconds());
        }

        public static async Task VerifyExpectationAsync(Action expression, TimeSpan timeout)
        {
            var started = DateTime.Now;
            var timeoutAt = DateTime.Now + timeout;
            do
            {
                try
                {
                    expression.Invoke();
                    return;
                }
                catch
                {
                }
                
                await Task.Delay(50.Milliseconds());
                Console.WriteLine(
                    "Waiting for {0} ms - Still Checking.", 
                    (DateTime.Now - started).TotalMilliseconds);
            } while (DateTime.Now < timeoutAt);

            expression.Invoke();
        }

        public static async Task AssertThatAsync(Func<bool> func)
        {
            await AssertThatAsync(func, 5.Seconds());
        }

        public static async Task AssertThatAsync(Func<bool> func, TimeSpan timeout)
        {
            var started = DateTime.Now;
            var timeoutAt = DateTime.Now + timeout;
            do
            {
                if (func.Invoke())
                {
                    return;
                }

                await Task.Delay(50.Milliseconds());
                Console.WriteLine(
                    "Waiting for {0} ms - Still Checking.",
                    (DateTime.Now - started).TotalMilliseconds);
            } while (DateTime.Now < timeoutAt);

            Assert.True(func.Invoke());
        }
    }
    public static class Extensions
    {
        public static TimeSpan Seconds(this int n)
        {
            return TimeSpan.FromSeconds(n);
        }
        public static TimeSpan Milliseconds(this int n)
        {
            return TimeSpan.FromMilliseconds(n);
        }
    }
}
