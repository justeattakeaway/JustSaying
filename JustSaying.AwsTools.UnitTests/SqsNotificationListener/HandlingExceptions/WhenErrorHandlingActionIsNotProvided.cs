using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener.HandlingExceptions
{
    public class WhenErrorHandlingActionIsNotProvided : BaseQueuePollingTest
    {

        protected override void When()
        {
            var listener = new JustSaying.AwsTools.SqsNotificationListener(null, null, new NullMessageFootprintStore(), null);

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