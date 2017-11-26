using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override Task When()
        {
            SystemUnderTest.Create(new SqsBasicConfiguration {ErrorQueueOptOut = true}, attempt: 0);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ThereIsNoErrorQueue()
        {
            await Patiently.AssertThatAsync(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}
