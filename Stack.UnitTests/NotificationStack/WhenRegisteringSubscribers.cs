using JustEat.Testing;
using NSubstitute;
using SimplesNotificationStack.Messaging;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenRegisteringSubscribers : NotificationStackBaseTest
    {
        private IMessageSubscriber _subscriber1;
        private IMessageSubscriber _subscriber2;

        protected override void Given()
        {
            _subscriber1 = Substitute.For<IMessageSubscriber>();
            _subscriber2 = Substitute.For<IMessageSubscriber>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, _subscriber1);
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.CustomerCommunication, _subscriber2);
            SystemUnderTest.Start();
        }

        [Then]
        public void SubscribersStartedUp()
        {
            _subscriber1.Received().Listen();
            _subscriber2.Received().Listen();
        }
    }
}
