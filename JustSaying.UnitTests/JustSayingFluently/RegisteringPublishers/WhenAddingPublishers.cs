using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.RegisteringPublishers
{
    public class WhenAddingPublishers : BehaviourTest<JustSaying.JustSayingFluently>
    {
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



        /// Note: Ignored tests are here for fluent api exploration & expecting compile time issues when working on the fluent interface stuff...
        
        [Then, Ignore]
        public void ASqsPublisherCanBeSetup()
        {
            SystemUnderTest.ConfigurePublisherWith(conf => conf.PublishFailureBackoffMilliseconds = 50)
                .WithSnsMessagePublisher<GenericMessage>();
        }

        [Then, Ignore]
        public void MultipleSqsPublishersCanBeSetup()
        {
            SystemUnderTest.ConfigurePublisherWith(conf => conf.PublishFailureBackoffMilliseconds = 50)
                .WithSnsMessagePublisher<GenericMessage>()
                .WithSnsMessagePublisher<GenericMessage>();
        }

        [Then, Ignore]
        public void ASqsPublisherCanBeSetupWithConfiguration()
        {
            SystemUnderTest.WithSqsMessagePublisher<GenericMessage>(c =>
            {
                c.VisibilityTimeoutSeconds = 1;
                c.RetryCountBeforeSendingToErrorQueue = 2;
                c.MessageRetentionSeconds = 3;
                c.ErrorQueueOptOut = true;
            });
        }
    }
}
