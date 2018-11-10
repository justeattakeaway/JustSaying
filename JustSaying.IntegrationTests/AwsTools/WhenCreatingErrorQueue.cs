using System;
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
        protected override Task Given() => Task.CompletedTask;

        protected override async Task When()
        {
            var queueConfig = new SqsBasicConfiguration
            {
                ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod,
                ErrorQueueOptOut = true
            };

            await SystemUnderTest.CreateAsync(queueConfig);

            queueConfig.ErrorQueueRetentionPeriod = TimeSpam.FromSeconds(100);

            await SystemUnderTest.UpdateQueueAttributeAsync(queueConfig);
        }

        protected override Task<ErrorQueue> CreateSystemUnderTestAsync()
        {
            var fixture = new JustSayingFixture();

            return Task.FromResult(new ErrorQueue(
                fixture.Region,
                fixture.UniqueName,
                fixture.CreateSqsClient(),
                fixture.LoggerFactory));
        }

        protected override async Task PostAssertTeardownAsync()
        {
            await SystemUnderTest.DeleteAsync();
        }

        [AwsFact]
        public void TheRetentionPeriodOfTheErrorQueueStaysAsMaximum()
        {
            SystemUnderTest.MessageRetentionPeriod.ShouldBe(TimeSpan.FromSeconds(100));
        }
    }
}
