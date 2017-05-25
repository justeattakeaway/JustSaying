using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(
                new SqsReadConfiguration(SubscriptionType.ToTopic), 
                attempt:600);
            await SystemUnderTest.DeleteAsync();
        }

        [Test]
        public async Task TheErrorQueueIsDeleted()
        {
            await Patiently.AssertThatAsync(
                async () => !await SystemUnderTest.ErrorQueue.ExistsAsync());
        }
    }
}