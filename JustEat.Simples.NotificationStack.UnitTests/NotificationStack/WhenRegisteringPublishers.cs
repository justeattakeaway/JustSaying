using System.Threading;
using JustEat.Simples.NotificationStack.Messaging;
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
            SystemUnderTest.AddMessagePublisher<OrderAccepted>("OrderDispatch", _publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>("OrderDispatch", _publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>("CustomerCommunication", _publisher);

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
}