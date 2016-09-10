using System;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public  class WhenCreatingErrorQueue : BehaviourTest<ErrorQueue>
    {
        protected string QueueUniqueKey;

        protected override void Given()
        { }
        protected override void When()
        {

            SystemUnderTest.Create(new SqsBasicConfiguration { ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD, ErrorQueueOptOut = true});

            SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration { ErrorQueueRetentionPeriodSeconds = 100 })
                .GetAwaiter().GetResult();
        }

        protected override ErrorQueue CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            return new ErrorQueue(RegionEndpoint.EUWest1, QueueUniqueKey, CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1));
        }
        public override void PostAssertTeardown()
        {
            SystemUnderTest.Delete();
            base.PostAssertTeardown();
        }

        [Test]
        public void TheRetentionPeriodOfTheErrorQueueStaysAsMaximum()
        {
            Assert.AreEqual(100, SystemUnderTest.MessageRetentionPeriod);
        }
    }
}
