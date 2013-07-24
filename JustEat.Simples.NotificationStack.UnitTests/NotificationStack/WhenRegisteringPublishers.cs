using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.NotificationStack
{
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