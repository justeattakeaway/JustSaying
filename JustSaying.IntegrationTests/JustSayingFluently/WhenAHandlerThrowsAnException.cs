using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenAHandlerThrowsAnException
    {
        private ThrowingHandler _handler;
        private Action<Exception, Amazon.SQS.Model.Message> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;
        
        private async Task Setup()
        {
            // Setup
            _globalErrorHandler = (ex, m) => { _handledException = true; };
            _monitoring = Substitute.For<IMessageMonitor>();

            // Given
            _handler = new ThrowingHandler();

            var bus = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(TestEnvironment.Region.SystemName)
                .WithMonitoring(_monitoring)
                .ConfigurePublisherWith(c =>
                    {
                        c.PublishFailureBackoffMilliseconds = 1;
                        c.PublishFailureReAttempts = 3;
                    })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .ConfigureSubscriptionWith(cfg =>
                    {
                        cfg.MessageRetentionSeconds = 60;
                        cfg.InstancePosition = 1;
                        cfg.OnError = _globalErrorHandler;
                    })
                .WithMessageHandler(_handler);

            // When
            bus.StartListening();

            await bus.PublishAsync(new SimpleMessage());

            // Teardown
            await _handler.DoneSignal.Task;
            bus.StopListening();
        }

        [Fact]
        public async Task MessageReceivedAndExceptionHandled()
        {
            await Setup();
            _handler.MessageReceived.ShouldNotBeNull();
            _handledException.ShouldBeTrue();
        }
    }
}
