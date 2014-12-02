using Amazon;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    public class PublishToTwoRegionsWillBeReceivedByConsumer : WhenSubscribingToTwoRegions
    {
        private GenericMessage _message1;
        private GenericMessage _message2; 

        protected override void When()
        {
            var publisher1 = CreateMeABus
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithMonitoring(Monitoring)
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = Config.PublishFailureBackoffMilliseconds;
                    c.PublishFailureReAttempts = Config.PublishFailureReAttempts;
                })
                .WithSnsMessagePublisher<GenericMessage>();

            var publisher2 = CreateMeABus
                .InRegion(RegionEndpoint.USEast1.SystemName)
                .WithMonitoring(Monitoring)
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = Config.PublishFailureBackoffMilliseconds;
                    c.PublishFailureReAttempts = Config.PublishFailureReAttempts;
                })
                .WithSnsMessagePublisher<GenericMessage>();

            _message1 = new GenericMessage() { Content = "message1" };
            publisher1.Publish(_message1);
            _message2 = new GenericMessage() { Content = "message2" };
            publisher2.Publish(_message2);
        }

        [Test]
        public void ThenItGetsHandled()
        {
            Patiently.AssertThat(() => Handler.HasReceived(_message1));
            Patiently.AssertThat(() => Handler.HasReceived(_message2));
        }
    }
}