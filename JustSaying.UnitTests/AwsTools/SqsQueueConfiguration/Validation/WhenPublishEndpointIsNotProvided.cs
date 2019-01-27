using System;
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

        protected override void WhenAction()
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
            return new SqsReadConfiguration(SubscriptionType.ToTopic)
            {
                MessageRetention = JustSayingConstants.MinimumRetentionPeriod.Add(TimeSpan.FromSeconds(1)),
                Topic = "ATopic",
                PublishEndpoint = null
            };
        }
    }
}
