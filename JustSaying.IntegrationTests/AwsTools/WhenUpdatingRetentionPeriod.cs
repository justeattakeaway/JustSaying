using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenUpdatingRetentionPeriod : WhenCreatingQueuesByName
    {
        private TimeSpan _oldRetentionPeriod;
        private TimeSpan _newRetentionPeriod;

        protected override void Given()
        {
            _oldRetentionPeriod = TimeSpan.FromSeconds(600);
            _newRetentionPeriod = TimeSpan.FromSeconds(700);

            base.Given();
        }

        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(
                new SqsBasicConfiguration { MessageRetention = _oldRetentionPeriod });

            await SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration { MessageRetention = _newRetentionPeriod });
        }

        [AwsFact]
        public void TheRedrivePolicyIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.MessageRetentionPeriod.ShouldBe(_newRetentionPeriod);
        }
    }
}
