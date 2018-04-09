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
    [Collection(GlobalSetup.CollectionName)]
    public  class WhenCreatingErrorQueue : XBehaviourTest<ErrorQueue>
    {
        protected string QueueUniqueKey;

        protected override void Given()
        { }
        protected override void When()
        {

            SystemUnderTest.CreateAsync(new SqsBasicConfiguration { ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD, ErrorQueueOptOut = true}).GetAwaiter().GetResult();

            SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration {ErrorQueueRetentionPeriodSeconds = 100}).GetAwaiter().GetResult();
        }

        protected override ErrorQueue CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            return new ErrorQueue(RegionEndpoint.EUWest1, QueueUniqueKey, CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1), new LoggerFactory());
        }

        protected override void PostAssertTeardown()
        {
            SystemUnderTest.DeleteAsync().GetAwaiter().GetResult();
            base.PostAssertTeardown();
        }

        [Fact]
        public void TheRetentionPeriodOfTheErrorQueueStaysAsMaximum()
        {
            SystemUnderTest.MessageRetentionPeriod.ShouldBe(100);
        }
    }
}
