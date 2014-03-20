using System.Threading;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;
using Tests.MessageStubs;

namespace SimpleMessageMule.UnitTests.NotificationStack
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

            SystemUnderTest.Publish(new OrderAccepted());
            SystemUnderTest.Publish(new OrderRejected());

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