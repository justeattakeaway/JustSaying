using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.SqsQueueConfiguration.Validation
{
    class WhenPublishEndpointIsNotProvided : BehaviourTest<SqsReadConfiguration>
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.Validate();
        }

        [Then]
        public void ThrowsException()
        {
            Assert.IsNotNull(ThrownException);
        }

        protected override SqsReadConfiguration CreateSystemUnderTest()
        {
            return new SqsReadConfiguration(SubscriptionType.ToTopic) { MessageRetentionSeconds = JustSayingConstants.MINIMUM_RETENTION_PERIOD +1, Topic = "ATopic", PublishEndpoint = null };
        }
    }
}
