using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenPublishingWithoutAMonitor
    {
        private IAmJustSayingFluently _bus;
        private CancellationTokenSource _busCts;
        private readonly IHandlerAsync<SimpleMessage> _handler = Substitute.For<IHandlerAsync<SimpleMessage>>();

        public WhenPublishingWithoutAMonitor(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        private async Task Given()
        {
            // Setup
            var doneSignal = new TaskCompletionSource<object>();

            // Given
            _handler.Handle(Arg.Any<SimpleMessage>())
                .Returns(true)
                .AndDoes(_ => Tasks.DelaySendDone(doneSignal));

            var fixture = new JustSayingFixture(OutputHelper);

            _bus = fixture.Builder()
                .ConfigurePublisherWith(c =>
                    {
                        c.PublishFailureBackoff = TimeSpan.FromMilliseconds(1);
                        c.PublishFailureReAttempts = 1;
                    })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue(fixture.UniqueName)
                .ConfigureSubscriptionWith(cfg => cfg.InstancePosition = 1)
                .WithMessageHandler(_handler);

            // When
            _busCts = new CancellationTokenSource();
            _bus.StartListening(_busCts.Token);
            await _bus.PublishAsync(new SimpleMessage());

            // Teardown
            await doneSignal.Task;
            _busCts.Cancel();
        }

        [AwsFact]
        public async Task AMessageCanStillBePublishedAndPopsOutTheOtherEnd()
        {
            await Given();
            Received.InOrder(async () => await _handler.Handle(Arg.Any<SimpleMessage>()));
        }
    }
}
