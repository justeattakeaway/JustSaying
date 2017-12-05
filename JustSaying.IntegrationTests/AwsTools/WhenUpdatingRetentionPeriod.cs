using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenUpdatingRetentionPeriod : WhenCreatingQueuesByName
    {
        private int _oldRetentionPeriod;
        private int _newRetentionPeriod;

        protected override void Given()
        {
            _oldRetentionPeriod = 600;
            _newRetentionPeriod = 700;

            base.Given();
        }

        protected override Task When()
        {

            SystemUnderTest.Create(new SqsBasicConfiguration { MessageRetentionSeconds = _oldRetentionPeriod });

            SystemUnderTest.UpdateQueueAttribute(
                new SqsBasicConfiguration {MessageRetentionSeconds = _newRetentionPeriod});

            return Task.CompletedTask;
        }

        [Fact]
        public void TheRedrivePolicyIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.MessageRetentionPeriod.ShouldBe(_newRetentionPeriod);
        }
    }
}
