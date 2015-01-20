using JustBehave;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingMessages : GivenAServiceBus
    {
        private readonly IPublisher _publisher = Substitute.For<IPublisher>();
        
        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);

            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public void PublisherIsCalledToPublish()
        {
            Patiently.VerifyExpectation(() => _publisher.Received().Publish(Arg.Any<string>(), Arg.Any<string>()));
        }

        [Then]
        public void PublishMessageTimeStatsSent()
        {
            Patiently.VerifyExpectation(() => Monitor.Received(1).PublishMessageTime(Arg.Any<long>()), 10.Seconds());
        }
    }
}