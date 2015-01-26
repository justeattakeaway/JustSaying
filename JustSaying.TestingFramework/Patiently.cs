using System;
using System.Threading;
using NUnit.Framework;

namespace JustSaying.TestingFramework
{
    public static class Patiently
    {
        public static void VerifyExpectation(Action expression)
        {
            VerifyExpectation(expression, 5.Seconds());
        }

        public static void VerifyExpectation(Action expression, TimeSpan timeout)
        {
            bool hasTimedOut;
            var started = DateTime.Now;
            do
            {
                try
                {
                    expression.Invoke();
                    return;
                }
                catch { }
                hasTimedOut = timeout < DateTime.Now - started;
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                Console.WriteLine("Waiting for {0} ms - Still Checking.", (DateTime.Now - started).TotalMilliseconds);
            } while (!hasTimedOut);
            expression.Invoke();
        }

        public static void AssertThat(Func<bool> func)
        {
            AssertThat(func, 10.Seconds());
        }

        public static void AssertThat(Func<bool> func, TimeSpan timeout)
        {
            bool result;
            bool hasTimedOut;
            var started = DateTime.Now;
            do
            {
                result = func.Invoke();
                hasTimedOut = timeout < DateTime.Now - started;
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                Console.WriteLine("Waiting for {0} ms - Still Checking.", (DateTime.Now - started).TotalMilliseconds);
            } while (!result && !hasTimedOut);

            Assert.True(result);
        }
    }
    public static class Extensions
    {
        public static TimeSpan Seconds(this int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }
    }
}
