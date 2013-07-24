using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenRegisteringTheSamePublisherTwice : NotificationStackBaseTest
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(NotificationTopic.OrderDispatch, null);
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(NotificationTopic.OrderDispatch, null);
        }

        [Then]
        public void AnExceptionIsThrown()
        {
            Assert.NotNull(ThrownException);
        }
    }
}