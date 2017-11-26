using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.AwsTools.UnitTests.SqsQueueConfiguration.Validation
{
    public class WhenPublishEndpointIsNotProvided : XBehaviourTest<SqsReadConfiguration>
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.Validate();
        }

        [Fact]
        public void ThrowsException()
        {
            ThrownException.ShouldNotBeNull();
        }

        protected override SqsReadConfiguration CreateSystemUnderTest()
        {
            return new SqsReadConfiguration(SubscriptionType.ToTopic) { MessageRetentionSeconds = JustSayingConstants.MINIMUM_RETENTION_PERIOD +1, Topic = "ATopic", PublishEndpoint = null };
        }
    }
}
