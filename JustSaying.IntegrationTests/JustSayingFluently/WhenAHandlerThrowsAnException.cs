using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenAHandlerThrowsAnException
    {
        private ThrowingHandler _handler;
        private Action<Exception, Amazon.SQS.Model.Message> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;

        public WhenAHandlerThrowsAnException(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        private async Task Setup()
        {
            // Setup
            _globalErrorHandler = (ex, m) => { _handledException = true; };
            _monitoring = Substitute.For<IMessageMonitor>();

            // Given
            _handler = new ThrowingHandler();

            var fixture = new JustSayingFixture(OutputHelper);

            var bus = fixture.Builder()
                .WithMonitoring(_monitoring)
                .ConfigurePublisherWith(c =>
                    {
                        c.PublishFailureBackoff = TimeSpan.FromMilliseconds(1);
                        c.PublishFailureReAttempts = 3;
                    })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue(fixture.UniqueName)
                .ConfigureSubscriptionWith(cfg =>
                    {
                        cfg.MessageRetention = TimeSpan.FromSeconds(60);
                        cfg.InstancePosition = 1;
                        cfg.OnError = _globalErrorHandler;
                    })
                .WithMessageHandler(_handler);

            // When
            var cts = new CancellationTokenSource();
            bus.StartListening(cts.Token);

            await bus.PublishAsync(new SimpleMessage());

            // Teardown
            await _handler.DoneSignal.Task;
            cts.Cancel();
        }

        [AwsFact]
        public async Task MessageReceivedAndExceptionHandled()
        {
            await Setup();

            _handler.MessageReceived.ShouldNotBeNull();
            _handledException.ShouldBeTrue();
        }
    }
}
