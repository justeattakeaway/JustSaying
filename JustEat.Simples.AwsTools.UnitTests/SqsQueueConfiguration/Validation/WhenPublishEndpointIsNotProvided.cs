using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Stack.Amazon;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests
{
    class WhenPublishEndpointIsNotProvided : BehaviourTest<SqsConfiguration>
    {
        private SqsConfiguration _sut;


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

        protected override SqsConfiguration CreateSystemUnderTest()
        {
            return new SqsConfiguration() { MessageRetentionSeconds = NotificationStackConstants.MINIMUM_RETENTION_PERIOD +1, Topic = "ATopic", PublishEndpoint = null };
        }
    }
}
