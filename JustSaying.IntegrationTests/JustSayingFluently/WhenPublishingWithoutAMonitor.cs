using Amazon;
using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [TestFixture]
    public class WhenPublishingWithoutAMonitor
    {
        private IAmJustSayingFluently _bus;
        private readonly IHandler<GenericMessage> _handler = Substitute.For<IHandler<GenericMessage>>();

        [TestFixtureSetUp]
        public void Given()
        {
            var bus = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName).ConfigurePublisherWith(c =>
            {
                c.PublishFailureBackoffMilliseconds = 1;
                c.PublishFailureReAttempts = 1;
                
            })
                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename")
                .WithMessageHandler(_handler);

            _bus = bus;
            _bus.StartListening();
        }

        [SetUp]
        public void When()
        {
            _bus.Publish(new GenericMessage());
        }

        [Then]
        public void AMessageCanStillBePublishedAndPopsOutTheOtherEnd()
        {
            Patiently.VerifyExpectation(() => _handler.Received().Handle(Arg.Any<GenericMessage>()));
        }

        [TearDown]
        public void ByeBye()
        {
            _bus.StopListening();
            _bus = null;
        }
    }
}