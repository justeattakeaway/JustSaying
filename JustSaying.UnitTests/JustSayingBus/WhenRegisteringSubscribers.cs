using JustBehave;
using JustSaying.Messaging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringSubscribers : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber1;
        private INotificationSubscriber _subscriber2;

        protected override void Given()
        {
            base.Given();
            _subscriber1 = Substitute.For<INotificationSubscriber>();
            _subscriber1.Queue.Returns("queue1");
            _subscriber2 = Substitute.For<INotificationSubscriber>();
            _subscriber2.Queue.Returns("queue2");
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber1);
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber2);
            SystemUnderTest.Start();
        }

        [Then]
        public void SubscribersStartedUp()
        {
            _subscriber1.Received().Listen();
            _subscriber2.Received().Listen();
        }

        [Then]
        public void StateIsListening()
        {
            Assert.True(SystemUnderTest.Listening);
        }

        [Then]
        public void CallingStartTwiceDoesNotStartListeningTwice()
        {
            SystemUnderTest.Start();
            _subscriber1.Received(1).Listen();
            _subscriber2.Received(1).Listen();
        }
    }
}
