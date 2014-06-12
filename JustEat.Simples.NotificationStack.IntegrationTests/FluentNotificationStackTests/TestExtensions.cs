using System;
using NUnit.Framework;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public static class TestExtensions
    {
        public static void ShouldBeTrue(this bool boolean)
        {
            Assert.IsTrue(boolean);
        }
        public static void ShouldBeFalse(this bool boolean)
        {
            Assert.IsFalse(boolean);
        }

        public static TimeSpan Seconds(this int numberOfSeconds)
        {
            return TimeSpan.FromSeconds(numberOfSeconds);
        }
    }
}