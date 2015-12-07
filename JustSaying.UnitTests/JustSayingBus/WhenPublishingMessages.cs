using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingMessages : GivenAServiceBus
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        
        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);

            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public async Task PublisherIsCalledToPublish()
        {
            await Patiently.VerifyExpectationAsync(
                () => _publisher.Received().Publish(Arg.Any<GenericMessage>()));
        }

        [Then]
        public async Task PublishMessageTimeStatsSent()
        {
            await Patiently.VerifyExpectationAsync(
                () => Monitor.Received(1).PublishMessageTime(Arg.Any<long>()), 
                10.Seconds());
        }
    }
}