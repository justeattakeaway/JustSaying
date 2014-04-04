using JustSaying.Messaging.Messages;
using JustEat.Testing;
using NUnit.Framework;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenRegisteringTheSamePublisherTwice : GivenAServiceBus
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<Message>("OrderDispatch", null);
            SystemUnderTest.AddMessagePublisher<Message>("OrderDispatch", null);
        }

        [Then]
        public void AnExceptionIsThrown()
        {
            Assert.NotNull(ThrownException);
        }
    }
}