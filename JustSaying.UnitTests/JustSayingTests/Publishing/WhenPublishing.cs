using JustSaying.Messaging.Messages;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingTests.Publishing
{
    public class WhenPublishing : FluentMessageMuleTestBase
    {
        private readonly Message _message = new GenericMessage();

        protected override void Given()
        {
            
        }

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void TheMessageIsPublished()
        {
            NotificationStack.Received().Publish(_message);
        }
    }
}