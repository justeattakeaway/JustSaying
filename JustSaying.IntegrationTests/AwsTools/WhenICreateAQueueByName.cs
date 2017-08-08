using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenICreateAQueueByName : WhenCreatingQueuesByName
    {
        private bool _isQueueCreated;

        protected override async Task When()
        {
            _isQueueCreated = await SystemUnderTest.CreateAsync(new SqsBasicConfiguration());
        }

        [Then]
        public void TheQueueIsCreated()
        {
            Assert.That(_isQueueCreated, Is.True);
        }

        [Then, Explicit("Extremely long running test")]
        public async Task DeadLetterQueueIsCreated()
        {
            await Patiently.AssertThatAsync(
                () => SystemUnderTest.ErrorQueue.ExistsAsync(),
                40.Seconds());
        }
    }
}
