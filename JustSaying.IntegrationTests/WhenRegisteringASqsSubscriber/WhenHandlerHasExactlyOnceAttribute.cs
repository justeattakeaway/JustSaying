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
    public class WhenHandlerHasExactlyOnceAttribute
    {
        private ExactlyOnceHandlerWithTimeout _handler;

        public WhenHandlerHasExactlyOnceAttribute(ITestOutputHelper outputHelper)
        {
            LoggerFactory = outputHelper.AsLoggerFactory();
        }

        private ILoggerFactory LoggerFactory { get; }

        protected async Task Act()
        {
            string region = TestEnvironment.Region.SystemName;
            _handler = new ExactlyOnceHandlerWithTimeout();

            var publisher = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(region)
                .WithSnsMessagePublisher<SimpleMessage>();

            var bus = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(region)
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename-" + DateTime.Now.Ticks)
                .ConfigureSubscriptionWith(cfg =>
                {
                    cfg.MessageRetentionSeconds = 60;
                }).WithMessageHandler(_handler);

            publisher.StartListening();
            bus.StartListening();

            var message = new SimpleMessage { Id = Guid.NewGuid() };

            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
        }

        [Fact]
        public async Task MessageHasBeenCalledOnce()
        {
            await Act();

            await Task.Delay(5.Seconds());

            _handler.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
        }
    }
}
