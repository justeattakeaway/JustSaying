using System;
using Amazon;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    public  class WhenCreatingErrorQueue : XBehaviourTest<ErrorQueue>
    {
        protected string QueueUniqueKey;

        protected override void Given()
        { }
        protected override void When()
        {

            SystemUnderTest.Create(new SqsBasicConfiguration { ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD, ErrorQueueOptOut = true});

            SystemUnderTest.UpdateQueueAttribute(
                new SqsBasicConfiguration {ErrorQueueRetentionPeriodSeconds = 100});
        }

        protected override ErrorQueue CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            return new ErrorQueue(RegionEndpoint.EUWest1, QueueUniqueKey, CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1), new LoggerFactory());
        }

        protected override void PostAssertTeardown()
        {
            SystemUnderTest.Delete();
            base.PostAssertTeardown();
        }

        [Fact]
        public void TheRetentionPeriodOfTheErrorQueueStaysAsMaximum()
        {
            SystemUnderTest.MessageRetentionPeriod.ShouldBe(100);
        }
    }
}
