using System;
using Amazon;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsPointToPointSubscriber
{
    [TestFixture]
    public class WhenSubscribedToTwoRegions
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>();

        private IHaveFulfilledPublishRequirements _publisher1;
        private IHaveFulfilledPublishRequirements _publisher2;

        private GenericMessage _message1;
        private GenericMessage _message2;

        [Test]
        public void AMessagedPublishedToBothRegionsWillBeReceived()
        {
            GivenASubscriptionToAQueueInTwoRegions(RegionEndpoint.EUWest1.SystemName, RegionEndpoint.USEast1.SystemName);
            AndAPublisherToThePrimaryRegion(RegionEndpoint.EUWest1.SystemName);
            AndAPublisherToTheSecondaryRegion(RegionEndpoint.USEast1.SystemName);

            WhenMessagesArePublishedToBothRegions();

            ThenTheyAreReceivedByBothSubscribers();
        }

        private void GivenASubscriptionToAQueueInTwoRegions(string primaryRegion, string secondaryRegion)
        {
            var handler = Substitute.For<IHandler<GenericMessage>>();
            handler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(x => _handler.Complete((GenericMessage) x.Args()[0]));

            CreateMeABus
                .InRegion(primaryRegion)
                .WithFailoverRegion(secondaryRegion)
                .WithSqsPointToPointSubscriber()
                .IntoQueue(string.Empty)
                .WithMessageHandler(handler)
                .StartListening();
        }

        private void AndAPublisherToThePrimaryRegion(string primaryRegion)
        {
            _publisher1 = CreateMeABus
                .InRegion(primaryRegion)
                .WithSqsMessagePublisher<GenericMessage>(configuration => { });
        }

        private void AndAPublisherToTheSecondaryRegion(string secondaryRegion)
        {
            _publisher2 = CreateMeABus
                .InRegion(secondaryRegion)
                .WithSqsMessagePublisher<GenericMessage>(configuration => { });
        }

        private void WhenMessagesArePublishedToBothRegions()
        {
            _message1 = new GenericMessage {Id = Guid.NewGuid()};
            _message2 = new GenericMessage {Id = Guid.NewGuid()};

            _publisher1.Publish(_message1);
            _publisher2.Publish(_message2);
        }

        private void ThenTheyAreReceivedByBothSubscribers()
        {
            Patiently.AssertThat(() => _handler.HasReceived(_message1));
            Patiently.AssertThat(() => _handler.HasReceived(_message2));
        }
    }
}