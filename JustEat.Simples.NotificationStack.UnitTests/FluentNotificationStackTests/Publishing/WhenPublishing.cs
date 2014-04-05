using JustSaying.Messaging.Messages;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.Publishing
{
    public class WhenPublishing : FluentNotificationStackTestBase
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

        [Then]
        public void TheComponentIsPopulatedOnMessage()
        {
            NotificationStack.Received().Publish(Arg.Is<GenericMessage>(x => x.RaisingComponent == Component));
        }

        [Then]
        public void TheTenantIsPopulatedOnMessage()
        {
            NotificationStack.Received().Publish(Arg.Is<GenericMessage>(x => x.Tenant == Tenant));
        }
    }
}