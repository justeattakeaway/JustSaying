using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Testing;
using NSubstitute;
using JustEat.Simples.NotificationStack.Messaging;
using NUnit.Framework;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenRegisteringSubscribers : NotificationStackBaseTest
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

    public class WhenRegisteringPublishers : NotificationStackBaseTest
    {
        private IMessagePublisher _publisherublisher;

        protected override void Given()
        {
            _publisherublisher = Substitute.For<IMessagePublisher>();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(NotificationTopic.OrderDispatch, _publisherublisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(NotificationTopic.OrderDispatch, _publisherublisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(NotificationTopic.CustomerCommunication, _publisherublisher);

            SystemUnderTest.Publish(new OrderAccepted(0, 0, 0));
            SystemUnderTest.Publish(new OrderRejected(0, 0, 0, OrderRejectReason.TooBusy));
        }

        [Then]
        public void AcceptedOrderWasPublishedOnce()
        {
            _publisherublisher.Received(1).Publish(Arg.Any<OrderAccepted>());
        }

        [Then]
        public void RejectedOrderWasPublishedTwice()
        {
            _publisherublisher.Received(2).Publish(Arg.Any<OrderRejected>());
        }
    }
}
