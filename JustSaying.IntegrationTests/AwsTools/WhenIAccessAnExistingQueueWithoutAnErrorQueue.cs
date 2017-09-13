using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration());
        }

        [Then]
        public async Task ThereIsNoErrorQueue()
        {
            await Patiently.AssertThatAsync(async () => !await SystemUnderTest.ErrorQueue.ExistsAsync());
        }
    }
}