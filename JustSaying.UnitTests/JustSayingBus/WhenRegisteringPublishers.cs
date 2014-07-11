using JustBehave;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
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
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(_publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(_publisher);
            SystemUnderTest.Publish(new OrderAccepted());
            SystemUnderTest.Publish(new OrderRejected());
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