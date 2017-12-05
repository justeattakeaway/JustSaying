using System;
using System.Threading.Tasks;
using Amazon;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenAFailoverRegionIsSetup
    {
        private static readonly string PrimaryRegion = RegionEndpoint.EUWest1.SystemName;
        private static readonly string SecondaryRegion = RegionEndpoint.USEast1.SystemName;

        private readonly Future<GenericMessage> _primaryHandler = new Future<GenericMessage>();
        private readonly Future<GenericMessage> _secondaryHandler = new Future<GenericMessage>();

        private IHaveFulfilledPublishRequirements _publisher;
        private GenericMessage _message;
        private static string _activeRegion;
        private readonly Func<string> _getActiveRegion = () => _activeRegion;

        private IHaveFulfilledSubscriptionRequirements _primaryBus;
        private IHaveFulfilledSubscriptionRequirements _secondaryBus;

        [Fact]
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
            var primaryHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            primaryHandler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            primaryHandler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(async x => await _primaryHandler.Complete((GenericMessage)x.Args()[0]));

            _primaryBus = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(PrimaryRegion)
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .WithMessageHandler(primaryHandler);
            _primaryBus.StartListening();

            var secondaryHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            secondaryHandler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            secondaryHandler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(async x => await _secondaryHandler.Complete((GenericMessage)x.Args()[0]));

            _secondaryBus = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(SecondaryRegion)
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .WithMessageHandler(secondaryHandler);
            _secondaryBus.StartListening();
        }

        private void AndAPublisherWithAFailoverRegion()
        {
            _publisher = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(PrimaryRegion)
                .WithFailoverRegion(SecondaryRegion)
                .WithActiveRegion(_getActiveRegion)
                .WithSnsMessagePublisher<GenericMessage>();
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
            _message = new GenericMessage { Id = Guid.NewGuid() };
            await _publisher.PublishAsync(_message);
        }

        private async Task ThenTheMessageIsReceivedInThatRegion(Future<GenericMessage> handler)
        {
            var done = await Tasks.WaitWithTimeoutAsync(handler.DoneSignal);
            done.ShouldBeTrue();

            handler.ReceivedMessageCount.ShouldBeGreaterThanOrEqualTo(1);
            handler.HasReceived(_message).ShouldBeTrue();
        }
    }
}
