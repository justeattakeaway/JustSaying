using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenCreatingErrorQueue : XAsyncBehaviourTest<ErrorQueue>
    {
        protected override void Given()
        {
        }

        protected override async Task When()
        {
            var queueConfig = new SqsBasicConfiguration
            {
                ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD,
                ErrorQueueOptOut = true
            };

            await SystemUnderTest.CreateAsync(queueConfig);

            queueConfig.ErrorQueueRetentionPeriodSeconds = 100;

            await SystemUnderTest.UpdateQueueAttributeAsync(queueConfig);
        }

        protected override ErrorQueue CreateSystemUnderTest()
        {
            var fixture = new JustSayingFixture();

            return new ErrorQueue(
                fixture.Region,
                fixture.UniqueName,
                fixture.CreateSqsClient(),
                fixture.LoggerFactory);
        }

        protected override void PostAssertTeardown()
        {
            SystemUnderTest.DeleteAsync().ResultSync();
            base.PostAssertTeardown();
        }

        [Fact]
        public void TheRetentionPeriodOfTheErrorQueueStaysAsMaximum()
        {
            SystemUnderTest.MessageRetentionPeriod.ShouldBe(100);
        }
    }
}
