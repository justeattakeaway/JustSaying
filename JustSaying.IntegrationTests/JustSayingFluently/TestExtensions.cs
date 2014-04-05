using System;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public static class TestExtensions
    {
        public static bool ShouldBeTrue(this bool boolean)
        {
            return boolean;
        }

        public static TimeSpan Seconds(this int numberOfSeconds)
        {
            return TimeSpan.FromSeconds(numberOfSeconds);
        }
    }
}