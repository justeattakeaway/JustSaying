using System;
using System.Threading;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenRegisteringPublishers : NotificationStackBaseTest
    {
        private IMessagePublisher _publisher;

        protected override void Given()
        {
            _publisher = Substitute.For<IMessagePublisher>();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(NotificationTopic.OrderDispatch, _publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(NotificationTopic.OrderDispatch, _publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(NotificationTopic.CustomerCommunication, _publisher);

            SystemUnderTest.Publish(new OrderAccepted(0, 0, 0));
            SystemUnderTest.Publish(new OrderRejected(0, 0, 0, OrderRejectReason.TooBusy));

            Thread.Sleep(10);
        }

        [Then]
        public void AcceptedOrderWasPublishedOnce()
        {
            _publisher.Received(1).Publish(Arg.Any<OrderAccepted>());
        }

        [Then]
        public void RejectedOrderWasPublishedTwice()
        {
            _publisher.Received(2).Publish(Arg.Any<OrderRejected>());
        }
    }

    public class WhenPublishingFails : NotificationStackBaseTest
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        private const int PublishAttempts = 4;

        protected override void Given()
        {
            Config.PublishFailureReAttempts.Returns(4);
            Config.PublishFailureBackoffMilliseconds.Returns(1);
            RecordAnyExceptionsThrown();
            _publisher.When(x => x.Publish(Arg.Any<Message>())).Do(x => { throw new Exception(); });
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(NotificationTopic.OrderDispatch, _publisher);

            SystemUnderTest.Publish(new OrderAccepted(0, 0, 0));
            Thread.Sleep(20);
        }

        [Then]
        public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            _publisher.Received(PublishAttempts).Publish(Arg.Any<OrderAccepted>());
        }
    }
}