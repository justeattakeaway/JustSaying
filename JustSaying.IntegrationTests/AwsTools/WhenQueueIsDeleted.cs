using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(
                new SqsReadConfiguration(SubscriptionType.ToTopic),
                attempt: 600);

            await SystemUnderTest.DeleteAsync();
        }

        [Fact]
        public async Task TheErrorQueueIsDeleted()
        {
            await Patiently.AssertThatAsync(
                async () => !await SystemUnderTest.ErrorQueue.ExistsAsync());
        }
    }
}
