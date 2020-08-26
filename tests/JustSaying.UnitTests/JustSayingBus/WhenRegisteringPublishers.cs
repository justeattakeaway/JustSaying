using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

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

        protected override async Task WhenAsync()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(_publisher);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(_publisher);

            await SystemUnderTest.StartAsync(CancellationToken.None);

            await SystemUnderTest.PublishAsync(new OrderAccepted());
            await SystemUnderTest.PublishAsync(new OrderRejected());
            await SystemUnderTest.PublishAsync(new OrderRejected());
        }

        [Fact]
        public void AcceptedOrderWasPublishedOnce()
        {
            _publisher.Received(1).PublishAsync(
                Arg.Is<Message>(m => m is OrderAccepted),
                Arg.Any<PublishMetadata>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void RejectedOrderWasPublishedTwice()
        {
            _publisher.Received(2).PublishAsync(
                Arg.Is<Message>(m => m is OrderRejected),
                Arg.Any<PublishMetadata>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void AndInterrogationShowsPublishersHaveBeenSet()
        {
            dynamic response = SystemUnderTest.Interrogate();

            string[] publishedTypes = response.Data.PublishedMessageTypes;

            publishedTypes.ShouldContain(nameof(OrderAccepted));
            publishedTypes.ShouldContain(nameof(OrderRejected));
        }
    }
}
