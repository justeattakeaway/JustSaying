using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.AwsTools
{
    public abstract class WhenCreatingQueuesByName : XAsyncBehaviourTest<SqsQueueByName>
    {
        protected override Task Given() => Task.CompletedTask;

        protected override async Task<SqsQueueByName> CreateSystemUnderTestAsync()
        {
            var fixture = new JustSayingFixture();

            var queue = new SqsQueueByName(
                fixture.Region,
                fixture.UniqueName,
                fixture.CreateSqsClient(),
                1,
                fixture.LoggerFactory);

            // Force queue creation
            await queue.ExistsAsync();

            return queue;
        }

        protected override async Task PostAssertTeardownAsync()
        {
            await SystemUnderTest.DeleteAsync();
        }
    }
}
