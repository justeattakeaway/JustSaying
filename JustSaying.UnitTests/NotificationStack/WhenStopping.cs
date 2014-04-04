using JustSaying.Messaging;
using JustEat.Testing;
using NSubstitute;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenStopping : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber1;
        private INotificationSubscriber _subscriber2;

        protected override void Given()
        {
            base.Given();
            _subscriber1 = Substitute.For<INotificationSubscriber>();
            _subscriber2 = Substitute.For<INotificationSubscriber>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber("OrderDispatch", _subscriber1);
            SystemUnderTest.AddNotificationTopicSubscriber("CustomerCommunication", _subscriber2);
            SystemUnderTest.Start();
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