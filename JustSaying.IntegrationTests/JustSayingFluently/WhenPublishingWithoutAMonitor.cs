using System.Threading.Tasks;
using Amazon;
using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [TestFixture]
    public class WhenPublishingWithoutAMonitor
    {
        private IMessageBus _bus;
        private readonly IHandlerAsync<GenericMessage> _handler = Substitute.For<IHandlerAsync<GenericMessage>>();

        [OneTimeSetUp]
        public async Task Given()
        {
            // Setup
            var doneSignal = new TaskCompletionSource<object>();

            // Given
            _handler.Handle(Arg.Any<GenericMessage>())
                .Returns(true)
                .AndDoes(_ => Tasks.DelaySendDone(doneSignal));

#pragma warning disable CS0618 // Type or member is obsolete
            var bus = await CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = 1;
                    c.PublishFailureReAttempts = 1;

                })
                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .ConfigureSubscriptionWith(cfg => cfg.InstancePosition = 1)
                .WithMessageHandler(_handler)
                .BuildBusAsync();
#pragma warning restore CS0618 // Type or member is obsolete

            _bus = bus;

            // When
            _bus.Subscriber.StartListening();
            _bus.Publisher.Publish(new GenericMessage());

            // Teardown
            await doneSignal.Task;
            bus.Subscriber.StopListening();
        }

        [Then]
        public void AMessageCanStillBePublishedAndPopsOutTheOtherEnd()
        {
            _handler.Received().Handle(Arg.Any<GenericMessage>());
        }
    }
}
