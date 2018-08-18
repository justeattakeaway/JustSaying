using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenTwoDifferentHanldersHandleAMessageWithExactlyOnceAttribute
    {
        private ExactlyOnceHandlerWithTimeout _handler1;
        private ExactlyOnceHandlerWithTimeout _handler2;

        public WhenTwoDifferentHanldersHandleAMessageWithExactlyOnceAttribute(ITestOutputHelper outputHelper)
        {
            LoggerFactory = outputHelper.AsLoggerFactory();
        }

        private ILoggerFactory LoggerFactory { get; }

        protected async Task Act()
        {
            _handler1 = new ExactlyOnceHandlerWithTimeout();
            _handler2 = new ExactlyOnceHandlerNoTimeout();

            string region = TestEnvironment.Region.SystemName;

            var publisher = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(region)
                .ConfigurePublisherWith(_ => { })
                .WithSnsMessagePublisher<SimpleMessage>();

            var bus = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(region)
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber().IntoQueue("queuename-" + DateTime.Now.Ticks)
                .WithMessageHandlers(_handler1, _handler2);

            publisher.StartListening();
            bus.StartListening();

            await publisher.PublishAsync(new SimpleMessage { Id = Guid.NewGuid() });
        }

        [Fact]
        public async Task BothHandlersAreTriggered()
        {
            await Act();

            if (!TestEnvironment.IsSimulatorConfigured)
            {
                await Task.Delay(5.Seconds());
            }

            _handler1.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
            _handler2.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
        }
    }
}
