using JustEat.Testing;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener.HandlingExceptions
{
    public class WhenErrorHandlingActionIsNotProvided : BaseQueuePollingTest
    {

        protected override void When()
        {
            var listener = new JustSaying.AwsTools.SqsNotificationListener(null, null, null);

            listener.HandleMessage(null);
        }

        [Then]
        public void NoExceptionIsThrown()
        {
            Assert.That(ThrownException, Is.Null);
        }

        protected override void Given() { }
    }
}