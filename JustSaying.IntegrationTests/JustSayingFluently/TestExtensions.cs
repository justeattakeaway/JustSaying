using System;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public static class TestExtensions
    {
        public static TimeSpan Seconds(this int numberOfSeconds)
        {
            return TimeSpan.FromSeconds(numberOfSeconds);
        }
    }
}