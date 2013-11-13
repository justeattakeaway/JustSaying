using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener.HandlingExceptions
{
    public class WhenErrorHandlingActionIsNotProvided : BaseQueuePollingTest
    {

        protected override void When()
        {
            var listener = new JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener(null, null, new NullMessageFootprintStore(), null);

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