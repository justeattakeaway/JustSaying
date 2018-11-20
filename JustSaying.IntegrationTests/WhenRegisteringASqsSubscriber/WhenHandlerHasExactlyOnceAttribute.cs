using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
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
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        protected async Task Act()
        {
            _handler = new ExactlyOnceHandlerWithTimeout();

            var fixture = new JustSayingFixture(OutputHelper);

            var publisher = fixture.Builder()
                .WithSnsMessagePublisher<SimpleMessage>();

            var bus = fixture.Builder()
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber()
                .IntoQueue(fixture.UniqueName)
                .ConfigureSubscriptionWith(cfg => cfg.MessageRetention = TimeSpan.FromSeconds(60))
                .WithMessageHandler(_handler);

            publisher.StartListening();
            bus.StartListening();

            var message = new SimpleMessage { Id = Guid.NewGuid() };

            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
        }

        [AwsFact]
        public async Task MessageHasBeenCalledOnce()
        {
            await Act();

            await Task.Delay(5.Seconds());

            _handler.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
        }
    }
}
