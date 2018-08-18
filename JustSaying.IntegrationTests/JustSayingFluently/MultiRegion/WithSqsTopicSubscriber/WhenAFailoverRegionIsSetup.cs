using System;
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
    public class WhenAFailoverRegionIsSetup
    {
        private static readonly string PrimaryRegion = TestEnvironment.Region.SystemName;
        private static readonly string SecondaryRegion = TestEnvironment.SecondaryRegion.SystemName;

        private readonly Future<SimpleMessage> _primaryHandler = new Future<SimpleMessage>();
        private readonly Future<SimpleMessage> _secondaryHandler = new Future<SimpleMessage>();

        private IHaveFulfilledPublishRequirements _publisher;
        private SimpleMessage _message;
        private static string _activeRegion;
        private readonly Func<string> _getActiveRegion = () => _activeRegion;

        private IHaveFulfilledSubscriptionRequirements _primaryBus;
        private IHaveFulfilledSubscriptionRequirements _secondaryBus;

        public WhenAFailoverRegionIsSetup(ITestOutputHelper outputHelper)
        {
            LoggerFactory = outputHelper.AsLoggerFactory();
        }

        private ILoggerFactory LoggerFactory { get; }

        [AwsFact]
        public async Task MessagesArePublishedToTheActiveRegion()
        {
            GivenSubscriptionsToAQueueInTwoRegions();
            AndAPublisherWithAFailoverRegion();

            WhenThePrimaryRegionIsActive();
            await AndAMessageIsPublished();
            await ThenTheMessageIsReceivedInThatRegion(_primaryHandler);

            WhenTheFailoverRegionIsActive();
            await AndAMessageIsPublished();
            await ThenTheMessageIsReceivedInThatRegion(_secondaryHandler);

            _primaryBus.StopListening();
            _secondaryBus.StopListening();
        }

        private void GivenSubscriptionsToAQueueInTwoRegions()
        {
            var primaryHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            primaryHandler.Handle(Arg.Any<SimpleMessage>()).Returns(true);
            primaryHandler
                .When(x => x.Handle(Arg.Any<SimpleMessage>()))
                .Do(async x => await _primaryHandler.Complete((SimpleMessage)x.Args()[0]));

            string queueName = new JustSayingFixture().UniqueName;

            _primaryBus = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(PrimaryRegion)
                .WithSqsTopicSubscriber()
                .IntoQueue(queueName)
                .WithMessageHandler(primaryHandler);

            _primaryBus.StartListening();

            var secondaryHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            secondaryHandler.Handle(Arg.Any<SimpleMessage>()).Returns(true);
            secondaryHandler
                .When(x => x.Handle(Arg.Any<SimpleMessage>()))
                .Do(async x => await _secondaryHandler.Complete((SimpleMessage)x.Args()[0]));

            _secondaryBus = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(SecondaryRegion)
                .WithSqsTopicSubscriber()
                .IntoQueue(queueName)
                .WithMessageHandler(secondaryHandler);

            _secondaryBus.StartListening();
        }

        private void AndAPublisherWithAFailoverRegion()
        {
            _publisher = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(PrimaryRegion)
                .WithFailoverRegion(SecondaryRegion)
                .WithActiveRegion(_getActiveRegion)
                .WithSnsMessagePublisher<SimpleMessage>();
        }

        private void WhenThePrimaryRegionIsActive()
        {
            _activeRegion = PrimaryRegion;
        }

        private void WhenTheFailoverRegionIsActive()
        {
            _activeRegion = SecondaryRegion;
        }

        private async Task AndAMessageIsPublished()
        {
            _message = new SimpleMessage { Id = Guid.NewGuid() };
            await _publisher.PublishAsync(_message);

            await Task.Yield();
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        private async Task ThenTheMessageIsReceivedInThatRegion(Future<SimpleMessage> handler)
        {
            var done = await Tasks.WaitWithTimeoutAsync(handler.DoneSignal);
            done.ShouldBeTrue();

            handler.ReceivedMessageCount.ShouldBeGreaterThanOrEqualTo(1);
            handler.HasReceived(_message).ShouldBeTrue();
        }
    }
}
