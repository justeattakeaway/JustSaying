using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingMessages : GivenAServiceBus
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

        protected override async Task When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);

            await SystemUnderTest.PublishAsync(new GenericMessage());
        }

        [Fact]
        public void PublisherIsCalledToPublish()
        {
            _publisher.Received().PublishAsync(Arg.Any<GenericMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void PublishMessageTimeStatsSent()
        {
            Monitor.Received(1).PublishMessageTime(Arg.Any<long>());
        }
    }
}
