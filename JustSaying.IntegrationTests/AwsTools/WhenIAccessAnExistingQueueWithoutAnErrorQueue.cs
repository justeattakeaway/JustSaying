using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(new SqsBasicConfiguration() {ErrorQueueOptOut = true}, attempt: 0);
        }

        [Fact]
        public async Task ThereIsNoErrorQueue()
        {
            await Patiently.AssertThatAsync(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}
