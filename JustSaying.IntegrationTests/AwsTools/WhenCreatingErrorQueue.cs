using System;
using Amazon;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenCreatingErrorQueue : XBehaviourTest<ErrorQueue>
    {
        protected override void Given()
        {
        }

        protected override void When()
        {
            var queueConfig = new SqsBasicConfiguration
            {
                ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD,
                ErrorQueueOptOut = true
            };

            SystemUnderTest.CreateAsync(queueConfig).GetAwaiter().GetResult();

            queueConfig.ErrorQueueRetentionPeriodSeconds = 100;

            SystemUnderTest.UpdateQueueAttributeAsync(queueConfig).GetAwaiter().GetResult();
        }

        protected override ErrorQueue CreateSystemUnderTest()
        {
            string queueName = "test" + DateTime.Now.Ticks;
            RegionEndpoint region = TestEnvironment.Region;

            return new ErrorQueue(
                region,
                queueName,
                CreateMeABus.DefaultClientFactory().GetSqsClient(region),
                new LoggerFactory());
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
