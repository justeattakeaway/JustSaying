using System;
using System.Threading.Tasks;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public  class WhenCreatingErrorQueue : AsyncBehaviourTest<ErrorQueue>
    {
        protected string QueueUniqueKey;

        protected override void Given()
        { }
        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration { ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD, ErrorQueueOptOut = true});

            await SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration {ErrorQueueRetentionPeriodSeconds = 100});
        }

        protected override ErrorQueue CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            return new ErrorQueue(RegionEndpoint.EUWest1, QueueUniqueKey, CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1), new LoggerFactory());
        }
        public override void PostAssertTeardown()
        {
            SystemUnderTest.DeleteAsync().GetAwaiter().GetResult();
            base.PostAssertTeardown();
        }

        [Test]
        public void TheRetentionPeriodOfTheErrorQueueStaysAsMaximum()
        {
            Assert.AreEqual(100, SystemUnderTest.MessageRetentionPeriod);
        }
    }
}
