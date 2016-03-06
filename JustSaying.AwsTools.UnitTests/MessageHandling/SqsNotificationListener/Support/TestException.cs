using System;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support
{
    [Serializable]
    public class TestException : Exception
    {
        public TestException() { }
        public TestException(string message) : base(message)
        { }
    }
}
