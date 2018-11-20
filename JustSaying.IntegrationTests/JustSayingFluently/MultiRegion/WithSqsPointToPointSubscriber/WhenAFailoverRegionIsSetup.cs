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

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsPointToPointSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenAFailoverRegionIsSetup
    {
        private static string PrimaryRegion => TestEnvironment.Region.SystemName;
        private static string SecondaryRegion => TestEnvironment.SecondaryRegion.SystemName;

        private readonly Future<SimpleMessage> _primaryHandler = new Future<SimpleMessage>();
        private readonly Future<SimpleMessage> _secondaryHandler = new Future<SimpleMessage>();

        private IHaveFulfilledPublishRequirements _publisher;
        private SimpleMessage _message;
        private static string _activeRegion;
        private readonly Func<string> _getActiveRegion = () => _activeRegion;

        private IHaveFulfilledSubscriptionRequirements _primaryBus;
        private CancellationTokenSource _primaryBusCts;
        private IHaveFulfilledSubscriptionRequirements _secondaryBus;
        private CancellationTokenSource _secondaryBusCts;

        public WhenAFailoverRegionIsSetup(ITestOutputHelper outputHelper)
        {
            LoggerFactory = outputHelper.ToLoggerFactory();
        }

        private string QueueName { get; } = new JustSayingFixture().UniqueName;

        private ILoggerFactory LoggerFactory { get; }

        [NotSimulatorFact]
        public async Task MessagesArePublishedToTheActiveRegion()
        {
            GivenSubscriptionsToAQueueInTwoRegions();
            AndAPublisherWithAFailoverRegion();

            WhenThePrimaryRegionIsActive();
            await AndAMessageIsPublished();
            await ThenTheMessageIsReceivedInThatRegion(_primaryHandler, PrimaryRegion);

            WhenTheFailoverRegionIsActive();
            await AndAMessageIsPublished();
            await ThenTheMessageIsReceivedInThatRegion(_secondaryHandler, SecondaryRegion);

            _primaryBusCts.Cancel();
            _secondaryBusCts.Cancel();
        }

        private void GivenSubscriptionsToAQueueInTwoRegions()
        {
            var primaryHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            primaryHandler.Handle(Arg.Any<SimpleMessage>()).Returns(true);
            primaryHandler
                .When(x => x.Handle(Arg.Any<SimpleMessage>()))
                .Do(async x => await _primaryHandler.Complete((SimpleMessage)x.Args()[0]));

            _primaryBus = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(PrimaryRegion)
                .WithSqsPointToPointSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(primaryHandler);

            _primaryBusCts = new CancellationTokenSource();
            _primaryBus.StartListening(_primaryBusCts.Token);

            var secondaryHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            secondaryHandler.Handle(Arg.Any<SimpleMessage>()).Returns(true);
            secondaryHandler
                .When(x => x.Handle(Arg.Any<SimpleMessage>()))
                .Do(async x => await _secondaryHandler.Complete((SimpleMessage)x.Args()[0]));

            _secondaryBus = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(SecondaryRegion)
                .WithSqsPointToPointSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(secondaryHandler);

            _secondaryBusCts = new CancellationTokenSource();
            _secondaryBus.StartListening(_secondaryBusCts.Token);
        }

        private void AndAPublisherWithAFailoverRegion()
        {
            _publisher = CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(PrimaryRegion)
                .WithFailoverRegion(SecondaryRegion)
                .WithActiveRegion(_getActiveRegion)
                .WithSqsMessagePublisher<SimpleMessage>(cfg => cfg.QueueName = QueueName);
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

        private async Task ThenTheMessageIsReceivedInThatRegion(Future<SimpleMessage> handler, string regionName)
        {
            var done = await Tasks.WaitWithTimeoutAsync(handler.DoneSignal);
            done.ShouldBeTrue($"Handler did not complete in region {regionName}.");

            handler.ReceivedMessageCount.ShouldBeGreaterThanOrEqualTo(1, $"Received message count was incorrect in region {regionName}.");
            handler.HasReceived(_message).ShouldBeTrue($"Message was not received in region {regionName}.");
        }
    }
}
