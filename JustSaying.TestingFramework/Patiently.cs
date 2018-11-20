using System;
using System.Threading.Tasks;
using Shouldly;

namespace JustSaying.TestingFramework
{
    public static class Patiently
    {
        public static async Task AssertThatAsync(Func<bool> func)
            => await AssertThatAsync(func, 5.Seconds()).ConfigureAwait(false);

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

                await Task.Delay(50.Milliseconds()).ConfigureAwait(false);

                // TODO Use ITestOutputHelper
                Console.WriteLine(
                    $"Waiting for {(DateTime.Now - started).TotalMilliseconds} ms - Still Checking.");
            } while (DateTime.Now < timeoutAt);

            func.Invoke().ShouldBeTrue();
        }

        public static async Task AssertThatAsync(Func<Task<bool>> func) =>
            await AssertThatAsync(func, 5.Seconds()).ConfigureAwait(false);

        public static async Task AssertThatAsync(Func<Task<bool>> func, TimeSpan timeout)
        {
            var started = DateTime.Now;
            var timeoutAt = DateTime.Now + timeout;
            do
            {
                if (await func.Invoke().ConfigureAwait(false))
                {
                    return;
                }

                await Task.Delay(50.Milliseconds()).ConfigureAwait(false);

                // TODO Use ITestOutputHelper
                Console.WriteLine(
                    $"Waiting for {(DateTime.Now - started).TotalMilliseconds} ms - Still Checking.");
            } while (DateTime.Now < timeoutAt);

            var result = await func.Invoke().ConfigureAwait(false);
            result.ShouldBeTrue();
        }
    }

    public static class Extensions
    {
        public static TimeSpan Seconds(this int n) => TimeSpan.FromSeconds(n);

        public static TimeSpan Milliseconds(this int n) => TimeSpan.FromMilliseconds(n);
    }
}
