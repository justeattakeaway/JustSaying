using System;
using System.Threading.Tasks;
using Amazon;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsPointToPointSubscriber
{
    [TestFixture]
    public class WhenOneBusIsSubscribedToTwoRegions
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>();

        private IHaveFulfilledPublishRequirements _primaryPublisher;
        private IHaveFulfilledPublishRequirements _secondaryPublisher;
        private IHaveFulfilledSubscriptionRequirements _subscriber;

        private GenericMessage _message1;
        private GenericMessage _message2;

        [TearDown]
        public void TearDown()
        {
            _subscriber.StopListening();
        }

        [Test]
        public async Task AMessagedPublishedToBothRegionsWillBeReceived()
        {
            GivenASubscriptionToAQueueInTwoRegions(RegionEndpoint.EUWest1.SystemName, RegionEndpoint.USEast1.SystemName);
            AndAPublisherToThePrimaryRegion(RegionEndpoint.EUWest1.SystemName);
            AndAPublisherToTheSecondaryRegion(RegionEndpoint.USEast1.SystemName);

            WhenMessagesArePublishedToBothRegions();

            await ThenTheSubscriberReceivesBothMessages();
        }

        private void GivenASubscriptionToAQueueInTwoRegions(string primaryRegion, string secondaryRegion)
        {
            var handler = Substitute.For<IHandler<GenericMessage>>();
            handler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            handler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(x => _handler.Complete((GenericMessage) x.Args()[0]));

            _subscriber = CreateMeABus
                .InRegion(primaryRegion)
                .WithFailoverRegion(secondaryRegion)
                .WithActiveRegion(() => primaryRegion)
                .WithSqsPointToPointSubscriber()
                .IntoQueue(string.Empty)
                .WithMessageHandler(handler);
            _subscriber.StartListening();
        }

        private void AndAPublisherToThePrimaryRegion(string primaryRegion)
        {
            _primaryPublisher = CreateMeABus
                .InRegion(primaryRegion)
                .WithSqsMessagePublisher<GenericMessage>(configuration => { });
        }

        private void AndAPublisherToTheSecondaryRegion(string secondaryRegion)
        {
            _secondaryPublisher = CreateMeABus
                .InRegion(secondaryRegion)
                .WithSqsMessagePublisher<GenericMessage>(configuration => { });
        }

        private void WhenMessagesArePublishedToBothRegions()
        {
            _message1 = new GenericMessage {Id = Guid.NewGuid()};
            _message2 = new GenericMessage {Id = Guid.NewGuid()};

            _primaryPublisher.Publish(_message1);
            _secondaryPublisher.Publish(_message2);
        }

        private async Task ThenTheSubscriberReceivesBothMessages()
        {
            await Patiently.AssertThatAsync(() => _handler.HasReceived(_message1));
            await Patiently.AssertThatAsync(() => _handler.HasReceived(_message2));
        }
    }
}