using Amazon;
using JustEat.Testing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.Tests.MessageStubs;
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
                .WithSnsMessagePublisher<GenericMessage>("SomeTopic")
                .WithSqsTopicSubscriber("SomeTopic")
                .IntoQueue("queuename")
                .ConfigureSubscriptionWith(cfg => cfg.InstancePosition = 1)
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