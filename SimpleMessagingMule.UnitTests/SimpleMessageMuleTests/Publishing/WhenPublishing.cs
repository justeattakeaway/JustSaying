using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Testing;
using NSubstitute;
using Tests.MessageStubs;

namespace SimpleMessageMule.UnitTests.SimpleMessageMuleTests.Publishing
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