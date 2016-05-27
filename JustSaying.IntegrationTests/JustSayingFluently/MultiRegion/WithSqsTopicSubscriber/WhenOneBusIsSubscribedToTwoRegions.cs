using System;
using System.Threading.Tasks;
using Amazon;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
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

        [Test]
        public async Task MessagesPublishedToBothRegionsWillBeReceived()
        {
            GivenASubscriptionToAQueueInTwoRegions(RegionEndpoint.EUWest1.SystemName, RegionEndpoint.USEast1.SystemName);
            AndAPublisherToThePrimaryRegion(RegionEndpoint.EUWest1.SystemName);
            AndAPublisherToTheSecondaryRegion(RegionEndpoint.USEast1.SystemName);

            WhenMessagesArePublishedToBothRegions();

            await ThenTheSubscriberReceivesBothMessages();

            _subscriber.StopListening();
        }

        private void GivenASubscriptionToAQueueInTwoRegions(string primaryRegion, string secondaryRegion)
        {
            _handler.ExpectedMessageCount = 2;

            var handler = Substitute.For<IHandlerAsync<GenericMessage>>();
            handler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            handler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(async x => await _handler.Complete((GenericMessage) x.Args()[0]));

            _subscriber = CreateMeABus
                .InRegion(primaryRegion)
                .WithFailoverRegion(secondaryRegion)
                .WithActiveRegion(() => primaryRegion)
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .WithMessageHandler(handler);
            _subscriber.StartListening();
        }

        private void AndAPublisherToThePrimaryRegion(string primaryRegion)
        {
            _primaryPublisher = CreateMeABus
                .InRegion(primaryRegion)
                .WithSnsMessagePublisher<GenericMessage>();
        }

        private void AndAPublisherToTheSecondaryRegion(string secondaryRegion)
        {
            _secondaryPublisher = CreateMeABus
                .InRegion(secondaryRegion)
                .WithSnsMessagePublisher<GenericMessage>();
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
            var done = await Tasks.WaitWithTimeoutAsync(_handler.DoneSignal);
            Assert.That(done, Is.True);

            Assert.That(_handler.ReceivedMessageCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(_handler.HasReceived(_message1));
            Assert.That(_handler.HasReceived(_message2));
        }
    }
}