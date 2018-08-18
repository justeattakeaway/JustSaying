using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenICreateAQueueByName : WhenCreatingQueuesByName
    {
        private bool _isQueueCreated;

        protected override async Task When()
        {
            _isQueueCreated = await SystemUnderTest.CreateAsync(new SqsBasicConfiguration(), attempt: 0);
        }

        [Fact]
        public void TheQueueIsCreated()
        {
            _isQueueCreated.ShouldBeTrue();
        }

        [Fact]
        public async Task DeadLetterQueueIsCreated()
        {
            await Patiently.AssertThatAsync(
                () => SystemUnderTest.ErrorQueue.ExistsAsync(),
                40.Seconds());
        }
    }
}
