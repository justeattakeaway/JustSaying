using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenICreateAQueueByName : WhenCreatingQueuesByName
    {
        private bool _isQueueCreated;

        protected override Task When()
        {
            _isQueueCreated = SystemUnderTest.Create(new SqsBasicConfiguration(), attempt: 0);
            return Task.CompletedTask;
        }

        [Fact]
        public void TheQueueIsCreated()
        {
            _isQueueCreated.ShouldBeTrue();
        }

        [Fact(Skip = "Extremely long running test")]
        public async Task DeadLetterQueueIsCreated()
        {
            await Patiently.AssertThatAsync(
                () => SystemUnderTest.ErrorQueue.Exists(),
                40.Seconds());
        }
    }
}
