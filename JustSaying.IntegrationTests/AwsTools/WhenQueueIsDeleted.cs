using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override Task When()
        {
            SystemUnderTest.Create(
                new SqsReadConfiguration(SubscriptionType.ToTopic), 
                attempt:600);
            SystemUnderTest.Delete();

            return Task.CompletedTask;
        }

        [Fact]
        public async Task TheErrorQueueIsDeleted()
        {
            await Patiently.AssertThatAsync(
                () => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}
