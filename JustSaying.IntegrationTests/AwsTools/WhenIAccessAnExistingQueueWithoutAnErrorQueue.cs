using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration { ErrorQueueOptOut = true }, attempt: 0);
        }

        [Fact]
        public async Task ThereIsNoErrorQueue()
        {
            await Patiently.AssertThatAsync(async () => !await SystemUnderTest.ErrorQueue.ExistsAsync());
        }
    }
}
