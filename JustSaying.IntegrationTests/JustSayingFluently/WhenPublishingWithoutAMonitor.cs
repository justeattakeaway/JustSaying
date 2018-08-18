using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenPublishingWithoutAMonitor
    {
        private IAmJustSayingFluently _bus;
        private readonly IHandlerAsync<SimpleMessage> _handler = Substitute.For<IHandlerAsync<SimpleMessage>>();

        private async Task Given()
        {
            // Setup
            var doneSignal = new TaskCompletionSource<object>();

            // Given
            _handler.Handle(Arg.Any<SimpleMessage>())
                .Returns(true)
                .AndDoes(_ => Tasks.DelaySendDone(doneSignal));

            var bus = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(TestEnvironment.Region.SystemName)
                .ConfigurePublisherWith(c =>
                    {
                        c.PublishFailureBackoffMilliseconds = 1;
                        c.PublishFailureReAttempts = 1;
                    })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .ConfigureSubscriptionWith(cfg => cfg.InstancePosition = 1)
                .WithMessageHandler(_handler);

            _bus = bus;

            // When
            _bus.StartListening();
            await _bus.PublishAsync(new SimpleMessage());

            // Teardown
            await doneSignal.Task;
            bus.StopListening();
        }

        [Fact]
        public async Task AMessageCanStillBePublishedAndPopsOutTheOtherEnd()
        {
            await Given();
            Received.InOrder(async () => await _handler.Handle(Arg.Any<SimpleMessage>()));
        }
    }
}
