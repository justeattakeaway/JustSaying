using JustBehave;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringPublishers : GivenAServiceBus
    {
        private IPublisher _publisher;

        protected override void Given()
        {
            base.Given();
            _publisher = Substitute.For<IPublisher>();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(_publisher, string.Empty);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(_publisher, string.Empty);
            SystemUnderTest.Publish(new OrderAccepted());
            SystemUnderTest.Publish(new OrderRejected());
            SystemUnderTest.Publish(new OrderRejected());
        }

        [Then]
        public void AcceptedOrderWasPublishedOnce()
        {
            Patiently.VerifyExpectation(() => _publisher.Received(1).Publish("OrderAccepted", Arg.Any<string>()));
        }

        [Then]
        public void RejectedOrderWasPublishedTwice()
        {
            Patiently.VerifyExpectation(() => _publisher.Received(2).Publish("OrderRejected", Arg.Any<string>()));
        }
    }
}