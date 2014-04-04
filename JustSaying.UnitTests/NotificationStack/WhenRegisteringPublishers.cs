using JustSaying.Messaging;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenRegisteringPublishers : GivenAServiceBus
    {
        private IMessagePublisher _publisher;

        protected override void Given()
        {
            base.Given();
            _publisher = Substitute.For<IMessagePublisher>();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>("OrderDispatch", _publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>("OrderDispatch", _publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>("CustomerCommunication", _publisher);
            SystemUnderTest.Publish(new OrderAccepted());
            SystemUnderTest.Publish(new OrderRejected());
        }

        [Then]
        public void AcceptedOrderWasPublishedOnce()
        {
            Patiently.VerifyExpectation(() => _publisher.Received(1).Publish(Arg.Any<OrderAccepted>()));
        }

        [Then]
        public void RejectedOrderWasPublishedTwice()
        {
            Patiently.VerifyExpectation(() => _publisher.Received(2).Publish(Arg.Any<OrderRejected>()));
        }
    }
}