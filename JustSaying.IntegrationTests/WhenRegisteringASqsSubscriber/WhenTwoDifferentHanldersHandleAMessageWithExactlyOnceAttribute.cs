using System;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
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
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        protected async Task Act()
        {
            _handler1 = new ExactlyOnceHandlerWithTimeout();
            _handler2 = new ExactlyOnceHandlerNoTimeout();

            var fixture = new JustSayingFixture(OutputHelper);

            var publisher = fixture.Builder()
                .ConfigurePublisherWith(_ => { })
                .WithSnsMessagePublisher<SimpleMessage>();

            var subscribers = fixture.Builder()
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .WithMessageLockStoreOf(new MessageLockStore())
                .WithSqsTopicSubscriber().IntoQueue(fixture.UniqueName)
                .WithMessageHandlers(_handler1, _handler2);

            publisher.StartListening();
            subscribers.StartListening();

            await publisher.PublishAsync(new SimpleMessage { Id = Guid.NewGuid() });
        }

        [AwsFact]
        public async Task BothHandlersAreTriggered()
        {
            await Act();

            await Task.Delay(5.Seconds());

            _handler1.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
            _handler2.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
        }
    }
}
