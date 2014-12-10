using System;
using Amazon;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsPointToPointSubscriber
{
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

        [TearDown]
        public void TearDown()
        {
            _primaryBus.StopListening();
            _secondaryBus.StopListening();
        }

        [Test]
        public void MessagesArePublishedToTheActiveRegion()
        {
            GivenSubscriptionsToAQueueInTwoRegions();
            AndAPublisherWithAFailoverRegion();

            WhenThePrimaryRegionIsActive();
            AndAMessageIsPublished();
            ThenTheMessageIsReceivedInThatRegion(_primaryHandler);

            WhenTheFailoverRegionIsActive();
            AndAMessageIsPublished();
            ThenTheMessageIsReceivedInThatRegion(_secondaryHandler);
        }

        private void GivenSubscriptionsToAQueueInTwoRegions()
        {
            var primaryHandler = Substitute.For<IHandler<GenericMessage>>();
            primaryHandler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            primaryHandler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(x => _primaryHandler.Complete((GenericMessage)x.Args()[0]));

            _primaryBus = CreateMeABus
                .InRegion(PrimaryRegion)
                .WithSqsPointToPointSubscriber()
                .IntoQueue(string.Empty)
                .WithMessageHandler(primaryHandler);
            _primaryBus.StartListening();

            var secondaryHandler = Substitute.For<IHandler<GenericMessage>>();
            secondaryHandler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            secondaryHandler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(x => _secondaryHandler.Complete((GenericMessage)x.Args()[0]));

            _secondaryBus = CreateMeABus
                .InRegion(SecondaryRegion)
                .WithSqsPointToPointSubscriber()
                .IntoQueue(string.Empty)
                .WithMessageHandler(secondaryHandler);
            _secondaryBus.StartListening();
        }

        private void AndAPublisherWithAFailoverRegion()
        {
            _publisher = CreateMeABus
                .InRegion(PrimaryRegion)
                .WithFailoverRegion(SecondaryRegion)
                .WithActiveRegion(_getActiveRegion)
                .WithSqsMessagePublisher<GenericMessage>(configuration => { });
        }

        private void WhenThePrimaryRegionIsActive()
        {
            _activeRegion = PrimaryRegion;
        }

        private void WhenTheFailoverRegionIsActive()
        {
            _activeRegion = SecondaryRegion;
        }

        private void AndAMessageIsPublished()
        {
            _message = new GenericMessage { Id = Guid.NewGuid() };
            _publisher.Publish(_message);
        }

        private void ThenTheMessageIsReceivedInThatRegion(Future<GenericMessage> handler)
        {
            Patiently.AssertThat(() => handler.HasReceived(_message));
        }
    }
}