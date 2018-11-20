using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenOneBusIsSubscribedToTwoRegions
    {
        private readonly Future<SimpleMessage> _handler = new Future<SimpleMessage>();

        private IHaveFulfilledPublishRequirements _primaryPublisher;
        private IHaveFulfilledPublishRequirements _secondaryPublisher;
        private IHaveFulfilledSubscriptionRequirements _subscriber;
        private CancellationTokenSource _subscriberCts;

        private SimpleMessage _message1;
        private SimpleMessage _message2;

        public WhenOneBusIsSubscribedToTwoRegions(ITestOutputHelper outputHelper)
        {
            LoggerFactory = outputHelper.ToLoggerFactory();
        }

        private ILoggerFactory LoggerFactory { get; }

        private string QueueName { get; } = new JustSayingFixture().UniqueName;

        [AwsFact]
        public async Task MessagesPublishedToBothRegionsWillBeReceived()
        {
            var region1 = TestEnvironment.Region.SystemName;
            var region2 = TestEnvironment.SecondaryRegion.SystemName;

            GivenASubscriptionToAQueueInTwoRegions(region1, region2);

            AndAPublisherToThePrimaryRegion(region1);
            AndAPublisherToTheSecondaryRegion(region2);

            await WhenMessagesArePublishedToBothRegions();

            await ThenTheSubscriberReceivesBothMessages();

            _subscriberCts.Cancel();
        }

        private void GivenASubscriptionToAQueueInTwoRegions(string primaryRegion, string secondaryRegion)
        {
            _handler.ExpectedMessageCount = 2;

            var handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            handler.Handle(Arg.Any<SimpleMessage>()).Returns(true);
            handler
                .When(x => x.Handle(Arg.Any<SimpleMessage>()))
                .Do(async x => await _handler.Complete((SimpleMessage) x.Args()[0]));

            _subscriber = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(primaryRegion)
                .WithFailoverRegion(secondaryRegion)
                .WithActiveRegion(() => primaryRegion)
                .WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(handler);

            _subscriberCts = new CancellationTokenSource();
            _subscriber.StartListening(_subscriberCts.Token);
        }

        private void AndAPublisherToThePrimaryRegion(string primaryRegion)
        {
            _primaryPublisher = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(primaryRegion)
                .WithSnsMessagePublisher<SimpleMessage>();
        }

        private void AndAPublisherToTheSecondaryRegion(string secondaryRegion)
        {
            _secondaryPublisher = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(secondaryRegion)
                .WithSnsMessagePublisher<SimpleMessage>();
        }

        private async Task WhenMessagesArePublishedToBothRegions()
        {
            _message1 = new SimpleMessage { Id = Guid.NewGuid() };
            _message2 = new SimpleMessage { Id = Guid.NewGuid() };

            await _primaryPublisher.PublishAsync(_message1);
            await _secondaryPublisher.PublishAsync(_message2);

            await Task.Yield();
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        private async Task ThenTheSubscriberReceivesBothMessages()
        {
            var done = await Tasks.WaitWithTimeoutAsync(_handler.DoneSignal);
            done.ShouldBeTrue();

            _handler.ReceivedMessageCount.ShouldBeGreaterThanOrEqualTo(2);
            _handler.HasReceived(_message1).ShouldBeTrue();
            _handler.HasReceived(_message2).ShouldBeTrue();
        }
    }
}
