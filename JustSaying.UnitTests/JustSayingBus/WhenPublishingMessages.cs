using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingMessages : GivenAServiceBus
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

        protected override async Task WhenAction()
        {
            SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher, string.Empty);

            await SystemUnderTest.PublishAsync(new SimpleMessage());
        }

        [Fact]
        public void PublisherIsCalledToPublish()
        {
            _publisher.Received().PublishAsync(Arg.Any<Message>(),
                Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void PublishMessageTimeStatsSent()
        {
            Monitor.Received(1).PublishMessageTime(Arg.Any<TimeSpan>());
        }
    }
}
