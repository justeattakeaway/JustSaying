using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using SimplesNotificationStack.Messaging;

namespace Stack.UnitTests.NotificationStack
{
    [TestFixture]
    public class WhenRegisteringSubscribers : BehaviourTest<SimplesNotificationStack.Stack.NotificationStack>
    {
        private IMessageSubscriber _subscriber1;
        private IMessageSubscriber _subscriber2;

        protected override SimplesNotificationStack.Stack.NotificationStack CreateSystemUnderTest()
        {
            return new SimplesNotificationStack.Stack.NotificationStack(Component.BoxHandler);
        }

        protected override void Given()
        {
            _subscriber1 = Substitute.For<IMessageSubscriber>();
            _subscriber2 = Substitute.For<IMessageSubscriber>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, _subscriber1);
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, _subscriber2);
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
