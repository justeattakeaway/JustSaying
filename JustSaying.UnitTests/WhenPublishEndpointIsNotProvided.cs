using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustBehave;
using NUnit.Framework;

namespace JustSaying.UnitTests
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
            return new SqsReadConfiguration() { MessageRetentionSeconds = JustSayingConstants.MINIMUM_RETENTION_PERIOD +1, Topic = "ATopic", PublishEndpoint = null };
        }
    }
}
