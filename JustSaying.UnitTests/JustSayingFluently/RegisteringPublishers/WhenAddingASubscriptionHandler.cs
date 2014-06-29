using JustEat.Testing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenAddingSubscribers : BehaviourTest<JustSaying.JustSayingFluently>
    {
        private const string Topic = "SomeTopic";
        private readonly IAmJustSaying _bus = Substitute.For<IAmJustSaying>();

        protected override JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            return new JustSaying.JustSayingFluently(_bus, null);
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();

            var config = new MessagingConfig {Region = "fake_region"};
            _bus.Config.Returns(config);
        }

        protected override void When()
        {
        }

        [Then]
        public void ConfigurationIsRequired()
        {
            SystemUnderTest.ConfigurePublisherWith(conf => conf.PublishFailureBackoffMilliseconds = 50);
        }
        
        [Then, Ignore]
        public void APublisherCanBeSetup()
        {
            SystemUnderTest.ConfigurePublisherWith(conf => conf.PublishFailureBackoffMilliseconds = 50)
                .WithSnsMessagePublisher<GenericMessage>(Topic);
        }

        [Then, Ignore]
        public void MultiplePublishersCanBeSetup()
        {
            SystemUnderTest.ConfigurePublisherWith(conf => conf.PublishFailureBackoffMilliseconds = 50)
                .WithSnsMessagePublisher<GenericMessage>(Topic)
                .WithSnsMessagePublisher<GenericMessage>(Topic);
        }
    }
}
