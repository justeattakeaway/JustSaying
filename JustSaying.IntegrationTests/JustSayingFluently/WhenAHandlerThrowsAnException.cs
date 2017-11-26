using System;
using System.Threading.Tasks;
using Amazon;
using JustBehave;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [TestFixture]
    public class WhenAHandlerThrowsAnException
    {
        private ThrowingHandler _handler;
        private Action<Exception, Amazon.SQS.Model.Message> _globalErrorHandler;
        private bool _handledException;
        private IMessageMonitor _monitoring;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Setup
            _globalErrorHandler = (ex, m) => { _handledException = true; };
            _monitoring = Substitute.For<IMessageMonitor>();

            // Given
            _handler = new ThrowingHandler();

            var bus = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithMonitoring(_monitoring)
                .ConfigurePublisherWith(c =>
                    {
                        c.PublishFailureBackoffMilliseconds = 1;
                        c.PublishFailureReAttempts = 3;
                    })
                .WithSnsMessagePublisher<GenericMessage>()
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

            await bus.PublishAsync(new GenericMessage());

            // Teardown
            await _handler.DoneSignal.Task;
            bus.StopListening();
        }

        [Fact]
        public void MessagePopsOutAtTheOtherEnd()
        {
            Assert.That(_handler.MessageReceived, Is.Not.Null);
        }

        [Fact]
        public void CustomExceptionHandlingIsCalled()
        {
            Assert.That(_handledException, Is.True);
        }
    }
}
