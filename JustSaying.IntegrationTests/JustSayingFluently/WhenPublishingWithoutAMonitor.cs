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
            var bus = CreateMe.ABus(c =>
            {
                c.PublishFailureBackoffMilliseconds = 1;
                c.PublishFailureReAttempts = 1;
                c.Region = RegionEndpoint.EUWest1.SystemName;
            })
                .WithSnsMessagePublisher<GenericMessage>("SomeTopic")
                .WithSqsTopicSubscriber("SomeTopic", 60, instancePosition: 1)
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
        public void AMessageanStillBePublishedAndPopsOutTheOtherEnd()
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