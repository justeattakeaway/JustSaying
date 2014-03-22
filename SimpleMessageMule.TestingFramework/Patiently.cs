using System;
using System.Threading;
using NUnit.Framework;

namespace SimpleMessageMule.TestingFramework
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
            AssertThat(func, 5.Seconds());
        }

        public static void AssertThat(Func<bool> func, TimeSpan timeout)
        {
            bool result;
            bool timedOut;
            var started = DateTime.Now;
            do
            {
                result = func.Invoke();
                timedOut = timeout < DateTime.Now - started;
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Console.WriteLine("{0} - Still Checking...", (DateTime.Now - started).TotalSeconds);
            } while (!result && !timedOut);

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
