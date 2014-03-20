using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Testing;
using NUnit.Framework;

namespace SimpleMessageMule.UnitTests.NotificationStack
{
    public class WhenRegisteringTheSamePublisherTwice : NotificationStackBaseTest
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