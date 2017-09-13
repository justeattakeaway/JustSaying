using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
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

        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration { MessageRetentionSeconds = _oldRetentionPeriod });

            await SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration {MessageRetentionSeconds = _newRetentionPeriod});
        }

        [Test]
        public void TheRedrivePolicyIsUpdatedWithTheNewValue()
        {
            Assert.AreEqual(_newRetentionPeriod, SystemUnderTest.MessageRetentionPeriod);
        }
    }
}
