using JustBehave;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.SqsQueueConfiguration.Validation
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
            return new SqsReadConfiguration(SubscriptionType.ToTopic) { MessageRetentionSeconds = JustSayingConstants.MinimumRetentionPeriod +1, Topic = "ATopic", PublishEndpoint = null };
        }
    }
}
