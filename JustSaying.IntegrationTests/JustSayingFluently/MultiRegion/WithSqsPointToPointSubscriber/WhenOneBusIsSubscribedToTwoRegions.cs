using System;
using System.Threading.Tasks;
using Amazon;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsPointToPointSubscriber
{
    [TestFixture]
    public class WhenOneBusIsSubscribedToTwoRegions
    {
        private readonly Future<GenericMessage> _handler = new Future<GenericMessage>();

        private IMessagePublisher _primaryPublisher;
        private IMessagePublisher _secondaryPublisher;
        private IMessageSubscriber _subscriber;

        private GenericMessage _message1;
        private GenericMessage _message2;

        [Test]
        public async Task MessagesPublishedToBothRegionsWillBeReceived()
        {
            await GivenASubscriptionToAQueueInTwoRegions(RegionEndpoint.EUWest1.SystemName, RegionEndpoint.USEast1.SystemName);
            await AndAPublisherToThePrimaryRegion(RegionEndpoint.EUWest1.SystemName);
            await AndAPublisherToTheSecondaryRegion(RegionEndpoint.USEast1.SystemName);

            WhenMessagesArePublishedToBothRegions();

            await ThenTheSubscriberReceivesBothMessages();

            _subscriber.StopListening();
        }

        private async Task GivenASubscriptionToAQueueInTwoRegions(string primaryRegion, string secondaryRegion)
        {
            _handler.ExpectedMessageCount = 2;
            var handler = Substitute.For<IHandlerAsync<GenericMessage>>();
            handler.Handle(Arg.Any<GenericMessage>()).Returns(true);
            handler
                .When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(async x => await _handler.Complete((GenericMessage) x.Args()[0]));

            _subscriber = await CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(primaryRegion)
                .WithFailoverRegion(secondaryRegion)
                .WithActiveRegion(() => primaryRegion)
                .WithSqsPointToPointSubscriber()
                .IntoDefaultQueue()
                .WithMessageHandler(handler)
                .BuildSubscriberAsync();

            _subscriber.StartListening();
        }

        private async Task AndAPublisherToThePrimaryRegion(string primaryRegion)
        {
            _primaryPublisher = await CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(primaryRegion)
                .WithSqsMessagePublisher<GenericMessage>(configuration => { })
                .BuildPublisherAsync();
        }

        private async Task AndAPublisherToTheSecondaryRegion(string secondaryRegion)
        {
            _secondaryPublisher = await CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(secondaryRegion)
                .WithSqsMessagePublisher<GenericMessage>(configuration => { })
                .BuildPublisherAsync();
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