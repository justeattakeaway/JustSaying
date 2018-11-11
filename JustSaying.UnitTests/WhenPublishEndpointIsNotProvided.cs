using System;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustBehave;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests
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
            => new SqsReadConfiguration(SubscriptionType.ToTopic)
            {
                MessageRetention = JustSayingConstants.MinimumRetentionPeriod.Add(TimeSpan.FromSeconds(1)),
                Topic = "ATopic",
                PublishEndpoint = null
            };
    }
}
