using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenStopping : NotificationStackBaseTest
    {
        private INotificationSubscriber _subscriber1;
        private INotificationSubscriber _subscriber2;

        protected override void Given()
        {
            _subscriber1 = Substitute.For<INotificationSubscriber>();
            _subscriber2 = Substitute.For<INotificationSubscriber>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, _subscriber1);
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.CustomerCommunication, _subscriber2);

            SystemUnderTest.Stop();
        }

        [Then]
        public void SubscribersAreToldToStopListening()
        {
            _subscriber1.Received().StopListening();
            _subscriber2.Received().StopListening();
        }
    }
}