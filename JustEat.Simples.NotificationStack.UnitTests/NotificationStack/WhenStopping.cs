using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

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

    public class WhenStartingThenStopping : NotificationStackBaseTest
    {
        private INotificationSubscriber _subscriber1;

        protected override void Given()
        {
            _subscriber1 = Substitute.For<INotificationSubscriber>();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(NotificationTopic.OrderDispatch, _subscriber1);
            SystemUnderTest.Start();
            SystemUnderTest.Stop();
        }

        [Then]
        public void StateIsNotListening()
        {
            Assert.False(SystemUnderTest.Listening);
        }

        [Then]
        public void CallingStopTwiceDoesNotStopListeningTwice()
        {
            SystemUnderTest.Stop();
            _subscriber1.Received(1).StopListening();
        }
    }
    
}